"""Bölüm dosyasını çözücüyle doğrular (CI). Kullanım: python Tools/validate_levels.py Data/levels/region*.json
Her bölüm: çözülebilir mi, 3★ (tam desen) mümkün mü, stars.3 = oynanabilir hücre mi."""
import json, os, sys
sys.path.insert(0, os.path.dirname(__file__))
from kilim_solver import solve_fast  # noqa: E402

def main(paths):
    levels = [lv for path in paths for lv in json.load(open(path, encoding='utf-8'))]
    bad = 0
    for lv in levels:
        size = lv['size']
        playable = size * size - len(lv['blocked'])
        errs = []
        if sum(len(p['shape']) for p in lv['queue']) != playable: errs.append('parça hücresi ≠ oynanabilir')
        if lv['stars']['3'] != playable: errs.append('stars.3 ≠ oynanabilir')
        if solve_fast(lv) is not True: errs.append('çözülemiyor')
        if solve_fast(lv, perfect=True) is not True: errs.append('3★ yok')
        print(f"bölüm {lv['id']:>3}: {'OK' if not errs else ', '.join(errs)}")
        bad += bool(errs)
    print(f"{len(levels) - bad}/{len(levels)} geçerli")
    return 1 if bad else 0

if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
