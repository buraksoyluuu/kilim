"""Testere dişi zorluk (geri bildirim: ~20. bölümde 'çok zorlaşıyor').
Her 5'li blokta (6–10, 11–15, …): 3 orta, 1 nefes (en kolay, 3. sıra), 1 zor (en zor, 5. sıra; patron varsa o).
Zorluk ölçüsü: playerWinRate (yüksek = kolay). 1–5 elle tasarlı, dokunulmaz. Kimlikler sırayla yeniden yazılır."""
import json, glob, shutil
for path in sorted(glob.glob('Data/levels/region*.json')):
    lv = json.load(open(path))
    ids = [l['id'] for l in lv]
    out = []
    i = 0
    while i < len(lv):
        start_id = lv[i]['id']
        if start_id <= 5 or lv[i].get('handmade'):
            out.append(lv[i]); i += 1; continue
        chunk = lv[i:i + 5]; i += 5
        if len(chunk) < 5: out += chunk; continue
        boss = [c for c in chunk if c.get('boss')]
        rest = sorted([c for c in chunk if not c.get('boss')], key=lambda c: -c.get('playerWinRate', 0.5))
        easy = rest.pop(0)
        hard = boss[0] if boss else rest.pop()     # rest artık kolaydan zora
        mids = rest + boss[1:]
        order = mids[:2] + [easy] + mids[2:3] + [hard]
        out += order
    for l, want in zip(out, ids): l['id'] = want
    json.dump(out, open(path, 'w'), ensure_ascii=False)
    shutil.copy(path, path.replace('Data/levels', 'Unity/Kilim/Assets/Kilim/Resources/levels'))
print('ok')
