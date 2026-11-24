using System.Collections.Generic;
using Kilim.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kilim.Game
{
    /// <summary>
    /// 2 · Oyun ekranı (Spec 1 §1–§5, Spec 3 §1 ekran 2 ve 9). Üst: duraklat, bölüm, desen çubuğu, kalan parça.
    /// Orta: tahta. Alt: 4'lü tezgâh (+ Tek İlmek yuvaları) ve güçlendiriciler.
    /// </summary>
    public sealed class GameScreen : UiScreen
    {
        public const float BoardSize = 1000f;
        public const float BoardPad = 10f;
        public const float CellGap = 6f;
        public const float DragLift = 180f;

        private readonly LevelRun _run;
        private GameSession S => _run.Session;
        private LevelData L => _run.Level;

        private RectTransform _board;
        private Image[,] _cells;
        private Text[,] _glyphs;
        private Outline[,] _outlines;
        private float _pitch;
        private readonly HashSet<Cell> _ghost = new HashSet<Cell>();
        private string _ghostColor;

        private RectTransform _trayRow, _bonusRow;
        private readonly List<TraySlotView> _slots = new List<TraySlotView>();
        private Text _remaining, _percent;
        private System.Action<float> _bar;
        private RectTransform _boosterRow;
        private Text _hint;
        private bool _swapMode;
        private bool _ending;
        private BoosterService _boosters;

        public GameScreen(LevelRun run) { _run = run; }

        public override void Build(RectTransform root)
        {
            _boosters = new BoosterService(App.Meta.Wallet, App.Config.Economy);
            S.StateChanged += OnState;
            S.AutoUndone += _ => { App.Nav.Toast("Tezgâh doldu, son parça geri alındı."); Refresh(); };

            var v = Ui.VStack(root, 18, 24).Stretch();
            BuildTop(v);
            _hint = Ui.Label(v, "", 38, Theme.Ink, TextAnchor.MiddleCenter, false, 70);
            BuildBoard(v);
            Ui.Spacer(v, 10);
            _trayRow = Ui.HStack(v, 16, 0, TextAnchor.MiddleCenter, 240, "Tray");
            _bonusRow = Ui.HStack(v, 16, 0, TextAnchor.MiddleCenter, 120, "Bonus");
            _boosterRow = Ui.HStack(v, 16, 0, TextAnchor.MiddleCenter, 150, "Boosters");
            Ui.Spacer(v);

            BuildTray();
            BuildBoosters();
            Refresh();
            ShowIntroHints();
        }

        public override void OnClose() => S.StateChanged -= OnState;

        public override bool OnBack() { Pause(); return true; }

        // -----------------------------------------------------------------
        // Üst çubuk
        // -----------------------------------------------------------------

        private void BuildTop(Transform parent)
        {
            var top = Ui.HStack(parent, 16, 0, TextAnchor.MiddleCenter, 110, "Top");
            top.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            Ui.Button(top, "II", Pause, Theme.Neutral, Theme.Ink, 100, 48, 110);
            var mid = Ui.VStack(top, 6, 0, TextAnchor.MiddleCenter);
            Ui.Size(mid.gameObject, -1, 110, 1);
            Ui.Label(mid, _run.IsDaily ? "Günün Deseni" : $"Bölüm {L.Id}{(L.Boss ? " · Usta" : "")}", 40, Theme.Ink, TextAnchor.MiddleCenter, true, 50);
            var barRow = Ui.HStack(mid, 10, 0, TextAnchor.MiddleCenter, 40);
            barRow.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            var barHolder = Ui.Rect(barRow, "BarHolder");
            Ui.Size(barHolder.gameObject, -1, 30, 1);
            _bar = Ui.Bar(barHolder, 30, Theme.Gold, Theme.Neutral);
            ((RectTransform)barHolder.GetChild(0)).Stretch();
            // 2★ ve 3★ işaretleri
            float t2 = (float)L.Star2Cells / L.Star3Cells;
            Marker(barHolder, t2, "2★");
            Marker(barHolder, 1f, "3★");
            _percent = Ui.Label(barRow, "%0", 32, Theme.Ink, TextAnchor.MiddleRight, true);
            Ui.Size(_percent.gameObject, 110, 40);
            _remaining = Ui.Label(top, "", 34, Theme.Muted, TextAnchor.MiddleCenter, true);
            Ui.Size(_remaining.gameObject, 170, 110);
        }

        private static void Marker(Transform bar, float t, string label)
        {
            var m = Ui.Box(bar, "Marker", Theme.Ink);
            var rt = (RectTransform)m.transform;
            rt.anchorMin = new Vector2(t, 0);
            rt.anchorMax = new Vector2(t, 1);
            rt.sizeDelta = new Vector2(4, 0);
            var lbl = Ui.Label(bar, label, 22, Theme.Ink);
            var lrt = (RectTransform)lbl.transform;
            lrt.anchorMin = new Vector2(t, 1);
            lrt.anchorMax = new Vector2(t, 1);
            lrt.pivot = new Vector2(t >= 1 ? 1 : 0.5f, 0);
            lrt.sizeDelta = new Vector2(70, 28);
        }

        // -----------------------------------------------------------------
        // Tahta
        // -----------------------------------------------------------------

        private void BuildBoard(Transform parent)
        {
            var holder = Ui.Rect(parent, "BoardHolder");
            Ui.Size(holder.gameObject, -1, BoardSize);
            var bg = Ui.Box(holder, "Board", Theme.BoardBg);
            _board = (RectTransform)bg.transform;
            _board.anchorMin = _board.anchorMax = new Vector2(0.5f, 1);
            _board.pivot = new Vector2(0, 1);
            _board.sizeDelta = new Vector2(BoardSize, BoardSize);
            _board.anchoredPosition = new Vector2(-BoardSize / 2, 0);

            int n = L.Size;
            _pitch = (BoardSize - 2 * BoardPad + CellGap) / n;
            float cell = _pitch - CellGap;
            _cells = new Image[n, n];
            _glyphs = new Text[n, n];
            _outlines = new Outline[n, n];
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                {
                    var img = Ui.Box(_board, $"C{r}_{c}", Theme.CellEmpty);
                    var rt = (RectTransform)img.transform;
                    rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
                    rt.pivot = new Vector2(0, 1);
                    rt.sizeDelta = new Vector2(cell, cell);
                    rt.anchoredPosition = new Vector2(BoardPad + c * _pitch, -(BoardPad + r * _pitch));
                    img.raycastTarget = false;
                    var g = Ui.Label(img.transform, "", (int)(cell * 0.42f), Color.white);
                    ((RectTransform)g.transform).Stretch();
                    var o = img.gameObject.AddComponent<Outline>();
                    o.effectColor = Theme.Gold;
                    o.effectDistance = new Vector2(5, -5);
                    o.enabled = false;
                    _cells[r, c] = img;
                    _glyphs[r, c] = g;
                    _outlines[r, c] = o;
                    if (!L.IsPlayable(r, c)) img.color = new Color(0, 0, 0, 0);
                }
        }

        private void RefreshBoard()
        {
            int n = L.Size;
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                {
                    if (!L.IsPlayable(r, c)) continue;
                    var img = _cells[r, c];
                    var g = _glyphs[r, c];
                    string motif = L.Motif[r, c];
                    var here = new Cell(r, c);
                    if (S.Board.OwnerAt(r, c) != Board.Empty)
                    {
                        string col = S.Board.ColorAt(r, c);
                        img.color = Theme.Motif(col);
                        g.text = Theme.Glyph(string.IsNullOrEmpty(col) ? "" : col);
                        g.color = new Color(1, 1, 1, 0.55f);
                        _outlines[r, c].enabled = S.Board.IsMatchAt(r, c);
                    }
                    else if (_ghost.Contains(here))
                    {
                        img.color = Theme.WithAlpha(Theme.Motif(_ghostColor), 0.45f);
                        g.text = "";
                        _outlines[r, c].enabled = L.Matches(r, c, _ghostColor);
                    }
                    else
                    {
                        // Boş hücre: motif rengi %22 + şekil işareti (renk körlüğü)
                        var mc = Theme.Motif(motif.Split('|')[0]);
                        img.color = Color.Lerp(Theme.CellEmpty, mc, 0.22f);
                        g.text = Theme.Glyph(motif.Split('|')[0]);
                        g.color = Theme.WithAlpha(mc, 0.6f);
                        _outlines[r, c].enabled = false;
                    }
                }
        }

        /// <summary>Ekran noktasını, sürüklenen şekil için tahta hücre çapasına çevirir (mıknatıs dahil).</summary>
        public Cell? AnchorFor(Vector2 screenPos, Shape shape)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_board, screenPos, null, out var local)) return null;
            float w = shape.Width * _pitch, h = shape.Height * _pitch;
            float tlx = local.x - w / 2f;
            float tly = local.y + DragLift + h;
            int col = Mathf.RoundToInt((tlx - BoardPad) / _pitch);
            int row = Mathf.RoundToInt((-tly - BoardPad) / _pitch);
            return S.Board.SnapAnchor(shape, new Cell(row, col));
        }

        public float Pitch => _pitch;

        public void SetGhost(Shape shape, Cell? anchor, string color)
        {
            _ghost.Clear();
            _ghostColor = color;
            if (anchor.HasValue)
                foreach (var c in shape.Cells) _ghost.Add(anchor.Value.Offset(c));
            RefreshBoard();
        }

        // -----------------------------------------------------------------
        // Tezgâh
        // -----------------------------------------------------------------

        private void BuildTray()
        {
            for (int i = 0; i < GameSession.TraySlots; i++) _slots.Add(TraySlotView.Create(_trayRow, this, i, 230));
            for (int i = 0; i < GameSession.BonusSlots; i++)
                _slots.Add(TraySlotView.Create(_bonusRow, this, GameSession.TraySlots + i, 110));
            if (App.Profile.LeftHanded) // sol el modu: tezgâh ve güçlendiriciler aynalanır (şekiller değil, sıra)
            {
                _trayRow.GetComponent<HorizontalLayoutGroup>().reverseArrangement = true;
                _boosterRow.GetComponent<HorizontalLayoutGroup>().reverseArrangement = true;
            }
        }

        public TrayPiece PieceAt(int slot) => S.Tray[slot];
        public bool IsPlaying => S.State == SessionState.Playing && !_ending && !App.Nav.HasModal;

        /// <summary>Tezgâh parçasına dokunuş: döndür (ya da Değiştir modunda değiştir).</summary>
        public void OnSlotTap(int slot)
        {
            if (!IsPlaying || S.Tray[slot] == null) return;
            if (_swapMode)
            {
                _swapMode = false;
                UseBooster(Booster.Swap, slot);
                return;
            }
            if (!TutorialAllowsRotate()) return;
            S.Rotate(slot);
            Refresh();
        }

        /// <summary>Bırakma. Geçerliyse yerleşir; değilse parça yuvasına döner (ceza yok).</summary>
        public bool OnDrop(int slot, Cell? anchor)
        {
            SetGhost(null, null, null);
            if (!IsPlaying || !anchor.HasValue) return false;
            if (!TutorialAllowsMove(slot, anchor.Value)) { App.Nav.Toast("Önce gösterilen hamleyi yap."); return false; }
            var piece = S.Tray[slot];
            bool rotated = piece.Rotation != 0;
            var res = S.Place(slot, anchor.Value);
            if (!res.Success) return false;
            Audio.Place();
            if (S.PlacementCount % 5 == 1)
                App.Analytics.Track("piece_placed", ("level", L.Id), ("index", S.PlacementCount), ("rotated", rotated), ("motif_match_cells", res.MatchedCells));
            Refresh();
            return true;
        }

        // -----------------------------------------------------------------
        // Güçlendiriciler
        // -----------------------------------------------------------------

        private void BuildBoosters()
        {
            Ui.Clear(_boosterRow);
            if (_run.IsDaily) return; // zen modunda güçlendirici yok
            BoosterButton(Booster.Undo, "Geri Al");
            BoosterButton(Booster.Swap, "Değiştir");
            BoosterButton(Booster.SingleStitch, "Tek İlmek");
        }

        private void BoosterButton(Booster b, string name)
        {
            var w = App.Meta.Wallet;
            var cfg = App.Config.Economy;
            string label;
            bool unlocked = w.IsUnlocked(b) || w.BoosterCount(b) > 0;
            if (!unlocked) label = $"{name}\nBölüm {cfg.BoosterUnlockLevel[b]}";
            else if (w.BoosterCount(b) > 0) label = $"{name}\n×{w.BoosterCount(b)}";
            else label = $"{name}\n● {cfg.BoosterPrice[b]}";
            var btn = Ui.Button(_boosterRow, label, () => OnBoosterTap(b), unlocked ? Theme.Panel : Theme.Neutral, Theme.Ink, 150, 34);
            bool applicable = unlocked && (_boosters.IsApplicable(S, b, FirstOccupiedSlot()) || b == Booster.Swap && S.RemainingInQueue > 0);
            Ui.SetEnabled(btn, applicable && S.State == SessionState.Playing);
        }

        private int FirstOccupiedSlot()
        {
            for (int i = 0; i < GameSession.TraySlots; i++) if (S.Tray[i] != null) return i;
            return -1;
        }

        private void OnBoosterTap(Booster b)
        {
            if (!IsPlaying) return;
            if (b == Booster.Swap)
            {
                _swapMode = true;
                App.Nav.Toast("Değiştirilecek parçaya dokun.");
                return;
            }
            UseBooster(b, -1);
        }

        private void UseBooster(Booster b, int slot)
        {
            var w = App.Meta.Wallet;
            bool fromStock = w.BoosterCount(b) > 0;
            var res = _boosters.Use(S, b, slot);
            switch (res)
            {
                case BoosterUseResult.Applied:
                    _run.BoostersUsed.Add(b.ToString());
                    App.Analytics.Track("booster_used", ("type", b.ToString()), ("level", L.Id), ("source", fromStock ? "free" : "coin"));
                    App.Save();
                    break;
                case BoosterUseResult.NotEnoughCoins:
                    CoinPacks.Show(App, App.Config.Economy.BoosterPrice[b] - w.Coins, Refresh);
                    break;
                case BoosterUseResult.NotApplicable:
                    App.Nav.Toast(b == Booster.Undo ? "Geri alınacak yerleşim yok." : "Şu an kullanılamaz.");
                    break;
            }
            Refresh();
        }

        // -----------------------------------------------------------------
        // Durum
        // -----------------------------------------------------------------

        public void Refresh()
        {
            if (_board == null) return;
            RefreshBoard();
            foreach (var s in _slots) s.Render();
            _remaining.text = $"Kalan {S.RemainingPieces}";
            float pct = (float)S.PatternPercent;
            _bar(pct);
            _percent.text = $"%{Mathf.RoundToInt(pct * 100)}";
            _bonusRow.gameObject.SetActive(S.Tray[GameSession.TraySlots] != null || S.Tray[GameSession.TraySlots + 1] != null);
            BuildBoosters();
            UpdateTutorialHint();
        }

        private void OnState(SessionState st)
        {
            if (st == SessionState.Won)
            {
                _ending = true;
                Refresh();
                // 9.3 son parça oturunca dokuma animasyonu (1,4 sn), sonra Kazandın
                App.StartCoroutine(WeaveAnim());
                App.Delay(1.4f, () =>
                {
                    var report = App.Meta.CompleteWin(_run);
                    App.Save();   // ödül tek işlemde kalıcı (UAT WIN-08)
                    App.Analytics.Track("level_win", ("level", L.Id), ("stars", report.Stars), ("motif_pct", Mathf.RoundToInt((float)report.PatternPercent * 100)),
                        ("duration_s", report.DurationSeconds), ("attempts", _run.Attempt), ("boosters_used", _run.BoostersUsed.Count));
                    if (_run.IsDaily) App.Analytics.Track("daily_pattern", ("date", App.Meta.Daily.Today), ("stars", report.Stars));
                    App.Nav.Show(new WinScreen(_run, report));
                });
            }
            else if (st == SessionState.Failed)
            {
                Refresh();
                App.Analytics.Track("level_fail", ("level", L.Id), ("attempt", _run.Attempt), ("pieces_left", S.RemainingPieces),
                    ("cells_left", S.Board.PlayableCells - S.Board.FilledCount));
                App.Delay(0.6f, () => FailFlow.Show(_run, this)); // 600 ms tahta görünür
            }
        }

        private System.Collections.IEnumerator WeaveAnim()
        {
            int n = L.Size;
            for (int d = 0; d < 2 * n; d++)
            {
                for (int r = 0; r < n; r++)
                {
                    int c = d - r;
                    if (c < 0 || c >= n || !L.IsPlayable(r, c) || _cells == null) continue;
                    _cells[r, c].transform.localScale = new Vector3(1.08f, 1.08f, 1);
                }
                yield return new WaitForSecondsRealtime(1.2f / (2 * n));
            }
            if (_cells == null) yield break;
            foreach (var img in _cells) if (img != null) img.transform.localScale = Vector3.one;
        }

        /// <summary>Devam alındıktan sonra oyuna dönüş.</summary>
        public void Resume()
        {
            _ending = false;
            Refresh();
        }

        private void Pause()
        {
            if (_ending) return;
            App.Nav.Choice("Duraklatıldı", _run.IsDaily ? "Günün deseni kaydedilmez, istediğin zaman yeniden başlayabilirsin." : null, new (string, System.Action)[]
            {
                ("Devam", null),
                ("Yeniden başlat", () =>
                {
                    if (_run.IsDaily) App.StartDaily();
                    else { App.Meta.GiveUp(_run); App.Save(); App.StartLevel(L.Id); }
                }),
                ("Ana menü", () => App.GoHome()),
            });
        }

        // -----------------------------------------------------------------
        // Öğretici (Spec 3 8.4): Bölüm 1 ilk hamle zorunlu; 2: desen; 4: döndürme. Güçlendirici açılışları.
        // -----------------------------------------------------------------

        private bool FirstMoveForced => !_run.IsDaily && L.Id == 1 && L.Tutorial && S.PlacementCount == 0 && L.Solution.Count > 0;

        private bool TutorialAllowsMove(int slot, Cell anchor)
        {
            if (!FirstMoveForced) return true;
            var step = L.Solution[0];
            var piece = S.Tray[slot];
            if (piece == null || piece.QueueIndex != step.QueueIndex) return false;
            var target = new HashSet<Cell>(step.Cells);
            foreach (var c in piece.Shape.Cells) if (!target.Contains(anchor.Offset(c))) return false;
            return true;
        }

        private bool TutorialAllowsRotate() => !FirstMoveForced;

        private void UpdateTutorialHint()
        {
            if (_hint == null) return;
            if (FirstMoveForced)
            {
                var step = L.Solution[0];
                string colorName = L.Queue[step.QueueIndex].Color == "L" ? "Lacivert" : "Bu";
                _hint.text = $"Nine: \"{colorName} parçayı aynı renkli izin üstüne koy.\"";
                SetGhost(Shape.From(step.Cells), new Cell(MinRow(step.Cells), MinCol(step.Cells)), L.Queue[step.QueueIndex].Color);
                _ghost.Clear(); // yalnızca bir kez çiz (Refresh döngüsünü engelle)
            }
            else if (!_run.IsDaily && L.Id == 2 && S.PlacementCount < 2) _hint.text = "Rengi motifle eşleşen parça desen yıldızı kazandırır.";
            else if (!_run.IsDaily && L.Id == 4 && S.PlacementCount == 0) _hint.text = "Parçaya dokun: saat yönünde döner.";
            else if (_run.IsDaily) _hint.text = "Süre yok, kaybetmek yok.";
            else _hint.text = "";
        }

        private static int MinRow(IReadOnlyList<Cell> cs) { int m = int.MaxValue; foreach (var c in cs) m = Mathf.Min(m, c.Row); return m; }
        private static int MinCol(IReadOnlyList<Cell> cs) { int m = int.MaxValue; foreach (var c in cs) m = Mathf.Min(m, c.Col); return m; }

        private void ShowIntroHints()
        {
            if (_run.IsDaily) return;
            foreach (var kv in App.Config.Economy.BoosterUnlockLevel)
                if (kv.Value == L.Id && _run.Attempt == 1)
                {
                    string name = kv.Key == Booster.Undo ? "Geri Al" : kv.Key == Booster.Swap ? "Değiştir" : "Tek İlmek";
                    string what = kv.Key == Booster.Undo ? "Son yerleşimi geri alır."
                        : kv.Key == Booster.Swap ? "Seçtiğin parçayı kuyruğun sonuna gönderir."
                        : "Tezgâha 1 hücrelik ek parça koyar (desen yüzdesini düşürür).";
                    App.Nav.Confirm($"Yeni: {name}", $"{what}\n3 tane hediye!", "Anladım", null, null, null);
                }
        }
    }

    /// <summary>Tezgâh yuvası: dokun = döndür, sürükle = yerleştir (Spec 1 §2–§3).</summary>
    public sealed class TraySlotView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private GameScreen _game;
        private int _slot;
        private float _size;
        private RectTransform _content;
        private RectTransform _floating;
        private bool _dragging;
        private int _pointerId = int.MinValue;

        public static TraySlotView Create(Transform parent, GameScreen game, int slot, float size)
        {
            var bg = Ui.Box(parent, $"Slot{slot}", Theme.WithAlpha(Theme.Neutral, 0.6f), size, size);
            var v = bg.gameObject.AddComponent<TraySlotView>();
            v._game = game;
            v._slot = slot;
            v._size = size;
            v._content = Ui.Rect(bg.transform, "Piece");
            v._content.anchorMin = v._content.anchorMax = v._content.pivot = new Vector2(0.5f, 0.5f);
            return v;
        }

        public void Render()
        {
            Ui.Clear(_content);
            var p = _game.PieceAt(_slot);
            if (p == null || _dragging) return;
            float cell = Mathf.Min((_size - 24) / Mathf.Max(p.Shape.Width, p.Shape.Height), _size / 4.2f);
            PieceRenderer.Draw(_content, p.Shape, p.Color, cell, cell * 0.08f);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (_dragging) return;
            _game.OnSlotTap(_slot);
        }

        public void OnBeginDrag(PointerEventData e)
        {
            var p = _game.PieceAt(_slot);
            if (p == null || !_game.IsPlaying || _pointerId != int.MinValue) return; // ikinci parmak yok sayılır
            _pointerId = e.pointerId;
            _dragging = true;
            Render();
            _floating = Ui.Rect(App.I.Nav.Overlay, "Dragging");
            _floating.anchorMin = _floating.anchorMax = new Vector2(0, 0);
            _floating.pivot = new Vector2(0.5f, 0);
            float cell = _game.Pitch - GameScreen.CellGap;
            PieceRenderer.Draw(_floating, p.Shape, p.Color, cell, GameScreen.CellGap, topLeftOrigin: true);
            Move(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (!_dragging || e.pointerId != _pointerId) return;
            Move(e);
        }

        private void Move(PointerEventData e)
        {
            var p = _game.PieceAt(_slot);
            if (p == null || _floating == null) return;
            var overlay = App.I.Nav.Overlay;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay, e.position, null, out var local);
            var size = overlay.rect.size;
            _floating.anchoredPosition = new Vector2(local.x + size.x * overlay.pivot.x, local.y + size.y * overlay.pivot.y + GameScreen.DragLift);
            var anchor = _game.AnchorFor(e.position, p.Shape);
            _game.SetGhost(p.Shape, anchor, p.Color);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (!_dragging || e.pointerId != _pointerId) return;
            var p = _game.PieceAt(_slot);
            var anchor = p != null ? _game.AnchorFor(e.position, p.Shape) : null;
            EndDrag();
            _game.OnDrop(_slot, anchor);
            _game.Refresh();
        }

        /// <summary>Uygulama arka plana giderse parça tezgâha döner (Spec 1 §3).</summary>
        private void OnApplicationPause(bool paused) { if (paused && _dragging) { EndDrag(); _game.SetGhost(null, null, null); } }

        private void OnDisable() { if (_dragging) EndDrag(); }

        private void EndDrag()
        {
            _dragging = false;
            _pointerId = int.MinValue;
            if (_floating != null) Destroy(_floating.gameObject);
            _floating = null;
            Render();
        }
    }

    public static class PieceRenderer
    {
        /// <summary>Şekli hücre kutularıyla çizer. topLeftOrigin=false: kök merkezine ortalar.</summary>
        public static void Draw(RectTransform root, Shape shape, string color, float cell, float gap, bool topLeftOrigin = false)
        {
            float pitch = cell + gap;
            float w = shape.Width * pitch - gap, h = shape.Height * pitch - gap;
            root.sizeDelta = new Vector2(w, h);
            foreach (var c in shape.Cells)
            {
                var img = Ui.Box(root, "P", Theme.Motif(color));
                img.raycastTarget = false;
                var rt = (RectTransform)img.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.sizeDelta = new Vector2(cell, cell);
                rt.anchoredPosition = new Vector2(c.Col * pitch, -c.Row * pitch);
                var g = Ui.Label(img.transform, Theme.Glyph(color), (int)(cell * 0.45f), new Color(1, 1, 1, 0.6f));
                ((RectTransform)g.transform).Stretch();
            }
        }
    }
}
