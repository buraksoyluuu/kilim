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


if __name__ == '__main__':
    os.makedirs(SVG_DIR, exist_ok=True)
    for key in SCENES:
        for restored in (False, True):
            name = f'area_{key}_{"new" if restored else "old"}'
            p = os.path.join(SVG_DIR, name + '.svg')
            open(p, 'w').write(svg(key, restored))
            render(p, os.path.join(ART, name + '.png'))
    print('ok', len(SCENES) * 2)
