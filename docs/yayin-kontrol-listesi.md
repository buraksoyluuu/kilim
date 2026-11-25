# Kilim – Yayın kontrol listesi (adım 3–5)

Sahip: **[S]** = Burak (hesap/onay), **[G]** = geliştirici/Claude. Sıra önemli; ✅ = repoda hazır.

## A. Hesaplar ve kimlikler [S]
- [x] **Paket kimliği:** `app.kilim.game` (iOS ve Android aynı; FemmeCare'deki `app.<ürün>.<alt>` düzeni). Unity › Player Settings › Identification.
- [x] **Apple Developer Program** — bireysel hesap aktif (Team ID 3UTU4G8NGA, yenileme 28 Eylül 2027).
- [x] Apple App ID `app.kilim.game` kaydedildi; App Store Connect kaydı **Kilim: Pattern Puzzle** (Apple ID 6838868701, SKU `kilim-ios`, ana dil English (U.S.)).
- [ ] App Store Connect › Business: yasal kimlik bilgisini güncelle → **Paid Apps Agreement** kabul → banka hesabı → vergi formları (W-8BEN) [S]. Bu olmadan satın alma ürünleri açılamaz.
- [ ] AB Dijital Hizmetler Yasası (DSA) satıcı (trader) beyanı [S] — "trader" seçilirse adres/telefon AB mağazasında herkese açık görünür.
- [ ] **Google Play Console** (tek seferlik 25 $; yeni kişisel hesaplarda yayından önce 12 test kullanıcısıyla 14 gün kapalı test zorunluluğu var — şirket hesabı önerilir).
- [x] **Firebase projesi** `kilim-game` (proje no 114466984555): Anonymous Auth açık, Firestore `europe-west1` üretim modu, web istemci anahtarı `Resources/backend.json`'da.
- [ ] Firebase **Blaze** planına geçiş (kart bilgisi) [S] → ardından Cloud Functions deploy [G].
- [ ] **AdMob hesabı** → iOS ve Android uygulama + 2 reklam birimi (ödüllü, tam ekran). Mediation: ilk etapta AdMob + 2–3 ağ (ör. AppLovin, Unity Ads, Meta).
- [ ] Ödeme/vergi profilleri: App Store Connect › Agreements, Tax, Banking; Play Console › Payments profile.

## B. Mağaza ürünleri [S] (ID'ler kodla birebir aynı olmalı — `Core/Runtime/Config.cs › Skus`)
| Ürün ID | Tür (Apple / Google) | Fiyat (USD taban) |
|---|---|---|
| `kilim.coins.1000` | Consumable / Managed (tüketilebilir) | 0,99 |
| `kilim.coins.2500` | Consumable | 2,49 |
| `kilim.coins.6000` | Consumable | 4,99 |
| `kilim.coins.14000` | Consumable | 9,99 |
| `kilim.coins.32000` | Consumable | 19,99 |
| `kilim.piggy` | Consumable | 2,99 |
| `kilim.goldpass` | Consumable (sezonluk) | 4,99 |
| `kilim.noads` | Non-Consumable / Managed (kalıcı) | 4,99 |
| `kilim.starter` | Non-Consumable | 1,99 |

## C. SDK'ları açma [G] (A ve B bitince)
- [ ] Paketler: `com.unity.purchasing` 4.12+, Google Mobile Ads Unity 9.x, `com.unity.ads.ios-support`.
- [ ] `Integrations.cs` → AdMob test ID'lerini gerçek ID'lerle değiştir; `GoogleMobileAdsSettings` içine App ID'ler.
- [ ] Scripting Define Symbols: `KILIM_UNITY_IAP;KILIM_ADMOB`.
- [ ] `Resources/backend.json` (baseUrl + Web API key). ✅ İstemci kodu hazır (`Backend.cs`).
- [ ] iOS `Info.plist`: `NSUserTrackingUsageDescription` = "Sana daha alakalı reklamlar göstermek için." / EN karşılığı; `SKAdNetworkItems` (AdMob + mediation listeleri).
- [ ] Android: `AD_ID` izni (yalnızca yetişkin akışı kullanır; Play formunda beyan et).

## D. Hukuki [S + avukat]
- [ ] `docs/legal/gizlilik-politikasi.md` ve `kullanim-kosullari.md` taslakları ✅ → avukat incelemesi → herkese açık URL'de yayın (şirket sitesi ya da Notion/Pages).
- [ ] Oyundaki "Aydınlatma metni" ve "Kullanım koşulları · Gizlilik" bağlantılarını bu URL'lerle güncelle [G].
- [ ] VERBİS kaydı gerekip gerekmediğini avukatla netleştir.
- [ ] Firestore TTL politikası: `races/*` 90 gün (politikada yazan süre) [G].

## E. Mağaza formları [S] — önerilen cevaplar
**Apple yaş derecelendirmesi:** tüm içerik kategorileri "Yok"; "Kontrolsüz web erişimi" Hayır; "Kumar/ganimet kutusu" Hayır → beklenen 4+ ama uygulama 13+ hedefliyor: **App Store › Age Rating'de "Made for Kids" seçme**; pazarlama 13+.
**Google IARC:** şiddet/cinsellik/kumar yok; kullanıcı etkileşimi: hayır (serbest metin yok); dijital satın alma: evet → beklenen PEGI 3 / Everyone. **Target audience: 13–15, 16–17, 18+** (13 altını seçme — Families politikası devreye girer).
**Apple App Privacy (Nutrition label):**
| Veri | Toplanıyor | Kimliğe bağlı | İzleme |
|---|---|---|---|
| Purchase history | Evet (App Functionality) | Hayır (anonim ID) | Hayır |
| Product interaction (analitik) | Evet (Analytics, rızalı) | Hayır | Hayır |
| Device ID (IDFA) | Evet (Third-Party Advertising) | Hayır | **Evet** (yalnızca ATT izni verilirse) |
| Crash/performance | Evet | Hayır | Hayır |
**Google Data safety:** Uygulama etkinliği (etkileşim), Uygulama bilgileri ve performans, Cihaz veya diğer kimlikler (reklam ID) → toplanıyor; aktarım: reklam ağları; şifreleme: evet; silme talebi: evet (uygulama içi). Hesap oluşturma yok.

## F. Görseller [S → illüstratör]
- [ ] Final illüstrasyonlar: Nine (portre + 3 duygu), 5 ev alanı × önce/sonra, parça dokuları; `Resources/art` dosya adlarıyla.
- [ ] Mağaza: ikon ✅ (`Assets/Kilim/Art/icon_1024.png`), 6.7" ve 5.5" iPhone + iPad + Android telefon ekran görüntüleri (TR/EN), 30 sn tanıtım videosu (isteğe bağlı).
- [ ] Mağaza metinleri TR/EN: kısa açıklama, uzun açıklama, anahtar kelimeler [G taslak yazabilir].

## G. Beta (adım 5)
- [ ] Unity'de projeyi bir kez aç → `ProjectSettings/` oluşur → **commit et** (CI build için şart) [G/S].
- [ ] GitHub › Settings › Secrets: `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`; Android: `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASS`, `ANDROID_KEYALIAS_NAME`, `ANDROID_KEYALIAS_PASS`, `PLAY_SERVICE_ACCOUNT_JSON`; iOS: `APPSTORE_KEY_ID`, `APPSTORE_ISSUER_ID`, `APPSTORE_P8`, `MATCH_PASSWORD` + sertifika deposu [S].
- [ ] `.github/workflows/beta.yml` ✅ → Actions › "Beta build" › Run → Android AAB Play **internal testing**'e, iOS TestFlight'a.
- [ ] Kapalı test: 20–50 kişi, 2 hafta. Hedefler: D1 ≥ %40, D7 ≥ %15, çökme dışı oturum ≥ %99,5, bölüm 1–20 ilk deneme kazanma eğrisi Spec 3'e yakın.
- [ ] Bulgular → UAT tablosunda Kaldı/Bloklu → düzeltme → yeni build.

## H. Soft launch (adım 6, sonrası)
Önerilen pazar: Kanada veya Avustralya + Filipinler (ucuz CPI ile retention ölçümü). 4 hafta; LTV180 ≥ 1,2×CPI olmadan ücretli kullanıcı edinimi (UA) yok.
