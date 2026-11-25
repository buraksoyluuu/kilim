// Mağaza doğrulama: Apple App Store Server API ve Google Play Developer API (WebCrypto ile servis JWT'si).
// Apple yanıtı Apple'ın kendi API'sinden (kimlikli, TLS) geldiği için imzalı işlem gövdesi doğrudan okunur.
import { decodePayload, signJwt } from './jwt';

export interface Env {
  DB: D1Database;
  FIREBASE_PROJECT_ID: string;
  APPLE_BUNDLE_ID: string;
  ANDROID_PACKAGE: string;
  APPLE_ENV: string;
  APPLE_PRIVATE_KEY?: string;
  APPLE_KEY_ID?: string;
  APPLE_ISSUER_ID?: string;
  GOOGLE_SERVICE_ACCOUNT?: string;
  RTDN_TOKEN?: string;
}

export interface Verified { valid: boolean; sku?: string; txId?: string; reason?: string }

export async function appleTransaction(env: Env, transactionId: string): Promise<any | null> {
  if (!env.APPLE_PRIVATE_KEY || !env.APPLE_KEY_ID || !env.APPLE_ISSUER_ID) return null;
  const iat = Math.floor(Date.now() / 1000);
  const jwt = await signJwt({ alg: 'ES256', kid: env.APPLE_KEY_ID, typ: 'JWT' },
    { iss: env.APPLE_ISSUER_ID, iat, exp: iat + 1200, aud: 'appstoreconnect-v1', bid: env.APPLE_BUNDLE_ID }, env.APPLE_PRIVATE_KEY);
  const host = env.APPLE_ENV === 'Production' ? 'api.storekit.itunes.apple.com' : 'api.storekit-sandbox.itunes.apple.com';
  const r = await fetch(`https://${host}/inApps/v1/transactions/${encodeURIComponent(transactionId)}`, { headers: { Authorization: `Bearer ${jwt}` } });
  if (!r.ok) return null;
  const body = (await r.json()) as { signedTransactionInfo?: string };
  return body.signedTransactionInfo ? decodePayload(body.signedTransactionInfo) : null;
}

export async function verifyApple(env: Env, transactionId: string): Promise<Verified> {
  if (!env.APPLE_PRIVATE_KEY) return { valid: false, reason: 'apple_not_configured' };
  const tx = await appleTransaction(env, transactionId);
  if (!tx) return { valid: false, reason: 'not_found' };
  if (tx.bundleId !== env.APPLE_BUNDLE_ID) return { valid: false, reason: 'bundle_mismatch' };
  if (tx.revocationDate) return { valid: false, reason: 'revoked' };
  return { valid: true, sku: tx.productId, txId: tx.originalTransactionId ?? tx.transactionId };
}

let googleToken: { token: string; exp: number } | null = null;

async function googleAccessToken(env: Env): Promise<string | null> {
  if (!env.GOOGLE_SERVICE_ACCOUNT) return null;
  if (googleToken && googleToken.exp > Date.now()) return googleToken.token;
  const sa = JSON.parse(env.GOOGLE_SERVICE_ACCOUNT) as { client_email: string; private_key: string; token_uri?: string };
  const iat = Math.floor(Date.now() / 1000);
  const aud = sa.token_uri ?? 'https://oauth2.googleapis.com/token';
  const assertion = await signJwt({ alg: 'RS256', typ: 'JWT' },
    { iss: sa.client_email, scope: 'https://www.googleapis.com/auth/androidpublisher', aud, iat, exp: iat + 3600 }, sa.private_key);
  const r = await fetch(aud, {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: `grant_type=${encodeURIComponent('urn:ietf:params:oauth:grant-type:jwt-bearer')}&assertion=${assertion}`,
  });
  if (!r.ok) return null;
  const j = (await r.json()) as { access_token: string; expires_in: number };
  googleToken = { token: j.access_token, exp: Date.now() + (j.expires_in - 120) * 1000 };
  return googleToken.token;
}

export async function verifyGoogle(env: Env, sku: string, purchaseToken: string): Promise<Verified> {
  const token = await googleAccessToken(env);
  if (!token) return { valid: false, reason: 'google_not_configured' };
  const url = `https://androidpublisher.googleapis.com/androidpublisher/v3/applications/${env.ANDROID_PACKAGE}/purchases/products/${encodeURIComponent(sku)}/tokens/${encodeURIComponent(purchaseToken)}`;
  const r = await fetch(url, { headers: { Authorization: `Bearer ${token}` } });
  if (!r.ok) return { valid: false, reason: `http_${r.status}` };
  const p = (await r.json()) as { purchaseState?: number; orderId?: string };
  if (p.purchaseState !== 0) return { valid: false, reason: `state_${p.purchaseState}` };   // 0 satın alındı, 1 iptal, 2 bekliyor
  return { valid: true, sku, txId: p.orderId ?? purchaseToken };
}
