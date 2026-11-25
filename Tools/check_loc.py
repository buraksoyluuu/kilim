"""Loc.cs çeviri tablosunda aynı anahtar iki kez yazılırsa uygulama açılışta çöker (TypeInitializationException).
CI bu kontrolü her push'ta çalıştırır."""
import re, sys
from collections import Counter
s = open('Unity/Kilim/Assets/Kilim/Scripts/Runtime/Loc.cs', encoding='utf-8').read()
keys = re.findall(r'\{ "((?:[^"\\]|\\.)*)", "(?:[^"\\]|\\.)*" \}', s[s.index('Table'):])
dups = [k for k, c in Counter(keys).items() if c > 1]
if dups: sys.exit('Çift çeviri anahtarı: ' + ', '.join(dups))
print(f'Loc: {len(keys)} anahtar, çift yok')
