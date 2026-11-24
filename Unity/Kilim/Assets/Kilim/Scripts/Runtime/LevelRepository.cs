using System.Collections.Generic;
using System.Threading.Tasks;
using Kilim.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Kilim.Game
{
    /// <summary>
    /// Bölüm dosyalarını Addressables'tan yükler (anahtar: "levels/region{n}").
    /// Spec 1 §6: her bölge bir JSON; remote config "level_overrides" Sprint 4'te eklenecek.
    /// </summary>
    public sealed class LevelRepository
    {
        public const int LevelsPerRegion = 20;
        private readonly Dictionary<int, List<LevelData>> _regions = new Dictionary<int, List<LevelData>>();

        public static int RegionOf(int levelId) => (levelId - 1) / LevelsPerRegion + 1;

        public async Task<LevelData> GetAsync(int levelId)
        {
            int region = RegionOf(levelId);
            if (!_regions.TryGetValue(region, out var list))
            {
                var handle = Addressables.LoadAssetAsync<TextAsset>($"levels/region{region}");
                var asset = await handle.Task;
                if (asset == null)
                {
                    Debug.LogError($"[Kilim] levels/region{region} yüklenemedi");
                    return null;
                }
                list = LevelParser.ParseMany(asset.text);
                Addressables.Release(handle);
                _regions[region] = list;
            }
            return list.Find(l => l.Id == levelId);
        }
    }
}
