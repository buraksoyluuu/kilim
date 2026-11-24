# Kilim – Sprint planı

Kaynak: [Tasarım dokümanı](https://claude.ai/artifact/HqmumwqWBua3CWCm8gCU2Z) · [UAT v1.1](https://claude.ai/artifact/Y4ZR74bJBTsPMHKu2u8dUR)

Kural: Sprint, kapsamındaki UAT testleri "Geçti" olmadan kapanmaz. Her PR başlığında UAT ID'leri yazar.

| Sprint | Kapsam | UAT modülleri | Durum |
|---|---|---|---|
| 0 | Repo, Core kütüphanesi (kurallar + ekonomi), test koşucusu, CI, bölüm doğrulayıcı, Unity paket iskeleti | LVL | ✅ |
| 1 | Unity: tahta/tezgâh görünümü, sürükle-bırak, hayalet, mıknatıs, döndürme animasyonu, Kazandın/Kaybettin ekranları, 20 bölüm | CORE, ROT, TRAY, STAR, WIN, FAIL, TUT | ✅ v0.2 |
| 2 | Cüzdan kaydı, güçlendirici UI ve öğreticiler, devam merdiveni, kumbara, Dekor | ECO, BST, PIG, META, SAVE | ✅ v0.2 (bulut kayıt hariç) |
| 3 | Yaş kapısı, KVKK/UMP/ATT, AdMob mediation, IAP (StoreKit 2 / Play Billing 7), sunucu doğrulama, iade | AGE, PRV, ADS, IAP, OFFER | 🟡 Akış + kurallar + adaptörler hazır; SDK hesapları ve sunucu doğrulama bekliyor |
| 4 | Günün Deseni, Tezgâh Yarışı, sezon/pass, remote config, analytics, performans, erişilebilirlik | DAILY, RACE, PASS, CFG, ANL, PERF, A11Y, L10N, INT, ETH | 🟡 İstemci tarafı hazır; yarış/saat/analitik backend'i, 8 dil, ses bekliyor |

## Sprint 0 notları
- **Spec düzeltmesi:** Spec 1 §2 parça boyunu "2–5 hücre" diyor; bölüm 11, 12 ve 15'te 2×3 dikdörtgen (6 hücre) var. Core kuralı **2–6** olarak alındı (`LevelData.MaxPieceCells`). İçerik ekibi 5'e indirmek isterse üretici `RECTS` listesinden 2×3 çıkarılmalı.
- **Geri Al:** yalnızca son eylem yerleştirme ise kullanılabilir; Değiştir ya da Tek İlmek'ten sonra pasif (geri alınan şeyin belirsiz olmaması için).
- **Tek İlmek:** tezgâhın 4 yuvasına değil, 2 adet ek yuvaya gelir (devam paketi +2 Tek İlmek verdiği için).
- **Devam sonrası:** oturum "oynanıyor"a döner; kayıp kontrolü bir sonraki yerleşimde tekrar çalışır.

## v0.2 notları
- Tüm ekranlar koddan kurulur (uGUI); sahne/prefab yok. Görsel varlıklar gelince `Ui.cs` / `Theme` üzerinden değiştirilir.
- Yaş: yalnızca doğum yılı sorulduğu için yaş muhafazakâr hesaplanır (bu yıl − doğum yılı − 1).
- Tek İlmek ve Değiştir, devam paketinden gelmişse kilitli olsa da kullanılabilir (aksi halde devam boşa giderdi).
- Sezon kartı ödül tablosu tasarımda 5 seviye örnekliydi; 30 seviye kural tabanlı üretildi (`SeasonPass.FreeReward/GoldReward`). Tasarım nihai tabloyu verirse değiştirilir.
- Ev restorasyonunda 5. alan "Bahçe" ve görev adları/notları eklendi (tasarımda 4 alan örneği vardı).

## v0.4 (24 Kasım 2026)
- 160 bölüm (8 bölge × 20): hepsi çözülebilir, 3★ mümkün, 3★ çözüm yolu kayıtlı; bölge içi testere dişi zorluk, her bölgede 2 usta bölüm. Üretici: `Tools/gen_regions.py`.
- Bölge 2'nin "çift motif" hücresi bu üretimde yok (çözücü desteklemiyor); Core destekliyor, içerik sonraki turda.
- Ev restorasyonu yalnızca Bölge 1 için (20 görev); Bölge 2+ evleri içerik ve illüstrasyon bekliyor.
- Backend (Firebase): `Backend/` — saat, config, bulut kayıt, makbuz doğrulama, iade bildirimleri, yarış, veri silme; 6 test.
- Unity istemcisi `Backend.cs`: `Resources/backend.json` yoksa çevrimdışı mod.
- Hukuki taslaklar `docs/legal/`, yayın kontrol listesi `docs/yayin-kontrol-listesi.md`, beta hattı `.github/workflows/beta.yml` + `fastlane/`.
