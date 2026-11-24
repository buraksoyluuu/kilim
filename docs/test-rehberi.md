# Kilim – Test rehberi (v0.3)

UAT listesi: https://claude.ai/artifact/Y4ZR74bJBTsPMHKu2u8dUR

## 1. Kurulum (ilk sefer, ~10 dk)
1. Unity Hub → **Add project from disk** → `Unity/Kilim` klasörü. Sürüm: Unity 6 LTS (6000.0.x).
2. **Edit › Project Settings › Player › Other Settings › Active Input Handling = Both** (UI olay sistemi eski giriş modülünü kullanıyor).
3. Herhangi bir sahneyi aç (varsayılan `SampleScene` yeterli) → **Play**. Oyun kendini kurar; sahneye bir şey eklemek gerekmez.
4. Game penceresinde çözünürlüğü dikey seç (ör. 1080×1920 veya iPhone 15).

Kaydı sıfırlamak için: Ayarlar › Verilerimi sil, ya da `Application.persistentDataPath/kilim_save.json` dosyasını sil.

## 2. Bu sürümde ne var
| Alan | Durum |
|---|---|
| Açılış → yaş kapısı → KVKK rızası → öğretici (Bölüm 1) | ✅ |
| Oyun: tahta, 4'lü tezgâh, sürükle-bırak, hayalet, mıknatıs, döndürme, desen çubuğu (2★/3★ işaretli), kalan parça | ✅ |
| Kazandın (yıldız, coin, kumbara, bayrak, sezon), coin ×2 | ✅ |
| Tezgâh doldu → devam merdiveni (900/1.900/2.900, reklamla 1 kez, paket fiyatı), Pes et, 3. kayıpta Geri Al hediyesi | ✅ |
| Güçlendiriciler (6/9/12'de açılır, 3 hediye, stok yoksa coin) | ✅ |
| Ana ekran, Ev restorasyonu (20 görev, stil seçimi), Mağaza, Kumbara, Sezon kartı, Günün Deseni (zen + takvim + sandık), Tezgâh Yarışı, Ayarlar | ✅ |
| Reklam kuralları (bölüm 12+, 2 bölüm/90 sn, kayıptan/ilk oturumdan/satın almadan 72 sa sonra yok, genç 3 bölüm) | ✅ |
| Gerçek reklam / IAP / UMP / ATT | ⚙️ Adaptörler hazır, define ile açılır (bkz. §4) |
| Sunucu: makbuz doğrulama, yarış eşleşmesi, sunucu saati, veri silme, bulut kayıt | ❌ Backend yok; yerel/simüle |
| Ses efektleri (13) + müzik döngüsü (48 sn) — koddan sentez, telifsiz | ✅ |
| Kilim görsel stili: desenli arka plan, kenar bandı, yuvarlak karolar, motif şekilleri, coin/ilmek/yıldız ikonları, Nine yer tutucu portresi, uygulama ikonu | ✅ (final illüstrasyon değil) |
| Türkçe + İngilizce (Ayarlar › Dil; ilk açılışta cihaz diline göre) | ✅ |
| Final illüstrasyonlar (Nine, ev sahneleri, parça dokuları), diğer 6 dil, animasyon cilası | ❌ İllüstratör / çeviri |

## 3. Ses, görsel ve dil
- Varlıklar `Tools/gen_assets.py` ile üretilir (`python Tools/gen_assets.py`); çıktı `Assets/Kilim/Resources/{audio,art}`. Final çizimler gelince **aynı dosya adlarıyla** değiştirmek yeterli, kod değişmez.
- Uygulama ikonu: `Assets/Kilim/Art/icon_1024.png` → Player Settings › Icon.
- Yeni metin eklerken İngilizcesini `Loc.cs › En.Table`'a ekle; eksik çeviri editörde Console'a `[Loc] Çeviri yok` uyarısı olarak düşer.

## 4. Test ipuçları
- **Reklam ve satın alma** editörde test penceresi açar: "Sonuna kadar izle / Erken kapat", "Satın al / Bekleyen ödeme / İptal". UAT ADS-12, IAP-03, IAP-11 bu pencerelerle test edilir.
- **Tam ekran reklam** ilk oturumda hiç çıkmaz (UAT ADS-04). Görmek için oyunu kapatıp yeniden aç, bölüm 12'yi geç.
- **Yarış** yalnızca Cuma 10:00 – Pazar 22:00 (yerel) açık. Rakipler sunucu gelene kadar yerel olarak üretilir.
- **Bölüm sayısı:** 20 bölüm var. 20'yi bitirince "Yeni desenler yakında" kartı görünür (UAT LVL-05).
- **Saat hilesi testleri** (DAILY-06, OFFER-02): `TrustedClock` sunucu zamanıyla senkronlanınca çalışır; backend gelene kadar cihaz saati kullanılır.

## 5. Gerçek SDK'ları açma
1. Paketleri kur: `com.unity.purchasing` (4.12+), Google Mobile Ads Unity Plugin (9.x), iOS için `com.unity.ads.ios-support`.
2. Player Settings › Scripting Define Symbols: `KILIM_UNITY_IAP;KILIM_ADMOB`.
3. `Integrations.cs` içindeki AdMob test birim ID'lerini kendi ID'lerinle değiştir; App Store / Play Console'da `Config.cs › Skus` ürünlerini aynı ID'lerle aç.
4. Üretimde makbuzu sunucuda doğrula, sonra `Meta.ApplyPurchase` çağır.

## 6. Otomatik testler
```bash
dotnet run --project Core.Tests                               # 56 test, UAT ID'leriyle
dotnet build Tools/UnityTypecheck/Kilim.UnityTypecheck.csproj  # Unity katmanı tip kontrolü
python Tools/validate_levels.py Data/levels/region1.json       # bölümler çözülebilir + 3★ mümkün
```
