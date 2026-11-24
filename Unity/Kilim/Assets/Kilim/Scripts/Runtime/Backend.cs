using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Kilim.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace Kilim.Game
{
    /// <summary>
    /// Firebase backend istemcisi (Backend/README.md). Resources/backend.json yoksa ya da ağ yoksa oyun çevrimdışı
    /// kurallarla aynen çalışır; bağlantı gelince saat, config, bulut kayıt ve iadeler eşitlenir.
    /// Kimlik: Firebase Anonymous Auth (REST), yenileme jetonu PlayerPrefs'te.
    /// </summary>
    public sealed class Backend
    {
        private readonly App _app;
        private readonly string _base, _apiKey;
        private string _idToken;
        private DateTime _tokenExpiry;
        private float _nextCloudSave;

        public bool Configured => !string.IsNullOrEmpty(_base) && !string.IsNullOrEmpty(_apiKey);
        public bool Online { get; private set; }
        public string Platform => Application.platform == RuntimePlatform.IPhonePlayer ? "ios" : "android";

        public Backend(App app)
        {
            _app = app;
            var cfg = Resources.Load<TextAsset>("backend");
            if (cfg == null) return;
            var d = MiniJson.Parse(cfg.text) as Dictionary<string, object>;
            _base = Data.Str(d, "baseUrl", "")?.TrimEnd('/');
            _apiKey = Data.Str(d, "apiKey", "");
        }

        // ------------------------------------------------------------ açılış eşitlemesi

        /// <summary>Kimlik → sunucu saati → config → bulut kayıt → iadeler. Hata olursa sessizce çevrimdışı kalır.</summary>
        public IEnumerator Startup()
        {
            if (!Configured) yield break;
            yield return EnsureToken();
            if (_idToken == null) yield break;

            yield return Call("GET", "/time", null, r => { _app.Clock.Sync(Data.FromMs(Data.Long(r, "now"))); Online = true; });
            if (!Online) yield break;
            yield return Call("GET", "/config", null, r => _app.Config.Apply(MiniJson.Serialize(r)));
            yield return Call("GET", "/save", null, r =>
            {
                var cloud = Data.Str(r, "save");
                if (cloud == null) return;
                var cp = PlayerProfile.FromJson(cloud);
                if (cp != null && cp.MaxLevelReached > _app.Profile.MaxLevelReached) _app.ReplaceProfile(PlayerProfile.Merge(_app.Profile, cp));
            });
            yield return SyncRefunds();
        }

        public IEnumerator SyncRefunds()
        {
            var acked = new List<object>();
            yield return Call("GET", "/entitlements", null, r =>
            {
                foreach (var o in Data.List(r, "refunds"))
                {
                    var x = (Dictionary<string, object>)o;
                    _app.Meta.ApplyRefund(Data.Str(x, "sku"));
                    acked.Add(Data.Str(x, "txId"));
                }
            });
            if (acked.Count == 0) yield break;
            _app.Save();
            yield return Call("POST", "/entitlements/ack", new Dictionary<string, object> { { "txIds", acked } }, null);
        }

        /// <summary>App.Save'den çağrılır; en fazla 30 sn'de bir buluta yazar (kapanışta zorla).</summary>
        public void CloudSave(bool force = false)
        {
            if (!Online || (!force && Time.unscaledTime < _nextCloudSave)) return;
            _nextCloudSave = Time.unscaledTime + 30f;
            var body = new Dictionary<string, object> { { "save", _app.Profile.ToJson() }, { "maxLevel", _app.Profile.MaxLevelReached } };
            _app.StartCoroutine(Call("PUT", "/save", body, null));
        }

        // ------------------------------------------------------------ satın alma

        /// <summary>Makbuzu sunucuda doğrular. done(valid, granted). Backend yoksa (çevrimdışı mod) yerelde teslim edilir.</summary>
        public void VerifyPurchase(string sku, string tx, Action<bool, bool> done)
        {
            if (!Online) { done(true, true); return; }
            var body = new Dictionary<string, object> { { "platform", Platform }, { "sku", sku }, { "transactionId", tx } };
            _app.StartCoroutine(Call("POST", "/purchase", body,
                r => done(Data.Bool(r, "valid"), Data.Bool(r, "granted")),
                () => done(false, false)));
        }

        // ------------------------------------------------------------ yarış

        private int TzMinutes => (int)_app.Clock.LocalOffset.TotalMinutes;

        public void RaceJoin(string nick) =>
            _app.StartCoroutine(Call("POST", "/race/join", new Dictionary<string, object> { { "nick", nick }, { "tz", TzMinutes } }, null));

        public void RaceFlag(int level, int attempt) =>
            _app.StartCoroutine(Call("POST", "/race/flag", new Dictionary<string, object> { { "level", level }, { "attempt", attempt }, { "tz", TzMinutes } }, null));

        public void RaceStandings(Action<List<RaceEntry>> done) =>
            _app.StartCoroutine(Call("GET", $"/race?tz={TzMinutes}", null, r =>
            {
                var list = new List<RaceEntry>();
                foreach (var o in Data.List(r, "standings"))
                {
                    var x = (Dictionary<string, object>)o;
                    list.Add(new RaceEntry { Name = Data.Str(x, "nick"), Flags = Data.Int(x, "flags"), IsPlayer = Data.Bool(x, "me") });
                }
                done(list);
            }, () => done(null)));

        public void RaceClaim(string weekId, Action<int, int> done) =>
            _app.StartCoroutine(Call("POST", "/race/claim", new Dictionary<string, object> { { "weekId", weekId }, { "tz", TzMinutes } },
                r => done(Data.Int(r, "rank"), Data.Int(r, "coins")), () => done(0, 0)));

        // ------------------------------------------------------------ silme

        public IEnumerator DeleteAccount()
        {
            if (!Online) yield break;
            yield return Call("POST", "/delete", new Dictionary<string, object>(), null);
            PlayerPrefs.DeleteKey("kilim.refresh");
            _idToken = null;
        }

        // ------------------------------------------------------------ HTTP

        private IEnumerator EnsureToken()
        {
            if (_idToken != null && DateTime.UtcNow < _tokenExpiry) yield break;
            var refresh = PlayerPrefs.GetString("kilim.refresh", "");
            string url;
            string body;
            if (string.IsNullOrEmpty(refresh))
            {
                url = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={_apiKey}";
                body = "{\"returnSecureToken\":true}";
            }
            else
            {
                url = $"https://securetoken.googleapis.com/v1/token?key={_apiKey}";
                body = MiniJson.Serialize(new Dictionary<string, object> { { "grant_type", "refresh_token" }, { "refresh_token", refresh } });
            }
            using (var req = Json("POST", url, body, null))
            {
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success) { Debug.LogWarning($"[Kilim][Backend] auth: {req.error}"); yield break; }
                var r = MiniJson.Parse(req.downloadHandler.text) as Dictionary<string, object>;
                _idToken = Data.Str(r, "idToken") ?? Data.Str(r, "id_token");
                var newRefresh = Data.Str(r, "refreshToken") ?? Data.Str(r, "refresh_token");
                int expires = int.TryParse(Data.Str(r, "expiresIn") ?? Data.Str(r, "expires_in") ?? "3600", out var e) ? e : 3600;
                _tokenExpiry = DateTime.UtcNow.AddSeconds(expires - 120);
                if (!string.IsNullOrEmpty(newRefresh)) { PlayerPrefs.SetString("kilim.refresh", newRefresh); PlayerPrefs.Save(); }
            }
        }

        private IEnumerator Call(string method, string path, Dictionary<string, object> body,
                                 Action<Dictionary<string, object>> ok, Action fail = null)
        {
            yield return EnsureToken();
            if (_idToken == null) { fail?.Invoke(); yield break; }
            using (var req = Json(method, _base + path, body != null ? MiniJson.Serialize(body) : null, _idToken))
            {
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[Kilim][Backend] {method} {path}: {req.responseCode} {req.error}");
                    if (path == "/time") Online = false;
                    fail?.Invoke();
                    yield break;
                }
                Dictionary<string, object> r = null;
                try { r = MiniJson.Parse(req.downloadHandler.text) as Dictionary<string, object>; }
                catch (FormatException) { }
                if (r == null) { fail?.Invoke(); yield break; }
                ok?.Invoke(r);
            }
        }

        private static UnityWebRequest Json(string method, string url, string body, string token)
        {
            var req = new UnityWebRequest(url, method) { downloadHandler = new DownloadHandlerBuffer(), timeout = 10 };
            if (body != null) req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.SetRequestHeader("Content-Type", "application/json");
            if (token != null) req.SetRequestHeader("Authorization", "Bearer " + token);
            return req;
        }
    }
}
