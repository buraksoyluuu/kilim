import { privacyPage } from './privacy';
// Kilim backend: Cloudflare Worker + D1. Kimlik: Firebase Anonymous Auth ID token (ücretsiz Spark planı).
// İstemci: Unity/Kilim/Assets/Kilim/Scripts/Runtime/Backend.cs — uçlar Backend/README.md'de.
import { D1Store } from './d1store';
import { decodePayload, verifyFirebaseToken } from './jwt';
import * as L from './logic';
import { Store } from './store';
import { Env, appleTransaction, verifyApple, verifyGoogle } from './stores';

type Handler = (c: { uid: string; body: any; url: URL; store: Store; env: Env }) => Promise<unknown>;

const routes: Record<string, Handler> = {
  'GET /time': async () => ({ now: Date.now() }),
  'GET /config': async ({ store }) => (await store.get('config/live')) ?? {},

  'GET /save': async ({ uid, store }) => {
    const p = await store.get(`players/${uid}`);
    return { save: p?.save ?? null, maxLevel: p?.maxLevel ?? 0 };
  },
  'PUT /save': async ({ uid, body, store }) => L.putSave(store, uid, String(body.save), Number(body.maxLevel), Date.now()),

  'POST /purchase': async ({ uid, body, store, env }) => {
    const v = body.platform === 'ios' ? await verifyApple(env, String(body.transactionId))
                                      : await verifyGoogle(env, String(body.sku), String(body.transactionId));
    if (!v.valid) return { valid: false, reason: v.reason };
    if (v.sku !== body.sku) return { valid: false, reason: 'sku_mismatch' };
    const r = await L.recordPurchase(store, uid, v.txId!, v.sku!, String(body.platform), Date.now());
    return { valid: true, granted: r.granted, txId: v.txId };
  },

  'GET /entitlements': async ({ uid, store }) => {
    const refunds = await store.list(`players/${uid}/refunds`);
    return { refunds: refunds.filter((r) => !r.data.acked).map((r) => ({ txId: r.id, sku: r.data.sku })) };
  },
  'POST /entitlements/ack': async ({ uid, body, store }) => {
    await store.run(async (tx) => {
      for (const id of (body.txIds ?? []) as string[]) {
        const p = `players/${uid}/refunds/${id}`;
        const d = await tx.get(p);
        if (d) tx.set(p, { ...d, acked: true });
      }
    });
    return { ok: true };
  },

  'POST /race/join': async ({ uid, body, store }) => {
    const w = L.raceWindow(Date.now(), Number(body.tz ?? 0));
    if (!w.active) return { joined: false, reason: 'inactive' };
    return { joined: true, group: await L.joinRace(store, uid, String(body.nick).slice(0, 24), w.weekId), weekId: w.weekId };
  },
  'POST /race/flag': async ({ uid, body, store }) =>
    ({ result: await L.addFlag(store, uid, Number(body.level), Number(body.attempt), Date.now(), Number(body.tz ?? 0)) }),
  'GET /race': async ({ uid, url, store }) => {
    const w = L.raceWindow(Date.now(), Number(url.searchParams.get('tz') ?? 0));
    return { weekId: w.weekId, active: w.active, endMs: w.endMs, standings: await L.standings(store, uid, w.weekId) };
  },
  'POST /race/claim': async ({ uid, body, store }) => L.claimRace(store, uid, String(body.weekId), Date.now(), Number(body.tz ?? 0)),

  'POST /delete': async ({ uid, store }) => { await L.deletePlayer(store, uid, Date.now()); return { ok: true }; },
};

const json = (data: unknown, status = 200) =>
  new Response(JSON.stringify(data), { status, headers: { 'content-type': 'application/json' } });

export default {
  async fetch(req: Request, env: Env): Promise<Response> {
    const url = new URL(req.url);
    const path = url.pathname.replace(/\/+$/, '') || '/';
    const store = new D1Store(env.DB);
    try {
      if (req.method === 'GET' && (path === '/privacy' || path === '/gizlilik'))
        return privacyPage(url.searchParams.get('lang') ?? (path === '/privacy' && !(req.headers.get('accept-language') ?? '').startsWith('tr') ? 'en' : 'tr'));
      // Mağaza bildirimleri (kimliksiz; içerik Apple/Google API'sinden yeniden doğrulanır)
      if (req.method === 'POST' && path === '/notify/apple') return json(await appleNotification(env, store, await req.json()));
      if (req.method === 'POST' && path === '/notify/google') {
        if (!env.RTDN_TOKEN || url.searchParams.get('token') !== env.RTDN_TOKEN) return json({ error: 'forbidden' }, 403);
        return json(await googleNotification(store, await req.json()));
      }

      const h = routes[`${req.method} ${path}`];
      if (!h) return json({ error: 'not_found' }, 404);
      const auth = req.headers.get('authorization') ?? '';
      const uid = auth.startsWith('Bearer ') ? await verifyFirebaseToken(auth.slice(7), env.FIREBASE_PROJECT_ID).catch(() => null) : null;
      if (!uid) return json({ error: 'unauthenticated' }, 401);
      const body = req.method === 'GET' ? {} : await req.json().catch(() => ({}));
      return json(await h({ uid, body, url, store, env }));
    } catch (e) {
      console.error(path, e);
      return json({ error: 'internal' }, 500);
    }
  },
};

/** App Store Server Notifications v2: REFUND/REVOKE → işlem Apple API'sinden yeniden çekilir, iptal tarihi varsa iade işlenir. */
async function appleNotification(env: Env, store: Store, body: any) {
  const n = decodePayload(String(body.signedPayload ?? ''));
  if (n.notificationType !== 'REFUND' && n.notificationType !== 'REVOKE') return { ignored: n.notificationType };
  const claimed = decodePayload(String(n.data?.signedTransactionInfo ?? ''));
  const tx = await appleTransaction(env, String(claimed.transactionId));
  if (!tx?.revocationDate) return { ignored: 'not_revoked' };
  return { refunded: await L.markRefunded(store, tx.originalTransactionId ?? tx.transactionId, Date.now()) };
}

/** Google Play RTDN (Pub/Sub push): iptal edilen satın alma. */
async function googleNotification(store: Store, body: any) {
  const msg = JSON.parse(atob(String(body?.message?.data ?? '')));
  const orderId = msg?.voidedPurchaseNotification?.orderId;
  if (!orderId) return { ignored: true };
  return { refunded: await L.markRefunded(store, orderId, Date.now()) };
}
