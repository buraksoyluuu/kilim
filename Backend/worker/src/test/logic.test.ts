import { test } from 'node:test';
import assert from 'node:assert/strict';
import { MemoryStore } from '../store';
import * as L from '../logic';

const TZ = 180; // İstanbul
const fri = (h: number, m = 0) => Date.UTC(2026, 10, 13, h - 3, m); // 13 Kasım 2026 Cuma, yerel saat h

test('yarış penceresi Cuma 10:00 – Pazar 22:00 yerel', () => {
  assert.equal(L.raceWindow(fri(9, 59), TZ).active, false);
  assert.equal(L.raceWindow(fri(10), TZ).active, true);
  assert.equal(L.raceWindow(fri(10), TZ).weekId, '2026-11-13');
  assert.equal(L.raceWindow(fri(10) + 60 * 3_600_000 - 1, TZ).active, true);   // Pazar 21:59
  assert.equal(L.raceWindow(fri(10) + 60 * 3_600_000, TZ).active, false);       // Pazar 22:00
});

test('30 kişilik gruplar, tekrar katılım aynı grup', async () => {
  const s = new MemoryStore();
  for (let i = 0; i < 31; i++) await L.joinRace(s, `u${i}`, `n${i}`, 'w');
  assert.equal(await L.joinRace(s, 'u0', 'x', 'w'), 'g0');
  assert.equal(await L.joinRace(s, 'u30', 'x', 'w'), 'g1');
});

test('bayrak kuralları: ilk deneme, artan bölüm, 15 sn', async () => {
  const s = new MemoryStore();
  await L.joinRace(s, 'a', 'A', '2026-11-13');
  assert.equal(await L.addFlag(s, 'a', 5, 2, fri(11), TZ), 'not_first_try');
  assert.equal(await L.addFlag(s, 'a', 5, 1, fri(11), TZ), 'ok');
  assert.equal(await L.addFlag(s, 'a', 5, 1, fri(11, 5), TZ), 'duplicate_level');
  assert.equal(await L.addFlag(s, 'a', 6, 1, fri(11) + 5_000, TZ), 'too_fast');
  assert.equal(await L.addFlag(s, 'a', 6, 1, fri(11, 1), TZ), 'ok');
  assert.equal(await L.addFlag(s, 'b', 6, 1, fri(11, 1), TZ), 'not_joined');
  assert.equal(await L.addFlag(s, 'a', 7, 1, fri(9), TZ), 'inactive');
});

test('sıralama eşitlikte önce ulaşan, ödül bir kez, 0 bayrak ödülsüz', async () => {
  const s = new MemoryStore();
  for (const u of ['a', 'b', 'c']) await L.joinRace(s, u, u.toUpperCase(), '2026-11-13');
  await L.addFlag(s, 'b', 1, 1, fri(11), TZ);
  await L.addFlag(s, 'a', 1, 1, fri(12), TZ);
  const st = await L.standings(s, 'a', '2026-11-13');
  assert.deepEqual(st!.map((x) => x.nick), ['B', 'A', 'C']);
  const after = fri(10) + 3 * 86_400_000;
  assert.equal((await L.claimRace(s, 'b', '2026-11-13', fri(12), TZ)).status, 'not_ended');
  const rb = await L.claimRace(s, 'b', '2026-11-13', after, TZ);
  assert.deepEqual([rb.status, (rb as any).coins, (rb as any).frame], ['ok', 1500, true]);
  assert.equal((await L.claimRace(s, 'b', '2026-11-13', after, TZ)).status, 'already');
  assert.equal((await L.claimRace(s, 'c', '2026-11-13', after, TZ) as any).coins, 0);
});

test('satın alma idempotent, iade bildirimi kuyruğa düşer', async () => {
  const s = new MemoryStore();
  assert.equal((await L.recordPurchase(s, 'u', 't1', 'kilim.coins.6000', 'ios', 1)).granted, true);
  assert.equal((await L.recordPurchase(s, 'u', 't1', 'kilim.coins.6000', 'ios', 2)).granted, false);
  assert.equal(await L.markRefunded(s, 't1', 3), true);
  assert.equal(await L.markRefunded(s, 't1', 4), false);
  assert.equal((await s.list('players/u/refunds')).length, 1);
});

test('bulut kayıt: ileri bölüm kazanır, silme her şeyi kaldırır', async () => {
  const s = new MemoryStore();
  await L.putSave(s, 'u', 'A', 10, 1);
  const r = await L.putSave(s, 'u', 'B', 6, 2);
  assert.deepEqual([r.accepted, r.save], [false, 'A']);
  assert.equal((await L.putSave(s, 'u', 'C', 12, 3)).accepted, true);
  await L.deletePlayer(s, 'u', 4);
  assert.equal(await s.get('players/u'), null);
  assert.ok(await s.get('deletions/u'));
});
