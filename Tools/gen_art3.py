"""Görsel kalite turu (v0.6): dokuma karolar, gerçek kilim motifleri, kabartmalı arayüz sprite'ları, ahşap tezgâh
çerçevesi, ışıklı arka plan, yeni Nine portresi ve sahnelere ışık/derinlik efekti.

Motifler Anadolu kilim geleneğinden: Elibelinde (anne/bereket), Yıldız (mutluluk), Koçboynuzu (güç),
Hayat Ağacı (ölümsüzlük), Göz (nazar). Hepsi koddan çizilir, telifsiz.
Çalıştırma sırası: gen_art2.py (sahneler) → gen_art3.py (bu dosya, sahneleri de rötuşlar).
"""
import math, os, random
from PIL import Image, ImageDraw, ImageFilter, ImageChops, ImageEnhance

ART = os.path.join(os.path.dirname(__file__), '..', 'Unity', 'Kilim', 'Assets', 'Kilim', 'Resources', 'art')
S4 = 4  # süper örnekleme


def P(name): return os.path.join(ART, name + '.png')


def down(img, size): return img.resize(size, Image.LANCZOS)


def rounded_mask(n, r):
    m = Image.new('L', (n, n), 0)
    ImageDraw.Draw(m).rounded_rectangle([0, 0, n - 1, n - 1], radius=r, fill=255)
    return m


def vgrad(w, h, top, bottom):
    g = Image.new('L', (1, h))
    for y in range(h): g.putpixel((0, y), int(top + (bottom - top) * y / max(1, h - 1)))
    return g.resize((w, h))


# ---------------------------------------------------------------- arayüz karosu (beyaz, çalışmada renklenir)
def ui_tile(name, n=128, r=34, bevel=10):
    N = n * S4
    body = vgrad(N, N, 255, 226)                     # üstten alta hafif koyulaşma
    img = Image.merge('RGBA', (body, body, body, rounded_mask(N, r * S4)))
    d = ImageDraw.Draw(img)
    # alt kabartma kenarı
    edge = Image.new('RGBA', (N, N), (0, 0, 0, 0))
    ImageDraw.Draw(edge).rounded_rectangle([0, N - bevel * S4 - r * S4, N - 1, N - 1], radius=r * S4, fill=(196, 196, 196, 255))
    edge.putalpha(ImageChops.multiply(edge.getchannel('A'), rounded_mask(N, r * S4)))
    base = Image.new('RGBA', (N, N), (0, 0, 0, 0))
    base.alpha_composite(edge)
    top = img.crop((0, 0, N, N - bevel * S4))
    tm = rounded_mask(N, r * S4).crop((0, 0, N, N - bevel * S4))
    top.putalpha(tm)
    base.alpha_composite(top, (0, 0))
    down(base, (n, n)).save(P(name))


def pill(n=128):
    ui_tile('pill', n, r=63, bevel=8)


