"""Mağaza vitrini: telefon ekran görüntüleri (iOS 6.7" 1290x2796, Play 1080x1920), Play tanıtım görseli 1024x500, simge 512.
Çıktı: Tools/store/<dil>/... Kendi sanatımız + gerçek bölüm desenleri; telifli içerik yok."""
import json, os
from PIL import Image, ImageDraw, ImageFont, ImageFilter

R = 'Unity/Kilim/Assets/Kilim/Resources/'
ART = R + 'art/'
F = R + 'fonts/KilimSans-Bold.ttf'
OUT = 'Tools/store'
COL = {'K': (190, 52, 40), 'L': (38, 62, 120), 'S': (226, 170, 52), 'Z': (88, 126, 56), 'M': (118, 42, 88)}
RED, CREAM, INK = (156, 38, 30), (251, 244, 232), (52, 34, 24)
CAP = {
    'tr': ["Anadolu desenlerini\nparça parça doku", "Nine'nin evini\nyeniden canlandır", "4 ev · 20 mekân\n160 el yapımı bölüm",
           "Her gün yeni desen\nhaftalık yarış", "Sakin ve keyifli\nAnadolu ezgileriyle"],
    'en': ["Weave Anatolian\npatterns piece by piece", "Bring Grandma's\nhome back to life", "4 homes · 20 rooms\n160 handcrafted levels",
           "A new pattern daily\nweekly races", "Calm and cozy,\nwith Anatolian melodies"],
}
SUB = {'tr': "Kilim · Desen Bulmacası", 'en': "Kilim · Pattern Puzzle"}

def font(s): return ImageFont.truetype(F, s)
def img(n, w=None):
    i = Image.open(ART + n).convert('RGBA')
    return i.resize((w, int(i.height * w / i.width)), Image.LANCZOS) if w else i

