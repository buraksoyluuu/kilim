"""Kilim arayüz simgeleri, logo, tanıtım ve ev alanı illüstrasyonları (PIL, telifsiz, koddan üretilir).

Çıktı: Unity/Kilim/Assets/Kilim/Resources/art/
  icon_*.png        beyaz simgeler (oyunda renklendirilir)
  logo.png          kilim göbeği amblemi
  bubble.png        konuşma balonu
  intro_1..4.png    tanıtım sayfaları
  area_{key}_old.png / area_{key}_new.png   ev alanları: bakımsız / onarılmış

Final illüstrasyonlar geldiğinde aynı dosya adlarıyla değiştirmek yeterli.
"""
import math, os, random
from PIL import Image, ImageDraw, ImageFilter, ImageEnhance, ImageOps

ART = os.path.join(os.path.dirname(__file__), '..', 'Unity', 'Kilim', 'Assets', 'Kilim', 'Resources', 'art')
os.makedirs(ART, exist_ok=True)

K, L, S, Z, M = (178, 58, 46), (38, 56, 95), (217, 162, 58), (107, 122, 46), (110, 58, 94)
CREAM, SAND, BROWN, DARK, WHITE = (246, 239, 227), (241, 228, 204), (91, 70, 54), (43, 36, 32), (255, 255, 255)
SKY, GRASS, STONE, WOOD, COPPER, WATER = (190, 222, 236), (142, 176, 92), (178, 168, 152), (150, 102, 62), (196, 112, 60), (96, 160, 200)


def save(img, name, size=None):
    if size: img = img.resize(size, Image.LANCZOS)
    img.save(os.path.join(ART, name + '.png'))


