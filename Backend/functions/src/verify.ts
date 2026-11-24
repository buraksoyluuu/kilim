// Mağaza makbuz doğrulama. Gizli anahtarlar Firebase Secret Manager'da (bkz. Backend/README.md).
import { AppStoreServerAPIClient, Environment, SignedDataVerifier } from '@apple/app-store-server-library';
import { google } from 'googleapis';
import * as fs from 'fs';
import * as path from 'path';

export interface Verified { valid: boolean; sku?: string; txId?: string; reason?: string }

const BUNDLE_ID = process.env.APPLE_BUNDLE_ID ?? 'app.kilim.game';
const PACKAGE_NAME = process.env.ANDROID_PACKAGE ?? 'app.kilim.game';

function appleEnv(): Environment {
  return process.env.APPLE_ENV === 'Production' ? Environment.PRODUCTION : Environment.SANDBOX;
}

/** Apple: işlem ID'si ile App Store Server API'den imzalı işlemi alır, Apple kök sertifikalarıyla doğrular. */
export async function verifyApple(transactionId: string): Promise<Verified> {
  const key = process.env.APPLE_PRIVATE_KEY;
  const keyId = process.env.APPLE_KEY_ID;
  const issuerId = process.env.APPLE_ISSUER_ID;
  if (!key || !keyId || !issuerId) return { valid: false, reason: 'apple_not_configured' };
  const client = new AppStoreServerAPIClient(key, keyId, issuerId, BUNDLE_ID, appleEnv());
  const info = await client.getTransactionInfo(transactionId);
  if (!info.signedTransactionInfo) return { valid: false, reason: 'not_found' };
  const certDir = path.join(__dirname, '..', 'certs');
  const roots = fs.existsSync(certDir) ? fs.readdirSync(certDir).filter((f) => f.endsWith('.cer')).map((f) => fs.readFileSync(path.join(certDir, f))) : [];
  const appAppleId = process.env.APPLE_APP_ID ? Number(process.env.APPLE_APP_ID) : undefined;
  const verifier = new SignedDataVerifier(roots, true, appleEnv(), BUNDLE_ID, appAppleId);
  const tx = await verifier.verifyAndDecodeTransaction(info.signedTransactionInfo);
  if (tx.revocationDate) return { valid: false, reason: 'revoked', txId: tx.transactionId, sku: tx.productId };
  return { valid: true, sku: tx.productId, txId: tx.originalTransactionId ?? tx.transactionId };
}

/** Google Play: satın alma jetonunu Android Publisher API ile doğrular (servis hesabı). */
export async function verifyGoogle(sku: string, purchaseToken: string): Promise<Verified> {
  const json = process.env.GOOGLE_SERVICE_ACCOUNT;
  if (!json) return { valid: false, reason: 'google_not_configured' };
  const auth = new google.auth.GoogleAuth({ credentials: JSON.parse(json), scopes: ['https://www.googleapis.com/auth/androidpublisher'] });
  const api = google.androidpublisher({ version: 'v3', auth });
  const r = await api.purchases.products.get({ packageName: PACKAGE_NAME, productId: sku, token: purchaseToken });
  if (r.data.purchaseState !== 0) return { valid: false, reason: `state_${r.data.purchaseState}` };   // 0 satın alındı, 1 iptal, 2 bekliyor
  return { valid: true, sku, txId: r.data.orderId ?? purchaseToken };
}
