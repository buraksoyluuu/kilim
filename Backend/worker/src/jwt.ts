// WebCrypto ile JWT: Firebase ID token doğrulama (RS256) ve Apple/Google servis JWT'si imzalama.
const enc = new TextEncoder();

export function b64urlDecode(s: string): Uint8Array<ArrayBuffer> {
  s = s.replace(/-/g, '+').replace(/_/g, '/');
  while (s.length % 4) s += '=';
  const bin = atob(s);
  return Uint8Array.from(bin, (c) => c.charCodeAt(0)) as Uint8Array<ArrayBuffer>;
}

export function b64urlEncode(b: ArrayBuffer | Uint8Array | string): string {
  const bytes = typeof b === 'string' ? enc.encode(b) : b instanceof Uint8Array ? b : new Uint8Array(b);
  let s = '';
  bytes.forEach((x) => (s += String.fromCharCode(x)));
  return btoa(s).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

export function decodePayload<T = any>(jwt: string): T {
  return JSON.parse(new TextDecoder().decode(b64urlDecode(jwt.split('.')[1])));
}

function pemToDer(pem: string): ArrayBuffer {
  const b64 = pem.replace(/-----[^-]+-----/g, '').replace(/\s+/g, '');
  return Uint8Array.from(atob(b64), (c) => c.charCodeAt(0)).buffer;
}

/** Servis JWT'si imzalar. alg ES256 (Apple .p8) ya da RS256 (Google servis hesabı). */
export async function signJwt(header: Record<string, unknown>, payload: Record<string, unknown>, pkcs8Pem: string): Promise<string> {
  const alg = header.alg === 'ES256' ? { name: 'ECDSA', namedCurve: 'P-256' } : { name: 'RSASSA-PKCS1-v1_5', hash: 'SHA-256' };
  const key = await crypto.subtle.importKey('pkcs8', pemToDer(pkcs8Pem), alg, false, ['sign']);
  const input = `${b64urlEncode(JSON.stringify(header))}.${b64urlEncode(JSON.stringify(payload))}`;
  const sig = await crypto.subtle.sign(header.alg === 'ES256' ? { name: 'ECDSA', hash: 'SHA-256' } : 'RSASSA-PKCS1-v1_5', key, enc.encode(input));
  return `${input}.${b64urlEncode(sig)}`;
}

type Jwk = JsonWebKey & { kid: string };
let jwksCache: { keys: Jwk[]; exp: number } | null = null;

const FIREBASE_JWKS = 'https://www.googleapis.com/service_accounts/v1/jwk/securetoken@system.gserviceaccount.com';

/** Firebase Auth ID token doğrular; geçerliyse uid döner. fetchImpl testte değiştirilebilir. */
export async function verifyFirebaseToken(token: string, projectId: string, nowSec = Math.floor(Date.now() / 1000),
                                          fetchImpl: typeof fetch = fetch): Promise<string | null> {
  const parts = token.split('.');
  if (parts.length !== 3) return null;
  const header = JSON.parse(new TextDecoder().decode(b64urlDecode(parts[0])));
  const p = decodePayload(token);
  if (header.alg !== 'RS256') return null;
  if (p.aud !== projectId || p.iss !== `https://securetoken.google.com/${projectId}`) return null;
  if (typeof p.sub !== 'string' || !p.sub || p.exp < nowSec || p.iat > nowSec + 300) return null;
  if (!jwksCache || jwksCache.exp < Date.now()) {
    const r = await fetchImpl(FIREBASE_JWKS);
    const maxAge = Number(/max-age=(\d+)/.exec(r.headers.get('cache-control') ?? '')?.[1] ?? 3600);
    jwksCache = { keys: ((await r.json()) as { keys: Jwk[] }).keys, exp: Date.now() + maxAge * 1000 };
  }
  const jwk = jwksCache.keys.find((k) => k.kid === header.kid);
  if (!jwk) return null;
  const key = await crypto.subtle.importKey('jwk', jwk, { name: 'RSASSA-PKCS1-v1_5', hash: 'SHA-256' }, false, ['verify']);
  const ok = await crypto.subtle.verify('RSASSA-PKCS1-v1_5', key, b64urlDecode(parts[2]), enc.encode(`${parts[0]}.${parts[1]}`));
  return ok ? p.sub : null;
}

export function _resetJwksCache() { jwksCache = null; }
