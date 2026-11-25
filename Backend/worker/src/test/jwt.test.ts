import { test } from 'node:test';
import assert from 'node:assert/strict';
import { webcrypto } from 'node:crypto';
import { b64urlEncode, verifyFirebaseToken, _resetJwksCache } from '../jwt';

const enc = new TextEncoder();

async function makeToken(claims: Record<string, unknown>, kid = 'k1') {
  const kp = await webcrypto.subtle.generateKey({ name: 'RSASSA-PKCS1-v1_5', modulusLength: 2048, publicExponent: new Uint8Array([1, 0, 1]), hash: 'SHA-256' }, true, ['sign', 'verify']);
  const input = `${b64urlEncode(JSON.stringify({ alg: 'RS256', kid }))}.${b64urlEncode(JSON.stringify(claims))}`;
  const sig = await webcrypto.subtle.sign('RSASSA-PKCS1-v1_5', kp.privateKey, enc.encode(input));
  const jwk = { ...(await webcrypto.subtle.exportKey('jwk', kp.publicKey)), kid };
  const fetchImpl = (async () => new Response(JSON.stringify({ keys: [jwk] }), { headers: { 'cache-control': 'max-age=60' } })) as typeof fetch;
  return { token: `${input}.${b64urlEncode(sig)}`, fetchImpl };
}

const now = 1_800_000_000;
const good = { aud: 'kilim-game', iss: 'https://securetoken.google.com/kilim-game', sub: 'uid123', iat: now - 10, exp: now + 3600 };

test('geçerli Firebase token uid döner', async () => {
  _resetJwksCache();
  const { token, fetchImpl } = await makeToken(good);
  assert.equal(await verifyFirebaseToken(token, 'kilim-game', now, fetchImpl), 'uid123');
});

test('yanlış proje, süresi dolmuş ya da bozuk imza reddedilir', async () => {
  _resetJwksCache();
  const a = await makeToken({ ...good, aud: 'baska' });
  assert.equal(await verifyFirebaseToken(a.token, 'kilim-game', now, a.fetchImpl), null);
  _resetJwksCache();
  const b = await makeToken({ ...good, exp: now - 1 });
  assert.equal(await verifyFirebaseToken(b.token, 'kilim-game', now, b.fetchImpl), null);
  _resetJwksCache();
  const c = await makeToken(good);
  const tampered = c.token.replace(/\.[^.]+\./, `.${b64urlEncode(JSON.stringify({ ...good, sub: 'hacker' }))}.`);
  assert.equal(await verifyFirebaseToken(tampered, 'kilim-game', now, c.fetchImpl), null);
});
