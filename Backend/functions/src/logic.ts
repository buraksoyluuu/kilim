// Saf iş kuralları (Spec 2 §3–§4). Firebase'den bağımsız; birim testli.
import { Store } from './store';

export const GROUP_SIZE = 30;
export const PRIZES = [1500, 800, 400];
export const MIN_FLAG_GAP_MS = 15_000;

/** Yerel saate göre yarış penceresi: Cuma 10:00 – Pazar 22:00. tzOffsetMin = yerel − UTC (İstanbul +180). */
export function raceWindow(nowMs: number, tzOffsetMin: number) {
  const local = new Date(nowMs + tzOffsetMin * 60_000);
  const dow = local.getUTCDay();                       // 0 Pazar … 5 Cuma
  const sinceFri = (dow - 5 + 7) % 7;
  const start = Date.UTC(local.getUTCFullYear(), local.getUTCMonth(), local.getUTCDate() - sinceFri, 10);
  let s = start;
  if (s > local.getTime()) s -= 7 * 86_400_000;
  const e = s + (2 * 24 + 12) * 3_600_000;
  const weekId = new Date(s).toISOString().slice(0, 10);
  const toUtc = (x: number) => x - tzOffsetMin * 60_000;
  return { weekId, startMs: toUtc(s), endMs: toUtc(e), active: local.getTime() >= s && local.getTime() < e };
}

export interface RaceMember { uid: string; nick: string; flags: number; reachedMs: number; lastLevel: number }

export function rank(members: RaceMember[]) {
  return [...members].sort((a, b) => b.flags - a.flags || a.reachedMs - b.reachedMs);
}

/** Oyuncuyu haftanın açık grubuna yerleştirir (dolunca yeni grup). Zaten varsa grubunu döner. */
export async function joinRace(store: Store, uid: string, nick: string, weekId: string): Promise<string> {
  return store.run(async (tx) => {
    const me = await tx.get(`races/${weekId}/players/${uid}`);
    if (me) return me.group as string;
    const meta = (await tx.get(`races/${weekId}`)) ?? { open: 0, openCount: 0 };
    let gid = meta.open as number;
    let count = meta.openCount as number;
    if (count >= GROUP_SIZE) { gid += 1; count = 0; }
    const gpath = `races/${weekId}/groups/g${gid}`;
    const group = (await tx.get(gpath)) ?? { members: {} };
    group.members[uid] = { uid, nick, flags: 0, reachedMs: 0, lastLevel: 0 } as RaceMember;
    tx.set(gpath, group);
    tx.set(`races/${weekId}`, { open: gid, openCount: count + 1 });
    tx.set(`races/${weekId}/players/${uid}`, { group: `g${gid}` });
    return `g${gid}`;
  });
}

export type FlagResult = 'ok' | 'inactive' | 'not_joined' | 'not_first_try' | 'duplicate_level' | 'too_fast';

/** İlk denemede kazanılan bölüm = 1 bayrak. Aynı bölüm iki kez sayılmaz, bölümler artan sırada, 15 sn alt sınır. */
export async function addFlag(store: Store, uid: string, levelId: number, attempt: number, nowMs: number, tzOffsetMin: number): Promise<FlagResult> {
  const w = raceWindow(nowMs, tzOffsetMin);
  if (!w.active) return 'inactive';
  if (attempt !== 1) return 'not_first_try';
  return store.run(async (tx) => {
    const me = await tx.get(`races/${w.weekId}/players/${uid}`);
    if (!me) return 'not_joined';
    const gpath = `races/${w.weekId}/groups/${me.group}`;
    const g = await tx.get(gpath);
    const m: RaceMember = g!.members[uid];
    if (levelId <= m.lastLevel) return 'duplicate_level';
    if (m.reachedMs && nowMs - m.reachedMs < MIN_FLAG_GAP_MS) return 'too_fast';
    m.flags += 1;
    m.reachedMs = nowMs;
    m.lastLevel = levelId;
    tx.set(gpath, g!);
    return 'ok';
  });
}

