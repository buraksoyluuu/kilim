"""Bölüm desenlerini gerçek kilim göbeği düzenine çevirir (oyuncu geri bildirimi: tahta tek renk, "tablo doldurmak" gibi).
Çözümdeki her parça, ağırlık merkezinin tahta merkezine Manhattan uzaklığına göre bir halkaya düşer; halka rengi
bölgenin paletinden gelir → ortada göbek, çevresinde baklava halkaları. Parça rengi ile desen birlikte değiştiği için
çözümler yine 3 yıldıza çıkar (Core testi AllSolutionsReplayTo3Stars)."""
import ast, glob, json, os

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
        for qi, cells in sol:
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