def bg(W, H):
    b = Image.new('RGB', (W, H), CREAM); d = ImageDraw.Draw(b)
    for y in range(H):
        t = y / H; d.line([(0, y), (W, y)], fill=tuple(int(RED[k] * (1 - t) * .9 + CREAM[k] * (.1 + t * .9)) for k in range(3)))
    pat = Image.open(ART + 'pattern_bg.png').convert('RGBA').resize((W // 3, W // 3))
    layer = Image.new('RGBA', (W, H))
    for x in range(0, W, pat.width):
        for y in range(0, H, pat.height): layer.alpha_composite(pat, (x, y))
    layer.putalpha(layer.getchannel('A').point(lambda a: a * 0.08))
    b = b.convert('RGBA'); b.alpha_composite(layer); return b

def caption(c, text, W, top):
    d = ImageDraw.Draw(c); f = font(int(W * 0.075)); y = top
    for line in text.split('\n'):
        w = d.textlength(line, font=f)
        d.text(((W - w) / 2 + 3, y + 4), line, font=f, fill=(0, 0, 0, 90)); d.text(((W - w) / 2, y), line, font=f, fill=(255, 255, 255))
        y += int(W * 0.088)
    return y

def card(c, im, box, r=48):
    x0, y0, x1, y1 = box; im = im.resize((x1 - x0, y1 - y0), Image.LANCZOS)
    m = Image.new('L', im.size, 0); ImageDraw.Draw(m).rounded_rectangle([0, 0, im.width, im.height], r, fill=255)
    sh = Image.new('RGBA', c.size); ImageDraw.Draw(sh).rounded_rectangle([x0 + 10, y0 + 24, x1 + 10, y1 + 24], r, fill=(0, 0, 0, 110))
    c.alpha_composite(sh.filter(ImageFilter.GaussianBlur(22))); c.paste(im, (x0, y0), m)

def board(level, px):
    n = level['size']; cell = px // (n + 1); pad = (px - cell * n) // 2
    b = Image.new('RGBA', (px, px), (0, 0, 0, 0)); d = ImageDraw.Draw(b)
    d.rounded_rectangle([0, 0, px - 1, px - 1], px // 16, fill=(118, 72, 38), outline=(70, 40, 22), width=max(4, px // 90))
    tile = Image.open(ART + 'tile.png').convert('RGBA').resize((cell - 6, cell - 6))
    for r, row in enumerate(level['motif']):
        for c_, v in enumerate(row):
            x, y = pad + c_ * cell, pad + r * cell
            if not v: d.rounded_rectangle([x + 3, y + 3, x + cell - 3, y + cell - 3], 10, fill=(60, 40, 28)); continue
            t = Image.new('RGBA', tile.size, COL.get(v, (200, 200, 200)) + (255,))
            t = Image.blend(t, tile, .25); t.putalpha(tile.getchannel('A')); b.alpha_composite(t, (x + 3, y + 3))
    return b

def phone(lang, W, H):
    lv = {l['id']: l for l in json.load(open('Data/levels/region1.json'))}
    outs = []
    for i, cap in enumerate(CAP[lang]):
        c = bg(W, H); y = caption(c, cap, W, int(H * .06)); top = y + int(H * .03); m = int(W * .07)
        if i == 0:
            bw = W - 2 * m; c.alpha_composite(board(lv[13], bw), (m, top + int(H * .03)))
            tray = int(H * .14); yy = top + bw + int(H * .06)
            for k in range(4):
                bx = m + k * (bw // 4); ImageDraw.Draw(c).rounded_rectangle([bx + 8, yy, bx + bw // 4 - 8, yy + tray], 28, fill=(244, 232, 210))
                shape = [[(0, 0), (1, 0), (2, 0)], [(0, 0), (0, 1), (1, 0), (1, 1)], [(0, 0), (1, 0), (1, 1)], [(0, 0), (0, 1), (0, 2)]][k]
                colr = COL['KSLK'[k]]; cs = tray // 4; ox = bx + (bw // 4 - cs * 3) // 2 + cs // 2; oy = yy + (tray - cs * 3) // 2 + cs // 2
                for (r_, c2) in shape:
                    ImageDraw.Draw(c).rounded_rectangle([ox + c2 * cs, oy + r_ * cs, ox + c2 * cs + cs - 6, oy + r_ * cs + cs - 6], 10, fill=colr)
        elif i == 1:
            h2 = int((H - top - H * .06) / 2) - 20
            card(c, img('area_oda_old.png'), (m, top, W - m, top + h2)); card(c, img('area_oda_new.png'), (m, top + h2 + 40, W - m, top + 2 * h2 + 40))
            nine = img('nine.png', int(W * .34)); c.alpha_composite(nine, (W - m - nine.width + 20, top + 2 * h2 - nine.height + 60))
        elif i == 2:
            keys = ['avlu', 'bahce', 'bag', 'seki', 'oba', 'havuz']; cw = (W - 2 * m - 30) // 2; ch = cw // 2
            for k, key in enumerate(keys):
                x = m + (k % 2) * (cw + 30); yy = top + (k // 2) * (ch + 40)
                card(c, img(f'area_{key}_new.png'), (x, yy, x + cw, yy + ch), 36)
            c.alpha_composite(board(lv[20], int(W * .5)), (int(W * .25), top + 3 * (ch + 40) + 20))
        elif i == 3:
            d = ImageDraw.Draw(c); f = font(int(W * .05))
            for k, (ic, t) in enumerate([('icon_daily.png', 'Günlük desen' if lang == 'tr' else 'Daily pattern'),
                                         ('icon_race.png', 'Haftalık yarış' if lang == 'tr' else 'Weekly race'),
                                         ('icon_pass.png', 'Sezon ödülleri' if lang == 'tr' else 'Season rewards')]):
                yy = top + k * int(H * .1); d.rounded_rectangle([m, yy, W - m, yy + int(H * .085)], 40, fill=(255, 250, 240))
                ico = img(ic, int(H * .06)); tint = Image.new('RGBA', ico.size, RED + (255,)); tint.putalpha(ico.getchannel('A')); c.alpha_composite(tint, (m + 30, yy + int(H * .012))); d.text((m + 60 + int(H * .06), yy + int(H * .025)), t, font=f, fill=INK)
            c.alpha_composite(board(lv[8], int(W * .7)), (int(W * .15), top + int(H * .33)))
        else:
            card(c, img('intro_4.png'), (m, top, W - m, top + int((W - 2 * m) * .5))); yy = top + int((W - 2 * m) * .5) + 50
            card(c, img('area_mutfak_new.png'), (m, yy, W - m, yy + int((W - 2 * m) * .5)))
        d = ImageDraw.Draw(c); f = font(int(W * .04)); s = SUB[lang]; w = d.textlength(s, font=f)
        d.text(((W - w) / 2, H - int(H * .05)), s, font=f, fill=RED)
        outs.append(c.convert('RGB'))
    return outs

def feature(lang):
    c = bg(1024, 500).resize((1024, 500)); sc = img('area_bahce_new.png', 560)
    card(c, sc, (440, 60, 1000, 340), 28)
    lv = json.load(open('Data/levels/region1.json'))[12]; c.alpha_composite(board(lv, 300), (620, 180))
    logo = img('logo.png', 200); c.alpha_composite(logo, (60, 40))
    d = ImageDraw.Draw(c); d.text((60, 250), 'KİLİM', font=font(110), fill=(255, 255, 255))
    d.text((64, 380), 'Desen Bulmacası' if lang == 'tr' else 'Pattern Puzzle', font=font(44), fill=INK)
    return c.convert('RGB')

for lang in ('tr', 'en'):
    os.makedirs(f'{OUT}/{lang}', exist_ok=True)
    for k, im in enumerate(phone(lang, 1290, 2796)): im.save(f'{OUT}/{lang}/ios_{k + 1}.png')
    for k, im in enumerate(phone(lang, 1080, 1920)): im.save(f'{OUT}/{lang}/play_{k + 1}.png')
    feature(lang).save(f'{OUT}/{lang}/play_feature.png')
Image.open('Unity/Kilim/Assets/Kilim/Art/icon_1024.png').convert('RGB').resize((512, 512), Image.LANCZOS).save(f'{OUT}/icon_512.png')
print('ok')
