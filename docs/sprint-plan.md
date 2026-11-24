# Kilim – Sprint planı

Kaynak: [Tasarım dokümanı](https://claude.ai/artifact/HqmumwqWBua3CWCm8gCU2Z) · [UAT v1.1](https://claude.ai/artifact/Y4ZR74bJBTsPMHKu2u8dUR)

Kural: Sprint, kapsamındaki UAT testleri "Geçti" olmadan kapanmaz. Her PR başlığında UAT ID'leri yazar.

| Sprint | Kapsam | UAT modülleri | Durum |
|---|---|---|---|
| 0 | Repo, Core kütüphanesi (kurallar + ekonomi), test koşucusu, CI, bölüm doğrulayıcı, Unity paket iskeleti | LVL | ✅ Core hazır (36 test geçiyor) |
| 1 | Unity: tahta/tezgâh görünümü, sürükle-bırak, hayalet, mıknatıs, döndürme animasyonu, Kazandın/Kaybettin ekranları, 20 bölüm | CORE, ROT, TRAY, STAR, WIN, FAIL, TUT | Sırada |
| 2 | Cüzdan kaydı, güçlendirici UI ve öğreticiler, devam merdiveni, kumbara, Dekor | ECO, BST, PIG, META, SAVE | |
| 3 | Yaş kapısı, KVKK/UMP/ATT, AdMob mediation, IAP (StoreKit 2 / Play Billing 7), sunucu doğrulama, iade | AGE, PRV, ADS, IAP, OFFER | |
| 4 | Günün Deseni, Tezgâh Yarışı, sezon/pass, remote config, analytics, performans, erişilebilirlik | DAILY, RACE, PASS, CFG, ANL, PERF, A11Y, L10N, INT, ETH | |

## Sprint 0 notları
- **Spec düzeltmesi:** Spec 1 §2 parça boyunu "2–5 hücre" diyor; bölüm 11, 12 ve 15'te 2×3 dikdörtgen (6 hücre) var. Core kuralı **2–6** olarak alındı (`LevelData.MaxPieceCells`). İçerik ekibi 5'e indirmek isterse üretici `RECTS` listesinden 2×3 çıkarılmalı.
- **Geri Al:** yalnızca son eylem yerleştirme ise kullanılabilir; Değiştir ya da Tek İlmek'ten sonra pasif (geri alınan şeyin belirsiz olmaması için).
- **Tek İlmek:** tezgâhın 4 yuvasına değil, 2 adet ek yuvaya gelir (devam paketi +2 Tek İlmek verdiği için).
- **Devam sonrası:** oturum "oynanıyor"a döner; kayıp kontrolü bir sonraki yerleşimde tekrar çalışır.
