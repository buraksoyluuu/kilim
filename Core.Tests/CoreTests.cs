using System.Linq;
using static Kilim.Core.Tests.TestLevels;

namespace Kilim.Core.Tests
{
    public static class ShapeTests
    {
        [Test("ROT-01")]
        public static void RotateIsClockwise()
        {
            // └ (dikey + sağa ayak) saat yönünde 90° → ┌ (üstte yatay + solda bacak)
            var l = S(0, 0, 1, 0, 1, 1);
            Assert.Equal(S(0, 0, 0, 1, 1, 0), l.RotateCW());
        }

        [Test("ROT-02")]
        public static void FourRotationsReturnOriginal()
        {
            var t = S(0, 0, 0, 1, 0, 2, 1, 1);
            Assert.Equal(t, t.RotateCW().RotateCW().RotateCW().RotateCW());
        }

        [Test]
        public static void DistinctRotationCounts()
        {
            Assert.Equal(1, S(0, 0, 0, 1, 1, 0, 1, 1).DistinctRotations().Count, "kare");
            Assert.Equal(2, S(0, 0, 0, 1, 0, 2).DistinctRotations().Count, "I3");
            Assert.Equal(4, S(0, 0, 1, 0, 1, 1).DistinctRotations().Count, "L3");
        }

        [Test]
        public static void ShapeNormalizes()
        {
            Assert.Equal(S(0, 0, 0, 1), Shape.From(new[] { new Cell(5, 4), new Cell(5, 3) }));
            Assert.Throws<System.ArgumentException>(() => Shape.From(new[] { new Cell(0, 0), new Cell(0, 0) }));
        }
    }

    public static class LevelTests
    {
        [Test("LVL-01")]
        public static void Region1Has20ValidLevels()
        {
            Assert.Equal(20, Region1.Count);
            foreach (var l in Region1)
            {
                var errs = l.Validate();
                Assert.True(errs.Count == 0, $"bölüm {l.Id}: {string.Join("; ", errs)}");
            }
        }

        [Test("LVL-02, STAR-03")]
        public static void Region1SolutionsReplayTo3Stars()
        {
            foreach (var l in Region1)
            {
                Assert.True(l.Solution.Count == l.Queue.Count, $"bölüm {l.Id} çözüm yolu eksik");
                var s = new GameSession(l);
                foreach (var step in l.Solution)
                {
                    Assert.Equal(SessionState.Playing, s.State, $"bölüm {l.Id}");
                    var r = PlaceCells(s, SlotOf(s, step.QueueIndex), step.Cells);
                    Assert.True(r.Success, $"bölüm {l.Id} adım q{step.QueueIndex} yerleşmedi");
                }
                Assert.Equal(SessionState.Won, s.State, $"bölüm {l.Id}");
                Assert.Equal(3, s.Stars, $"bölüm {l.Id}");
                Assert.Equal(1.0, s.PatternPercent, $"bölüm {l.Id}");
            }
        }

        [Test]
        public static void DoubleMotifCellAcceptsBothColors()
        {
            var l = Make(new[] { "KKK", "KKK", "KKK" }, (S(0, 0), "K"));
            l.Motif[1, 1] = "K|L";
            Assert.True(l.Matches(1, 1, "K"));
            Assert.True(l.Matches(1, 1, "L"));
            Assert.False(l.Matches(1, 1, "S"));
            Assert.False(l.Matches(1, 1, ""));
        }
    }

    public static class TrayTests
    {
        [Test("TRAY-01")]
        public static void TrayShowsFirstFourAndRemaining()
        {
            var s = new GameSession(Region1[0]); // 9 parça
            Assert.Equal("0,1,2,3", string.Join(",", s.Tray.Take(4).Select(p => p.QueueIndex)));
            Assert.Equal(5, s.RemainingInQueue);
            Assert.Equal(9, s.RemainingPieces);
        }

        [Test("TRAY-02")]
        public static void RefillGoesToEmptiedSlot()
        {
            var l = Region1[0];
            var s = new GameSession(l);
            var step = l.Solution[0]; // q3
            int slot = SlotOf(s, step.QueueIndex);
            Assert.True(PlaceCells(s, slot, step.Cells).Success);
            Assert.Equal(4, s.Tray[slot].QueueIndex, "boşalan yuvaya sıradaki (q4)");
            Assert.Equal(0, s.Tray[0].QueueIndex, "diğer yuvalar yerinde");
            Assert.Equal(8, s.RemainingPieces);
        }

        [Test("CORE-03")]
        public static void InvalidPlacementChangesNothing()
        {
            var s = new GameSession(ThreeByThree());
            Assert.False(s.Place(0, new Cell(2, 2)).Success, "taşan konum");
            Assert.False(s.Place(0, new Cell(-1, 0)).Success, "negatif");
            Assert.Equal(0, s.Board.FilledCount);
            Assert.True(s.Place(0, new Cell(0, 0)).Success);
            Assert.False(s.CanPlace(1, new Cell(0, 0)), "dolu hücre");
        }

