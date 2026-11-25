# Kilim backend (Cloudflare Workers + D1, ücretsiz)

Kimlik: **Firebase Anonymous Auth** (ücretsiz Spark planı, proje `kilim-game`). İstemci REST ile anonim hesap alır, ID token'ı `Authorization: Bearer` ile gönderir; Worker token'ı Google'ın açık anahtarlarıyla doğrular.
Veri: **Cloudflare D1** (`docs` tablosu, yol → JSON). İş kuralları `worker/src/logic.ts` (testli).

| Uç | İş | UAT |
|---|---|---|
| `GET /time` | Sunucu saati (günlük/teklif/yarış hile koruması) | DAILY-06, OFFER-02, INT-04 |
| `GET /config` | Remote config (`config/live` belgesi; anahtarlar `GameConfig.Apply`) | CFG-01..04 |
| `GET/PUT /save` | Bulut kayıt; daha ileri bölüm kazanır | SAVE-01..05 |
| `POST /purchase` | Makbuz doğrulama (Apple App Store Server API / Google Play Developer API), işlem başına tek teslim | IAP-01..11 |
| `GET /entitlements`, `POST /entitlements/ack` | İade edilen işlemler → istemci geri alır | IAP-10 |
| `POST /race/join`, `POST /race/flag`, `GET /race`, `POST /race/claim` | Tezgâh Yarışı | RACE-01..07 |
| `POST /delete` | KVKK/GDPR veri silme | PRV-07 |
| `POST /notify/apple`, `POST /notify/google?token=…` | Mağaza iade bildirimleri (Apple/Google API'sinden yeniden doğrulanır) | IAP-10 |

## Kurulum
1. Cloudflare hesabı (ücretsiz) → API token: "Edit Cloudflare Workers" şablonu + D1 Edit izni.
2. `cd worker && npm ci`
3. `CLOUDFLARE_API_TOKEN=… npx wrangler d1 create kilim` → çıktıdaki `database_id`'yi `wrangler.toml`'a yaz.
4. `npx wrangler d1 execute kilim --remote --file=schema.sql`
5. `npx wrangler deploy` → adres `https://kilim-api.<hesap>.workers.dev`
6. Gizli değerler (mağaza hesapları hazır olunca): `npx wrangler secret put APPLE_PRIVATE_KEY` (ve `APPLE_KEY_ID`, `APPLE_ISSUER_ID`, `GOOGLE_SERVICE_ACCOUNT`, `RTDN_TOKEN`).
7. App Store Connect › App Information › App Store Server Notifications V2 → `…/notify/apple`; Play Console › Monetization setup › Real-time developer notifications → Pub/Sub push `…/notify/google?token=<RTDN_TOKEN>`.
8. Unity `Assets/Kilim/Resources/backend.json` → `baseUrl` = Worker adresi.

## Test
`cd worker && npm test` (iş kuralları + JWT doğrulama, 8 test) · `npx wrangler dev --local` ile yerel deneme.

## Sınırlar
D1'de etkileşimli işlem yok: aynı belgeye eşzamanlı yazmada son yazan kazanır (oyuncu başına belge olduğu için risk düşük). Ücretsiz plan: günde 100.000 istek, D1 5 GB.
