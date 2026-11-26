"""Bölüm desenlerini gerçek kilim göbeği düzenine çevirir (oyuncu geri bildirimi: tahta tek renk, "tablo doldurmak" gibi).
Çözümdeki her parça, ağırlık merkezinin tahta merkezine Manhattan uzaklığına göre bir halkaya düşer; halka rengi
bölgenin paletinden gelir → ortada göbek, çevresinde baklava halkaları. Parça rengi ile desen birlikte değiştiği için
çözümler yine 3 yıldıza çıkar (Core testi AllSolutionsReplayTo3Stars)."""
import ast, glob, json, os

# İlk 30 bölüm: elle seçilmiş kilim desenleri (göz, koçboynuzu zikzağı, haç, bant, çapraz baklava…).
# f(r, c, n) → paletten indeks; parça rengi = hücrelerinin çoğunluk değeri.
def _d(r, c, n): return abs(r - (n - 1) / 2) + abs(c - (n - 1) / 2)
HAND = [
    lambda r, c, n: 0 if _d(r, c, n) < 1 else (1 if _d(r, c, n) < n / 3 else 2),              # göz
    lambda r, c, n: (r * 3 // n) % 3,                                                        # bantlar
    lambda r, c, n: ((c + abs(r - (n - 1) / 2)) * 3 // n) % 3,                               # zikzak
    lambda r, c, n: 0 if min(abs(r - (n - 1) / 2), abs(c - (n - 1) / 2)) < n / 6 else 1,     # haç
    lambda r, c, n: 0 if min(abs(r - c), abs(r + c - n + 1)) < n / 5 else 1 + (r < (n - 1) / 2),  # çapraz
    lambda r, c, n: int(_d(r, c, n) * 2.2 / n) % 3,                                           # baklava halkası
    lambda r, c, n: 0 if abs(c - (n - 1) / 2) < n / 5 else (1 if r % 4 < 2 else 2),          # orta yol + bant
    lambda r, c, n: (int(abs(r - (n - 1) / 2) * 3 / n) + int(abs(c - (n - 1) / 2) * 3 / n)) % 3,  # dörtlü göbek
    lambda r, c, n: 0 if (r < n / 3 or r >= 2 * n / 3) and abs(c - (n - 1) / 2) < n / 4 else (1 if _d(r, c, n) < n / 2.5 else 2),  # sandık
    lambda r, c, n: (r // 2 + c // 2) % 2 + (0 if _d(r, c, n) > n / 3 else 1),                # damalı göbek
]

PALETTES = {1: 'KSL', 2: 'LKS', 3: 'SKZ', 4: 'MSL', 5: 'ZSK', 6: 'KLSM', 7: 'LSKZ', 8: 'KZSL'}

for path in sorted(glob.glob('Data/levels/region*.json')):
    levels = json.load(open(path))
    for lv in levels:
        n = lv['size']
        pal = PALETTES.get(lv['region'], 'KSL')
        queue = ast.literal_eval(lv['queue']) if isinstance(lv['queue'], str) else lv['queue']
        sol = ast.literal_eval(lv['solution']) if isinstance(lv['solution'], str) else lv['solution']
        c = (n - 1) / 2
        ring_w = max(0.9, n / (2.2 * len(pal)))
        motif = [row[:] for row in lv['motif']]
        hand = None
        if lv['id'] <= 30:   # sıradaki desen; tek renge çöken (küçük tahta/iri parça) desen atlanır
            for k in range(len(HAND)):
                f = HAND[(lv['id'] - 1 + k) % len(HAND)]
                cnt = {}
                for qi, cells in sol:
                    v = [int(f(r, col, n)) % len(pal) for r, col in cells]
                    m = max(set(v), key=v.count); cnt[m] = cnt.get(m, 0) + len(cells)
                if len(cnt) > 1 and max(cnt.values()) <= 0.7 * sum(cnt.values()): hand = f; break
            hand = hand or HAND[(lv['id'] - 1) % len(HAND)]
        for qi, cells in sol:
            if hand:
                votes = [int(hand(r, col, n)) % len(pal) for r, col in cells]
                color = pal[max(set(votes), key=votes.count)]
                queue[qi]['color'] = color
                for r, col in cells: motif[r][col] = color
                continue
            cy = sum(r for r, _ in cells) / len(cells)
            cx = sum(col for _, col in cells) / len(cells)
            d = abs(cy - c) + abs(cx - c)
            color = pal[int(d / ring_w) % len(pal)]
            queue[qi]['color'] = color
            for r, col in cells: motif[r][col] = color
        lv['motif'] = motif
        lv['queue'] = str(queue) if isinstance(lv['queue'], str) else queue
    json.dump(levels, open(path, 'w'), ensure_ascii=False)
    dst = os.path.join('Unity/Kilim/Assets/Kilim/Resources/levels', os.path.basename(path))
    json.dump(levels, open(dst, 'w'), ensure_ascii=False)
print('ok')
