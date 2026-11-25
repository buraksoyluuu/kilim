// D1 üzerinde belge deposu. run(): okumalar anında, yazmalar sonda tek batch (atomik uygulanır).
// Not: D1'de etkileşimli işlem yok; aynı belgeye eşzamanlı iki istek son yazanı kazandırır.
// Bu oyunda çakışma riski düşük (oyuncu başına belge); yarış gruplarında grup dolum sayısı ±1 sapabilir.
import { Doc, Store, Tx } from './store';

const parentOf = (p: string) => p.slice(0, p.lastIndexOf('/'));

export class D1Store implements Store {
  constructor(private db: D1Database) {}

  async get(p: string): Promise<Doc | null> {
    const r = await this.db.prepare('SELECT data FROM docs WHERE path = ?').bind(p).first<{ data: string }>();
    return r ? JSON.parse(r.data) : null;
  }

  async run<T>(fn: (tx: Tx) => Promise<T>): Promise<T> {
    const writes: D1PreparedStatement[] = [];
    const tx: Tx = {
      get: (p) => this.get(p),
      set: (p, d) => writes.push(this.db.prepare('INSERT INTO docs(path, parent, data) VALUES (?, ?, ?) ON CONFLICT(path) DO UPDATE SET data = excluded.data').bind(p, parentOf(p), JSON.stringify(d))),
      del: (p) => writes.push(this.db.prepare('DELETE FROM docs WHERE path = ?').bind(p)),
    };
    const r = await fn(tx);
    if (writes.length) await this.db.batch(writes);
    return r;
  }

  async list(col: string) {
    const r = await this.db.prepare('SELECT path, data FROM docs WHERE parent = ?').bind(col).all<{ path: string; data: string }>();
    return (r.results ?? []).map((x) => ({ id: x.path.slice(col.length + 1), data: JSON.parse(x.data) }));
  }

  async deleteTree(p: string) {
    await this.db.prepare("DELETE FROM docs WHERE path = ? OR path LIKE ? ESCAPE '\\'").bind(p, p.replace(/[%_\\]/g, '\\$&') + '/%').run();
  }
}