export async function standings(store: Store, uid: string, weekId: string) {
  const me = await store.get(`races/${weekId}/players/${uid}`);
  if (!me) return null;
  const g = await store.get(`races/${weekId}/groups/${me.group}`);
  const list = rank(Object.values(g!.members) as RaceMember[]);
  return list.map((m, i) => ({ rank: i + 1, nick: m.nick, flags: m.flags, me: m.uid === uid }));
}

/** Yarış bitince ödül: sıra 1–3 ve bayrak > 0 (UAT RACE-07). Bir kez. */
export async function claimRace(store: Store, uid: string, weekId: string, nowMs: number, tzOffsetMin: number) {
  const cur = raceWindow(nowMs, tzOffsetMin);
  if (cur.weekId === weekId && cur.active) return { status: 'not_ended' as const };
  return store.run(async (tx) => {
    const ppath = `races/${weekId}/players/${uid}`;
    const me = await tx.get(ppath);
    if (!me) return { status: 'not_joined' as const };
    if (me.claimed) return { status: 'already' as const };
    const g = await tx.get(`races/${weekId}/groups/${me.group}`);
    const list = rank(Object.values(g!.members) as RaceMember[]);
    const idx = list.findIndex((m) => m.uid === uid);
    const flags = list[idx].flags;
    const coins = flags > 0 && idx < PRIZES.length ? PRIZES[idx] : 0;
    tx.set(ppath, { ...me, claimed: true });
    return { status: 'ok' as const, rank: idx + 1, coins, frame: coins > 0 && idx === 0 };
  });
}

/** Satın alma kaydı: aynı işlem ikinci kez gelirse teslim edilmez (UAT IAP-04). */
export async function recordPurchase(store: Store, uid: string, txId: string, sku: string, platform: string, nowMs: number) {
  return store.run(async (tx) => {
    const p = `purchases/${txId}`;
    const existing = await tx.get(p);
    if (existing) return { granted: false, owner: existing.uid as string };
    tx.set(p, { uid, sku, platform, at: nowMs, refunded: false });
    return { granted: true, owner: uid };
  });
}

/** Mağaza bildirimi: iade/chargeback. İstemci /entitlements ile öğrenir ve geri alır (UAT IAP-10). */
export async function markRefunded(store: Store, txId: string, nowMs: number) {
  return store.run(async (tx) => {
    const p = `purchases/${txId}`;
    const doc = await tx.get(p);
    if (!doc || doc.refunded) return false;
    tx.set(p, { ...doc, refunded: true, refundedAt: nowMs });
    tx.set(`players/${doc.uid}/refunds/${txId}`, { sku: doc.sku, at: nowMs, acked: false });
    return true;
  });
}

/** Bulut kayıt: daha ileri bölümdeki kayıt kazanır; eşitse yeni olan. Dönen: sunucudaki güncel kayıt. */
export async function putSave(store: Store, uid: string, save: string, maxLevel: number, nowMs: number) {
  return store.run(async (tx) => {
    const p = `players/${uid}`;
    const cur = (await tx.get(p)) ?? {};
    if (cur.save && cur.maxLevel > maxLevel) return { accepted: false, save: cur.save as string, maxLevel: cur.maxLevel as number };
    tx.set(p, { ...cur, save, maxLevel, savedAt: nowMs });
    return { accepted: true, save, maxLevel };
  });
}

/** KVKK/GDPR silme: oyuncu verisi hemen silinir; satın alma kayıtları yasal saklama için anonimleştirilir. */
export async function deletePlayer(store: Store, uid: string, nowMs: number) {
  await store.deleteTree(`players/${uid}`);
  await store.run(async (tx) => {
    tx.set(`deletions/${uid}`, { at: nowMs });
  });
}
