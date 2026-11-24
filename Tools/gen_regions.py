"""Bölge 2–8 bölüm üretici (bölüm 21–160). Her bölüm: çözülebilir, 3★ (tam desen) mümkün, 3★ çözüm yolu kayıtlı,
oyuncu-ajan kazanma oranı zorluk eğrisi penceresinde.
Kullanım: python Tools/gen_regions.py 2 8      → Data/levels/region2.json … region8.json (kaldığı yerden devam eder)

Zorluk eğrisi (bölge içinde testere dişi): x1 kolay → x0 usta (10 ve 20). Bölge ilerledikçe pencere sıkılaşır.
İnsan ilk deneme kazanma ≈ ajan × 2,5 + 0,30 (Spec 3 kalibrasyonu)."""
import json
import multiprocessing as mp
import os
import random
import sys
import time

sys.path.insert(0, os.path.dirname(__file__))
from kilim_solver import gen_mix, gen_rect, player_rollouts, solve_fast, solution_path  # noqa: E402
import kilim_solver  # noqa: E402

OUT = os.path.join(os.path.dirname(__file__), '..', 'Data', 'levels')


def silhouettes(size):
    c, m = size - 1, size // 2
    corners = {(0, 0), (0, c), (c, 0), (c, c)}
    opts = [set(), corners]
    if size % 2 == 1:
        opts += [corners | {(m, m)}, {(0, m), (c, m), (m, 0), (m, c)}, corners | {(m, m), (0, m), (c, m)}]
    else:
        opts += [corners | {(m - 1, m - 1), (m - 1, m), (m, m - 1), (m, m)}, {(0, 0), (0, 1), (1, 0), (0, c), (0, c - 1), (1, c),
                                                                               (c, 0), (c - 1, 0), (c, 1), (c, c), (c - 1, c), (c, c - 1)}]
    return opts


def spec_for(level_id):
    region = (level_id - 1) // 20 + 1
    i = (level_id - 1) % 20 + 1
    boss = i in (10, 20)
    p = ((i - 1) % 10) / 9.0                          # 0 kolay … 1 zor
    tighten = 0.035 * (region - 2)
    hi = max(0.12, 0.55 - 0.38 * p - tighten)
    lo = max(0.03, hi - 0.22)
    if boss:
        lo, hi = 0.01, 0.10
    size = 6 if (region <= 3 and i % 10 in (1, 2, 3)) else 7
    if region >= 6 and i % 10 in (5, 8, 0):
        size = 8
    ncol = 4 if region <= 3 else 5
    kind = 'rect' if i % 4 == 1 else 'mix'
    ratio = 0.35 + 0.1 * (region % 3)
    return dict(id=level_id, region=region, size=size, ncol=ncol, kind=kind, ratio=ratio, lo=lo, hi=hi, boss=boss)


def _origin(lv, q):
    """Parçanın tahtadaki asıl yerinin ilk hücresi (satır*size+sütun). gen_* parçayı normalize ettiği için
    motif'ten değil, üreticinin bıraktığı _at alanından okunur; yoksa 0."""
    return q.get('_at', 0)


def make(level_id, k=40, max_att=240):
    """Pencereye giren ilk adayı döner; yoksa max_att denemede pencereye en yakın geçerli adayı (süre sınırı)."""
    s = spec_for(level_id)
    rng = random.Random(20261124 + level_id * 7919)
    sils = silhouettes(s['size'])
    best, best_d = None, 9.0
    for att in range(max_att):
        blocked = rng.choice(sils)
        lv = gen_rect(s['size'], blocked, s['ncol'], rng) if s['kind'] == 'rect' else gen_mix(s['size'], blocked, s['ncol'], rng, s['ratio'])
        if not lv or len(lv['queue']) < 6:
            continue
        # Kuyruk sırası: parçalar kabaca yukarıdan aşağı (tam desen ulaşılabilir kalsın), zorluk arttıkça daha karışık.
        jitter = s['size'] * (0.6 + 2.2 * ((level_id - 1) % 10) / 9.0)
        lv['queue'].sort(key=lambda q: _origin(lv, q) + rng.uniform(0, jitter))
        if not solve_fast(lv, perfect=True, limit=300_000):
            continue
        wr = player_rollouts(lv, k, rng=random.Random(att))
        d = 0.0 if s['lo'] <= wr <= s['hi'] else min(abs(wr - s['lo']), abs(wr - s['hi']))
        if d < best_d and wr > 0:
            best, best_d = (lv, wr, att), d
        if d == 0.0:
            break
    if best is None:
        return None, max_att
    lv, wr, att = best
    path = solution_path(lv, perfect=True)
    if not path:
        return None, att
    for q in lv['queue']:
        q.pop('_at', None)
    tot = sum(len(p['shape']) for p in lv['queue'])
    lv.update(id=level_id, region=s['region'], pieces=len(lv['queue']), cells=tot, playerWinRate=round(wr, 2),
              perfectPossible=True, stars={'2': int(tot * 0.6 + 0.999), '3': tot}, boss=s['boss'], tutorial=False,
              solution=[[qi, cells] for qi, cells in path])
    return lv, att


def work(level_id):
    t0 = time.time()
    lv, att = make(level_id)
    return level_id, lv, att, time.time() - t0


def main(r_from, r_to):
    os.makedirs(OUT, exist_ok=True)
    for region in range(r_from, r_to + 1):
        path = os.path.join(OUT, f'region{region}.json')
        done = {l['id']: l for l in json.load(open(path))} if os.path.exists(path) else {}
        todo = [i for i in range((region - 1) * 20 + 1, region * 20 + 1) if i not in done]
        with mp.Pool(max(1, os.cpu_count())) as pool:
            for level_id, lv, att, sec in pool.imap_unordered(work, todo):
                if lv is None:
                    print(f'FAIL {level_id}', flush=True)
                    continue
                done[level_id] = lv
                json.dump([done[k] for k in sorted(done)], open(path, 'w'), ensure_ascii=False)
                print(f'{level_id:4} size {lv["size"]} pcs {lv["pieces"]:2} wr {lv["playerWinRate"]:.2f} att {att:4} {sec:5.1f}s', flush=True)


if __name__ == '__main__':
    kilim_solver.TRAY = 4
    main(int(sys.argv[1]), int(sys.argv[2]))
