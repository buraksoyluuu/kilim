using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace Kilim.EditorTools
{
    /// <summary>
    /// Tek tık build: proje ayarlarını (paket kimliği, ikon, yön, IL2CPP, giriş sistemi) koddan uygular,
    /// sahne yoksa oluşturur, APK/AAB ya da Xcode projesi üretir. CI (GameCI buildMethod) ve menü aynı yolu kullanır.
    /// Menü: Kilim › Build …
    /// </summary>
    public static class BuildScript
    {
        public const string BundleId = "app.kilim.game";            // iOS (App Store'da kayıtlı)
        public const string AndroidPackage = "com.kilimgames.kilim"; // Google Play (app.kilim.game Play'de başkasında)
        public const string ProductName = "Kilim";
        public const string Company = "Burak Soylu";
        private const string ScenePath = "Assets/Kilim/Scenes/Main.unity";

        [MenuItem("Kilim/Proje ayarlarını uygula")]
        public static void Configure()
        {
            PlayerSettings.companyName = Company;
            PlayerSettings.productName = ProductName;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, AndroidPackage);
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, BundleId);
            var version = Environment.GetEnvironmentVariable("KILIM_VERSION");
            PlayerSettings.bundleVersion = string.IsNullOrEmpty(version) ? "0.5.0" : version;
            int build = int.TryParse(Environment.GetEnvironmentVariable("KILIM_BUILD"), out var b) ? b : 1;
            PlayerSettings.Android.bundleVersionCode = build;
            PlayerSettings.iOS.buildNumber = build.ToString();

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.statusBarHidden = true;

            // Android: IL2CPP + ARM64 (Play zorunluluğu), min API 24
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            // iOS
            PlayerSettings.iOS.targetOSVersionString = "14.0";
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            var team = Environment.GetEnvironmentVariable("APPLE_TEAM_ID");
            PlayerSettings.iOS.appleDeveloperTeamID = string.IsNullOrEmpty(team) ? "3UTU4G8NGA" : team;

            ConfigureAds();
            ConfigureNotifications();
            SetActiveInputHandlingBoth();
            SetIcon();
            EnsureScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[Kilim] Proje ayarları uygulandı");
        }

        /// <summary>
        /// AdMob uygulama kimliği (GoogleMobileAdsSettings, internal → yansıma). Kaynak: Resources/ads.json
        /// (CI, ADMOB_* secret'larından yazar; android_app, ios_app, *_rewarded, *_interstitial). Yoksa Google test kimlikleri.
        /// </summary>
        private static void ConfigureAds()
        {
            const string path = "Assets/Kilim/Resources/ads.json";
            var json = File.Exists(path) ? File.ReadAllText(path) : "";
            string Get(string k, string test)
            {
                var m = System.Text.RegularExpressions.Regex.Match(json, "\"" + k + "\"\\s*:\\s*\"([^\"]+)\"");
                return m.Success ? m.Groups[1].Value : test;
            }
            var t = Type.GetType("GoogleMobileAds.Editor.GoogleMobileAdsSettings, GoogleMobileAds.Editor");
            var inst = t?.GetMethod("LoadInstance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)?.Invoke(null, null);
            if (inst == null) { Debug.LogWarning("[Kilim] GoogleMobileAdsSettings bulunamadı"); return; }
            t.GetProperty("GoogleMobileAdsAndroidAppId").SetValue(inst, Get("android_app", "ca-app-pub-3940256099942544~3347511713"));
            t.GetProperty("GoogleMobileAdsIOSAppId").SetValue(inst, Get("ios_app", "ca-app-pub-3940256099942544~1458002511"));
            EditorUtility.SetDirty((UnityEngine.Object)inst);
            Debug.Log($"[Kilim] AdMob: {(json.Contains("android_app") ? "gerçek kimlikler" : "test kimlikleri")}");
        }

        /// <summary>iOS bildirim izni açılışta değil, ilk kazanılan bölümden sonra istenir (Push.RequestPermission).</summary>
        private static void ConfigureNotifications()
        {
            var t = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                .FirstOrDefault(x => x.FullName == "Unity.Notifications.NotificationSettings+iOSSettings");
            var prop = t?.GetProperty("RequestAuthorizationOnAppLaunch");
            if (prop == null) { Debug.LogWarning("[Kilim] Bildirim ayarı bulunamadı"); return; }
            prop.SetValue(null, false);
            Debug.Log("[Kilim] iOS bildirim izni: açılışta sorulmaz");
        }

        /// <summary>UI olay sistemi eski giriş modülünü kullanıyor: Active Input Handling = Both (2).</summary>
        private static void SetActiveInputHandlingBoth()
        {
            var ps = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset").FirstOrDefault();
            if (ps == null) return;
            var so = new SerializedObject(ps);
            var prop = so.FindProperty("activeInputHandler");
            if (prop == null || prop.intValue == 2) return;
            prop.intValue = 2;
            so.ApplyModifiedProperties();
        }

        private static void SetIcon()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Kilim/Art/icon_1024.png");
            if (icon == null) return;
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        }

        /// <summary>Oyun App.Boot ile kendini kurar; build için boş bir sahne yeterli.</summary>
        private static void EnsureScene()
        {
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        [MenuItem("Kilim/Build Android APK (telefona doğrudan kurulum)")]
        public static void BuildAndroidApk() => BuildAndroid(false);

        [MenuItem("Kilim/Build Android AAB (Google Play)")]
        public static void BuildAndroidAab() => BuildAndroid(true);

        [MenuItem("Kilim/Build iOS (Xcode projesi)")]
        public static void BuildIos() => Build(BuildTarget.iOS, "build/iOS/Kilim");

        private static void BuildAndroid(bool aab)
        {
            EditorUserBuildSettings.buildAppBundle = aab;
            var ks = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PATH");
            if (!string.IsNullOrEmpty(ks))
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = ks;
                PlayerSettings.Android.keystorePass = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PASS");
                PlayerSettings.Android.keyaliasName = Environment.GetEnvironmentVariable("ANDROID_KEYALIAS_NAME");
                PlayerSettings.Android.keyaliasPass = Environment.GetEnvironmentVariable("ANDROID_KEYALIAS_PASS");
            }
            Build(BuildTarget.Android, aab ? "build/Android/Kilim.aab" : "build/Android/Kilim.apk");
        }

        /// <summary>CI giriş noktası: -customBuildTarget ya da GameCI'nin -buildTarget argümanına göre.</summary>
        public static void BuildFromCommandLine()
        {
            var args = Environment.GetCommandLineArgs();
            string Arg(string name) { int i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
            var target = Arg("-buildTarget") ?? Arg("-customBuildTarget") ?? "Android";
            var output = Arg("-customBuildPath");
            if (target.Equals("iOS", StringComparison.OrdinalIgnoreCase)) Build(BuildTarget.iOS, output ?? "build/iOS/Kilim");
            else
            {
                // GameCI: -androidExportType androidAppBundle → AAB; imza anahtarı -androidKeystore* argümanlarıyla gelir.
                bool aab = (Arg("-androidExportType") ?? "").Equals("androidAppBundle", StringComparison.OrdinalIgnoreCase)
                           || Environment.GetEnvironmentVariable("KILIM_AAB") == "1";
                EditorUserBuildSettings.buildAppBundle = aab;
                var ks = Arg("-androidKeystoreName");
                if (!string.IsNullOrEmpty(ks) && File.Exists(ks))
                {
                    PlayerSettings.Android.useCustomKeystore = true;
                    PlayerSettings.Android.keystoreName = Path.GetFullPath(ks);
                    PlayerSettings.Android.keystorePass = Arg("-androidKeystorePass");
                    PlayerSettings.Android.keyaliasName = Arg("-androidKeyaliasName");
                    PlayerSettings.Android.keyaliasPass = Arg("-androidKeyaliasPass");
                    Debug.Log("[Kilim] Kalıcı yükleme anahtarıyla imzalanıyor");
                }
                else if (aab) throw new Exception("AAB için imza anahtarı gerekli (ANDROID_SIGNING secret'ı)");
                Build(BuildTarget.Android, output ?? (aab ? "build/Android/Kilim.aab" : "build/Android/Kilim.apk"));
            }
        }

        private static void Build(BuildTarget target, string path)
        {
            Configure();
            // CI: GameCI -androidVersionCode verir (her build'de artan sayı, Play aynı kodu iki kez kabul etmez).
            var cl = Environment.GetCommandLineArgs();
            int vi = Array.IndexOf(cl, "-androidVersionCode");
            if (vi >= 0 && vi + 1 < cl.Length && int.TryParse(cl[vi + 1], out var vcode) && vcode > 0)
                PlayerSettings.Android.bundleVersionCode = vcode;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = path,
                target = target,
                options = BuildOptions.None,
            });
            Debug.Log($"[Kilim] Build {target}: {report.summary.result}, {report.summary.totalSize / (1024 * 1024)} MB → {path}");
            if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

#if UNITY_IOS
        /// <summary>Info.plist: şifreleme beyanı (TestFlight her yüklemede sormasın) ve ATT açıklaması.</summary>
        [PostProcessBuild(100)]
        public static void OnPostprocess(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS) return;
            var plistPath = Path.Combine(path, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            plist.root.SetString("NSUserTrackingUsageDescription", "Sana daha alakalı reklamlar göstermek için.");
            plist.WriteToFile(plistPath);
        }
#endif
    }
}
