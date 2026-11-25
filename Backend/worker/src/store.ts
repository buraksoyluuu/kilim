// Depolama soyutlaması: üretimde Cloudflare D1 (src/d1store.ts), testlerde bellek.
export type Doc = Record<string, any>;

export interface Tx {
  get(path: string): Promise<Doc | null>;
  set(path: string, data: Doc): void;
  del(path: string): void;
}

export interface Store {
  run<T>(fn: (tx: Tx) => Promise<T>): Promise<T>;
  get(path: string): Promise<Doc | null>;
  list(collectionPath: string): Promise<{ id: string; data: Doc }[]>;
  deleteTree(path: string): Promise<void>;
}

export class MemoryStore implements Store {
  readonly docs = new Map<string, Doc>();

  async run<T>(fn: (tx: Tx) => Promise<T>): Promise<T> {
    const writes: [string, Doc | null][] = [];
    const tx: Tx = {
      get: async (p) => (this.docs.has(p) ? structuredClone(this.docs.get(p)!) : null),
      set: (p, d) => writes.push([p, structuredClone(d)]),
      del: (p) => writes.push([p, null]),
    };
    const r = await fn(tx);
    for (const [p, d] of writes) (d === null ? this.docs.delete(p) : this.docs.set(p, d));
    return r;
  }

  async get(p: string) { return this.docs.has(p) ? structuredClone(this.docs.get(p)!) : null; }

  async list(col: string) {
    const out: { id: string; data: Doc }[] = [];
    for (const [k, v] of this.docs) {
      if (k.startsWith(col + '/') && !k.slice(col.length + 1).includes('/')) out.push({ id: k.slice(col.length + 1), data: structuredClone(v) });
    }
    return out;
  }

  async deleteTree(p: string) { for (const k of [...this.docs.keys()]) if (k === p || k.startsWith(p + '/')) this.docs.delete(k); }
}
