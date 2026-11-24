# Kilim

Mobil bulmaca oyunu (15–40 yaş). Unity 6 LTS · URP 2D · Addressables.

## Kaynaklar
- Tasarım dokümanı: https://claude.ai/artifact/HqmumwqWBua3CWCm8gCU2Z
- Kullanıcı kabul testleri (UAT v1.1): https://claude.ai/artifact/Y4ZR74bJBTsPMHKu2u8dUR

## Yapı
```
Core/              Unity'den bağımsız oyun mantığı (.NET Standard 2.1)
Core.Tests/        Core birim testleri (xUnit)
Unity/Kilim/       Unity 6 projesi (Core'u Packages/ üzerinden kullanır)
Data/levels/       Bölüm JSON'ları
Tools/             Bölüm üretici / çözücü
docs/              Sprint planı, mimari notlar
```

## Çalıştırma
```bash
dotnet run --project Core.Tests                       # Core birim testleri (UAT ID'leriyle)
python Tools/validate_levels.py Data/levels/region1.json  # bölümler çözülebilir + 3★ mümkün
```
Unity: `Unity/Kilim` klasörünü Unity 6 LTS (6000.0.x) ile aç, Active Input Handling = Both yap, herhangi bir sahnede Play'e bas. Ayrıntı: [docs/test-rehberi.md](docs/test-rehberi.md).

## Kural
- Oyun kuralları, ekonomi ve zaman mantığı **yalnızca Core'da**; Unity katmanı sadece görüntü, girdi ve SDK.
- Her PR ilgili UAT ID'lerini başlıkta taşır (ör. `CORE-01, ROT-02`).
- Bir sprint, ilgili UAT testleri "Geçti" olmadan kapanmaz.
