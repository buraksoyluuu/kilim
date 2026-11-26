"""Isınma eğrisi (oyuncu geri bildirimi: ilk bölümler zor, döndürme anlaşılmıyor).
1–3: elle tasarlanmış küçük kilim bantları, döndürme yok. 4: döndürme yok. 5: tek parça döndürülmeli.
'handmade': True olan bölümleri recolor_levels.py değiştirmez."""
import json, os

def norm(cells):
    r0 = min(r for r, _ in cells); c0 = min(c for _, c in cells)
    return sorted([[r - r0, c - c0] for r, c in cells])

def rot(shape):  # saat yönü (Core: (r,c)→(c,-r)), sonra normalize
    return norm([[c, -r] for r, c in shape])

def build(lid, n, pieces, tutorial):
    motif = [[''] * n for _ in range(n)]
    queue, sol = [], []
    for i, (color, cells) in enumerate(pieces):
        for r, c in cells: motif[r][c] = color
        queue.append({'shape': norm(cells), 'color': color})
        sol.append([i, [list(x) for x in cells]])
    cells = n * n
    return {'size': n, 'blocked': [], 'motif': motif, 'queue': queue, 'id': lid, 'region': 1, 'pieces': len(pieces),
            'cells': cells, 'playerWinRate': 0.95, 'perfectPossible': True, 'stars': {'2': round(cells * .6), '3': cells},
            'boss': False, 'tutorial': tutorial, 'solution': sol, 'handmade': True}

row = lambda r, c0, c1: [(r, c) for c in range(c0, c1 + 1)]
col = lambda c, r0, r1: [(r, c) for r in range(r0, r1 + 1)]
sq = lambda r, c: [(r, c), (r, c + 1), (r + 1, c), (r + 1, c + 1)]

L1 = build(1, 4, [('K', row(0, 0, 3)), ('S', sq(1, 0)), ('S', sq(1, 2)), ('K', row(3, 0, 3))], True)
L2 = build(2, 4, [('K', col(0, 0, 3)), ('L', sq(0, 1)), ('S', col(1, 2, 3)), ('S', col(2, 2, 3)), ('K', col(3, 0, 3))], True)
L3 = build(3, 5, [('K', row(0, 0, 4)), ('S', col(0, 1, 3)), ('L', row(1, 1, 3)), ('L', row(2, 1, 3)), ('L', row(3, 1, 3)),
                  ('S', col(4, 1, 3)), ('K', row(4, 0, 4))], False)

for path in ('Data/levels/region1.json', 'Unity/Kilim/Assets/Kilim/Resources/levels/region1.json'):
    lv = json.load(open(path))
    lv[0], lv[1], lv[2] = L1, L2, L3
    for k in (3, 4):   # 4 ve 5: parçalar çözümdeki yönde gelsin
        l = lv[k]
        q = l['queue']; qs = isinstance(q, str)
        import ast
        q = ast.literal_eval(q) if qs else q
        s = ast.literal_eval(l['solution']) if isinstance(l['solution'], str) else l['solution']
        rotated = False
        for qi, cells in s:
            target = norm(cells)
            if l['id'] == 5 and not rotated and target != rot(target) and len(cells) >= 3:
                q[qi]['shape'] = rot(target); rotated = True   # öğretici tek döndürme
            else:
                q[qi]['shape'] = target
        l['queue'] = str(q) if qs else q
        l['handmade'] = True
        if l['id'] == 5: l['tutorial'] = True
    json.dump(lv, open(path, 'w'), ensure_ascii=False)
print('ok')
