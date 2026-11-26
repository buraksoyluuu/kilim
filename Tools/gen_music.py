"""Kilim müzikleri: Anadolu makamları ve çalgı sentezi (numpy). Telifsiz, koddan üretilir.

  music_menu.wav  Hüseyni makamı · ney + bağlama arpeji + dem (bordun) · sakin, döngü
  music_game.wav  Hicaz makamı · bağlama ezgisi + darbuka (düyek usulü) + dem · oyun sırasında, döngü
  win.wav         Rast makamı · bağlama ve kanun çıkışı, def vuruşları, kapanış akoru
  fail.wav        Uşşak makamı · ney ile kısa iniş (yumuşak, cezalandırmayan)

Döngüler kesintisiz: sona taşan sesler başa sarılır.
"""
import os, numpy as np, wave

SR = 32000
OUT = os.path.join(os.path.dirname(__file__), '..', 'Unity', 'Kilim', 'Assets', 'Kilim', 'Resources', 'audio')
rng = np.random.default_rng(11)


def hz(midi): return 440.0 * 2 ** ((midi - 69) / 12)


def save(name, x):
    x = x / (np.max(np.abs(x)) + 1e-9) * 0.89
    pcm = (x * 32767).astype(np.int16)
    with wave.open(os.path.join(OUT, name + '.wav'), 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(pcm.tobytes())


# ------------------------------------------------------------------ çalgılar
def baglama(freq, dur, vel=1.0, bright=0.55):
    """Karplus–Strong (blok blok vektörel), çift tel hafif akort farkıyla → bağlama/saz tınısı."""
    n = int(dur * SR)
    out = np.zeros(n)
    for det in (1.0, 1.0035):
        p = max(2, int(SR / (freq * det)))
        y = np.zeros(n + p + 1)
        y[:p] = rng.uniform(-1, 1, p) * vel
        # ilk periyodu parlaklığa göre yumuşat
        y[:p] = bright * y[:p] + (1 - bright) * np.convolve(y[:p], [0.5, 0.5], 'same')
        i = p
        while i < n:
            j = min(i + p, n)
            y[i:j] = 0.996 * 0.5 * (y[i - p:j - p] + y[i - p + 1:j - p + 1])
            i = j
        out += y[:n]
    env = np.exp(-np.linspace(0, 4.2, n))
    return out * env * 0.5


def kanun(freq, dur, vel=1.0):
    n = int(dur * SR)
    t = np.arange(n) / SR
    x = sum(np.sin(2 * np.pi * freq * k * t) / k ** 1.3 for k in range(1, 7))
    return x * np.exp(-t * 5.5) * vel * 0.5


def ney(freq, dur, vel=1.0):
    """Ney: temel + zayıf harmonikler + nefes gürültüsü + gecikmeli vibrato, yavaş atak."""
    n = int(dur * SR)
    t = np.arange(n) / SR
    vib = 1 + 0.006 * np.sin(2 * np.pi * 5.2 * t) * np.clip(t / 0.35, 0, 1)
    ph = 2 * np.pi * np.cumsum(freq * vib) / SR
    x = np.sin(ph) + 0.18 * np.sin(2 * ph) + 0.07 * np.sin(3 * ph)
    breath = np.convolve(rng.normal(0, 1, n), np.ones(9) / 9, 'same') * 0.12
    a = np.clip(t / 0.12, 0, 1) * np.clip((dur - t) / 0.18, 0, 1)
    return (x + breath) * a * vel * 0.42


def drone(freq, dur, vel=0.5):
    n = int(dur * SR)
    t = np.arange(n) / SR
    x = np.sin(2 * np.pi * freq * t) + 0.35 * np.sin(2 * np.pi * 2 * freq * t) + 0.12 * np.sin(2 * np.pi * 3 * freq * t)
    swell = 0.75 + 0.25 * np.sin(2 * np.pi * t / 7)
    return x * swell * vel * 0.25


def dum(vel=1.0):
    n = int(0.35 * SR); t = np.arange(n) / SR
    f = 95 * np.exp(-t * 9) + 55
    return (np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 11) + rng.normal(0, 0.15, n) * np.exp(-t * 60)) * vel


def tek(vel=1.0):
    n = int(0.12 * SR); t = np.arange(n) / SR
    noise = rng.normal(0, 1, n)
    hp = noise - np.convolve(noise, np.ones(6) / 6, 'same')
    return (hp * 0.8 + np.sin(2 * np.pi * 1450 * t) * 0.4) * np.exp(-t * 42) * vel * 0.55


def place(buf, x, at, wrap):
    i = int(at * SR)
    n = len(buf)
    for k in range(len(x)):
        j = i + k
        if j >= n:
            if not wrap: break
            j %= n
        buf[j] += x[k]


def reverb(x, wet=0.22):
    """Basit oda yankısı (birkaç gecikme hattı)."""
    y = x.copy()
    for d_ms, g in ((47, 0.45), (73, 0.38), (109, 0.3), (151, 0.24), (211, 0.18)):
        d = int(SR * d_ms / 1000)
        y[d:] += x[:-d] * g * wet * 2
    return y


# ------------------------------------------------------------------ makamlar (12-TET yaklaşık; MIDI derece listeleri)
HUSEYNI_A = [57, 59, 60, 62, 64, 66, 67, 69, 71, 72, 74, 76]      # La Hüseyni: La Si Do Re Mi Fa# Sol La
HICAZ_D = [62, 63, 66, 67, 69, 70, 72, 74, 75, 78, 79, 81]        # Re Hicaz: Re Mi♭ Fa# Sol La Si♭ Do Re
RAST_G = [55, 57, 59, 60, 62, 64, 66, 67, 69, 71, 72, 74]          # Sol Rast (Si ve Fa# yaklaşık)
USSAK_A = [57, 58, 60, 62, 64, 65, 67, 69]                          # La Uşşak (Si♭ yaklaşık)


