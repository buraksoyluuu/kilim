"""Nine'nin Evi illüstrasyonları (SVG → PNG). Düz-vektör mobil oyun dekor tarzı: degrade duvar/zemin, gün ışığı
huzmesi, yumuşak gölgeler, kilim dokuları. Her alanın 'new' (onarılmış) ve 'old' (bakımsız) hâli aynı sahneden
üretilir: old = soluk/sepya filtre + toz + çatlak + örümcek ağı, onarımla gelen eşyalar yok.

Çıktı: Tools/svg/*.svg (Design artifact'ı da bunları gösterir) ve Resources/art/area_{key}_{old,new}.png (1024×512).
Gereksinim: rsvg-convert (librsvg) ya da cairosvg.
"""
import os, subprocess, random

HERE = os.path.dirname(__file__)
SVG_DIR = os.path.join(HERE, 'svg')
ART = os.path.join(HERE, '..', 'Unity', 'Kilim', 'Assets', 'Kilim', 'Resources', 'art')
W, H = 1024, 512
K, L, S, Z, M, C = '#B23A2E', '#26385F', '#D9A23A', '#6B7A2E', '#6E3A5E', '#F6EFE3'

DEFS = f'''
<defs>
  <linearGradient id="wall" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#F6E3C2"/><stop offset="1" stop-color="#E7C79A"/></linearGradient>
  <linearGradient id="wallK" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#F1DDBD"/><stop offset="1" stop-color="#DDBA8A"/></linearGradient>
  <linearGradient id="floor" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#B9814E"/><stop offset="1" stop-color="#8E5C34"/></linearGradient>
  <linearGradient id="sky" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#9ED3EE"/><stop offset="1" stop-color="#E8F4F2"/></linearGradient>
  <linearGradient id="grass" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#9DBF5E"/><stop offset="1" stop-color="#6F9440"/></linearGradient>
  <linearGradient id="stone" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#D8CBB6"/><stop offset="1" stop-color="#B7A68C"/></linearGradient>
  <linearGradient id="wood" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#A86F40"/><stop offset="1" stop-color="#7C4E2A"/></linearGradient>
  <linearGradient id="copper" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#E6A066"/><stop offset=".5" stop-color="#C46E3A"/><stop offset="1" stop-color="#8E4A24"/></linearGradient>
  <linearGradient id="beam" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#FFF6DC" stop-opacity=".75"/><stop offset="1" stop-color="#FFF6DC" stop-opacity="0"/></linearGradient>
  <radialGradient id="sun" cx=".5" cy=".5" r=".5"><stop offset="0" stop-color="#FFF4C8"/><stop offset=".55" stop-color="#FFE08A" stop-opacity=".9"/><stop offset="1" stop-color="#FFE08A" stop-opacity="0"/></radialGradient>
  <radialGradient id="glow" cx=".5" cy=".5" r=".5"><stop offset="0" stop-color="#FFE7A8" stop-opacity=".9"/><stop offset="1" stop-color="#FFE7A8" stop-opacity="0"/></radialGradient>
  <radialGradient id="vig" cx=".5" cy=".45" r=".75"><stop offset=".6" stop-color="#000" stop-opacity="0"/><stop offset="1" stop-color="#3A2010" stop-opacity=".35"/></radialGradient>
  <filter id="soft" x="-20%" y="-20%" width="140%" height="140%"><feGaussianBlur stdDeviation="8"/></filter>
  <filter id="soft2" x="-20%" y="-20%" width="140%" height="140%"><feGaussianBlur stdDeviation="3"/></filter>
  <filter id="aged"><feColorMatrix type="matrix" values=".42 .42 .16 0 .03  .36 .40 .14 0 .02  .30 .32 .18 0 .0  0 0 0 1 0"/></filter>
  <pattern id="kilim" width="60" height="40" patternUnits="userSpaceOnUse">
    <rect width="60" height="40" fill="{K}"/><path d="M30 4 L52 20 L30 36 L8 20Z" fill="{S}"/><path d="M30 12 L41 20 L30 28 L19 20Z" fill="{L}"/>
    <path d="M30 17 L34 20 L30 23 L26 20Z" fill="{C}"/><path d="M0 0 L8 0 L0 6Z M60 0 L52 0 L60 6Z M0 40 L8 40 L0 34Z M60 40 L52 40 L60 34Z" fill="{C}"/>
  </pattern>
  <pattern id="kilimL" width="60" height="40" patternUnits="userSpaceOnUse">
    <rect width="60" height="40" fill="{L}"/><path d="M30 4 L52 20 L30 36 L8 20Z" fill="{K}"/><path d="M30 12 L41 20 L30 28 L19 20Z" fill="{S}"/>
  </pattern>
  <pattern id="band" width="34" height="26" patternUnits="userSpaceOnUse">
    <rect width="34" height="26" fill="{K}"/><path d="M17 3 L29 13 L17 23 L5 13Z" fill="{S}"/><path d="M17 9 L22 13 L17 17 L12 13Z" fill="{L}"/>
  </pattern>
  <pattern id="planks" width="120" height="40" patternUnits="userSpaceOnUse">
    <rect width="120" height="40" fill="none"/><path d="M0 39.5 H120 M70 0 V40" stroke="#6E4425" stroke-opacity=".35" stroke-width="2"/>
  </pattern>
  <pattern id="tiles" width="64" height="32" patternUnits="userSpaceOnUse">
    <rect width="64" height="32" fill="#D9C7AA"/><rect x="2" y="2" width="60" height="28" rx="6" fill="#E6D6BC"/>
  </pattern>
</defs>'''


