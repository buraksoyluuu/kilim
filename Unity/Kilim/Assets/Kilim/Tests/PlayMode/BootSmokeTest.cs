using System.Collections;
using Kilim.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Kilim.Tests
{
    /// <summary>Uygulama boş sahnede açılır, ekranlar görünür olur (geçiş animasyonu biter) ve hata loglanmaz.</summary>
    public class BootSmokeTest
    {
        [UnityTest]
        public IEnumerator BootsAndScreensBecomeVisible()
        {
            for (int i = 0; i < 50 && App.I == null; i++) yield return null;
            Assert.IsNotNull(App.I, "App kurulmadı");
            yield return new WaitForSecondsRealtime(3.5f);
            var cur = App.I.Nav.Current;
            Assert.IsNotNull(cur, "aktif ekran yok");
            Debug.Log($"[Smoke] ekran: {cur.GetType().Name}, frame: {Time.frameCount}, t: {Time.unscaledTime}");
            var cg = cur.Root.GetComponent<CanvasGroup>();
            Debug.Log($"[Smoke] alpha: {(cg != null ? cg.alpha : -1)}, scale: {cur.Root.localScale}, children: {cur.Root.childCount}");
            if (cg != null) Assert.Greater(cg.alpha, 0.99f, "ekran görünmez kaldı (geçiş animasyonu ilerlemedi)");
        }
    }
}