def music_menu():
    bpm, beats = 72, 64
    spb = 60 / bpm
    L = beats * spb
    buf = np.zeros(int(L * SR))
    S = HUSEYNI_A
    # dem: La ve Mi
    place(buf, drone(hz(45), L + 2, 0.55), 0, True)
    place(buf, drone(hz(52), L + 2, 0.25), 0, True)
    # bağlama arpeji (her vuruş), 4 ölçüde bir akor değişir
    chords = [[0, 2, 4, 7], [3, 5, 7, 10], [2, 4, 6, 9], [0, 2, 4, 7]]
    for b in range(beats):
        ch = chords[(b // 16) % 4]
        for k, deg in enumerate((ch[0], ch[2], ch[1], ch[3])):
            if (b * 2 + k) % 2 == 0 or k < 2:
                place(buf, baglama(hz(S[deg] - 12 if deg < 7 else S[deg - 7]), 1.4, 0.35), b * spb + k * spb / 4, True)
    # ney ezgisi: Hüseyni seyri (dereceler, vuruş uzunlukları)
    phrase = [(4, 2), (5, 1), (4, 1), (2, 2), (3, 1), (2, 1), (1, 2), (0, 2),
              (2, 1), (4, 1), (7, 2), (6, 1), (5, 1), (4, 3), (None, 1),
              (7, 2), (9, 1), (8, 1), (7, 2), (5, 1), (4, 1), (3, 2), (2, 2),
              (4, 1), (3, 1), (2, 1), (1, 1), (0, 4)]
    t = 0.0
    for rep in range(2):
        for deg, dur in phrase:
            if deg is not None:
                place(buf, ney(hz(S[deg] + 12), dur * spb * 0.98, 0.9), t, True)
            t += dur * spb
    return reverb(buf, 0.3)


def music_game():
    bpm, bars = 100, 16
    spb = 60 / bpm
    beats = bars * 4
    L = beats * spb
    buf = np.zeros(int(L * SR))
    S = HICAZ_D
    place(buf, drone(hz(50), L + 2, 0.45), 0, True)
    place(buf, drone(hz(57), L + 2, 0.18), 0, True)
    # düyek usulü (8/8): DÜM . TEK TEK DÜM . TEK .
    pattern = [(0, dum, 1.0), (1, tek, 0.8), (1.5, tek, 0.5), (2, dum, 0.9), (3, tek, 0.8), (3.5, tek, 0.35)]
    for bar in range(bars):
        for off, fn, v in pattern:
            place(buf, fn(v * 0.5), (bar * 4 + off) * spb, True)
    # bağlama ezgisi: Hicaz seyri, 2 ölçülük cümleler
    phr = [[0, 1, 2, 3, 4, 3, 2, 1], [2, 3, 4, 5, 4, 3, 2, 2], [4, 5, 7, 5, 4, 3, 2, 1], [3, 2, 1, 2, 1, 0, 0, 0]]
    for bar in range(0, bars, 2):
        p = phr[(bar // 2) % 4]
        for k, deg in enumerate(p):
            octave = 0 if bar < bars // 2 else 12
            place(buf, baglama(hz(S[deg] + octave), 0.9, 0.55), (bar * 4 + k) * spb, True)
            if k % 2 == 1:
                place(buf, baglama(hz(S[deg] + octave), 0.5, 0.25), (bar * 4 + k + 0.5) * spb, True)
    # her 4 ölçüde kanun süslemesi
    for bar in range(3, bars, 4):
        for k, deg in enumerate((7, 6, 5, 4)):
            place(buf, kanun(hz(S[deg] + 12), 0.6, 0.35), (bar * 4 + 2 + k * 0.25) * spb, True)
    return reverb(buf, 0.18)


def win_stinger():
    L = 4.2
    buf = np.zeros(int(L * SR))
    S = RAST_G
    for k, deg in enumerate((0, 2, 4, 5, 7, 9, 11)):
        place(buf, baglama(hz(S[deg] + 12), 1.2, 0.8), k * 0.11, False)
        place(buf, kanun(hz(S[deg] + 24), 0.8, 0.4), k * 0.11 + 0.03, False)
    for k in range(4): place(buf, tek(0.5 + 0.1 * k), 0.8 + k * 0.08, False)
    place(buf, dum(1.0), 1.15, False)
    for deg in (0, 4, 7, 11):                      # kapanış: Sol akoru
        place(buf, baglama(hz(S[deg] + 12), 2.8, 0.7), 1.15, False)
        place(buf, kanun(hz(S[deg] + 24), 2.5, 0.35), 1.18, False)
    place(buf, ney(hz(S[7] + 12), 2.4, 0.6), 1.2, False)
    return reverb(buf, 0.3)


def fail_stinger():
    L = 3.2
    buf = np.zeros(int(L * SR))
    S = USSAK_A
    for k, (deg, dur) in enumerate(((4, 0.45), (3, 0.45), (2, 0.45), (1, 0.6), (0, 1.3))):
        place(buf, ney(hz(S[deg] + 12), dur, 0.8), sum(d for _, d in ((4, 0.45), (3, 0.45), (2, 0.45), (1, 0.6))[:k]), False)
    place(buf, baglama(hz(S[0]), 2.0, 0.5), 1.95, False)
    place(buf, dum(0.5), 1.95, False)
    return reverb(buf, 0.3)


if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    save('music_menu', music_menu())
    save('music_game', music_game())
    save('win', win_stinger())
    save('fail', fail_stinger())
    print('ok')