# ---------------------------------------------------------------- dokuma karo (tahta hücresi + parça)
def weave_tile(name, n=160, r=26, raised=True):
    N = n * S4
    rnd = random.Random(7)
    img = Image.new('L', (N, N), 230)
    d = ImageDraw.Draw(img)
    th = N // 16                                     # iplik kalınlığı
    for y in range(0, N, th):
        for x in range(0, N, th * 2):
            off = th if (y // th) % 2 else 0
            v = 236 + rnd.randint(-8, 8)
            d.rounded_rectangle([x + off + 2, y + 2, x + off + th * 2 - 3, y + th - 3], radius=th // 2, fill=v)
            d.line([x + off + 6, y + th * 0.35, x + off + th * 2 - 8, y + th * 0.35], fill=min(255, v + 14), width=S4 * 2)
    img = img.filter(ImageFilter.GaussianBlur(S4 * 0.6))
    shade = vgrad(N, N, 255, 205) if raised else vgrad(N, N, 238, 255)
    img = ImageChops.multiply(img, shade)
    rgba = Image.merge('RGBA', (img, img, img, rounded_mask(N, r * S4)))
    if raised:
        # iç kenar gölgesi (yastık hissi)
        inner = Image.new('L', (N, N), 0)
        ImageDraw.Draw(inner).rounded_rectangle([0, 0, N - 1, N - 1], radius=r * S4, outline=255, width=S4 * 7)
        inner = inner.filter(ImageFilter.GaussianBlur(S4 * 5))
        dark = Image.new('RGBA', (N, N), (0, 0, 0, 0))
        dark.putalpha(ImageChops.multiply(inner.point(lambda v: v * 0.45), rounded_mask(N, r * S4)))
        rgba.alpha_composite(dark)
        hl = Image.new('RGBA', (N, N), (0, 0, 0, 0))
        ImageDraw.Draw(hl).ellipse([int(N * 0.1), int(-N * 0.25), int(N * 0.9), int(N * 0.35)], fill=(255, 255, 255, 70))
        rgba.alpha_composite(hl.filter(ImageFilter.GaussianBlur(S4 * 6)))
    down(rgba, (n, n)).save(P(name))


# ---------------------------------------------------------------- kilim motifleri (beyaz)
def motif(name, draw, n=128):
    N = n * S4
    img = Image.new('RGBA', (N, N), (0, 0, 0, 0))
    draw(ImageDraw.Draw(img), N, (255, 255, 255, 255))
    down(img, (n, n)).save(P(name))


def diamond(d, cx, cy, rx, ry, fill):
    d.polygon([(cx, cy - ry), (cx + rx, cy), (cx, cy + ry), (cx - rx, cy)], fill=fill)


def m_elibelinde(d, N, W):          # K: anne figürü (eller belde)
    c = N / 2
    diamond(d, c, N * 0.2, N * 0.11, N * 0.11, W)                       # baş
    d.polygon([(c - N * 0.08, N * 0.32), (c + N * 0.08, N * 0.32), (c + N * 0.06, N * 0.62), (c - N * 0.06, N * 0.62)], fill=W)
    for s in (-1, 1):                                                   # kollar: belde üçgen
        d.polygon([(c + s * N * 0.06, N * 0.34), (c + s * N * 0.38, N * 0.46), (c + s * N * 0.08, N * 0.56)], fill=W)
        d.polygon([(c + s * N * 0.05, N * 0.6), (c + s * N * 0.3, N * 0.9), (c + s * N * 0.14, N * 0.9), (c, N * 0.68)], fill=W)


def m_yildiz(d, N, W):              # L: sekiz köşeli yıldız
    c = N / 2
    for a in (0, 45):
        r = N * 0.42
        pts = []
        for k in range(4):
            ang = math.radians(a + k * 90)
            pts += [(c + math.cos(ang) * r, c + math.sin(ang) * r)]
        d.polygon([pts[0], pts[1], pts[2], pts[3]], fill=W)
    diamond(d, c, c, N * 0.12, N * 0.12, (0, 0, 0, 0))


def m_kocboynuzu(d, N, W):          # S: koç boynuzu
    t = int(N * 0.1)
    c = N / 2
    d.line([c, N * 0.88, c, N * 0.45], fill=W, width=t)
    for s in (-1, 1):
        d.line([c, N * 0.5, c + s * N * 0.3, N * 0.28, c + s * N * 0.38, N * 0.1], fill=W, width=t, joint='curve')
        d.line([c + s * N * 0.38, N * 0.1, c + s * N * 0.22, N * 0.12], fill=W, width=t)
    diamond(d, c, N * 0.62, N * 0.09, N * 0.09, W)


def m_hayat(d, N, W):               # Z: hayat ağacı
    c = N / 2
    d.rectangle([c - N * 0.04, N * 0.18, c + N * 0.04, N * 0.9], fill=W)
    for i, y in enumerate((0.3, 0.5, 0.7)):
        w = N * (0.18 + 0.08 * i)
        d.polygon([(c, N * (y - 0.14)), (c + w, N * y), (c, N * (y - 0.04)), (c - w, N * y)], fill=W)
    diamond(d, c, N * 0.12, N * 0.08, N * 0.08, W)


def m_goz(d, N, W):                 # M: göz (nazar)
    c = N / 2
    diamond(d, c, c, N * 0.44, N * 0.3, W)
    diamond(d, c, c, N * 0.3, N * 0.18, (0, 0, 0, 0))
    diamond(d, c, c, N * 0.13, N * 0.13, W)


# ---------------------------------------------------------------- yıldız, çerçeve, arka plan, vinyet
def star_art(n=256):
    N = n * S4
    img = Image.new('RGBA', (N, N), (0, 0, 0, 0))
    def pts(r1, r2, cx, cy):
        out = []
        for i in range(10):
            r = r1 if i % 2 == 0 else r2
            a = -math.pi / 2 + i * math.pi / 5
            out.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
        return out
    c = N / 2
    d = ImageDraw.Draw(img)
    d.polygon(pts(N * 0.48, N * 0.21, c, c + N * 0.03), fill=(170, 170, 170, 255))   # kenar/gölge
    d.polygon(pts(N * 0.45, N * 0.19, c, c), fill=(255, 255, 255, 255))
    # sol üst parlama: yarı yüzey açık
    hl = Image.new('RGBA', (N, N), (0, 0, 0, 0))
    ImageDraw.Draw(hl).polygon(pts(N * 0.3, N * 0.12, c - N * 0.04, c - N * 0.05), fill=(255, 255, 255, 255))
    shade = Image.new('RGBA', (N, N), (0, 0, 0, 0))
    ImageDraw.Draw(shade).polygon([(c, c), (c + N, c), (c + N, N), (c - N * 0.2, N)], fill=(0, 0, 0, 40))
    shade.putalpha(ImageChops.multiply(shade.getchannel('A'), img.getchannel('A')))
    img.alpha_composite(shade)
    down(img, (n, n)).save(P('star'))


def wood_frame(n=256, r=44):
    N = n * S4
    rnd = random.Random(3)
    wood = Image.new('RGB', (N, N), (122, 78, 46))
    d = ImageDraw.Draw(wood)
    for y in range(0, N, S4 * 3):                      # damar
        v = rnd.randint(-14, 14)
        d.line([0, y + math.sin(y / 90) * 8, N, y + math.sin(y / 70 + 2) * 8], fill=(122 + v, 78 + v // 2, 46 + v // 3), width=S4 * 2)
    wood = wood.filter(ImageFilter.GaussianBlur(S4))
    wood = ImageChops.multiply(wood, Image.merge('RGB', [vgrad(N, N, 255, 200)] * 3))
    img = wood.convert('RGBA')
    img.putalpha(rounded_mask(N, r * S4))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([S4 * 4, S4 * 4, N - S4 * 4, N - S4 * 4], radius=(r - 4) * S4, outline=(170, 118, 70, 255), width=S4 * 3)
    # iç kısım (dokuma zemini) koyu
    inner = S4 * 34
    d.rounded_rectangle([inner, inner, N - inner, N - inner], radius=S4 * 16, fill=(74, 52, 38, 255))
    sh = Image.new('L', (N, N), 0)
    ImageDraw.Draw(sh).rounded_rectangle([inner, inner, N - inner, N - inner], radius=S4 * 16, outline=255, width=S4 * 10)
    sh = ImageChops.multiply(sh.filter(ImageFilter.GaussianBlur(S4 * 6)), Image.new('L', (N, N), 150))
    dark = Image.new('RGBA', (N, N), (0, 0, 0, 0)); dark.putalpha(sh)
    img.alpha_composite(dark)
    down(img, (n, n)).save(P('frame_wood'))


def background(n=512):
    """Sıcak krem zemin + çok soluk kilim kafesi (döşenir)."""
    N = n * 2
    img = Image.new('RGB', (N, N), (246, 236, 220))
    d = ImageDraw.Draw(img)
    s = N // 4
    for y in range(0, N + s, s):
        for x in range(0, N + s, s):
            ox = s // 2 if (y // s) % 2 else 0
            cx, cy = x + ox, y
            for k, col in ((0.42, (240, 226, 204)), (0.3, (246, 236, 220)), (0.18, (238, 222, 198))):
                r = s * k
                d.polygon([(cx, cy - r), (cx + r, cy), (cx, cy + r), (cx - r, cy)], fill=col)
            for dx, dy in ((-s * 0.5, 0), (0, -s * 0.5)):
                d.rectangle([cx + dx - 5, cy + dy - 5, cx + dx + 5, cy + dy + 5], fill=(238, 222, 198))
    img = img.filter(ImageFilter.GaussianBlur(1.2))
    down(img, (n, n)).save(P('pattern_bg'))


def vignette(w=540, h=960):
    img = Image.new('L', (w, h), 0)
    ImageDraw.Draw(img).ellipse([-w * 0.35, -h * 0.25, w * 1.35, h * 1.2], fill=255)
    img = img.filter(ImageFilter.GaussianBlur(90))
    a = img.point(lambda v: int((255 - v) * 0.42))
    out = Image.new('RGBA', (w, h), (92, 52, 30, 0)); out.putalpha(a)
    # üstte sıcak ışık
    glow = Image.new('L', (w, h), 0)
    ImageDraw.Draw(glow).ellipse([w * 0.1, -h * 0.25, w * 0.9, h * 0.25], fill=90)
    glow = glow.filter(ImageFilter.GaussianBlur(80))
    g = Image.new('RGBA', (w, h), (255, 236, 200, 0)); g.putalpha(glow)
    out.alpha_composite(g)
    out.save(P('vignette'))


# ---------------------------------------------------------------- Nine portresi
def nine(n=512):
    N = n * S4
    img = Image.new('RGBA', (N, N), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    c = N / 2
    skin, cheek, scarf, scarf2, dress = (236, 196, 160), (226, 150, 130), (176, 48, 44), (210, 160, 60), (38, 56, 95)
    d.ellipse([0, 0, N - 1, N - 1], fill=(246, 234, 214))                         # zemin
    d.pieslice([N * 0.12, N * 0.62, N * 0.88, N * 1.3], 180, 360, fill=dress)      # omuzlar
    for i in range(5):
        x = N * (0.3 + i * 0.1)
        diamond(d, x, N * 0.86, N * 0.03, N * 0.03, scarf2)
    d.ellipse([N * 0.18, N * 0.1, N * 0.82, N * 0.78], fill=scarf)                 # yemeni
    d.ellipse([N * 0.28, N * 0.24, N * 0.72, N * 0.72], fill=skin)                 # yüz
    d.chord([N * 0.24, N * 0.12, N * 0.76, N * 0.5], 180, 360, fill=scarf)          # alın bandı
    for i in range(9):                                                             # oya kenarı
        x = N * (0.28 + i * 0.055)
        d.ellipse([x - N * 0.018, N * 0.29, x + N * 0.018, N * 0.33], fill=scarf2)
    for s in (-1, 1):
        ex = c + s * N * 0.09
        d.arc([ex - N * 0.045, N * 0.43, ex + N * 0.045, N * 0.5], 200, 340, fill=(70, 50, 40), width=S4 * 5)  # gülen göz
        d.ellipse([ex - N * 0.06 + s * N * 0.02, N * 0.52, ex + N * 0.04 + s * N * 0.02, N * 0.59], fill=cheek)
        d.arc([ex - N * 0.06, N * 0.39, ex + N * 0.06, N * 0.42], 200, 340, fill=(200, 200, 200), width=S4 * 4)  # kaş (ak)
    d.arc([c - N * 0.08, N * 0.52, c + N * 0.08, N * 0.64], 20, 160, fill=(150, 60, 50), width=S4 * 6)       # gülümseme
    d.ellipse([c - N * 0.02, N * 0.5, c + N * 0.02, N * 0.54], fill=(214, 170, 136))
    # yuvarlak kırp + kenar
    mask = Image.new('L', (N, N), 0)
    ImageDraw.Draw(mask).ellipse([0, 0, N - 1, N - 1], fill=255)
    img.putalpha(ImageChops.multiply(img.getchannel('A'), mask))
    ImageDraw.Draw(img).ellipse([S4 * 4, S4 * 4, N - S4 * 4, N - S4 * 4], outline=(176, 48, 44, 255), width=S4 * 10)
    down(img, (n, n)).save(P('nine'))


# ---------------------------------------------------------------- sahne rötuşu: ışık, derinlik, vinyet, doku
def grade_scene(path, restored):
    img = Image.open(path).convert('RGB')
    w, h = img.size
    if restored:
        img = ImageEnhance.Color(img).enhance(1.12)
        light = Image.new('L', (w, h), 0)
        ImageDraw.Draw(light).ellipse([w * 0.45, -h * 0.6, w * 1.4, h * 0.6], fill=120)
        light = light.filter(ImageFilter.GaussianBlur(90))
        img = Image.composite(Image.new('RGB', (w, h), (255, 238, 200)), img, light.point(lambda v: v // 2))
    vig = Image.new('L', (w, h), 0)
    ImageDraw.Draw(vig).ellipse([-w * 0.2, -h * 0.3, w * 1.2, h * 1.3], fill=255)
    vig = vig.filter(ImageFilter.GaussianBlur(70)).point(lambda v: 255 - int((255 - v) * 0.55))
    img = ImageChops.multiply(img, Image.merge('RGB', [vig] * 3))
    rnd = random.Random(path)
    noise = Image.effect_noise((w, h), 18).point(lambda v: 128 + (v - 128) // 3)
    img = ImageChops.overlay(img, Image.merge('RGB', [noise] * 3))
    img.save(path)


if __name__ == '__main__':
    ui_tile('tile'); ui_tile('tile_flat', bevel=0); pill()
    weave_tile('weave', raised=True); weave_tile('weave_flat', raised=False)
    for name, fn in (('motif_circle', m_elibelinde), ('motif_square', m_yildiz), ('motif_triangle', m_kocboynuzu),
                     ('motif_diamond', m_hayat), ('motif_cross', m_goz)):
        motif(name, fn)
    star_art(); wood_frame(); background(); vignette(); nine()
    for f in sorted(os.listdir(ART)):
        if f.startswith('area_') or f.startswith('intro_'):
            grade_scene(os.path.join(ART, f), f.endswith('_new.png') or f.startswith('intro_'))
    print('ok')