# ------------------------------------------------------------------ simgeler
def icon(name, fn, n=128):
    s = 4
    img = Image.new('RGBA', (n * s, n * s), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    fn(d, n * s)
    save(img, 'icon_' + name, (n, n))


W = (255, 255, 255, 255)


def arrow_head(d, x, y, ang, size):
    a1, a2 = ang + 2.5, ang - 2.5
    d.polygon([(x + math.cos(ang) * size, y + math.sin(ang) * size),
               (x + math.cos(a1) * size * 0.9, y + math.sin(a1) * size * 0.9),
               (x + math.cos(a2) * size * 0.9, y + math.sin(a2) * size * 0.9)], fill=W)


def i_pause(d, n):
    w = n * 0.16
    for x in (n * 0.3, n * 0.54):
        d.rounded_rectangle([x, n * 0.22, x + w, n * 0.78], radius=w / 2, fill=W)


def i_undo(d, n):
    c, r, t = n / 2, n * 0.3, n * 0.1
    d.arc([c - r, c - r + n * 0.05, c + r, c + r + n * 0.05], 200, 470, fill=W, width=int(t))
    ang = math.radians(200)
    x, y = c + math.cos(ang) * r, c + n * 0.05 + math.sin(ang) * r
    arrow_head(d, x, y - n * 0.02, math.radians(-70), n * 0.17)


def i_swap(d, n):
    t = int(n * 0.09)
    d.line([n * 0.2, n * 0.36, n * 0.72, n * 0.36], fill=W, width=t)
    arrow_head(d, n * 0.8, n * 0.36, 0, n * 0.15)
    d.line([n * 0.8, n * 0.64, n * 0.28, n * 0.64], fill=W, width=t)
    arrow_head(d, n * 0.2, n * 0.64, math.pi, n * 0.15)


def i_stitch(d, n):
    t = int(n * 0.07)
    d.line([n * 0.22, n * 0.8, n * 0.76, n * 0.22], fill=W, width=t)
    d.ellipse([n * 0.68, n * 0.12, n * 0.86, n * 0.3], outline=W, width=int(t * 0.8))
    d.arc([n * 0.1, n * 0.42, n * 0.6, n * 0.92], 180, 330, fill=W, width=int(t * 0.7))
    d.rounded_rectangle([n * 0.12, n * 0.12, n * 0.38, n * 0.38], radius=n * 0.05, fill=W)


def i_pattern(d, n):
    g = n * 0.2
    for r in range(3):
        for c in range(3):
            x, y = n * 0.17 + c * (g + n * 0.04), n * 0.17 + r * (g + n * 0.04)
            if (r + c) % 2 == 0: d.rounded_rectangle([x, y, x + g, y + g], radius=g * 0.2, fill=W)
            else: d.rounded_rectangle([x, y, x + g, y + g], radius=g * 0.2, outline=W, width=int(n * 0.03))


def i_settings(d, n):
    c = n / 2
    for k in range(8):
        a = k * math.pi / 4
        x, y = c + math.cos(a) * n * 0.33, c + math.sin(a) * n * 0.33
        d.rounded_rectangle([x - n * 0.08, y - n * 0.08, x + n * 0.08, y + n * 0.08], radius=n * 0.03, fill=W)
    d.ellipse([c - n * 0.3, c - n * 0.3, c + n * 0.3, c + n * 0.3], fill=W)
    d.ellipse([c - n * 0.12, c - n * 0.12, c + n * 0.12, c + n * 0.12], fill=(0, 0, 0, 0))


def star(d, cx, cy, r1, r2, fill, n=5):
    pts = []
    for i in range(n * 2):
        r = r1 if i % 2 == 0 else r2
        a = -math.pi / 2 + i * math.pi / n
        pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
    d.polygon(pts, fill=fill)


def i_daily(d, n):
    d.rounded_rectangle([n * 0.14, n * 0.2, n * 0.86, n * 0.86], radius=n * 0.1, outline=W, width=int(n * 0.07))
    d.rectangle([n * 0.14, n * 0.2, n * 0.86, n * 0.36], fill=W)
    for x in (n * 0.32, n * 0.68): d.rounded_rectangle([x - n * 0.04, n * 0.1, x + n * 0.04, n * 0.28], radius=n * 0.03, fill=W)
    star(d, n / 2, n * 0.6, n * 0.17, n * 0.07, W)


def i_race(d, n):
    d.rounded_rectangle([n * 0.2, n * 0.12, n * 0.28, n * 0.9], radius=n * 0.03, fill=W)
    d.polygon([(n * 0.28, n * 0.14), (n * 0.84, n * 0.3), (n * 0.28, n * 0.5)], fill=W)


def i_pass(d, n):
    d.polygon([(n * 0.3, n * 0.5), (n * 0.2, n * 0.92), (n * 0.38, n * 0.82), (n * 0.46, n * 0.95), (n * 0.5, n * 0.55)], fill=W)
    d.polygon([(n * 0.7, n * 0.5), (n * 0.8, n * 0.92), (n * 0.62, n * 0.82), (n * 0.54, n * 0.95), (n * 0.5, n * 0.55)], fill=W)
    d.ellipse([n * 0.22, n * 0.08, n * 0.78, n * 0.64], fill=W)
    star(d, n / 2, n * 0.37, n * 0.17, n * 0.07, (0, 0, 0, 0))


def i_piggy(d, n):
    d.ellipse([n * 0.14, n * 0.28, n * 0.8, n * 0.78], fill=W)
    d.polygon([(n * 0.3, n * 0.34), (n * 0.34, n * 0.16), (n * 0.46, n * 0.3)], fill=W)
    d.rounded_rectangle([n * 0.76, n * 0.44, n * 0.9, n * 0.6], radius=n * 0.04, fill=W)
    for x in (n * 0.26, n * 0.56): d.rounded_rectangle([x, n * 0.7, x + n * 0.1, n * 0.88], radius=n * 0.03, fill=W)
    d.rounded_rectangle([n * 0.38, n * 0.3, n * 0.56, n * 0.35], radius=n * 0.02, fill=(0, 0, 0, 0))


def i_map(d, n):
    pts = [(n * 0.12, n * 0.22), (n * 0.38, n * 0.14), (n * 0.62, n * 0.22), (n * 0.88, n * 0.14),
           (n * 0.88, n * 0.78), (n * 0.62, n * 0.86), (n * 0.38, n * 0.78), (n * 0.12, n * 0.86)]
    d.polygon(pts, fill=W)
    for x in (n * 0.38, n * 0.62): d.line([x, n * 0.16, x, n * 0.84], fill=(0, 0, 0, 0), width=int(n * 0.03))
    d.ellipse([n * 0.44, n * 0.38, n * 0.56, n * 0.5], fill=(0, 0, 0, 0))


def i_home(d, n):
    d.polygon([(n * 0.5, n * 0.12), (n * 0.9, n * 0.48), (n * 0.1, n * 0.48)], fill=W)
    d.rounded_rectangle([n * 0.2, n * 0.42, n * 0.8, n * 0.88], radius=n * 0.05, fill=W)
    d.rounded_rectangle([n * 0.42, n * 0.6, n * 0.58, n * 0.88], radius=n * 0.04, fill=(0, 0, 0, 0))


def i_shop(d, n):
    d.rounded_rectangle([n * 0.16, n * 0.34, n * 0.84, n * 0.9], radius=n * 0.08, fill=W)
    d.arc([n * 0.32, n * 0.1, n * 0.68, n * 0.52], 180, 360, fill=W, width=int(n * 0.07))
    d.ellipse([n * 0.42, n * 0.52, n * 0.58, n * 0.68], fill=(0, 0, 0, 0))


def i_stuck(d, n):
    g = n * 0.18
    for r in range(3):
        for c in range(3):
            if (r, c) == (1, 1): continue
            x, y = n * 0.16 + c * (g + n * 0.05), n * 0.16 + r * (g + n * 0.05)
            d.rounded_rectangle([x, y, x + g, y + g], radius=g * 0.2, fill=W)
    d.rounded_rectangle([n * 0.46, n * 0.39, n * 0.54, n * 0.55], radius=n * 0.03, fill=W)
    d.ellipse([n * 0.46, n * 0.57, n * 0.54, n * 0.63], fill=W)


def i_shield(d, n):
    d.polygon([(n * 0.5, n * 0.08), (n * 0.86, n * 0.22), (n * 0.82, n * 0.56), (n * 0.5, n * 0.92), (n * 0.18, n * 0.56), (n * 0.14, n * 0.22)], fill=W)
    d.line([n * 0.34, n * 0.5, n * 0.46, n * 0.62, n * 0.68, n * 0.36], fill=(0, 0, 0, 0), width=int(n * 0.08), joint='curve')


def i_replay(d, n):
    c, r = n / 2, n * 0.3
    d.arc([c - r, c - r, c + r, c + r], 40, 330, fill=W, width=int(n * 0.1))
    a = math.radians(330)
    arrow_head(d, c + math.cos(a) * r, c + math.sin(a) * r, math.radians(70), n * 0.17)


def i_play(d, n):
    d.polygon([(n * 0.3, n * 0.18), (n * 0.84, n * 0.5), (n * 0.3, n * 0.82)], fill=W)


def i_lock(d, n):
    d.arc([n * 0.28, n * 0.12, n * 0.72, n * 0.56], 180, 360, fill=W, width=int(n * 0.09))
    d.line([n * 0.32, n * 0.34, n * 0.32, n * 0.46], fill=W, width=int(n * 0.09))
    d.line([n * 0.68, n * 0.34, n * 0.68, n * 0.46], fill=W, width=int(n * 0.09))
    d.rounded_rectangle([n * 0.18, n * 0.42, n * 0.82, n * 0.9], radius=n * 0.08, fill=W)
    d.ellipse([n * 0.44, n * 0.56, n * 0.56, n * 0.68], fill=(0, 0, 0, 0))


ICONS = {'pause': i_pause, 'undo': i_undo, 'swap': i_swap, 'stitch': i_stitch, 'pattern': i_pattern,
         'settings': i_settings, 'daily': i_daily, 'race': i_race, 'pass': i_pass, 'piggy': i_piggy, 'map': i_map,
         'home': i_home, 'shop': i_shop, 'stuck': i_stuck, 'shield': i_shield, 'replay': i_replay, 'play': i_play}



# ------------------------------------------------------------------ sahne çizimi
SW, SH = 1800, 1040


def shade(c, k):
    return tuple(max(0, min(255, int(v * k))) for v in c[:3])


def kilim_band(d, x0, y0, x1, h, colors=(K, S, L, CREAM)):
    d.rectangle([x0, y0, x1, y0 + h], fill=colors[0])
    step = h * 1.1
    x = x0
    i = 0
    while x < x1:
        cx, cy = x + step / 2, y0 + h / 2
        r = h * 0.38
        d.polygon([(cx, cy - r), (cx + r, cy), (cx, cy + r), (cx - r, cy)], fill=colors[1 + i % 2])
        d.polygon([(cx, cy - r * 0.45), (cx + r * 0.45, cy), (cx, cy + r * 0.45), (cx - r * 0.45, cy)], fill=colors[3])
        x += step
        i += 1


def rug(d, x, y, w, h, base=K):
    d.rounded_rectangle([x, y, x + w, y + h], radius=8, fill=base)
    d.rectangle([x + 14, y + 14, x + w - 14, y + h - 14], outline=CREAM, width=6)
    n = max(1, int(w / h * 1.4))
    for i in range(n):
        cx = x + w * (i + 0.5) / n
        cy = y + h / 2
        r = min(w / n, h) * 0.3
        d.polygon([(cx, cy - r), (cx + r, cy), (cx, cy + r), (cx - r, cy)], fill=S)
        d.polygon([(cx, cy - r * 0.5), (cx + r * 0.5, cy), (cx, cy + r * 0.5), (cx - r * 0.5, cy)], fill=L)
    for i in range(int(w / 26)):
        d.line([x + 10 + i * 26, y + h, x + 10 + i * 26, y + h + 18], fill=CREAM, width=4)


def window(d, x, y, w, h, frame=L, curtains=None, view=True):
    d.rounded_rectangle([x - 14, y - 14, x + w + 14, y + h + 14], radius=w * 0.5 if w < 300 else 30, fill=frame)
    if view:
        d.rectangle([x, y, x + w, y + h], fill=SKY)
        d.ellipse([x + w * 0.1, y + h * 0.55, x + w * 0.9, y + h * 1.3], fill=GRASS)
    d.line([x + w / 2, y, x + w / 2, y + h], fill=frame, width=10)
    d.line([x, y + h / 2, x + w, y + h / 2], fill=frame, width=10)
    if curtains:
        d.polygon([(x - 20, y - 20), (x + w * 0.32, y - 20), (x + w * 0.1, y + h + 20), (x - 20, y + h + 20)], fill=curtains)
        d.polygon([(x + w + 20, y - 20), (x + w * 0.68, y - 20), (x + w * 0.9, y + h + 20), (x + w + 20, y + h + 20)], fill=curtains)


def sedir(d, x, y, w, color=K):
    d.rounded_rectangle([x, y, x + w, y + 90], radius=16, fill=WOOD)
    d.rounded_rectangle([x + 10, y - 40, x + w - 10, y + 20], radius=20, fill=color)
    for i in range(3):
        cx = x + 60 + i * (w - 120) / 2
        d.rounded_rectangle([cx - 55, y - 120, cx + 55, y - 20], radius=24, fill=[S, L, Z][i])
        d.polygon([(cx, y - 100), (cx + 26, y - 70), (cx, y - 40), (cx - 26, y - 70)], fill=CREAM)


def chest(d, x, y, w=260, h=170):
    d.rounded_rectangle([x, y, x + w, y + h], radius=14, fill=WOOD)
    d.rectangle([x, y + 40, x + w, y + 56], fill=shade(WOOD, 0.7))
    for i in range(3): d.rectangle([x + 30 + i * (w - 60) / 2 - 8, y, x + 30 + i * (w - 60) / 2 + 8, y + h], fill=COPPER)
    d.rounded_rectangle([x + w / 2 - 22, y + 60, x + w / 2 + 22, y + 100], radius=6, fill=S)


def plant(d, x, y, flower=K, size=1.0):
    d.polygon([(x - 50 * size, y), (x + 50 * size, y), (x + 36 * size, y + 90 * size), (x - 36 * size, y + 90 * size)], fill=COPPER)
    for i in range(7):
        a = -math.pi / 2 + (i - 3) * 0.38
        ex, ey = x + math.cos(a) * 110 * size, y + math.sin(a) * 110 * size
        d.line([x, y, ex, ey], fill=Z, width=int(10 * size))
        d.ellipse([ex - 26 * size, ey - 26 * size, ex + 26 * size, ey + 26 * size], fill=flower)
        d.ellipse([ex - 9 * size, ey - 9 * size, ex + 9 * size, ey + 9 * size], fill=S)


def tree(d, x, y, size=1.0, fruit=None):
    d.rectangle([x - 26 * size, y - 220 * size, x + 26 * size, y], fill=WOOD)
    for dx, dy, r in ((0, -320, 170), (-120, -250, 120), (120, -250, 120), (-60, -400, 110), (70, -390, 110)):
        d.ellipse([x + (dx - r) * size, y + (dy - r) * size, x + (dx + r) * size, y + (dy + r) * size], fill=Z)
    if fruit:
        rnd = random.Random(int(x))
        for _ in range(9):
            fx, fy = x + rnd.uniform(-180, 180) * size, y + rnd.uniform(-460, -180) * size
            d.ellipse([fx - 14, fy - 14, fx + 14, fy + 14], fill=fruit)


def hills(d, base, colors=((160, 190, 110), (130, 165, 90))):
    for i, c in enumerate(colors):
        pts = [(0, SH)]
        for x in range(0, SW + 100, 100):
            pts.append((x, base + i * 60 + math.sin(x / 260 + i * 2) * 60))
        pts.append((SW, SH))
        d.polygon(pts, fill=c)


def sun(d, x, y, r=90):
    d.ellipse([x - r, y - r, x + r, y + r], fill=(250, 214, 120))


def stone_wall(d, x0, y0, x1, y1, color=STONE):
    d.rectangle([x0, y0, x1, y1], fill=color)
    rnd = random.Random(x0 * 7 + y0)
    for row in range(int((y1 - y0) / 50)):
        off = 0 if row % 2 == 0 else 50
        for x in range(int(x0) - off, int(x1), 100):
            d.rounded_rectangle([x + 4, y0 + row * 50 + 4, x + 96, y0 + row * 50 + 46], radius=12, fill=shade(color, rnd.uniform(0.85, 1.05)))


def fountain(d, x, y, flowing):
    stone_wall(d, x - 170, y - 300, x + 170, y)
    d.rounded_rectangle([x - 130, y - 380, x + 130, y - 290], radius=50, fill=shade(STONE, 0.9))
    d.rectangle([x - 12, y - 220, x + 60, y - 200], fill=COPPER)
    d.rounded_rectangle([x - 220, y - 10, x + 220, y + 70], radius=20, fill=shade(STONE, 0.8))
    if flowing:
        d.rectangle([x + 46, y - 200, x + 60, y], fill=WATER)
        d.rounded_rectangle([x - 200, y, x + 200, y + 30], radius=10, fill=WATER)


def loom(d, x, y, w, h, woven):
    d.rectangle([x - 30, y - 40, x + w + 30, y], fill=WOOD)
    d.rectangle([x - 30, y + h, x + w + 30, y + h + 40], fill=WOOD)
    d.rectangle([x - 30, y - 40, x, y + h + 120], fill=WOOD)
    d.rectangle([x + w, y - 40, x + w + 30, y + h + 120], fill=WOOD)
    for i in range(int(w / 22)): d.line([x + 11 + i * 22, y, x + 11 + i * 22, y + h], fill=CREAM, width=3)
    if woven: rug(d, x + 20, y + h * 0.35, w - 40, h * 0.6, K)


def yarn(d, x, y, colors):
    d.line([x - 40, y, x + 60 * len(colors), y], fill=WOOD, width=12)
    for i, c in enumerate(colors):
        cx = x + i * 60
        d.ellipse([cx - 24, y + 10, cx + 24, y + 110], fill=c)
        for k in range(4): d.line([cx - 20, y + 30 + k * 20, cx + 20, y + 26 + k * 20], fill=shade(c, 0.8), width=3)


def lamp(d, x, y, lit):
    if lit: d.ellipse([x - 140, y - 160, x + 140, y + 100], fill=(255, 232, 170))
    d.line([x, y - 200, x, y - 90], fill=DARK, width=6)
    d.rounded_rectangle([x - 34, y - 90, x + 34, y + 10], radius=20, fill=COPPER)
    d.ellipse([x - 20, y - 70, x + 20, y - 20], fill=(255, 220, 120) if lit else (120, 110, 100))


def stove(d, x, y, fire):
    stone_wall(d, x - 200, y - 320, x + 200, y, shade(STONE, 0.95))
    d.rounded_rectangle([x - 120, y - 200, x + 120, y], radius=60, fill=DARK)
    if fire:
        for i, c in enumerate(((240, 120, 40), (250, 190, 60))):
            r = 70 - i * 25
            d.polygon([(x - r, y - 10), (x, y - 140 + i * 30), (x + r, y - 10)], fill=c)


def shelf(d, x, y, w, items):
    d.rectangle([x, y, x + w, y + 18], fill=WOOD)
    for i, c in enumerate(items):
        cx = x + 50 + i * (w - 100) / max(1, len(items) - 1)
        d.ellipse([cx - 44, y - 90, cx + 44, y + 4], fill=c)
        d.rectangle([cx - 30, y - 110, cx + 30, y - 80], fill=shade(c, 0.85))


def table(d, x, y, w, cloth):
    d.rectangle([x + 20, y, x + 40, y + 160], fill=WOOD)
    d.rectangle([x + w - 40, y, x + w - 20, y + 160], fill=WOOD)
    d.rounded_rectangle([x, y - 20, x + w, y + 20], radius=10, fill=WOOD)
    if cloth:
        d.polygon([(x - 20, y - 24), (x + w + 20, y - 24), (x + w + 40, y + 80), (x - 40, y + 80)], fill=cloth)
        for i in range(int(w / 60)):
            cx = x + 30 + i * 60
            d.polygon([(cx, y + 10), (cx + 16, y + 30), (cx, y + 50), (cx - 16, y + 30)], fill=CREAM)


def pergola(d, x0, x1, y, vines):
    for x in (x0, x1): d.rectangle([x - 16, y, x + 16, y + 520], fill=WOOD)
    d.rectangle([x0 - 40, y - 20, x1 + 40, y + 10], fill=WOOD)
    if vines:
        rnd = random.Random(x0)
        for _ in range(40):
            cx, cy = rnd.uniform(x0 - 30, x1 + 30), y + rnd.uniform(-60, 80)
            d.ellipse([cx - 40, cy - 30, cx + 40, cy + 30], fill=rnd.choice([Z, (126, 146, 60)]))
        for _ in range(6):
            cx, cy = rnd.uniform(x0, x1), y + rnd.uniform(60, 130)
            for k in range(6): d.ellipse([cx - 14 + (k % 2) * 14, cy + k * 12, cx + 14 + (k % 2) * 14, cy + 24 + k * 12], fill=M)


def well(d, x, y, fixed):
    stone_wall(d, x - 150, y - 160, x + 150, y)
    if fixed:
        d.rectangle([x - 140, y - 400, x - 116, y - 160], fill=WOOD)
        d.rectangle([x + 116, y - 400, x + 140, y - 160], fill=WOOD)
        d.polygon([(x - 180, y - 390), (x, y - 480), (x + 180, y - 390)], fill=K)
        d.line([x, y - 380, x, y - 230], fill=DARK, width=4)
        d.rounded_rectangle([x - 30, y - 240, x + 30, y - 190], radius=8, fill=COPPER)


def gate(d, x, y, color):
    d.rectangle([x - 200, y - 380, x + 200, y], fill=shade(STONE, 0.9))
    d.rounded_rectangle([x - 150, y - 330, x + 150, y], radius=150, fill=color)
    d.line([x, y - 330, x, y], fill=shade(color, 0.7), width=8)
    d.ellipse([x + 20, y - 170, x + 44, y - 146], fill=S)


def fence(d, x0, x1, y):
    d.rectangle([x0, y - 70, x1, y - 56], fill=WOOD)
    d.rectangle([x0, y - 30, x1, y - 16], fill=WOOD)
    for x in range(int(x0), int(x1), 60): d.polygon([(x, y), (x, y - 100), (x + 12, y - 116), (x + 24, y - 100), (x + 24, y)], fill=WOOD)


def tent(d, x, y, w, h, color):
    d.polygon([(x - w / 2, y), (x, y - h), (x + w / 2, y)], fill=color)
    d.polygon([(x - w * 0.12, y), (x, y - h * 0.5), (x + w * 0.12, y)], fill=DARK)
    kilim_band(d, x - w * 0.36, y - h * 0.28, x + w * 0.36, 40)


def sheep(d, x, y):
    for dx, dy in ((-30, 0), (0, -20), (30, 0), (0, 10)):
        d.ellipse([x + dx - 50, y + dy - 40, x + dx + 50, y + dy + 40], fill=WHITE)
    d.ellipse([x + 60, y - 30, x + 110, y + 20], fill=DARK)
    for lx in (-40, 30): d.rectangle([x + lx, y + 30, x + lx + 14, y + 80], fill=DARK)


def barrel(d, x, y, c=WOOD):
    d.rounded_rectangle([x - 60, y - 140, x + 60, y], radius=40, fill=c)
    for k in (0.25, 0.75): d.rectangle([x - 60, y - 140 * k - 6, x + 60, y - 140 * k + 6], fill=shade(c, 0.6))


def pot(d, x, y, c=COPPER, size=1.0):
    d.ellipse([x - 70 * size, y - 150 * size, x + 70 * size, y], fill=c)
    d.rectangle([x - 34 * size, y - 180 * size, x + 34 * size, y - 130 * size], fill=shade(c, 0.85))


def column(d, x, y0, y1, c):
    d.rectangle([x - 26, y0, x + 26, y1], fill=c)
    d.rectangle([x - 40, y0 - 20, x + 40, y0 + 6], fill=shade(c, 0.85))
    d.rectangle([x - 40, y1 - 6, x + 40, y1 + 20], fill=shade(c, 0.85))


def pool(d, x, y, w, water, jet):
    d.ellipse([x - w / 2 - 30, y - 90, x + w / 2 + 30, y + 90], fill=STONE)
    d.ellipse([x - w / 2, y - 60, x + w / 2, y + 60], fill=WATER if water else (150, 140, 120))
    if jet:
        d.rectangle([x - 8, y - 260, x + 8, y], fill=(170, 210, 235))
        d.ellipse([x - 60, y - 300, x + 60, y - 230], fill=(190, 225, 240))


def creek(d, y, water):
    pts = [(0, y)] + [(x, y + math.sin(x / 200) * 40) for x in range(0, SW + 100, 100)] + [(SW, y + 140), (0, y + 140)]
    d.polygon(pts, fill=WATER if water else (160, 150, 130))


def flag(d, x, y, c=K):
    d.rectangle([x - 6, y - 320, x + 6, y], fill=WOOD)
    d.polygon([(x + 6, y - 316), (x + 170, y - 270), (x + 6, y - 220)], fill=c)


def swing(d, x, y):
    d.line([x - 60, y - 300, x - 60, y], fill=DARK, width=6)
    d.line([x + 60, y - 300, x + 60, y], fill=DARK, width=6)
    d.rounded_rectangle([x - 80, y, x + 80, y + 24], radius=8, fill=WOOD)


def scarecrow(d, x, y, hat):
    d.rectangle([x - 8, y - 300, x + 8, y], fill=WOOD)
    d.rectangle([x - 120, y - 240, x + 120, y - 226], fill=WOOD)
    d.ellipse([x - 40, y - 360, x + 40, y - 280], fill=SAND)
    d.polygon([(x - 80, y - 240), (x + 80, y - 240), (x + 60, y - 120), (x - 60, y - 120)], fill=L)
    d.polygon([(x - 70, y - 340), (x + 70, y - 340), (x, y - 420)], fill=hat)


def wall_room(d, wall=(232, 214, 186), floor=(196, 160, 118)):
    d.rectangle([0, 0, SW, SH * 0.72], fill=wall)
    d.rectangle([0, SH * 0.72, SW, SH], fill=floor)
    for x in range(0, SW, 160): d.line([x, SH * 0.72, x - 120, SH], fill=shade(floor, 0.9), width=4)


def outdoor(d, ground=GRASS):
    d.rectangle([0, 0, SW, SH], fill=SKY)
    sun(d, 1550, 170)
    hills(d, 560)
    d.rectangle([0, SH * 0.78, SW, SH], fill=ground)


def scene(key, new):
    img = Image.new('RGB', (SW, SH), SKY)
    d = ImageDraw.Draw(img)
    random.seed(key)
    if key == 'oda':
        wall_room(d); kilim_band(d, 0, 80, SW, 60)
        window(d, 220, 230, 380, 320, frame=L if new else BROWN, curtains=K if new else None)
        sedir(d, 760, 640, 820, K if new else BROWN)
        if new: rug(d, 300, 820, 1200, 160, K)
        chest(d, 330, 600) if new else chest(d, 330, 640, 240, 130)
    elif key == 'mutfak':
        wall_room(d, (238, 222, 196)); stove(d, 380, 760, new)
        shelf(d, 820, 330, 760, [COPPER, COPPER, S, COPPER] if new else [STONE, STONE])
        table(d, 860, 640, 640, (200, 80, 70) if new else None)
        if new: pot(d, 1200, 610, COPPER, 0.6)
    elif key == 'avlu':
        outdoor(d, (200, 180, 150) if new else (170, 160, 140))
        if new:
            for r in range(4):
                for c in range(18): d.rounded_rectangle([c * 100 + (r % 2) * 50, 820 + r * 56, c * 100 + 92 + (r % 2) * 50, 868 + r * 56], radius=10, fill=shade(STONE, 1.05 - 0.04 * (c % 3)))
        fountain(d, 400, 820, new)
        pergola(d, 900, 1600, 300, new)
        if new:
            for i, c in enumerate((K, (230, 120, 150), WHITE)): plant(d, 1000 + i * 220, 760, c, 0.8)
    elif key == 'tezgah':
        wall_room(d, (226, 206, 176)); loom(d, 300, 260, 620, 420, new)
        yarn(d, 1120, 220, [K, L, S, Z, M] if new else [STONE, STONE])
        if new: rug(d, 1100, 460, 520, 260, L)
        lamp(d, 1500, 640, new)
    elif key == 'bahce':
        outdoor(d); gate(d, 300, 840, L if new else BROWN)
        tree(d, 900, 860, 1.1, (140, 100, 60) if new else None)
        well(d, 1400, 860, new)
        if new: fence(d, 520, 760, 860); sedir(d, 1100, 900, 420, K)
    elif key == 'yol':
        outdoor(d)
        for i in range(10): d.ellipse([700 + i * 30 - 80, 820 + i * 22, 700 + i * 30 + 80, 860 + i * 22], fill=(200, 180, 140) if new else (150, 140, 120))
        stone_wall(d, 0, 700, 560, 860) if new else stone_wall(d, 0, 780, 300, 860)
        gate(d, 1250, 840, WOOD if new else BROWN)
        if new:
            for x in (1500, 1640): tree(d, x, 820, 0.6)
    elif key == 'bag':
        outdoor(d, (170, 150, 100))
        for row in range(3):
            y = 700 + row * 110
            d.line([0, y, SW, y], fill=WOOD, width=8)
            rnd = random.Random(row)
            for x in range(60, SW, 140):
                d.ellipse([x - 60, y - 70, x + 60, y + 10], fill=Z if new else (130, 120, 90))
                if new: [d.ellipse([x - 12 + (k % 2) * 14, y + k * 10, x + 12 + (k % 2) * 14, y + 20 + k * 10], fill=M) for k in range(5)]
        scarecrow(d, 1500, 680, K if new else BROWN) if new else None
    elif key == 'pekmez':
        wall_room(d, (226, 210, 180)); stove(d, 1300, 760, new)
        d.rounded_rectangle([200, 640, 820, 820], radius=20, fill=WOOD)
        if new: d.rounded_rectangle([230, 650, 790, 720], radius=20, fill=M)
        for i in range(4): pot(d, 300 + i * 160, 960, (150, 70, 60) if new else STONE, 0.7) if new or i < 2 else None
    elif key == 'seki':
        outdoor(d)
        d.rounded_rectangle([300, 640, 1500, 900], radius=20, fill=(220, 200, 170) if new else (170, 160, 140))
        if new:
            rug(d, 360, 600, 1080, 90, K)
            pergola(d, 360, 1440, 260, True)
            pot(d, 1300, 600, COPPER, 0.5)
    elif key == 'ambar':
        outdoor(d, (180, 160, 110))
        d.rectangle([450, 360, 1350, 860], fill=WOOD if new else (120, 100, 80))
        d.polygon([(400, 380), (900, 140 if new else 200), (1400, 380)], fill=K if new else (110, 90, 70))
        d.rectangle([780, 560, 1020, 860], fill=shade(WOOD, 0.7))
        if new:
            for i in range(4): d.rounded_rectangle([1420 + (i % 2) * 120, 880 - (i // 2) * 120, 1520 + (i % 2) * 120, 980 - (i // 2) * 120], radius=30, fill=SAND)
            d.ellipse([870, 400, 930, 460], fill=L)
    elif key == 'oba':
        outdoor(d, (150, 180, 100))
        tent(d, 700, 860, 900, 560, DARK if new else (90, 80, 70))
        if new: rug(d, 1200, 820, 420, 130, K); tent(d, 1550, 860, 420, 300, (70, 60, 55))
    elif key == 'ahir':
        outdoor(d, (170, 160, 110))
        d.rectangle([200, 400, 900, 860], fill=WOOD if new else (110, 90, 70))
        d.polygon([(160, 420), (550, 220), (940, 420)], fill=BROWN)
        if new:
            for x, y in ((1150, 880), (1380, 900), (1600, 870)): sheep(d, x, y)
            for i in range(3): d.rounded_rectangle([1000 + i * 70, 760 - i * 60, 1200 + i * 70, 860 - i * 60], radius=20, fill=S)
    elif key == 'ocak':
        outdoor(d, (160, 170, 110)); stove(d, 600, 860, new)
        if new:
            d.ellipse([480, 520, 720, 560], fill=DARK)
            pot(d, 1200, 880, COPPER, 0.9); barrel(d, 1450, 880, SAND)
    elif key == 'cayir':
        outdoor(d, (150, 190, 100))
        if new:
            rnd = random.Random(3)
            for _ in range(90):
                x, y = rnd.uniform(0, SW), rnd.uniform(820, SH)
                d.ellipse([x - 12, y - 12, x + 12, y + 12], fill=rnd.choice([K, WHITE, M, S]))
            fence(d, 0, 700, 900); swing(d, 1300, 640); flag(d, 1650, 860, K)
        else:
            fence(d, 0, 300, 900)
    elif key == 'kaynak':
        outdoor(d, (140, 180, 100)); creek(d, 780, new)
        stone_wall(d, 160, 560, 520, 800)
        if new:
            d.rectangle([330, 680, 350, 800], fill=WATER)
            d.arc([800, 620, 1300, 940], 180, 360, fill=STONE, width=60)
            tree(d, 1600, 860, 0.7)
            for i, c in enumerate((K, WHITE, Z)): d.rectangle([1560 + i * 30, 520, 1580 + i * 30, 600], fill=c)
    elif key == 'hayat':
        wall_room(d, (236, 222, 200), (170, 130, 90))
        for x in (300, 700, 1100, 1500): column(d, x, 200, 760, WHITE if new else (190, 180, 170))
        d.rectangle([0, 160, SW, 200], fill=WOOD)
        if new:
            d.line([900, 200, 900, 330], fill=DARK, width=4)
            d.rounded_rectangle([860, 330, 940, 430], radius=20, outline=COPPER, width=6)
            d.ellipse([885, 375, 915, 405], fill=S)
            rug(d, 400, 840, 1000, 140, L)
    elif key == 'basoda':
        wall_room(d, (240, 226, 200)); kilim_band(d, 0, 40, SW, 50, (WOOD, S, K, CREAM))
        d.ellipse([750, 60, 1050, 160], fill=S if new else STONE)
        stove(d, 1450, 760, new)
        if new: rug(d, 200, 780, 1100, 200, K)
        d.rectangle([150, 260, 650, 700], fill=WOOD)
    elif key == 'selamlik':
        wall_room(d, (232, 218, 196))
        sedir(d, 200, 680, 1400, K if new else BROWN)
        if new:
            rug(d, 500, 180, 800, 320, L)
            table(d, 760, 860, 280, None); pot(d, 900, 820, COPPER, 0.35)
            lamp(d, 260, 380, True); lamp(d, 1540, 380, True)
    elif key == 'kapi':
        outdoor(d, (200, 190, 170))
        d.rectangle([300, 200, 1500, 860], fill=(236, 222, 200) if new else (180, 170, 160))
        gate(d, 900, 860, WOOD if new else BROWN)
        if new:
            d.rounded_rectangle([700, 230, 1100, 320], radius=14, fill=K)
            lamp(d, 560, 560, True); lamp(d, 1240, 560, True)
    elif key == 'havuz':
        outdoor(d, (170, 190, 120)); pool(d, 900, 860, 900, new, new)
        if new:
            for i, c in enumerate((K, (230, 130, 160), S)): plant(d, 260 + i * 160, 860, c, 0.7)
            rug(d, 1300, 720, 400, 200, K)
    else:
        wall_room(d)
    if not new:
        img = weather(img, key)
    return img


def weather(img, key):
    """Bakımsız hâl: soluk, kahverengi, çatlak ve örümcek ağı."""
    gray = ImageOps.grayscale(img).convert('RGB')
    img = Image.blend(img, gray, 0.72)
    img = Image.blend(img, Image.new('RGB', img.size, (120, 100, 80)), 0.22)
    img = ImageEnhance.Brightness(img).enhance(0.86)
    d = ImageDraw.Draw(img)
    rnd = random.Random(key + 'old')
    for _ in range(9):
        x, y = rnd.uniform(100, SW - 100), rnd.uniform(80, SH * 0.7)
        pts = [(x, y)]
        for _ in range(5):
            x += rnd.uniform(-60, 60); y += rnd.uniform(20, 70); pts.append((x, y))
        d.line(pts, fill=(80, 70, 60), width=4)
    for cx, cy, flip in ((0, 0, 1), (SW, 0, -1)):
        for k in range(1, 5):
            r = k * 45
            d.arc([cx - r, cy - r, cx + r, cy + r], 0 if flip > 0 else 90, 90 if flip > 0 else 180, fill=(235, 235, 230), width=3)
        for a in range(0, 91, 22):
            ang = math.radians(a if flip > 0 else 180 - a)
            d.line([cx, cy, cx + math.cos(ang) * 200, cy + math.sin(ang) * 200], fill=(235, 235, 230), width=3)
    return img


AREAS = ['oda', 'mutfak', 'avlu', 'tezgah', 'bahce', 'yol', 'bag', 'pekmez', 'seki', 'ambar',
         'oba', 'ahir', 'ocak', 'cayir', 'kaynak', 'hayat', 'basoda', 'selamlik', 'kapi', 'havuz']


def logo():
    n = 1024
    img = Image.new('RGBA', (n, n), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    c = n / 2
    layers = [(0.48, K), (0.4, CREAM), (0.34, L), (0.26, S), (0.18, K), (0.1, CREAM)]
    for r, col in layers:
        R = n * r
        d.polygon([(c, c - R), (c + R, c), (c, c + R), (c - R, c)], fill=col)
    for k in range(4):
        a = k * math.pi / 2
        x, y = c + math.cos(a) * n * 0.36, c + math.sin(a) * n * 0.36
        d.polygon([(x, y - 40), (x + 40, y), (x, y + 40), (x - 40, y)], fill=Z)
    save(img, 'logo', (512, 512))


def bubble():
    n = 256
    img = Image.new('RGBA', (n, n), (0, 0, 0, 0))
    ImageDraw.Draw(img).rounded_rectangle([0, 0, n - 1, n - 1], radius=56, fill=WHITE)
    save(img, 'bubble')


def intro_pages():
    nine = Image.open(os.path.join(ART, 'nine.png')).convert('RGBA')
    # 1: bakımsız ev + Nine
    p1 = scene('oda', False).convert('RGBA')
    p1.alpha_composite(nine.resize((520, 520)), (1180, 420))
    save(p1.convert('RGB'), 'intro_1', (1024, 512))
    # 2: tezgâh + renkli parçalar
    p2 = Image.new('RGB', (SW, SH), SAND)
    d = ImageDraw.Draw(p2)
    d.rounded_rectangle([200, 120, 1100, 940], radius=30, fill=BROWN)
    cols = [K, L, S, Z]
    rnd = random.Random(2)
    for r in range(6):
        for c in range(6):
            x, y = 240 + c * 142, 160 + r * 128
            fill = rnd.choice(cols) if r * 6 + c < 22 else (234, 223, 203)
            d.rounded_rectangle([x, y, x + 126, y + 112], radius=18, fill=fill)
    for i, (shape, col) in enumerate((([(0, 0), (1, 0), (2, 0)], K), ([(0, 0), (0, 1), (1, 1)], L), ([(0, 0), (1, 0)], S))):
        ox, oy = 1260 + (i % 2) * 250, 180 + i * 260
        for cx, cy in shape: d.rounded_rectangle([ox + cx * 100, oy + cy * 100, ox + cx * 100 + 90, oy + cy * 100 + 90], radius=16, fill=col)
    save(p2, 'intro_2', (1024, 512))
    # 3: desen + yıldızlar
    p3 = Image.new('RGB', (SW, SH), CREAM)
    d = ImageDraw.Draw(p3)
    rug(d, 260, 260, 1280, 520, K)
    for i in range(3): star(d, 600 + i * 300, 160 if i == 1 else 190, 110 if i == 1 else 90, 45, S)
    save(p3, 'intro_3', (1024, 512))
    # 4: onarılmış ev + Nine
    p4 = scene('bahce', True).convert('RGBA')
    p4.alpha_composite(nine.resize((520, 520)), (80, 440))
    save(p4.convert('RGB'), 'intro_4', (1024, 512))


def lock_white():
    icon('lock_tmp', i_lock)
    os.replace(os.path.join(ART, 'icon_lock_tmp.png'), os.path.join(ART, 'lock.png'))


if __name__ == '__main__':
    for name, fn in ICONS.items(): icon(name, fn)
    lock_white()
    logo()
    bubble()
    for key in AREAS:
        save(scene(key, False), f'area_{key}_old', (1024, 512))
        save(scene(key, True), f'area_{key}_new', (1024, 512))
    intro_pages()
    print('ok', len(ICONS), 'icons,', len(AREAS), 'areas')