        [Test("IAP-11, BST-10")]
        public static void DoublePlaceOnSameSlotIsSafe()
        {
            var s = new GameSession(ThreeByThree());
            Assert.True(s.Place(0, new Cell(0, 1)).Success);
            Assert.False(s.Place(0, new Cell(0, 1)).Success, "boş yuvadan ikinci yerleşim");
            Assert.Equal(4, s.Board.FilledCount);
        }
    }

    public static class MagnetTests
    {
        [Test("CORE-05")]
        public static void SnapsToNearestValidUpLeftOnTie()
        {
            var s = new GameSession(ThreeByThree());
            var square = s.Tray[0].Shape;
            Assert.Equal(new Cell(1, 1), s.Board.SnapAnchor(square, new Cell(1, 1)).Value, "geçerliyse kendisi");
            // (2,2) geçersiz; komşular: (1,1) çapraz → tek geçerli aday
            Assert.Equal(new Cell(1, 1), s.Board.SnapAnchor(square, new Cell(2, 2)).Value);
            // (2,1) geçersiz; (1,1) ve (1,0) dikey/çapraz: en yakın dikey (1,1)
            Assert.Equal(new Cell(1, 1), s.Board.SnapAnchor(square, new Cell(2, 1)).Value);
            // (1,2) geçersiz; aday (0,1),(1,1): (1,1) mesafe 1 → kazanır
            Assert.Equal(new Cell(1, 1), s.Board.SnapAnchor(square, new Cell(1, 2)).Value);
            // eşitlik: (2,0)... aday (1,0) mesafe 1 tek; daha uzak hedef → null
            Assert.True(s.Board.SnapAnchor(square, new Cell(5, 5)) == null);
        }

        [Test("CORE-05")]
        public static void TieBreaksUpThenLeft()
        {
            // I2 yatay, hedef (1,1) dolu: (0,1) ve (1,0)… eşit mesafe 1 → yukarı (0,1) kazanır
            var l = Make(new[] { "KKK", "KKK", "KKK" },
                (S(0, 0), "K"), (S(0, 0, 0, 1), "K"), (S(0, 0, 0, 1, 1, 0, 1, 1), "K"), (S(0, 0, 1, 0), "K"));
            var s = new GameSession(l);
            Assert.True(s.Place(0, new Cell(1, 1)).Success); // ortayı doldur
            var dom = s.Tray[1].Shape;
            Assert.Equal(new Cell(0, 1), s.Board.SnapAnchor(dom, new Cell(1, 1)).Value);
        }
    }

    public static class WinFailTests
    {
        [Test("WIN-01")]
        public static void FullBoardWins()
        {
            var s = new GameSession(ThreeByThree());
            Assert.True(s.Place(0, new Cell(0, 0)).Success);   // kare sol-üst
            Assert.True(PlaceCells(s, 1, new[] { new Cell(0, 2), new Cell(1, 2), new Cell(2, 2) }).Success);
            Assert.True(s.Place(2, new Cell(2, 0)).Success);   // domino alt
            Assert.Equal(SessionState.Won, s.State);
            Assert.Equal(3, s.Stars);
        }

        [Test("FAIL-01")]
        public static void TrayFullWhenNothingFits()
        {
            var s = new GameSession(ThreeByThree());
            SessionState? seen = null;
            s.StateChanged += st => seen = st;
            Assert.True(s.Place(0, new Cell(0, 1)).Success);    // kare sağ-üst
            Assert.True(s.Place(2, new Cell(2, 0)).Success);    // domino (2,0)-(2,1)
            // kalan boş: (0,0),(1,0),(2,2) → I3 hiçbir dönüşle sığmaz
            Assert.Equal(SessionState.Failed, s.State);
            Assert.Equal(SessionState.Failed, seen.Value, "olay tetiklendi");
            Assert.Equal(1, s.FailCount);
            Assert.False(s.Place(1, new Cell(0, 0)).Success, "kayıpta yerleşim yok");
            Assert.False(s.Rotate(1), "kayıpta döndürme yok");
        }

        [Test("FAIL-02")]
        public static void RotationIsConsideredBeforeFail()
        {
            // Yalnızca dikey I3 sığar; tezgâhtaki yatay I3 kayıp sayılmamalı.
            var l = Make(new[] { "K..", "K..", "K.." }, (S(0, 0, 0, 1, 0, 2), "K"));
            var s = new GameSession(l);
            Assert.Equal(SessionState.Playing, s.State);
            Assert.True(PlaceCells(s, 0, new[] { new Cell(0, 0), new Cell(1, 0), new Cell(2, 0) }).Success);
            Assert.Equal(SessionState.Won, s.State);
        }