def shadow(cx, cy, rx, ry, o=.28):
    return f'<ellipse cx="{cx}" cy="{cy}" rx="{rx}" ry="{ry}" fill="#2A1508" opacity="{o}" filter="url(#soft)"/>'


def room(wall='url(#wall)', floor_y=360):
    return (f'<rect width="{W}" height="{H}" fill="{wall}"/>'
            f'<rect y="0" width="{W}" height="26" fill="url(#band)"/>'
            f'<rect y="{floor_y}" width="{W}" height="{H - floor_y}" fill="url(#floor)"/>'
            f'<rect y="{floor_y}" width="{W}" height="{H - floor_y}" fill="url(#planks)"/>'
            f'<rect y="{floor_y - 6}" width="{W}" height="10" fill="#7C4E2A"/>')


def window(x, y, w, h, curtains, restored):
    out = [f'<rect x="{x - 12}" y="{y - 12}" width="{w + 24}" height="{h + 24}" rx="{w / 2 + 12}" ry="{w / 2 + 12}" fill="{L if restored else "#5B4636"}"/>',
           f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{w / 2}" ry="{w / 2}" fill="url(#sky)"/>',
           f'<ellipse cx="{x + w * .5}" cy="{y + h * .95}" rx="{w * .75}" ry="{h * .3}" fill="url(#grass)"/>',
           f'<path d="M{x + w / 2} {y} V{y + h} M{x} {y + h * .55} H{x + w}" stroke="{L if restored else "#5B4636"}" stroke-width="8"/>']
    if curtains:
        out.append(f'<path d="M{x - 30} {y - 20} Q{x + w * .3} {y + h * .4} {x + w * .05} {y + h + 26} L{x - 30} {y + h + 26}Z" fill="{K}"/>')
        out.append(f'<path d="M{x + w + 30} {y - 20} Q{x + w * .7} {y + h * .4} {x + w * .95} {y + h + 26} L{x + w + 30} {y + h + 26}Z" fill="{K}"/>')
        out.append(f'<rect x="{x - 40}" y="{y - 30}" width="{w + 80}" height="12" rx="6" fill="#7C4E2A"/>')
    return ''.join(out)


def beam(x, y, w, h):
    return f'<path d="M{x} {y} L{x + w} {y} L{x + w + 260} {y + h} L{x + 120} {y + h}Z" fill="url(#beam)" opacity=".8"/>'


def rug(x, y, w, h, pat='kilim'):
    fr = ''.join(f'<rect x="{x + 6 + i * 14}" y="{y + h}" width="4" height="14" fill="{C}"/>' for i in range(int(w / 14)))
    return (shadow(x + w / 2, y + h + 8, w / 2, 12, .2) +
            f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="6" fill="url(#{pat})"/>'
            f'<rect x="{x + 8}" y="{y + 8}" width="{w - 16}" height="{h - 16}" rx="4" fill="none" stroke="{C}" stroke-width="4"/>' + fr)


def sedir(x, y, w, restored):
    cush = ''
    if restored:
        for i, col in enumerate((S, L, Z)):
            cx = x + 70 + i * (w - 140) / 2
            cush += (f'<rect x="{cx - 46}" y="{y - 92}" width="92" height="78" rx="22" fill="{col}"/>'
                     f'<path d="M{cx} {y - 76} L{cx + 20} {y - 53} L{cx} {y - 30} L{cx - 20} {y - 53}Z" fill="{C}"/>')
    return (shadow(x + w / 2, y + 74, w / 2 + 10, 14) +
            f'<rect x="{x}" y="{y}" width="{w}" height="70" rx="14" fill="url(#wood)"/>'
            f'<rect x="{x + 8}" y="{y - 26}" width="{w - 16}" height="40" rx="18" fill="{K if restored else "#8B6E58"}"/>' + cush)


def chest(x, y, restored):
    return (shadow(x + 95, y + 128, 100, 10) +
            f'<rect x="{x}" y="{y}" width="190" height="124" rx="14" fill="url(#wood)"/>'
            f'<rect x="{x}" y="{y + 30}" width="190" height="10" fill="#5C381C"/>'
            + ''.join(f'<rect x="{x + 22 + i * 64}" y="{y}" width="14" height="124" fill="{"url(#copper)" if restored else "#6B5A4A"}"/>' for i in range(3))
            + f'<rect x="{x + 80}" y="{y + 48}" width="30" height="34" rx="6" fill="{S if restored else "#7A6A5A"}"/>')


def plant(x, y, col, s=1.0):
    leaves = ''.join(f'<ellipse cx="{x + dx * s}" cy="{y - 40 * s + dy * s}" rx="{22 * s}" ry="{12 * s}" fill="{Z}" transform="rotate({r} {x + dx * s} {y - 40 * s + dy * s})"/>'
                     for dx, dy, r in ((-26, 0, -30), (26, 0, 30), (-14, -24, -60), (14, -24, 60), (0, -38, 90)))
    flowers = ''.join(f'<circle cx="{x + dx * s}" cy="{y - 60 * s + dy * s}" r="{12 * s}" fill="{col}"/><circle cx="{x + dx * s}" cy="{y - 60 * s + dy * s}" r="{4 * s}" fill="{S}"/>'
                      for dx, dy in ((-20, -10), (18, -14), (0, -34), (-30, 14), (30, 10)))
    return (shadow(x, y + 54 * s, 40 * s, 8, .25) + leaves + flowers +
            f'<path d="M{x - 36 * s} {y} H{x + 36 * s} L{x + 26 * s} {y + 54 * s} H{x - 26 * s}Z" fill="url(#copper)"/>')


def lamp(x, y, lit):
    return ((f'<circle cx="{x}" cy="{y}" r="110" fill="url(#glow)"/>' if lit else '') +
            f'<path d="M{x} {y - 110} V{y - 40}" stroke="#3A2A20" stroke-width="4"/>'
            f'<path d="M{x - 26} {y - 40} H{x + 26} L{x + 18} {y + 20} H{x - 18}Z" fill="url(#copper)"/>'
            f'<ellipse cx="{x}" cy="{y - 6}" rx="12" ry="18" fill="{"#FFE08A" if lit else "#7A6A5A"}"/>')


def cobwebs():
    web = ''
    for cx, sx in ((0, 1), (W, -1)):
        for r in (40, 70, 100, 130):
            web += f'<path d="M{cx} {r + 26} Q{cx + sx * r * .5} {r * .5 + 26} {cx + sx * r} 26" fill="none" stroke="#F4F1EA" stroke-width="2" opacity=".8"/>'
        for a in (0, 25, 50, 75):
            import math
            web += f'<path d="M{cx} 26 L{cx + sx * 150 * math.cos(math.radians(a))} {26 + 150 * math.sin(math.radians(a))}" stroke="#F4F1EA" stroke-width="2" opacity=".8"/>'
    return web


def cracks(seed):
    rnd = random.Random(seed)
    out = ''
    for _ in range(7):
        x, y = rnd.uniform(60, W - 60), rnd.uniform(60, 300)
        d = f'M{x:.0f} {y:.0f}'
        for _ in range(5):
            x += rnd.uniform(-30, 30); y += rnd.uniform(15, 40); d += f' L{x:.0f} {y:.0f}'
        out += f'<path d="{d}" fill="none" stroke="#4A3A2C" stroke-width="3" opacity=".7"/>'
    dust = ''.join(f'<circle cx="{rnd.uniform(0, W):.0f}" cy="{rnd.uniform(0, H):.0f}" r="{rnd.uniform(1, 3):.1f}" fill="#FFF" opacity=".35"/>' for _ in range(80))
    return out + dust


# ------------------------------------------------------------------ sahneler
def oda(r):
    s = room()
    s += beam(140, 120, 170, 360) if r else ''
    s += window(150, 90, 170, 210, r, r)
    s += f'<rect x="0" y="54" width="{W}" height="8" fill="#C9A274" opacity=".5"/>'
    s += chest(380, 236, r)
    s += sedir(600, 286, 380, r)
    if r:
        s += rug(260, 410, 520, 72)
        s += plant(530, 300, K, .9)
        s += f'<rect x="680" y="70" width="230" height="150" rx="8" fill="url(#kilimL)"/><rect x="680" y="70" width="230" height="150" rx="8" fill="none" stroke="#7C4E2A" stroke-width="8"/>'
        s += lamp(950, 180, True)
    return s


def mutfak(r):
    s = room('url(#wallK)')
    s += f'<path d="M110 90 H350 V360 H110Z" fill="url(#stone)"/><path d="M150 360 V230 Q230 150 310 230 V360Z" fill="#2E2018"/>'
    if r:
        s += f'<circle cx="230" cy="300" r="90" fill="url(#glow)"/><path d="M190 360 Q200 290 230 270 Q250 300 270 360Z" fill="#F08A30"/><path d="M210 360 Q220 315 232 300 Q244 325 252 360Z" fill="#FFD25A"/>'
        s += f'<ellipse cx="230" cy="236" rx="56" ry="14" fill="#2E2018"/><path d="M178 236 Q180 290 230 292 Q280 290 282 236Z" fill="url(#copper)"/>'
    s += f'<rect x="430" y="140" width="520" height="16" rx="6" fill="url(#wood)"/>'
    for i in range(5 if r else 2):
        cx = 470 + i * 110
        s += f'<path d="M{cx - 34} 140 Q{cx - 40} 90 {cx} 84 Q{cx + 40} 90 {cx + 34} 140Z" fill="{"url(#copper)" if r else "#8A7A6A"}"/>'
    s += shadow(690, 470, 230, 14)
    s += f'<rect x="490" y="330" width="400" height="22" rx="8" fill="url(#wood)"/><rect x="510" y="350" width="18" height="120" fill="#6B4425"/><rect x="852" y="350" width="18" height="120" fill="#6B4425"/>'
    if r:
        s += f'<path d="M470 328 H910 L930 400 H450Z" fill="url(#kilim)"/>'
        s += f'<circle cx="600" cy="316" r="26" fill="{C}"/><circle cx="700" cy="314" r="30" fill="url(#copper)"/><path d="M780 300 h50 v18 h-50z" fill="#E8C27A"/>'
        s += beam(620, 30, 120, 300)
    return s


def avlu(r):
    s = f'<rect width="{W}" height="{H}" fill="url(#sky)"/>'
    s += f'<circle cx="880" cy="90" r="120" fill="url(#sun)"/>'
    s += f'<path d="M0 300 Q260 230 520 290 T1024 270 V512 H0Z" fill="#AFC77A"/>'
    s += f'<rect y="360" width="{W}" height="152" fill="{"url(#tiles)" if r else "#B9AE9A"}"/>'
    s += f'<path d="M80 360 V150 Q190 100 300 150 V360Z" fill="url(#stone)"/><rect x="60" y="350" width="260" height="40" rx="10" fill="#A8977E"/>'
    s += f'<path d="M190 210 h36 v12 h-26 v40 h-10z" fill="url(#copper)"/>'
    if r:
        s += f'<rect x="216" y="262" width="8" height="92" fill="#8FC7E8" opacity=".9"/><ellipse cx="190" cy="356" rx="110" ry="14" fill="#7DB7DD"/>'
    s += f'<rect x="520" y="140" width="16" height="230" fill="url(#wood)"/><rect x="900" y="140" width="16" height="230" fill="url(#wood)"/><rect x="500" y="128" width="440" height="16" rx="6" fill="url(#wood)"/>'
    if r:
        rnd = random.Random(3)
        for _ in range(34):
            x, y = rnd.uniform(500, 940), rnd.uniform(96, 176)
            s += f'<ellipse cx="{x:.0f}" cy="{y:.0f}" rx="30" ry="20" fill="{rnd.choice([Z, "#7E9A3C", "#5E7A2A"])}"/>'
        for x in (580, 700, 830):
            s += ''.join(f'<circle cx="{x + (k % 2) * 12}" cy="{190 + k * 11}" r="9" fill="{M}"/>' for k in range(6))
        for i, col in enumerate((K, '#E27A9A', '#FFFFFF')):
            s += plant(600 + i * 120, 410, col, .85)
        s += shadow(400, 470, 120, 10) + f'<rect x="300" y="420" width="200" height="40" rx="10" fill="url(#kilim)"/>'
    return s


def tezgah(r):
    s = room()
    s += beam(700, 40, 140, 360) if r else ''
    x, y, w, h = 120, 90, 380, 260
    s += shadow(x + w / 2, 470, w / 2 + 30, 14)
    s += f'<rect x="{x - 26}" y="{y - 30}" width="{w + 52}" height="28" rx="6" fill="url(#wood)"/><rect x="{x - 26}" y="{y + h}" width="{w + 52}" height="28" rx="6" fill="url(#wood)"/>'
    s += f'<rect x="{x - 26}" y="{y - 30}" width="24" height="{h + 150}" fill="url(#wood)"/><rect x="{x + w + 2}" y="{y - 30}" width="24" height="{h + 150}" fill="url(#wood)"/>'
    s += ''.join(f'<path d="M{x + 8 + i * 14} {y} V{y + h}" stroke="{C}" stroke-width="2"/>' for i in range(int(w / 14)))
    if r:
        s += f'<rect x="{x + 6}" y="{y + 110}" width="{w - 12}" height="{h - 110}" fill="url(#kilim)"/>'
    s += f'<rect x="600" y="80" width="360" height="12" rx="6" fill="url(#wood)"/>'
    cols = [K, L, S, Z, M] if r else ['#8A7A6A', '#7A6A5A']
    for i, col in enumerate(cols):
        cx = 640 + i * 70
        s += f'<ellipse cx="{cx}" cy="140" rx="24" ry="48" fill="{col}"/>' + ''.join(f'<path d="M{cx - 20} {112 + k * 16} Q{cx} {118 + k * 16} {cx + 20} {110 + k * 16}" stroke="#000" stroke-opacity=".18" stroke-width="3" fill="none"/>' for k in range(4))
    if r:
        s += rug(620, 230, 330, 110, 'kilimL')
        s += lamp(560, 200, True)
        s += f'<path d="M650 470 Q670 430 700 470Z" fill="{K}"/><circle cx="740" cy="458" r="16" fill="{S}"/><circle cx="772" cy="462" r="13" fill="{L}"/>'
    return s


def bahce(r):
    s = f'<rect width="{W}" height="{H}" fill="url(#sky)"/><circle cx="160" cy="80" r="110" fill="url(#sun)"/>'
    s += f'<path d="M0 290 Q300 220 600 280 T1024 250 V512 H0Z" fill="#A9C47A"/><rect y="370" width="{W}" height="142" fill="url(#grass)"/>'
    gate = L if r else '#6B5A4A'
    s += f'<path d="M40 380 V140 H300 V380Z" fill="url(#stone)"/><path d="M80 380 V210 Q170 120 260 210 V380Z" fill="{gate}"/><path d="M170 380 V160" stroke="#1D2A48" stroke-width="6"/><circle cx="196" cy="290" r="8" fill="{S}"/>'
    s += shadow(560, 392, 160, 16)
    s += f'<path d="M540 390 Q545 280 552 220 H570 Q576 280 582 390Z" fill="url(#wood)"/>'
    for dx, dy, rr in ((0, -40, 120), (-110, 10, 90), (110, 10, 90), (-60, -120, 80), (70, -115, 80)):
        s += f'<circle cx="{560 + dx}" cy="{200 + dy}" r="{rr}" fill="{"#6B8A34" if r else "#6E6A4E"}"/>'
    if r:
        s += ''.join(f'<circle cx="{x}" cy="{y}" r="11" fill="#8B5A2B"/>' for x, y in ((500, 160), (620, 150), (560, 110), (470, 220), (650, 220), (590, 200)))
    s += f'<path d="M780 400 V300 H960 V400Z" fill="url(#stone)"/>'
    if r:
        s += f'<path d="M790 300 V170 M950 300 V170" stroke="url(#wood)" stroke-width="16"/><path d="M760 180 L870 120 L980 180Z" fill="{K}"/><path d="M870 180 V250" stroke="#2E2018" stroke-width="3"/><rect x="852" y="248" width="36" height="30" rx="6" fill="url(#copper)"/>'
        s += f'<path d="M330 420 h200 v-20 h-200z" fill="url(#wood)"/>' + rug(330, 380, 200, 26)
        for i, col in enumerate((K, '#FFFFFF', '#E27A9A')): s += plant(700 + i * 70, 450, col, .6)
    return s


SCENES = {'oda': oda, 'mutfak': mutfak, 'avlu': avlu, 'tezgah': tezgah, 'bahce': bahce}


def svg(key, restored):
    body = SCENES[key](restored)
    if not restored:
        body = f'<g filter="url(#aged)">{body}</g>' + cracks(key) + cobwebs() + f'<rect width="{W}" height="{H}" fill="#6B5440" opacity=".18"/>'
    return f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {W} {H}" width="{W}" height="{H}">{DEFS}{body}<rect width="{W}" height="{H}" fill="url(#vig)"/></svg>'


def render(svg_path, png_path):
    try:
        subprocess.run(['rsvg-convert', '-w', str(W), '-h', str(H), '-o', png_path, svg_path], check=True)
    except FileNotFoundError:
        import cairosvg
        cairosvg.svg2png(url=svg_path, write_to=png_path, output_width=W, output_height=H)


# ------------------------------------------------------------------ diğer mekânlar (kısa, ortak parçalarla)
def outdoor(ground='url(#grass)', sun_x=860):
    return (f'<rect width="{W}" height="{H}" fill="url(#sky)"/><circle cx="{sun_x}" cy="90" r="110" fill="url(#sun)"/>'
            f'<path d="M0 290 Q300 220 600 280 T1024 250 V512 H0Z" fill="#A9C47A"/><rect y="372" width="{W}" height="140" fill="{ground}"/>')


def stonewall(x, y, w, h):
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="8" fill="url(#stone)"/>' + ''.join(
        f'<path d="M{x} {y + k * 28} H{x + w}" stroke="#9A8A72" stroke-width="2" opacity=".6"/>' for k in range(1, int(h / 28)))


def tree(x, y, s=1.0, fruit=None, r=True):
    leaf = '#6B8A34' if r else '#6E6A4E'
    out = shadow(x, y + 4, 90 * s, 12) + f'<path d="M{x - 10 * s} {y} Q{x - 6 * s} {y - 90 * s} {x - 4 * s} {y - 150 * s} H{x + 8 * s} Q{x + 10 * s} {y - 90 * s} {x + 14 * s} {y}Z" fill="url(#wood)"/>'
    for dx, dy, rr in ((0, -190, 90), (-70, -150, 62), (70, -150, 62), (-35, -240, 56), (40, -236, 56)):
        out += f'<circle cx="{x + dx * s}" cy="{y + dy * s}" r="{rr * s}" fill="{leaf}"/>'
    if fruit and r:
        out += ''.join(f'<circle cx="{x + dx * s}" cy="{y + dy * s}" r="{9 * s}" fill="{fruit}"/>' for dx, dy in ((-40, -170), (30, -200), (-10, -240), (60, -150), (-70, -140)))
    return out


def vine_row(y, r):
    out = f'<path d="M0 {y} H{W}" stroke="#7C4E2A" stroke-width="5"/>'
    for x in range(40, W, 110):
        out += f'<ellipse cx="{x}" cy="{y - 24}" rx="52" ry="30" fill="{Z if r else "#77705A"}"/>'
        if r: out += ''.join(f'<circle cx="{x - 8 + (k % 2) * 14}" cy="{y + 2 + k * 10}" r="8" fill="{M}"/>' for k in range(5))
    return out


def pot(x, y, s=1.0, col='url(#copper)'):
    return shadow(x, y + 6, 50 * s, 8, .22) + f'<path d="M{x - 40 * s} {y} Q{x - 54 * s} {y - 70 * s} {x - 22 * s} {y - 96 * s} H{x + 22 * s} Q{x + 54 * s} {y - 70 * s} {x + 40 * s} {y}Z" fill="{col}"/><rect x="{x - 26 * s}" y="{y - 110 * s}" width="{52 * s}" height="16" rx="6" fill="{col}"/>'


def hearth(x, y, fire):
    out = f'<path d="M{x - 120} {y} V{y - 220} H{x + 120} V{y}Z" fill="url(#stone)"/><path d="M{x - 80} {y} V{y - 120} Q{x} {y - 190} {x + 80} {y - 120} V{y}Z" fill="#2E2018"/>'
    if fire: out += f'<circle cx="{x}" cy="{y - 50}" r="90" fill="url(#glow)"/><path d="M{x - 40} {y} Q{x - 30} {y - 70} {x} {y - 96} Q{x + 30} {y - 70} {x + 40} {y}Z" fill="#F08A30"/><path d="M{x - 18} {y} Q{x - 10} {y - 40} {x + 2} {y - 56} Q{x + 14} {y - 40} {x + 20} {y}Z" fill="#FFD25A"/>'
    return out


def sheep(x, y):
    return shadow(x, y + 30, 50, 8, .2) + ''.join(f'<circle cx="{x + dx}" cy="{y + dy}" r="26" fill="#FBF8F1"/>' for dx, dy in ((-22, 0), (0, -14), (22, 0), (0, 8))) + f'<ellipse cx="{x + 46}" cy="{y - 8}" rx="18" ry="14" fill="#3A2E28"/><path d="M{x - 24} {y + 22} V{y + 44} M{x + 20} {y + 22} V{y + 44}" stroke="#3A2E28" stroke-width="6"/>'


def tent(x, y, w, h, r):
    col = '#3A2E28' if r else '#5E5246'
    out = shadow(x, y + 8, w / 2, 14) + f'<path d="M{x - w / 2} {y} L{x} {y - h} L{x + w / 2} {y}Z" fill="{col}"/><path d="M{x - w * .1} {y} L{x} {y - h * .45} L{x + w * .1} {y}Z" fill="#1E1612"/>'
    if r: out += f'<rect x="{x - w * .34}" y="{y - h * .3}" width="{w * .68}" height="30" fill="url(#band)"/>'
    return out


def column(x, y0, y1, r):
    c = '#FBF8F1' if r else '#B9B0A2'
    return f'<rect x="{x - 18}" y="{y0}" width="36" height="{y1 - y0}" fill="{c}"/><rect x="{x - 28}" y="{y0 - 12}" width="56" height="14" rx="4" fill="{c}"/><rect x="{x - 28}" y="{y1}" width="56" height="14" rx="4" fill="{c}"/>'


def pool(x, y, w, r):
    out = f'<ellipse cx="{x}" cy="{y}" rx="{w / 2 + 24}" ry="70" fill="url(#stone)"/><ellipse cx="{x}" cy="{y - 4}" rx="{w / 2}" ry="50" fill="{"#7DB7DD" if r else "#9A8E78"}"/>'
    if r: out += f'<path d="M{x - 4} {y - 4} V{y - 150} M{x + 4} {y - 4} V{y - 150}" stroke="#BFE2F4" stroke-width="5"/><ellipse cx="{x}" cy="{y - 160}" rx="44" ry="20" fill="#D8EEF8"/>'
    return out


def yol(r):
    s = outdoor() + f'<path d="M420 512 Q500 430 560 372 H640 Q620 430 700 512Z" fill="{"#D9C29A" if r else "#A89C84"}"/>'
    s += stonewall(0, 290, 380 if r else 200, 90)
    s += f'<path d="M760 380 V200 H990 V380Z" fill="url(#stone)"/><path d="M800 380 V250 Q875 190 950 250 V380Z" fill="{"url(#wood)" if r else "#6B5A4A"}"/>'
    if r: s += tree(160, 300, .7) + tree(300, 290, .6) + plant(700, 430, K, .6) + plant(1000, 450, '#FFFFFF', .5)
    return s


def bag(r):
    s = outdoor('#B49A5E') + vine_row(330, r) + vine_row(420, r) + vine_row(500, r)
    if r: s += f'<path d="M880 330 V180 M820 230 H940" stroke="url(#wood)" stroke-width="10"/><circle cx="880" cy="160" r="26" fill="#F1D7B4"/><path d="M840 150 H920 L880 100Z" fill="{K}"/><path d="M846 230 H914 L900 320 H860Z" fill="{L}"/>'
    return s


def pekmez(r):
    s = room('url(#wallK)') + hearth(800, 360, r)
    if r: s += f'<ellipse cx="800" cy="200" rx="70" ry="18" fill="#2E2018"/><path d="M732 200 Q736 266 800 268 Q864 266 868 200Z" fill="url(#copper)"/>'
    s += shadow(330, 470, 230, 14) + f'<rect x="120" y="340" width="420" height="110" rx="20" fill="url(#wood)"/>'
    if r: s += f'<rect x="140" y="350" width="380" height="40" rx="14" fill="{M}"/>' + pot(250, 500, .6, '#9A4A3A') + pot(360, 500, .6, '#9A4A3A') + pot(470, 500, .6, '#9A4A3A')
    return s


def seki(r):
    s = outdoor() + shadow(512, 470, 380, 16) + f'<rect x="140" y="330" width="740" height="140" rx="20" fill="{"#E8D2AE" if r else "#B8AA94"}"/>'
    if r:
        s += f'<rect x="170" y="300" width="680" height="44" rx="14" fill="url(#kilim)"/>'
        s += f'<path d="M150 330 V130 M870 330 V130 M130 130 H890" stroke="url(#wood)" stroke-width="14"/>'
        s += ''.join(f'<ellipse cx="{x}" cy="128" rx="44" ry="24" fill="{Z}"/>' for x in range(170, 880, 70))
        s += pot(760, 300, .5)
    return s


def ambar(r):
    s = outdoor('#B49A5E') + shadow(512, 400, 340, 16) + f'<rect x="230" y="180" width="560" height="220" fill="{"url(#wood)" if r else "#7A6656"}"/>'
    s += f'<path d="M190 190 L510 60 L830 190Z" fill="{K if r else "#6E5A4A"}"/><rect x="440" y="260" width="140" height="140" fill="#5C381C"/>'
    if r: s += ''.join(f'<rect x="{820 + (i % 2) * 70}" y="{420 - (i // 2) * 64}" width="62" height="58" rx="18" fill="#E8D2AE"/>' for i in range(4)) + f'<path d="M510 120 L530 140 L510 160 L490 140Z" fill="{L}"/>'
    return s


def oba(r):
    s = outdoor('#8FB45A') + tent(400, 420, 560, 320, r)
    if r: s += rug(700, 400, 260, 70) + tent(860, 380, 240, 160, r)
    return s


def ahir(r):
    s = outdoor('#A9A060') + shadow(300, 400, 240, 14) + f'<rect x="80" y="200" width="440" height="200" fill="{"url(#wood)" if r else "#7A6656"}"/><path d="M50 210 L300 90 L550 210Z" fill="#7C4E2A"/><rect x="250" y="270" width="100" height="130" fill="#5C381C"/>'
    if r: s += sheep(660, 430) + sheep(800, 450) + sheep(920, 420) + ''.join(f'<rect x="{560 + i * 40}" y="{320 - i * 40}" width="140" height="50" rx="16" fill="{S}"/>' for i in range(3))
    return s


def ocak(r):
    s = outdoor('#9AA66A') + hearth(330, 420, r)
    if r: s += pot(660, 440, .9) + pot(820, 450, .7, '#E8D2AE') + rug(560, 450, 360, 50)
    return s


def cayir(r):
    s = outdoor('url(#grass)')
    if r:
        rnd = random.Random(5)
        s += ''.join(f'<circle cx="{rnd.uniform(0, W):.0f}" cy="{rnd.uniform(390, 505):.0f}" r="7" fill="{rnd.choice([K, "#FFFFFF", M, S])}"/>' for _ in range(110))
        s += f'<path d="M0 380 H520" stroke="url(#wood)" stroke-width="10"/>' + ''.join(f'<rect x="{x}" y="330" width="14" height="70" fill="url(#wood)"/>' for x in range(20, 520, 70))
        s += f'<path d="M720 380 V170 M880 380 V170 M700 170 H900" stroke="url(#wood)" stroke-width="12"/><path d="M760 170 V320 M840 170 V320" stroke="#3A2A20" stroke-width="4"/><rect x="740" y="318" width="120" height="16" rx="6" fill="url(#wood)"/>'
        s += f'<path d="M960 380 V150" stroke="url(#wood)" stroke-width="8"/><path d="M964 154 L1020 174 L964 196Z" fill="{K}"/>'
    else:
        s += f'<path d="M0 380 H200" stroke="#6B5A4A" stroke-width="10"/>'
    return s


def kaynak(r):
    s = outdoor('#8FB45A') + f'<path d="M0 420 Q260 380 520 430 T1024 410 V490 Q760 470 520 500 T0 480Z" fill="{"#7DB7DD" if r else "#9A8E78"}"/>'
    s += stonewall(110, 260, 220, 150)
    if r:
        s += f'<path d="M214 330 V420" stroke="#8FC7E8" stroke-width="10"/><path d="M560 470 Q660 360 760 470" stroke="url(#stone)" stroke-width="44" fill="none"/>'
        s += tree(900, 380, .7) + ''.join(f'<rect x="{860 + i * 26}" y="210" width="14" height="60" fill="{c}"/>' for i, c in enumerate((K, "#FFFFFF", Z)))
    return s


def hayat(r):
    s = room('url(#wall)') + ''.join(column(x, 90, 340, r) for x in (150, 400, 650, 900))
    if r: s += f'<path d="M520 60 V150" stroke="#3A2A20" stroke-width="3"/><path d="M490 150 h60 v60 h-60z" fill="none" stroke="url(#copper)" stroke-width="5"/><circle cx="520" cy="182" r="12" fill="{S}"/>' + rug(270, 410, 500, 70, 'kilimL')
    return s


def basoda(r):
    s = room() + f'<ellipse cx="512" cy="56" rx="170" ry="30" fill="{S if r else "#9A8E7A"}"/><rect x="60" y="120" width="300" height="240" rx="10" fill="url(#wood)"/>' + hearth(820, 360, r)
    if r: s += rug(220, 400, 560, 90) + lamp(450, 200, True)
    return s


def selamlik(r):
    s = room('url(#wallK)') + sedir(80, 300, 860, r)
    if r: s += f'<rect x="300" y="60" width="420" height="150" rx="8" fill="url(#kilimL)"/>' + lamp(150, 170, True) + lamp(870, 170, True) + rug(320, 420, 380, 60)
    return s


def kapi(r):
    s = outdoor('#C9BBA0') + f'<rect x="160" y="110" width="700" height="270" fill="{"#F1E1C4" if r else "#B5A994"}"/>'
    s += f'<path d="M420 380 V230 Q510 140 600 230 V380Z" fill="{"url(#wood)" if r else "#6B5A4A"}"/><circle cx="560" cy="310" r="10" fill="{S}"/>'
    if r: s += f'<rect x="380" y="130" width="260" height="56" rx="12" fill="{K}"/><text x="510" y="168" font-size="30" font-family="sans-serif" font-weight="700" fill="{C}" text-anchor="middle">KİLİM EVİ</text>' + lamp(330, 260, True) + lamp(690, 260, True)
    return s


def havuz(r):
    s = outdoor('#A9C47A') + pool(512, 430, 520, r)
    if r: s += plant(160, 440, K, .7) + plant(260, 450, '#E27A9A', .6) + plant(860, 440, S, .7) + plant(950, 455, K, .6)
    return s


SCENES.update({'yol': yol, 'bag': bag, 'pekmez': pekmez, 'seki': seki, 'ambar': ambar, 'oba': oba, 'ahir': ahir, 'ocak': ocak,
               'cayir': cayir, 'kaynak': kaynak, 'hayat': hayat, 'basoda': basoda, 'selamlik': selamlik, 'kapi': kapi, 'havuz': havuz})


def intro_pages():
    """Tanıtım 1 ve 4: yeni sahneler + Nine."""
    from PIL import Image
    nine = Image.open(os.path.join(ART, 'nine.png')).convert('RGBA').resize((300, 300))
    for name, area, pos in (('intro_1', 'area_oda_old', (700, 190)), ('intro_4', 'area_bahce_new', (30, 200))):
        img = Image.open(os.path.join(ART, area + '.png')).convert('RGBA')
        img.alpha_composite(nine, pos)
        img.convert('RGB').save(os.path.join(ART, name + '.png'))
    _board_pages()


def _board_pages():
    """Tanıtım 2 (tezgâhta dokunan kilim, yarım) ve 3 (desen eşleşmesi + 3 yıldız): gerçek bölüm deseninden."""
    import json
    from PIL import Image, ImageDraw, ImageFilter
    col = {'K': (192, 57, 43), 'L': (39, 60, 117), 'S': (230, 176, 58), 'Z': (96, 128, 56), 'M': (120, 44, 90)}
    cand = json.load(open(os.path.join(ART, '..', 'levels', 'region1.json')))[:20]
    def spread(l):   # en dengeli renk dağılımlı desen
        cs = [v for row in l['motif'] for v in row if v]
        return min(cs.count(k) for k in set(cs)) * len(set(cs)) if len(set(cs)) > 1 else 0
    lv = max((l for l in cand if 5 <= l['size'] <= 7), key=spread)
    n, mo = lv['size'], lv['motif']

    def board(x0, y0, cell, filled):
        d = ImageDraw.Draw(img)
        d.rounded_rectangle([x0 - 18, y0 - 18, x0 + n * cell + 18, y0 + n * cell + 18], 18, fill=(120, 74, 40), outline=(70, 40, 22), width=6)
        for r in range(n):
            for c in range(n):
                v, x, y = mo[r][c], x0 + c * cell, y0 + r * cell
                if v is None: d.rectangle([x, y, x + cell - 2, y + cell - 2], fill=(60, 40, 28)); continue
                on = filled(r, c)
                f = col.get(v, (200, 200, 200)) if on else tuple(int(a * .35 + 232 * .65) for a in col.get(v, (200, 200, 200)))
                d.rounded_rectangle([x + 2, y + 2, x + cell - 3, y + cell - 3], 6, fill=f)
                if on: d.line([x + 6, y + cell // 2, x + cell - 7, y + cell // 2], fill=tuple(min(255, a + 40) for a in f), width=2)
        return d

    def star(d, cx, cy, R, fill):
        import math
        pts = [(cx + (R if k % 2 == 0 else R * .45) * math.sin(k * math.pi / 5), cy - (R if k % 2 == 0 else R * .45) * math.cos(k * math.pi / 5)) for k in range(10)]
        d.polygon(pts, fill=fill, outline=(150, 96, 20))

    for name, filled, stars in (('intro_2', lambda r, c: r < n // 2 + 1, False), ('intro_3', lambda r, c: True, True)):
        img = Image.open(os.path.join(ART, 'area_tezgah_new.png' if not stars else 'area_oda_new.png')).convert('RGBA').filter(ImageFilter.GaussianBlur(3))
        img.alpha_composite(Image.new('RGBA', img.size, (30, 18, 10, 90)))
        cell = 300 // n
        d = board(512 - n * cell // 2, 256 - n * cell // 2 + (14 if stars else 0), cell, filled)
        if stars:
            for i, x in enumerate((432, 512, 592)): star(d, x, 48 if i != 1 else 36, 30 if i != 1 else 38, (246, 196, 60))
        else:
            d.rounded_rectangle([700, 330, 820, 380], 10, fill=col['K']); d.rounded_rectangle([760, 330, 820, 430], 10, fill=col['K'])
        img.convert('RGB').save(os.path.join(ART, name + '.png'))


if __name__ == '__main__':
    os.makedirs(SVG_DIR, exist_ok=True)
    for key in SCENES:
        for restored in (False, True):
            name = f'area_{key}_{"new" if restored else "old"}'
            p = os.path.join(SVG_DIR, name + '.svg')
            open(p, 'w').write(svg(key, restored))
            render(p, os.path.join(ART, name + '.png'))
    intro_pages()
    print('ok', len(SCENES) * 2)
