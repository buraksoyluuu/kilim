"""Kilim ses ve görsel varlık üretici (deterministik, telifsiz; her şey koddan sentezlenir).
Kullanım: python Tools/gen_assets.py   → Unity/Kilim/Assets/Kilim/Resources/{audio,art}/ ve Art/icon_1024.png
Sesler: Karplus-Strong telli çalgı (bağlama benzeri) + sinüs çan/drone. Görseller: PIL, 4× çizip küçültme (yumuşak kenar)."""
import math
import os
import wave

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.join(os.path.dirname(__file__), '..', 'Unity', 'Kilim', 'Assets', 'Kilim')
AUDIO = os.path.join(ROOT, 'Resources', 'audio')
ART = os.path.join(ROOT, 'Resources', 'art')
ICON = os.path.join(ROOT, 'Art')
SR = 44100
rng = np.random.default_rng(7)


# ----------------------------------------------------------------- ses

def save_wav(name, x, sr=SR):
    x = np.asarray(x, dtype=np.float64)
    peak = np.max(np.abs(x)) or 1.0
    x = x / peak * 0.89
    fade = min(len(x) // 20, int(sr * 0.01))
    if fade > 0:
        x[-fade:] *= np.linspace(1, 0, fade)
    data = (x * 32767).astype('<i2')
    os.makedirs(AUDIO, exist_ok=True)
    with wave.open(os.path.join(AUDIO, name + '.wav'), 'wb') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(sr)
        w.writeframes(data.tobytes())


def t(sec, sr=SR):
    return np.arange(int(sec * sr)) / sr


def env(n, a=0.005, d=0.3, sr=SR):
    tt = np.arange(n) / sr
    e = np.minimum(1, tt / max(a, 1e-4)) * np.exp(-tt / max(d, 1e-4))
    return e


def pluck(freq, sec, bright=0.5, sr=SR, decay=0.996):
    """Karplus-Strong: bağlama benzeri telli tını."""
    n = int(sec * sr)
    period = max(2, int(sr / freq))
    buf = rng.uniform(-1, 1, period) * bright + rng.uniform(-1, 1, period) * (1 - bright) * 0.3
    out = np.zeros(n)
    for i in range(n):
        out[i] = buf[i % period]
        buf[i % period] = decay * 0.5 * (buf[i % period] + buf[(i + 1) % period])
    return out * env(n, 0.002, sec * 0.6, sr)


def bell(freq, sec, sr=SR):
    tt = t(sec, sr)
    x = (np.sin(2 * np.pi * freq * tt) + 0.4 * np.sin(2 * np.pi * freq * 2.76 * tt) * np.exp(-tt * 6)
         + 0.2 * np.sin(2 * np.pi * freq * 5.4 * tt) * np.exp(-tt * 10))
    return x * env(len(tt), 0.003, sec * 0.35, sr)


def mix(length_sec, parts, sr=SR):
    out = np.zeros(int(length_sec * sr))
    for start, x in parts:
        i = int(start * sr)
        j = min(len(out), i + len(x))
        out[i:j] += x[:j - i]
    return out


def note(semitones_from_a4):
    return 440.0 * 2 ** (semitones_from_a4 / 12)


# Re (D) dorian / hüseyni çağrışımlı dizi
D4, E4, F4, G4, A4, B4, C5, D5, E5, F5, A5 = (note(s) for s in (-7, -5, -4, -2, 0, 2, 3, 5, 7, 8, 12))


def gen_audio():
    tt = t(0.04)
    click = rng.uniform(-1, 1, len(tt)) * env(len(tt), 0.0005, 0.006)
    save_wav('click', np.convolve(click, np.ones(6) / 6, 'same'))

    tt = t(0.09)
    f = np.linspace(620, 900, len(tt))
    save_wav('rotate', np.sin(2 * np.pi * np.cumsum(f) / SR) * env(len(tt), 0.002, 0.03) * 0.6)

    save_wav('place', pluck(196, 0.22, bright=0.25, decay=0.985) + 0.5 * pluck(98, 0.22, bright=0.2, decay=0.98))
    save_wav('match', mix(0.45, [(0, bell(note(19), 0.4)), (0.07, bell(note(26), 0.38) * 0.7)]))
    tt = t(0.14)
    save_wav('invalid', np.sin(2 * np.pi * 110 * tt) * env(len(tt), 0.003, 0.05))
    for i, s in enumerate((15, 19, 22)):
        save_wav(f'star{i + 1}', bell(note(s), 0.6))
    save_wav('coin', mix(0.3, [(0, bell(note(27), 0.2)), (0.06, bell(note(31), 0.24))]))
    arp = [D4, F4, A4, C5, D5, E5, F5, A5]
    save_wav('win', mix(2.0, [(i * 0.11, pluck(f, 1.4, 0.6)) for i, f in enumerate(arp)] + [(0.95, bell(note(17), 1.0) * 0.6)]))
    save_wav('fail', mix(1.0, [(0, pluck(A4, 0.7, 0.4)), (0.22, pluck(F4, 0.75, 0.35))]))
    save_wav('unlock', mix(0.8, [(0, bell(note(10), 0.6)), (0.12, bell(note(17), 0.6)), (0.24, bell(note(22), 0.7))]))

    # Müzik: 48 sn kesintisiz döngü, 76 BPM; drone + yavaş telli melodi. 22,05 kHz (boyut için).
    sr = 22050
    bpm, bars = 76, 15
    beat = 60 / bpm
    length = bars * 4 * beat
    n = int(length * sr)
    tt = np.arange(n) / sr
    lfo = 0.75 + 0.25 * np.sin(2 * np.pi * tt / length * 2)  # döngüyle hizalı
    drone = (np.sin(2 * np.pi * 73.42 * tt) + 0.6 * np.sin(2 * np.pi * 110 * tt) + 0.25 * np.sin(2 * np.pi * 146.83 * tt)) * lfo
    music = drone * 0.22
    scale = [D4, E4, F4, G4, A4, C5, D5]
    mrng = np.random.default_rng(11)
    idx = 3
    for b in range(bars * 4 - 2):              # son 2 vuruş boş: döngü sınırında tını kuyruğu kalmaz
        if mrng.random() < 0.62:
            idx = int(np.clip(idx + mrng.choice([-2, -1, -1, 0, 1, 1, 2]), 0, len(scale) - 1))
            x = pluck(scale[idx] / 2 if mrng.random() < 0.2 else scale[idx], 1.6, 0.45, sr, 0.997) * 0.55
            i = int(b * beat * sr)
            j = min(n, i + len(x))
            music[i:j] += x[:j - i]
    save_wav('music_loop', music, sr)


# ----------------------------------------------------------------- görsel

S = 4  # süper örnekleme


def canvas(w, h):
    return Image.new('RGBA', (w * S, h * S), (0, 0, 0, 0))


def finish(img, w, h, name, folder=ART):
    os.makedirs(folder, exist_ok=True)
    img.resize((w, h), Image.LANCZOS).save(os.path.join(folder, name + '.png'))


def rounded(size, radius, fill=(255, 255, 255, 255), bevel=True, name='tile'):
    img = canvas(size, size)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([0, 0, size * S - 1, size * S - 1], radius * S, fill=fill)
    if bevel:  # üstte hafif açık, altta hafif koyu (dokuma kabartması)
        sh = canvas(size, size)
        sd = ImageDraw.Draw(sh)
        sd.rounded_rectangle([0, size * S * 0.88, size * S - 1, size * S - 1], radius * S, fill=(0, 0, 0, 40))
        sd.rounded_rectangle([0, 0, size * S - 1, size * S * 0.12], radius * S, fill=(255, 255, 255, 60))
        mask = img.split()[3]
        img = Image.alpha_composite(img, Image.composite(sh, Image.new('RGBA', sh.size, (0, 0, 0, 0)), mask))
    finish(img, size, size, name)


def outline(size, radius, width, name):
    img = canvas(size, size)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([0, 0, size * S - 1, size * S - 1], radius * S, outline=(255, 255, 255, 255), width=width * S)
    finish(img, size, size, name)


def motif(name, draw_fn, size=128):
    img = canvas(size, size)
    d = ImageDraw.Draw(img)
    draw_fn(d, size * S)
    finish(img, size, size, 'motif_' + name)


def star_points(cx, cy, r1, r2, n=5, rot=-math.pi / 2):
    pts = []
    for i in range(n * 2):
        r = r1 if i % 2 == 0 else r2
        a = rot + i * math.pi / n
        pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    return pts


def kilim_diamond(d, cx, cy, r, colors):
    """İç içe baklava + kenar kancaları (elibelinde/göz motifi çağrışımı)."""
    for k, col in enumerate(colors):
        rr = r * (1 - k * 0.28)
        d.polygon([(cx, cy - rr), (cx + rr, cy), (cx, cy + rr), (cx - rr, cy)], fill=col)
    hook = r * 0.22
    for sx, sy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        hx, hy = cx + sx * (r + hook * 0.6), cy + sy * (r + hook * 0.6)
        d.rectangle([hx - hook / 2, hy - hook / 2, hx + hook / 2, hy + hook / 2], fill=colors[0])


def gen_art():
    rounded(128, 26, name='tile')
    rounded(128, 26, bevel=False, name='tile_flat')
    outline(128, 26, 7, 'tile_outline')
    rounded(128, 40, name='pill')

    m = 0.2
    motif('circle', lambda d, s: d.ellipse([s * m, s * m, s * (1 - m), s * (1 - m)], fill='white'))
    motif('square', lambda d, s: d.rectangle([s * m, s * m, s * (1 - m), s * (1 - m)], fill='white'))
    motif('triangle', lambda d, s: d.polygon([(s / 2, s * m), (s * (1 - m), s * (1 - m)), (s * m, s * (1 - m))], fill='white'))
    motif('diamond', lambda d, s: d.polygon([(s / 2, s * 0.12), (s * 0.88, s / 2), (s / 2, s * 0.88), (s * 0.12, s / 2)], fill='white'))

    def cross(d, s):
        w = s * 0.2
        d.rectangle([s / 2 - w / 2, s * 0.15, s / 2 + w / 2, s * 0.85], fill='white')
        d.rectangle([s * 0.15, s / 2 - w / 2, s * 0.85, s / 2 + w / 2], fill='white')
    motif('cross', cross)

    # yıldız
    img = canvas(256, 256)
    ImageDraw.Draw(img).polygon(star_points(128 * S, 136 * S, 120 * S, 52 * S), fill='white')
    finish(img, 256, 256, 'star')

    # coin: altın disk + baklava
    img = canvas(128, 128)
    d = ImageDraw.Draw(img)
    d.ellipse([4 * S, 4 * S, 124 * S, 124 * S], fill=(217, 164, 65, 255))
    d.ellipse([14 * S, 14 * S, 114 * S, 114 * S], fill=(236, 192, 98, 255))
    kilim_diamond(d, 64 * S, 64 * S, 30 * S, [(178, 58, 46, 255), (236, 192, 98, 255), (178, 58, 46, 255)])
    finish(img, 128, 128, 'coin')

    # ilmek (makara)
    img = canvas(128, 128)
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([20 * S, 10 * S, 108 * S, 26 * S], 6 * S, fill=(120, 82, 54, 255))
    d.rounded_rectangle([20 * S, 102 * S, 108 * S, 118 * S], 6 * S, fill=(120, 82, 54, 255))
    d.rectangle([34 * S, 26 * S, 94 * S, 102 * S], fill=(178, 58, 46, 255))
    for y in range(30, 100, 9):
        d.line([34 * S, y * S, 94 * S, (y + 6) * S], fill=(214, 96, 80, 255), width=3 * S)
    finish(img, 128, 128, 'ilmek')

    # kilit
    img = canvas(128, 128)
    d = ImageDraw.Draw(img)
    d.arc([36 * S, 14 * S, 92 * S, 74 * S], 180, 360, fill='white', width=12 * S)
    d.rectangle([36 * S, 42 * S, 48 * S, 60 * S], fill='white')
    d.rectangle([80 * S, 42 * S, 92 * S, 60 * S], fill='white')
    d.rounded_rectangle([24 * S, 56 * S, 104 * S, 118 * S], 12 * S, fill='white')
    finish(img, 128, 128, 'lock')

    # Arka plan: 512 dikişsiz kilim deseni (düşük kontrast)
    W = 512
    img = Image.new('RGBA', (W * S, W * S), (246, 239, 227, 255))
    d = ImageDraw.Draw(img)
    cols = [(234, 222, 203, 255), (240, 230, 214, 255), (228, 213, 190, 255)]
    step = W * S // 4
    for gy in range(-1, 5):
        for gx in range(-1, 5):
            cx = gx * step + (step // 2 if gy % 2 else 0)
            cy = gy * step
            kilim_diamond(d, cx, cy, step * 0.36, cols)
    for y in range(0, W * S, step):
        d.line([0, y + step // 2, W * S, y + step // 2], fill=(238, 228, 210, 255), width=2 * S)
    finish(img, W, W, 'pattern_bg')

    # Kenar bandı: 512×64 yatay dikişsiz
    img = Image.new('RGBA', (512 * S, 64 * S), (178, 58, 46, 255))
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, 512 * S, 6 * S], fill=(38, 56, 95, 255))
    d.rectangle([0, 58 * S, 512 * S, 64 * S], fill=(38, 56, 95, 255))
    for i in range(8):
        cx = (i * 64 + 32) * S
        kilim_diamond(d, cx, 32 * S, 18 * S, [(236, 192, 98, 255), (178, 58, 46, 255), (246, 239, 227, 255)])
    finish(img, 512, 64, 'band')

    # Nine silüeti (yer tutucu, illüstratör final çizene kadar)
    img = canvas(256, 256)
    d = ImageDraw.Draw(img)
    d.ellipse([16 * S, 16 * S, 240 * S, 240 * S], fill=(234, 222, 203, 255))
    d.ellipse([88 * S, 52 * S, 168 * S, 132 * S], fill=(205, 160, 125, 255))
    d.pieslice([70 * S, 36 * S, 186 * S, 150 * S], 180, 360, fill=(178, 58, 46, 255))   # yemeni
    d.polygon([(70 * S, 94 * S), (186 * S, 94 * S), (196 * S, 140 * S), (60 * S, 140 * S)], fill=(178, 58, 46, 255))
    d.rounded_rectangle([60 * S, 138 * S, 196 * S, 240 * S], 40 * S, fill=(38, 56, 95, 255))
    kilim_diamond(d, 128 * S, 190 * S, 22 * S, [(236, 192, 98, 255), (38, 56, 95, 255), (236, 192, 98, 255)])
    mask = Image.new('L', img.size, 0)
    ImageDraw.Draw(mask).ellipse([16 * S, 16 * S, 240 * S, 240 * S], fill=255)
    img.putalpha(Image.composite(img.split()[3], Image.new('L', img.size, 0), mask))
    finish(img, 256, 256, 'nine')

    # Uygulama ikonu 1024
    img = Image.new('RGBA', (1024 * 2, 1024 * 2), (178, 58, 46, 255))
    d = ImageDraw.Draw(img)
    for i, rr in enumerate((900, 760, 560, 360, 180)):
        c = [(38, 56, 95, 255), (236, 192, 98, 255), (178, 58, 46, 255), (246, 239, 227, 255), (38, 56, 95, 255)][i]
        d.polygon([(1024, 1024 - rr), (1024 + rr, 1024), (1024, 1024 + rr), (1024 - rr, 1024)], fill=c)
    for sx, sy in ((1, 1), (1, -1), (-1, 1), (-1, -1)):
        kilim_diamond(d, 1024 + sx * 760, 1024 + sy * 760, 150, [(236, 192, 98, 255), (178, 58, 46, 255), (246, 239, 227, 255)])
    os.makedirs(ICON, exist_ok=True)
    img.resize((1024, 1024), Image.LANCZOS).convert('RGB').save(os.path.join(ICON, 'icon_1024.png'))


if __name__ == '__main__':
    gen_audio()
    gen_art()
    for folder in (AUDIO, ART, ICON):
        for f in sorted(os.listdir(folder)):
            print(f'{os.path.relpath(os.path.join(folder, f), ROOT):40} {os.path.getsize(os.path.join(folder, f)) // 1024:6} KB')
