-- Belge deposu: Firestore benzeri yol → JSON. Mantık katmanı (src/logic.ts) bu soyutlamayla çalışır.
CREATE TABLE IF NOT EXISTS docs (
  path TEXT PRIMARY KEY,
  parent TEXT NOT NULL,
  data TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS docs_parent ON docs(parent);
