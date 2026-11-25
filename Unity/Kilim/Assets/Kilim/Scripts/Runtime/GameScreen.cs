using System.Collections.Generic;
using Kilim.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kilim.Game
{
    /// <summary>
    /// 2 · Oyun ekranı (Spec 1 §1–§5, Spec 3 §1 ekran 2 ve 9). Üst: duraklat, bölüm, desen çubuğu (yıldız eşikleri
    /// çubuğun altında), desen kartı. Orta: tahta. Alt: kalan parça, 4'lü tezgâh (+ Tek İlmek yuvaları), güçlendiriciler.
    /// His: yerleşen hücreler zıplar, eşleşenler altın parlar, yıldız eşiği geçilince kutlama, geçersiz bırakmada titreme.
    /// </summary>
    public sealed class GameScreen : UiScreen
    {
        public const float BoardSize = 1000f;
        public const float BoardPad = 12f;
        public const float CellGap = 6f;
        public const float DragLift = 180f;

        private static readonly string[] Praise = { "Güzel!", "Harika!", "Muhteşem!", "Usta işi!" };

        private readonly LevelRun _run;
        private GameSession S => _run.Session;
        private LevelData L => _run.Level;
        private HintLevel _hint;

        private RectTransform _board;
        private Image[,] _cells;
        private Image[,] _glyphs;
        private Outline[,] _outlines;
        private float _pitch;
        private readonly HashSet<Cell> _ghost = new HashSet<Cell>();
        private string _ghostColor;

        private RectTransform _trayRow, _bonusRow;
        private readonly List<TraySlotView> _slots = new List<TraySlotView>();
        private Text _remaining, _percent, _star2, _star3;
        private System.Action<float> _bar;
        private RectTransform _boosterRow;
        private Text _hintText;
        private bool _swapMode;
        private bool _ending;
        private int _streak;
        private int _starsShown = 1;
        private BoosterService _boosters;

        public GameScreen(LevelRun run) { _run = run; }

        public bool SwapMode => _swapMode;

        public override void Build(RectTransform root)
        {
            _boosters = new BoosterService(App.Meta.Wallet, App.Config.Economy);
            _hint = HintPolicy.For(L.Id, App.Profile.EasyMode, _run.IsDaily);
            S.StateChanged += OnState;
            S.AutoUndone += _ => { App.Nav.Toast("Tezgâh doldu, son parça geri alındı."); Refresh(); };

            var v = Ui.VStack(root, 14, 20).Stretch();
            BuildTop(v);
            _hintText = Ui.Label(v, "", 36, Theme.Ink, TextAnchor.MiddleCenter, false, 64);
            BuildBoard(v);
            _remaining = Ui.Label(v, "", 32, Theme.Muted, TextAnchor.MiddleCenter, true, 46);
            _trayRow = Ui.HStack(v, 14, 0, TextAnchor.MiddleCenter, 236, "Tray");
            _bonusRow = Ui.HStack(v, 14, 0, TextAnchor.MiddleCenter, 120, "Bonus");
            _boosterRow = Ui.HStack(v, 16, 0, TextAnchor.MiddleCenter, 160, "Boosters");
            Ui.Spacer(v);

            BuildTray();
            BuildBoosters();
            Refresh();
            _starsShown = CurrentStars();
            IntroBoard();
            ShowIntroHints();
        }

        public override void OnClose() => S.StateChanged -= OnState;

        public override bool OnBack() { Pause(); return true; }

        // -----------------------------------------------------------------
        // Üst çubuk
        // -----------------------------------------------------------------

        private void BuildTop(Transform parent)
        {
            var top = Ui.HStack(parent, 18, 0, TextAnchor.MiddleCenter, 132, "Top");
            top.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            var pause = Ui.Button(top, "", Pause, Theme.Panel, Theme.Ink, 112, 40, 112);
            Ui.Icon(pause.transform, Art.Icon("pause"), 52, Theme.Ink);

            var mid = Ui.VStack(top, 8, 0, TextAnchor.MiddleCenter);
            Ui.Size(mid.gameObject, -1, 132, 1);
            string title = _run.IsDaily ? "Günün Deseni" : L.Boss ? $"Bölüm {L.Id} · Usta" : $"Bölüm {L.Id}";
            Ui.Label(mid, title, 44, Theme.Ink, TextAnchor.MiddleCenter, true, 54);

            var barRow = Ui.HStack(mid, 12, 0, TextAnchor.MiddleCenter, 70);
            barRow.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
            var barHolder = Ui.Rect(barRow, "BarHolder");
            Ui.Size(barHolder.gameObject, -1, 70, 1);
            var barArea = Ui.Rect(barHolder, "BarArea");
            barArea.anchorMin = new Vector2(0, 1);
            barArea.anchorMax = new Vector2(1, 1);
            barArea.pivot = new Vector2(0.5f, 1);
            barArea.sizeDelta = new Vector2(0, 30);
            _bar = Ui.Bar(barArea, 30, Theme.Gold, Theme.Neutral);
            ((RectTransform)barArea.GetChild(0)).Stretch();
            // Yıldız eşikleri çubuğun ALTINDA (başlıkla çakışmaz).
            float t2 = (float)L.Star2Cells / L.Star3Cells;
            _star2 = Marker(barHolder, t2, "★★");
            _star3 = Marker(barHolder, 1f, "★★★");
            _percent = Ui.Label(barRow, "%0", 34, Theme.Ink, TextAnchor.MiddleRight, true);
            Ui.Size(_percent.gameObject, 104, 70);

            if (_hint == HintLevel.Full)
            {
                var spacer = Ui.Rect(top, "Balance");
                Ui.Size(spacer.gameObject, 112, 112, 0);
            }
            else PatternCard(top, 124);
        }

        private static Text Marker(RectTransform holder, float t, string label)
        {
            var m = Ui.Box(holder, "Marker", Theme.Ink);
            m.sprite = null;
            m.type = Image.Type.Simple;
            m.raycastTarget = false;
            var rt = (RectTransform)m.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(t, 1);
            rt.pivot = new Vector2(t >= 1 ? 1 : 0.5f, 1);
            rt.sizeDelta = new Vector2(4, 34);
            var lbl = Ui.Label(holder, label, 24, Theme.Muted, t >= 1 ? TextAnchor.UpperRight : TextAnchor.UpperCenter, true);
            var lrt = (RectTransform)lbl.transform;
            lrt.anchorMin = lrt.anchorMax = new Vector2(t, 1);
            lrt.pivot = new Vector2(t >= 1 ? 1 : 0.5f, 1);
            lrt.anchoredPosition = new Vector2(0, -36);
            lrt.sizeDelta = new Vector2(120, 32);
            return lbl;
        }

        /// <summary>Desen kartı: hedef desenin küçük önizlemesi. Dokununca büyür (ipucu azaldıkça desen buradan okunur).</summary>
        private void PatternCard(Transform parent, float size)
        {
            var card = Ui.Box(parent, "PatternCard", Theme.BoardBg, size, size);
            Ui.Size(card.gameObject, size, size, 0);
            Ui.Soft(card, 6);
            DrawPattern(card.transform, size, 6);
            var btn = card.gameObject.AddComponent<Button>();
            btn.targetGraphic = card;
            card.gameObject.AddComponent<PressFx>();
            btn.onClick.AddListener(() => { Audio.Click(); ShowPatternLarge(); });
        }

        private void DrawPattern(Transform root, float size, float pad)
        {
            int n = L.Size;
            float pitch = (size - 2 * pad) / n;
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                {
                    if (!L.IsPlayable(r, c)) continue;
                    var img = Ui.Box(root, "P", Theme.Motif(L.Motif[r, c].Split('|')[0]));
                    img.sprite = Art.TileFlat;
                    img.raycastTarget = false;
                    var rt = (RectTransform)img.transform;
                    rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
                    rt.sizeDelta = new Vector2(pitch - 2, pitch - 2);
                    rt.anchoredPosition = new Vector2(pad + c * pitch + 1, -(pad + r * pitch + 1));
                }
        }

        private void ShowPatternLarge()
        {
            var card = App.Nav.Modal(dimTapCloses: true);
            Ui.Label(card, "Desen kartı", 52, Theme.Ink, TextAnchor.MiddleCenter, true);
            var holder = Ui.Rect(card, "Holder");
            Ui.Size(holder.gameObject, -1, 700);
            var board = Ui.Box(holder, "Big", Theme.BoardBg);
            var rt = (RectTransform)board.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(700, 700);
            DrawPattern(board.transform, 700, 14);
            Ui.Label(card, _hint == HintLevel.None
                ? "Tahtada artık ipucu yok. Parçaları bu desene göre yerleştir; eşleşen her hücre yıldıza yaklaştırır."
                : "Tahtada yalnızca şekiller var. Renkleri bu karttan oku.", 34, Theme.Muted);
            Ui.Button(card, "Tamam", () => App.Nav.Close(card));
        }

        // -----------------------------------------------------------------
        // Tahta
        // -----------------------------------------------------------------

        private void BuildBoard(Transform parent)
        {
            var holder = Ui.Rect(parent, "BoardHolder");
            Ui.Size(holder.gameObject, -1, BoardSize);
            var bg = Ui.Box(holder, "Board", Theme.BoardBg);
            Ui.Round(bg, 1.4f);
            Ui.Soft(bg, 12);
            _board = (RectTransform)bg.transform;
            _board.anchorMin = _board.anchorMax = new Vector2(0.5f, 1);
            _board.pivot = new Vector2(0, 1);
            _board.sizeDelta = new Vector2(BoardSize, BoardSize);
            _board.anchoredPosition = new Vector2(-BoardSize / 2, 0);

            int n = L.Size;
            _pitch = (BoardSize - 2 * BoardPad + CellGap) / n;
            float cell = _pitch - CellGap;
            _cells = new Image[n, n];
            _glyphs = new Image[n, n];
            _outlines = new Outline[n, n];
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                {
                    var img = Ui.Box(_board, $"C{r}_{c}", Theme.CellEmpty);
                    var rt = (RectTransform)img.transform;
                    rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(cell, cell);
                    rt.anchoredPosition = new Vector2(BoardPad + c * _pitch + cell / 2, -(BoardPad + r * _pitch + cell / 2));
                    img.raycastTarget = false;
                    var g = Ui.Icon(img.transform, null, 0);
                    ((RectTransform)g.transform).Stretch(cell * 0.28f, cell * 0.28f, cell * 0.28f, cell * 0.28f);
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

        /// <summary>Açılış: hücreler köşegen dalga halinde belirir.</summary>
        private void IntroBoard()
        {
            int n = L.Size;
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                    if (L.IsPlayable(r, c)) Fx.PopIn(_cells[r, c].transform, 0.32f, 0.02f * (r + c));
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
                    string motif = L.Motif[r, c].Split('|')[0];
                    var here = new Cell(r, c);
                    if (S.Board.OwnerAt(r, c) != Board.Empty)
                    {
                        string col = S.Board.ColorAt(r, c);
                        img.color = Theme.Motif(col);
                        Mark(g, string.IsNullOrEmpty(col) ? "" : col, new Color(1, 1, 1, 0.5f));
                        _outlines[r, c].enabled = S.Board.IsMatchAt(r, c);
                    }
                    else if (_ghost.Contains(here))
                    {
                        img.color = Theme.WithAlpha(Theme.Motif(_ghostColor), 0.5f);
                        Mark(g, "", Color.clear);
                        // İpucu yoksa sürüklerken eşleşme gösterilmez (desen kartından okunmalı).
                        _outlines[r, c].enabled = _hint != HintLevel.None && L.Matches(r, c, _ghostColor);
                    }
                    else
                    {
                        _outlines[r, c].enabled = false;
                        switch (_hint)
                        {
                            case HintLevel.Full:
                                var mc = Theme.Motif(motif);
                                img.color = Color.Lerp(Theme.CellEmpty, mc, 0.22f);
                                Mark(g, motif, Theme.WithAlpha(mc, 0.55f));
                                break;
                            case HintLevel.Glyph:
                                img.color = Theme.CellEmpty;
                                Mark(g, motif, Theme.WithAlpha(Theme.Muted, 0.32f));
                                break;
                            default:
                                img.color = Theme.CellEmpty;
                                Mark(g, "", Color.clear);
                                break;
                        }
                    }
                }
        }

        private static void Mark(Image g, string code, Color c)
        {
            var s = Art.Motif(code);
            g.sprite = s;
            g.color = c;
            g.enabled = s != null;
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
            if (anchor.HasValue && shape != null)
                foreach (var c in shape.Cells) _ghost.Add(anchor.Value.Offset(c));
            RefreshBoard();
        }

        /// <summary>Hücrenin tahta merkezine göre konumu (yüzen yazılar için).</summary>
        private Vector2 CellCenter(Cell c)
        {
            float cell = _pitch - CellGap;
            return new Vector2(BoardPad + c.Col * _pitch + cell / 2 - BoardSize / 2, -(BoardPad + c.Row * _pitch + cell / 2) + BoardSize / 2);
        }

        // -----------------------------------------------------------------
        // Tezgâh
        // -----------------------------------------------------------------

        private void BuildTray()
        {
            for (int i = 0; i < GameSession.TraySlots; i++) _slots.Add(TraySlotView.Create(_trayRow, this, i, 228));
            for (int i = 0; i < GameSession.BonusSlots; i++)
                _slots.Add(TraySlotView.Create(_bonusRow, this, GameSession.TraySlots + i, 112));
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
            if (_swapMode)
            {
                // Değiştir kayıp anında da (kurtarma) çalışır.
                if (S.State == SessionState.Won || App.Nav.HasModal || S.Tray[slot] == null) return;
                if (slot >= GameSession.TraySlots) { App.Nav.Toast("Tek İlmek değiştirilemez."); return; }
                ExitSwapMode();
                UseBooster(Booster.Swap, slot);
                _slots[slot].PopIn();
                return;
            }
            if (!IsPlaying || S.Tray[slot] == null) return;
            if (!TutorialAllowsRotate()) return;
            S.Rotate(slot);
            Audio.Play("rotate");
            Haptic.Tick();
            Refresh();
            _slots[slot].Spin();
        }

        /// <summary>Bırakma. Geçerliyse yerleşir; değilse parça yuvasına döner (ceza yok).</summary>
        public bool OnDrop(int slot, Cell? anchor)
        {
            SetGhost(null, null, null);
            if (!IsPlaying) return false;
            if (!anchor.HasValue) { Invalid(slot); return false; }
            if (!TutorialAllowsMove(slot, anchor.Value)) { Invalid(slot); App.Nav.Toast("Önce gösterilen hamleyi yap."); return false; }
            var piece = S.Tray[slot];
            bool rotated = piece.Rotation != 0;
            int starsBefore = CurrentStars();
            var res = S.Place(slot, anchor.Value);
            if (!res.Success) { Invalid(slot); return false; }
            Audio.Place(res.MatchedCells > 0);
            Haptic.Light();
            if (S.PlacementCount % 5 == 1)
                App.Analytics.Track("piece_placed", ("level", L.Id), ("index", S.PlacementCount), ("rotated", rotated), ("motif_match_cells", res.MatchedCells));
            Refresh();
            Celebrate(res, piece, starsBefore);
            if (slot < GameSession.TraySlots && S.Tray[slot] != null) _slots[slot].PopIn(0.12f);
            return true;
        }

        private void Invalid(int slot)
        {
            Audio.Play("invalid");
            Fx.Shake((RectTransform)_slots[slot].transform, 16, 0.32f);
        }

        /// <summary>Yerleşim kutlaması: hücreler zıplar, eşleşenler altın parlar, ardışık tam eşleşmede övgü, yıldız eşiği.</summary>
        private void Celebrate(PlaceResult res, TrayPiece piece, int starsBefore)
        {
            for (int i = 0; i < res.Cells.Count; i++)
            {
                var c = res.Cells[i];
                var img = _cells[c.Row, c.Col];
                Fx.Pop(img.transform, 1.16f, 0.3f, i * 0.035f);
                if (S.Board.IsMatchAt(c.Row, c.Col))
                    Fx.Tint(img, Color.Lerp(Theme.Motif(piece.Color), Color.white, 0.55f), Theme.Motif(piece.Color), 0.45f, Ease.OutQuad, i * 0.035f);
            }
            bool full = !piece.IsSingleStitch && res.MatchedCells == res.Cells.Count;
            _streak = full ? _streak + 1 : 0;
            var center = CellCenter(res.Cells[res.Cells.Count / 2]);
            if (res.MatchedCells > 0)
                Fx.FloatText(_board, center, _streak >= 2 ? Praise[Mathf.Min(_streak - 2, Praise.Length - 1)] : $"+{res.MatchedCells}", Theme.Gold, _streak >= 2 ? 64 : 54);

            int starsNow = CurrentStars();
            if (starsNow > starsBefore && starsNow > _starsShown)
            {
                _starsShown = starsNow;
                var lbl = starsNow >= 3 ? _star3 : _star2;
                if (lbl != null)
                {
                    lbl.color = Theme.Gold;
                    Fx.Pop(lbl.transform, 1.6f, 0.5f);
                }
                Audio.Play(starsNow >= 3 ? "star3" : "star2");
                Haptic.Medium();
            }
        }

        /// <summary>Şu anki eşleşmeye göre kazanılacak yıldız (çubuk işaretleri için).</summary>
        private int CurrentStars()
        {
            int m = S.Board.MatchCount;
            return m >= L.Star3Cells ? 3 : m >= L.Star2Cells ? 2 : 1;
        }

        // -----------------------------------------------------------------
        // Güçlendiriciler
        // -----------------------------------------------------------------

        private void BuildBoosters()
        {
            Ui.Clear(_boosterRow);
            if (_run.IsDaily) return; // zen modunda güçlendirici yok
            BoosterButton(Booster.Undo, "Geri Al", "undo");
            BoosterButton(Booster.Swap, _swapMode ? "Vazgeç" : "Değiştir", "swap");
            BoosterButton(Booster.SingleStitch, "Tek İlmek", "stitch");
        }

        private void BoosterButton(Booster b, string name, string icon)
        {
            var w = App.Meta.Wallet;
            var cfg = App.Config.Economy;
            bool unlocked = w.IsUnlocked(b) || w.BoosterCount(b) > 0;
            bool active = b == Booster.Swap && _swapMode;
            var bg = active ? Theme.Gold : unlocked ? Theme.Panel : Theme.Neutral;
            var btn = Ui.Button(_boosterRow, "", () => OnBoosterTap(b), bg, Theme.Ink, 156, 30);
            var label = Ui.ButtonLabel(btn);
            var inner = Ui.VStack(btn.transform, 2, 6, TextAnchor.MiddleCenter);
            inner.Stretch(8, 8, 8, 14);
            var ic = Ui.Icon(inner, unlocked ? Art.Icon(icon) : Art.Lock, 64, unlocked ? Theme.Ink : Theme.Muted);
            Ui.Size(ic.gameObject, 64, 64, 0);
            Ui.Label(inner, name, 30, unlocked ? Theme.Ink : Theme.Muted, TextAnchor.MiddleCenter, true, 36);
            label.text = "";
            if (!unlocked)
            {
                Ui.Label(inner, $"Bölüm {cfg.BoosterUnlockLevel[b]}'da", 24, Theme.Muted, TextAnchor.MiddleCenter, false, 28);
                return;
            }
            if (w.BoosterCount(b) > 0) Ui.Badge(btn.transform, w.BoosterCount(b).ToString(), Theme.Ok);
            else Ui.Badge(btn.transform, $"●{cfg.BoosterPrice[b]}", Theme.Gold);
            bool applicable = active || _boosters.IsApplicable(S, b, FirstOccupiedSlot()) || b == Booster.Swap && S.RemainingInQueue > 0;
            bool live = S.State == SessionState.Playing || S.State == SessionState.Failed && _swapMode;
            Ui.SetEnabled(btn, applicable && live);
            if (active) Fx.Pulse(btn.targetGraphic, Theme.Gold, Theme.Cream, 0.8f);
        }

        private int FirstOccupiedSlot()
        {
            for (int i = 0; i < GameSession.TraySlots; i++) if (S.Tray[i] != null) return i;
            return -1;
        }

        private void OnBoosterTap(Booster b)
        {
            var w = App.Meta.Wallet;
            if (!w.IsUnlocked(b) && w.BoosterCount(b) == 0)
            {
                App.Nav.Toast($"{BoosterName(b)} {App.Config.Economy.BoosterUnlockLevel[b]}. bölümde açılır.");
                return;
            }
            if (b == Booster.Swap)
            {
                if (_swapMode) { ExitSwapMode(); if (S.State == SessionState.Failed) FailFlow.Show(_run, this); return; }
                if (!IsPlaying) return;
                EnterSwapMode();
                return;
            }
            if (!IsPlaying) return;
            UseBooster(b, -1);
        }

        private void EnterSwapMode()
        {
            _swapMode = true;
            Haptic.Tick();
            foreach (var s in _slots) s.SetSwapHighlight(true);
            Refresh();
        }

        private void ExitSwapMode()
        {
            _swapMode = false;
            foreach (var s in _slots) s.SetSwapHighlight(false);
            Refresh();
        }

        public static string BoosterName(Booster b) => b == Booster.Undo ? "Geri Al" : b == Booster.Swap ? "Değiştir" : "Tek İlmek";

        /// <summary>Kayıp kartından kurtarma: güçlendirici uygulanır, oyun kaldığı yerden sürer.</summary>
        public void Rescue(Booster b)
        {
            if (b == Booster.Swap) { EnterSwapMode(); App.Nav.Toast("Değiştirmek istediğin parçaya dokun."); return; }
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
                    Audio.Play(b == Booster.Undo ? "rotate" : "unlock");
                    Haptic.Medium();
                    if (b == Booster.SingleStitch) App.Delay(0.05f, () => { foreach (var s in _slots) if (s.Slot >= GameSession.TraySlots) s.PopIn(); });
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
            int left = S.RemainingPieces;
            _remaining.text = Loc.T(left == 0 ? "Parça kalmadı" : $"Kalan {left} parça");
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
                if (_swapMode) ExitSwapMode();
                Audio.Play("win");
                Haptic.Success();
                Refresh();
                // 9.3 son parça oturunca dokuma dalgası + konfeti, sonra Kazandın
                WeaveAnim();
                App.Delay(1.5f, () =>
                {
                    var report = App.Meta.CompleteWin(_run);
                    App.Save();   // ödül tek işlemde kalıcı (UAT WIN-08)
                    if (report.RaceFlag) App.Backend.RaceFlag(L.Id, _run.Attempt);
                    App.Analytics.Track("level_win", ("level", L.Id), ("stars", report.Stars), ("motif_pct", Mathf.RoundToInt((float)report.PatternPercent * 100)),
                        ("duration_s", report.DurationSeconds), ("attempts", _run.Attempt), ("boosters_used", _run.BoostersUsed.Count));
                    if (_run.IsDaily) App.Analytics.Track("daily_pattern", ("date", App.Meta.Daily.Today), ("stars", report.Stars));
                    App.Nav.Show(new WinScreen(_run, report));
                });
            }
            else if (st == SessionState.Failed)
            {
                if (_swapMode) ExitSwapMode();
                Refresh();
                Audio.Play("fail");
                Haptic.Medium();
                Fx.Shake(_board, 14, 0.4f);
                App.Analytics.Track("level_fail", ("level", L.Id), ("attempt", _run.Attempt), ("pieces_left", S.RemainingPieces),
                    ("cells_left", S.Board.PlayableCells - S.Board.FilledCount));
                App.Delay(0.6f, () => { if (S.State == SessionState.Failed && App.Nav.Current == this) FailFlow.Show(_run, this); }); // 600 ms tahta görünür
            }
            else if (st == SessionState.Playing)
            {
                Refresh();
            }
        }

        private void WeaveAnim()
        {
            int n = L.Size;
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                {
                    if (!L.IsPlayable(r, c)) continue;
                    var img = _cells[r, c];
                    float d = 0.045f * (r + c);
                    Fx.Pop(img.transform, 1.14f, 0.34f, d);
                    var col = img.color;
                    Fx.Tint(img, Color.Lerp(col, Color.white, 0.6f), col, 0.5f, Ease.OutQuad, d);
                }
            App.Delay(0.045f * 2 * n, () => Fx.Confetti(App.Nav.Overlay, new Vector2(0, -100), 60));
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
                ("Bölüm haritası", () => App.Nav.Show(new LevelMapScreen())),
                ("Ana menü", () => App.GoHome()),
            });
        }

        // -----------------------------------------------------------------
        // Öğretici (Spec 3 8.4): Bölüm 1 ilk hamle zorunlu; 2: desen; 4: döndürme. Güçlendirici ve zorluk açılışları.
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
            if (_hintText == null) return;
            if (_swapMode) { _hintText.text = Loc.T("Değiştirmek istediğin parçaya dokun."); return; }
            if (FirstMoveForced)
            {
                var step = L.Solution[0];
                string colorName = L.Queue[step.QueueIndex].Color == "L" ? "Lacivert" : "Bu";
                _hintText.text = Loc.T($"Nine: \"{colorName} parçayı aynı renkli izin üstüne koy.\"");
                SetGhostSilently(Shape.From(step.Cells), new Cell(MinRow(step.Cells), MinCol(step.Cells)), L.Queue[step.QueueIndex].Color);
            }
            else if (!_run.IsDaily && L.Id == 2 && S.PlacementCount < 2) _hintText.text = Loc.T("Rengi motifle eşleşen parça desen yıldızı kazandırır.");
            else if (!_run.IsDaily && L.Id == 4 && S.PlacementCount == 0) _hintText.text = Loc.T("Parçaya dokun: saat yönünde döner.");
            else if (_run.IsDaily) _hintText.text = Loc.T("Süre yok, kaybetmek yok.");
            else if (_hint != HintLevel.Full && S.PlacementCount == 0) _hintText.text = Loc.T("Desen kartına dokun: hedef deseni gör.");
            else _hintText.text = "";
        }

        /// <summary>Öğretici hayaleti bir kez çizer (Refresh döngüsüne girmeden).</summary>
        private void SetGhostSilently(Shape shape, Cell anchor, string color)
        {
            _ghost.Clear();
            _ghostColor = color;
            foreach (var c in shape.Cells) _ghost.Add(anchor.Offset(c));
            RefreshBoard();
            _ghost.Clear();
        }

        private static int MinRow(IReadOnlyList<Cell> cs) { int m = int.MaxValue; foreach (var c in cs) m = Mathf.Min(m, c.Row); return m; }
        private static int MinCol(IReadOnlyList<Cell> cs) { int m = int.MaxValue; foreach (var c in cs) m = Mathf.Min(m, c.Col); return m; }

        private void ShowIntroHints()
        {
            if (_run.IsDaily || _run.Attempt != 1) return;
            foreach (var kv in App.Config.Economy.BoosterUnlockLevel)
                if (kv.Value == L.Id)
                {
                    string what = kv.Key == Booster.Undo ? "Son yerleşimi geri alır. Tezgâh dolduğunda da kurtarır."
                        : kv.Key == Booster.Swap ? "Seçtiğin parçayı kuyruğun sonuna gönderir, yerine sıradaki gelir."
                        : "Tezgâha 1 hücrelik ek parça koyar: kalan boşlukları kapatır (desen yüzdesini düşürür).";
                    NewThing($"Yeni: {BoosterName(kv.Key)}", $"{what}\n3 tane hediye!", Art.Icon(kv.Key == Booster.Undo ? "undo" : kv.Key == Booster.Swap ? "swap" : "stitch"));
                }
            if (!App.Profile.EasyMode && L.Id == HintPolicy.GlyphFrom)
                NewThing("Usta dokumacı", "Tahtada artık renkler yok, yalnızca şekiller. Renkleri sağ üstteki desen kartından oku.", Art.Icon("pattern"));
            if (!App.Profile.EasyMode && L.Id == HintPolicy.NoneFrom)
                NewThing("Ezbere dokuma", "Bundan sonra tahta boş. Desen kartına bak, deseni aklında tut ve doku!\n(Ayarlar › Kolay mod ipuçlarını geri getirir.)", Art.Icon("pattern"));
        }

        private void NewThing(string title, string body, Sprite icon)
        {
            var card = App.Nav.Modal();
            var ic = Ui.Icon(card, icon, 150, Theme.Primary);
            Fx.PopIn(ic.transform, 0.45f, 0.1f);
            Ui.Label(card, title, 54, Theme.Ink, TextAnchor.MiddleCenter, true);
            Ui.Label(card, body, 36, Theme.Muted);
            Ui.Button(card, "Anladım", () => App.Nav.Close(card));
            Audio.Play("unlock");
        }
    }

    /// <summary>Tezgâh yuvası: dokun = döndür, sürükle = yerleştir (Spec 1 §2–§3).</summary>
    public sealed class TraySlotView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private GameScreen _game;
        private int _slot;
        private float _size;
        private Image _bg;
        private RectTransform _content;
        private RectTransform _floating;
        private bool _dragging;
        private int _pointerId = int.MinValue;
        private TrayPiece _last;

        public int Slot => _slot;

        public static TraySlotView Create(Transform parent, GameScreen game, int slot, float size)
        {
            var bg = Ui.Box(parent, $"Slot{slot}", Theme.WithAlpha(Theme.Sand, 0.85f), size, size);
            Ui.Soft(bg, 6);
            var v = bg.gameObject.AddComponent<TraySlotView>();
            v._game = game;
            v._slot = slot;
            v._size = size;
            v._bg = bg;
            v._content = Ui.Rect(bg.transform, "Piece");
            v._content.anchorMin = v._content.anchorMax = v._content.pivot = new Vector2(0.5f, 0.5f);
            return v;
        }

        public void Render()
        {
            Ui.Clear(_content);
            var p = _game.PieceAt(_slot);
            if (p == null || _dragging) { _last = p; return; }
            float cell = Mathf.Min((_size - 28) / Mathf.Max(p.Shape.Width, p.Shape.Height), _size / 4.2f);
            PieceRenderer.Draw(_content, p.Shape, p.Color, cell, cell * 0.08f);
            if (_last != null && !ReferenceEquals(_last, p)) PopIn(); // yeni gelen parça
            _last = p;
        }

        public void PopIn(float delay = 0) => Fx.PopIn(_content, 0.34f, delay);

        public void Spin()
        {
            _content.localRotation = Quaternion.Euler(0, 0, 90);
            Fx.To(_content, 0.22f, Ease.OutBack, k => _content.localRotation = Quaternion.Euler(0, 0, 90 * (1 - k)), null, 0, "spin");
        }

        public void SetSwapHighlight(bool on)
        {
            if (_bg == null) return;
            Fx.Kill(_bg);
            if (on && _slot < GameSession.TraySlots && _game.PieceAt(_slot) != null) Fx.Pulse(_bg, Theme.WithAlpha(Theme.Sand, 0.85f), Theme.WithAlpha(Theme.Gold, 0.9f), 0.8f);
            else _bg.color = Theme.WithAlpha(Theme.Sand, 0.85f);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (_dragging) return;
            _game.OnSlotTap(_slot);
        }

        public void OnBeginDrag(PointerEventData e)
        {
            var p = _game.PieceAt(_slot);
            if (p == null || !_game.IsPlaying || _game.SwapMode || _pointerId != int.MinValue) return; // ikinci parmak yok sayılır
            _pointerId = e.pointerId;
            _dragging = true;
            Render();
            _floating = Ui.Rect(App.I.Nav.Overlay, "Dragging");
            _floating.anchorMin = _floating.anchorMax = new Vector2(0, 0);
            _floating.pivot = new Vector2(0.5f, 0);
            float cell = _game.Pitch - GameScreen.CellGap;
            PieceRenderer.Draw(_floating, p.Shape, p.Color, cell, GameScreen.CellGap, topLeftOrigin: true, lifted: true);
            Fx.Scale(_floating, 0.6f, 1f, 0.22f, Ease.OutBack);
            Haptic.Tick();
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
            bool placed = _game.OnDrop(_slot, anchor);
            _game.Refresh();
            if (!placed) PopIn();
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
            _last = null; // geri dönüşte "yeni parça" zıplaması olmasın
            Render();
            _last = _game.PieceAt(_slot);
        }
    }

    public static class PieceRenderer
    {
        /// <summary>Şekli hücre kutularıyla çizer. topLeftOrigin=false: kök merkezine ortalar. lifted: sürüklenirken gölge.</summary>
        public static void Draw(RectTransform root, Shape shape, string color, float cell, float gap, bool topLeftOrigin = false, bool lifted = false)
        {
            float pitch = cell + gap;
            float w = shape.Width * pitch - gap, h = shape.Height * pitch - gap;
            root.sizeDelta = new Vector2(w, h);
            var baseColor = Theme.Motif(color);
            foreach (var c in shape.Cells)
            {
                var img = Ui.Box(root, "P", baseColor);
                img.raycastTarget = false;
                var sh = img.gameObject.AddComponent<Shadow>();
                sh.effectColor = lifted ? new Color(0, 0, 0, 0.28f) : Theme.WithAlpha(Theme.Darker(baseColor, 0.62f), 1f);
                sh.effectDistance = lifted ? new Vector2(0, -cell * 0.18f) : new Vector2(0, -cell * 0.09f);
                var rt = (RectTransform)img.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.sizeDelta = new Vector2(cell, cell);
                rt.anchoredPosition = new Vector2(c.Col * pitch, -c.Row * pitch);
                var s = Art.Motif(color);
                if (s != null)
                {
                    var g = Ui.Icon(img.transform, s, 0, new Color(1, 1, 1, 0.55f));
                    ((RectTransform)g.transform).Stretch(cell * 0.28f, cell * 0.28f, cell * 0.28f, cell * 0.28f);
                }
            }
        }
    }
}
