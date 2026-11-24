// Kilim backend: tek HTTPS uç noktası (Cloud Functions v2), Firebase Auth (anonim) kimliği zorunlu.
// İstemci: Unity/Kilim/Assets/Kilim/Scripts/Runtime/Backend.cs
import { onRequest, Request } from 'firebase-functions/v2/https';
import { defineSecret } from 'firebase-functions/params';
import * as admin from 'firebase-admin';
import type { Response } from 'express';
import { Doc, Store, Tx } from './store';
import * as L from './logic';
import { verifyApple, verifyGoogle } from './verify';

admin.initializeApp();
const db = admin.firestore();

class FirestoreStore implements Store {
  run<T>(fn: (tx: Tx) => Promise<T>): Promise<T> {
    return db.runTransaction(async (t) => {
      const sets: [string, Doc | null][] = [];
      const tx: Tx = {
        get: async (p) => { const s = await t.get(db.doc(p)); return s.exists ? (s.data() as Doc) : null; },
        set: (p, d) => sets.push([p, d]),
        del: (p) => sets.push([p, null]),
      };
      const r = await fn(tx);
      for (const [p, d] of sets) (d === null ? t.delete(db.doc(p)) : t.set(db.doc(p), d));
      return r;
    });
  }
  async get(p: string) { const s = await db.doc(p).get(); return s.exists ? (s.data() as Doc) : null; }
  async list(col: string) { const q = await db.collection(col).get(); return q.docs.map((d) => ({ id: d.id, data: d.data() })); }
  async deleteTree(p: string) { await db.recursiveDelete(db.doc(p)); }
}

const store = new FirestoreStore();
const APPLE_PRIVATE_KEY = defineSecret('APPLE_PRIVATE_KEY');
const GOOGLE_SERVICE_ACCOUNT = defineSecret('GOOGLE_SERVICE_ACCOUNT');

async function uidOf(req: Request): Promise<string | null> {
  const h = req.header('authorization') ?? '';
  if (!h.startsWith('Bearer ')) return null;
  try { return (await admin.auth().verifyIdToken(h.slice(7))).uid; } catch { return null; }
}

type Handler = (uid: string, body: any, req: Request) => Promise<any>;

const routes: Record<string, Handler> = {
  'GET /time': async () => ({ now: Date.now() }),

  'GET /config': async () => (await store.get('config/live')) ?? {},

  'GET /save': async (uid) => {
    const p = await store.get(`players/${uid}`);
    return { save: p?.save ?? null, maxLevel: p?.maxLevel ?? 0 };
  },
  'PUT /save': async (uid, b) => L.putSave(store, uid, String(b.save), Number(b.maxLevel), Date.now()),

  'POST /purchase': async (uid, b) => {
    const v = b.platform === 'ios' ? await verifyApple(String(b.transactionId)) : await verifyGoogle(String(b.sku), String(b.transactionId));
    if (!v.valid) return { valid: false, reason: v.reason };
    if (v.sku !== b.sku) return { valid: false, reason: 'sku_mismatch' };
    const r = await L.recordPurchase(store, uid, v.txId!, v.sku!, String(b.platform), Date.now());
    return { valid: true, granted: r.granted, txId: v.txId };
  },

  'GET /entitlements': async (uid) => {
    const refunds = await store.list(`players/${uid}/refunds`);
    return { refunds: refunds.filter((r) => !r.data.acked).map((r) => ({ txId: r.id, sku: r.data.sku })) };
  },
  'POST /entitlements/ack': async (uid, b) => {
    await store.run(async (tx) => {
      for (const id of (b.txIds ?? []) as string[]) {
        const p = `players/${uid}/refunds/${id}`;
        const d = await tx.get(p);
        if (d) tx.set(p, { ...d, acked: true });
      }
    });
    return { ok: true };
  },

  'POST /race/join': async (uid, b) => {
    const w = L.raceWindow(Date.now(), Number(b.tz ?? 0));
    if (!w.active) return { joined: false, reason: 'inactive' };
    return { joined: true, group: await L.joinRace(store, uid, String(b.nick).slice(0, 24), w.weekId), weekId: w.weekId };
  },
  'POST /race/flag': async (uid, b) => ({ result: await L.addFlag(store, uid, Number(b.level), Number(b.attempt), Date.now(), Number(b.tz ?? 0)) }),
  'GET /race': async (uid, _b, req) => {
    const w = L.raceWindow(Date.now(), Number(req.query.tz ?? 0));
    return { weekId: w.weekId, active: w.active, endMs: w.endMs, standings: await L.standings(store, uid, w.weekId) };
  },
  'POST /race/claim': async (uid, b) => L.claimRace(store, uid, String(b.weekId), Date.now(), Number(b.tz ?? 0)),

  'POST /delete': async (uid) => {
    await L.deletePlayer(store, uid, Date.now());
    await admin.auth().deleteUser(uid);
    return { ok: true };
  },
};

export const api = onRequest({ region: 'europe-west1', secrets: [APPLE_PRIVATE_KEY, GOOGLE_SERVICE_ACCOUNT], cors: false },
  async (req: Request, res: Response) => {
    const key = `${req.method} ${req.path.replace(/\/+$/, '') || '/'}`;
    const h = routes[key];
    if (!h) { res.status(404).json({ error: 'not_found' }); return; }
    const uid = await uidOf(req);
    if (!uid) { res.status(401).json({ error: 'unauthenticated' }); return; }
    try { res.json(await h(uid, req.body ?? {}, req)); }
    catch (e) { console.error(key, e); res.status(500).json({ error: 'internal' }); }
  });

/** Apple App Store Server Notifications v2 (REFUND / REVOKE). Gövde: { signedPayload }. */
export const appleNotifications = onRequest({ region: 'europe-west1', secrets: [APPLE_PRIVATE_KEY] }, async (req, res) => {
  try {
    const { SignedDataVerifier, Environment } = await import('@apple/app-store-server-library');
    const env = process.env.APPLE_ENV === 'Production' ? Environment.PRODUCTION : Environment.SANDBOX;
    const v = new SignedDataVerifier([], true, env, process.env.APPLE_BUNDLE_ID ?? 'com.example.kilim');
    const n = await v.verifyAndDecodeNotification(req.body.signedPayload);
    if ((n.notificationType === 'REFUND' || n.notificationType === 'REVOKE') && n.data?.signedTransactionInfo) {
      const tx = await v.verifyAndDecodeTransaction(n.data.signedTransactionInfo);
      await L.markRefunded(store, tx.originalTransactionId ?? tx.transactionId!, Date.now());
    }
    res.status(200).send('ok');
  } catch (e) { console.error(e); res.status(400).send('bad'); }
});

/** Google Play Gerçek Zamanlı Geliştirici Bildirimleri (Pub/Sub push) — iptal edilen (voided) satın almalar. */
export const googleNotifications = onRequest({ region: 'europe-west1' }, async (req, res) => {
  try {
    const msg = JSON.parse(Buffer.from(req.body.message.data, 'base64').toString());
    const voided = msg.voidedPurchaseNotification;
    if (voided?.orderId) await L.markRefunded(store, voided.orderId, Date.now());
    res.status(204).send();
  } catch (e) { console.error(e); res.status(400).send(); }
});
