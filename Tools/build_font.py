"""Kilim Sans: Poppins (OFL) + DejaVu Sans sembolleri (★ ✦ ● ⚑ ✓ …) tek font dosyasında.
Unity'nin varsayılan fontu Türkçe ve sembol kapsamında zayıf; bu font her ikisini de kapsar.
Gereksinim: fontTools. Girdi: sistem fontları (fonts-dejavu, Poppins). Çıktı: Resources/fonts/KilimSans-*.ttf"""
import os, sys
from fontTools.ttLib import TTFont
from fontTools.ttLib.scaleUpem import scale_upem
from fontTools import subset
from fontTools.merge import Merger, Options

SYMS = '★☆✦●■▲◆✚⚑✓✕↻▶♥'
OUT = os.path.join(os.path.dirname(__file__), '..', 'Unity', 'Kilim', 'Assets', 'Kilim', 'Resources', 'fonts')
POPPINS = sys.argv[1] if len(sys.argv) > 1 else '/usr/share/fonts/truetype/google-fonts'
DEJAVU = '/usr/share/fonts/truetype/dejavu'

for weight, dv in (('Medium', 'DejaVuSans.ttf'), ('Bold', 'DejaVuSans-Bold.ttf')):
    d = TTFont(os.path.join(DEJAVU, dv))
    o = subset.Options(); o.notdef_outline = True; o.layout_features = []; o.hinting = False
    s = subset.Subsetter(o); s.populate(unicodes=[ord(c) for c in SYMS]); s.subset(d)
    scale_upem(d, 1000); d.save('/tmp/_sym.ttf')
    TTFont(os.path.join(POPPINS, f'Poppins-{weight}.ttf')).save('/tmp/_pop.ttf')
    f = Merger(options=Options(drop_tables=['GSUB', 'GPOS', 'GDEF', 'kern', 'hdmx', 'LTSH', 'VDMX', 'DSIG'])).merge(['/tmp/_pop.ttf', '/tmp/_sym.ttf'])
    for r in f['name'].names:
        if r.nameID in (1, 4, 16): r.string = r.toUnicode().replace('Poppins', 'Kilim Sans')
        if r.nameID == 6: r.string = r.toUnicode().replace('Poppins', 'KilimSans')
    os.makedirs(OUT, exist_ok=True)
    f.save(os.path.join(OUT, f'KilimSans-{weight}.ttf'))
    print('ok', weight)
