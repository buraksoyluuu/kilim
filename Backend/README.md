# Kilim backend (Firebase)

Tek HTTPS uç noktası `api` (Cloud Functions v2, europe-west1) + Firestore. Kimlik: Firebase Anonymous Auth (istemci REST ile alır).

| Uç | İş | UAT |
|---|---|---|
| `GET /time` | Sunucu saati (günlük/teklif/yarış hile koruması) | DAILY-06, OFFER-02, INT-04 |
| `GET /config` | Remote config (`config/live` belgesi; anahtarlar `GameConfig.Apply`) | CFG-01..04 |
| `GET/PUT /save` | Bulut kayıt; daha ileri bölüm kazanır | SAVE-01..05 |
| `POST /purchase` | Makbuz doğrulama (Apple App Store Server API / Google Play Developer API), işlem başına tek teslim | IAP-01..11 |
| `GET /entitlements`, `POST /entitlements/ack` | İade edilen işlemler → istemci geri alır | IAP-10 |
| `POST /race/join`, `POST /race/flag`, `GET /race`, `POST /race/claim` | Tezgâh Yarışı: 30'luk gruplar, bayrak kuralları, ödül | RACE-01..07 |
| `POST /delete` | KVKK/GDPR veri silme | PRV-07 |
| `appleNotifications`, `googleNotifications` | Mağaza iade bildirimleri | IAP-10 |

## Kurulum
1. Firebase projesi (Blaze planı: dış API çağrısı için gerekli) → Authentication › Anonymous açık, Firestore (europe-west) açık.
2. `npm i -g firebase-tools && firebase login && firebase use <proje-id>` (`.firebaserc`'deki `kilim-game`'u değiştir).
3. Gizli anahtarlar:
   - `firebase functions:secrets:set APPLE_PRIVATE_KEY` (App Store Connect › Users and Access › Integrations › In-App Purchase anahtarı, .p8 içeriği)
   - `firebase functions:secrets:set GOOGLE_SERVICE_ACCOUNT` (Play Console'a bağlı servis hesabı JSON'u)
   - Ortam: `APPLE_KEY_ID`, `APPLE_ISSUER_ID`, `APPLE_BUNDLE_ID`, `APPLE_APP_ID`, `APPLE_ENV` (Sandbox/Production), `ANDROID_PACKAGE` → `functions/.env`
   - Apple kök sertifikaları: `functions/certs/` içine `AppleRootCA-G3.cer` (apple.com/certificateauthority).
4. `cd functions && npm ci && npm test && cd .. && firebase deploy --only functions,firestore`
5. App Store Connect › App Information › Server Notifications V2 URL = `appleNotifications` adresi; Play Console › Monetization setup › Real-time notifications → Pub/Sub push = `googleNotifications` adresi.
6. Unity: `Assets/Kilim/Resources/backend.json` → `{ "baseUrl": "https://europe-west1-<proje>.cloudfunctions.net/api", "apiKey": "<Web API key>" }`. Dosya yoksa oyun çevrimdışı modda çalışır.

## Test
`cd functions && npm test` — iş kuralları bellek deposuyla (6 test).