        [Test("STAR-01")]
        public static void StarsByPattern()
        {
            // 5 hücre K, 4 hücre L. Hepsi K parçası → eşleşen 5/9 < stars.2 (6) → 1★
            var oneStar = Make(new[] { "KKK", "KKL", "LLL" },
                (S(0, 0, 0, 1, 0, 2), "K"), (S(0, 0, 0, 1, 0, 2), "K"), (S(0, 0, 0, 1, 0, 2), "K"));
            var s = new GameSession(oneStar);
            for (int r = 0; r < 3; r++) Assert.True(s.Place(r, new Cell(r, 0)).Success);
            Assert.Equal(SessionState.Won, s.State);
            Assert.Equal(1, s.Stars);

            // Satır 2 L → eşleşen 8/9 ≥ 6 ama <9 → 2★
            var twoStar = Make(new[] { "KKK", "KKL", "LLL" },
                (S(0, 0, 0, 1, 0, 2), "K"), (S(0, 0, 0, 1, 0, 2), "K"), (S(0, 0, 0, 1, 0, 2), "L"));
            s = new GameSession(twoStar);
            for (int r = 0; r < 3; r++) s.Place(r, new Cell(r, 0));
            Assert.Equal(2, s.Stars);
            Assert.Equal(0, new GameSession(twoStar).Stars, "bitmeden yıldız yok");
        }
    }

    public static class BoosterMechanicTests
    {
        [Test("BST-01")]
        public static void UndoRestoresBoardTrayAndQueue()
        {
            var l = Region1[0];
            var s = new GameSession(l);
            var step = l.Solution[0];
            int slot = SlotOf(s, step.QueueIndex);
            PlaceCells(s, slot, step.Cells);
            Assert.True(s.CanUndo);
            Assert.True(s.Undo());
            Assert.Equal(0, s.Board.FilledCount);
            Assert.Equal(step.QueueIndex, s.Tray[slot].QueueIndex, "parça eski yuvasında");
            Assert.Equal(5, s.RemainingInQueue, "kuyruk eski hali");
            Assert.Equal(0, s.PlacementCount);
        }

        [Test("BST-02")]
        public static void UndoNotAvailableAfterSwap()
        {
            var s = new GameSession(Region1[0]);
            PlaceCells(s, SlotOf(s, Region1[0].Solution[0].QueueIndex), Region1[0].Solution[0].Cells);
            Assert.True(s.Swap(0));
            Assert.False(s.CanUndo);
            Assert.False(s.Undo());
        }

        [Test("BST-03")]
        public static void SwapSendsToQueueEnd()
        {
            var s = new GameSession(Region1[0]); // tezgâh q0..q3, kuyruk q4..q8
            Assert.True(s.Swap(1));
            Assert.Equal(4, s.Tray[1].QueueIndex, "yuvaya sıradaki");
            Assert.Equal(5, s.RemainingInQueue, "sayı değişmez");
            // kuyruk q5..q8,q1 → q4,q5,q6,q7,q8 sırayla geçer, 5. değişimde q1 geri gelir
            for (int i = 0; i < 5; i++) s.Swap(1);
            Assert.Equal(1, s.Tray[1].QueueIndex);
        }

        [Test("BST-04")]
        public static void SwapDisabledWhenQueueEmpty()
        {
            var s = new GameSession(ThreeByThree()); // 3 parça, kuyruk boş
            Assert.False(s.CanSwap(0));
            Assert.False(s.Swap(0));
        }

        [Test("BST-05, FAIL-04")]
        public static void SingleStitchAfterContinue()
        {
            var s = new GameSession(ThreeByThree());
            s.Place(0, new Cell(0, 1));
            s.Place(2, new Cell(2, 0));
            Assert.Equal(SessionState.Failed, s.State);
            Assert.False(s.AddSingleStitch(), "kayıpta güçlendirici yok");
            Assert.True(s.ContinueAfterFail());
            Assert.True(s.AddSingleStitch());
            int bonus = GameSession.TraySlots;
            Assert.True(s.Tray[bonus].IsSingleStitch);
            Assert.True(s.Place(bonus, new Cell(2, 2)).Success);
            Assert.True(s.UsedSingleStitch);
            // Boş: (0,0),(1,0) → I3 yine sığmaz → ikinci kayıp (devam merdiveni 2. basamak)
            Assert.Equal(SessionState.Failed, s.State);
            Assert.Equal(2, s.FailCount);
        }

        [Test("BST-06, STAR-05")]
        public static void SingleStitchPreventsThreeStars()
        {
            var l = Make(new[] { "KKK", "...", "..." }, (S(0, 0, 0, 1), "K"));
            var s = new GameSession(l);
            Assert.True(s.AddSingleStitch());
            Assert.True(s.Place(GameSession.TraySlots, new Cell(0, 2)).Success);
            Assert.True(s.Place(0, new Cell(0, 0)).Success);
            Assert.Equal(SessionState.Won, s.State);
            Assert.Equal(2, s.Stars, "Tek İlmek hücresi eşleşmez");
        }

        [Test("BST-07")]
        public static void BonusSlotsAreLimited()
        {
            var s = new GameSession(Region1[0]);
            Assert.True(s.AddSingleStitch());
            Assert.True(s.AddSingleStitch());
            Assert.False(s.AddSingleStitch(), "en fazla 2 ek yuva");
        }
    }
}
