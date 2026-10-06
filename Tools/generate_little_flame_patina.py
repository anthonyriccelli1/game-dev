"""Deterministic, original painted finishes for the Little Flame art preview.

Run from the project root. These textures deliberately describe use and repair,
not a uniformly noisy 'dirty' filter over every surface.
"""
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

OUT = Path('Assets/Resources/TruckArtPass')
OUT.mkdir(parents=True, exist_ok=True)
W, H = 1024, 512
rng = np.random.default_rng(7041)


def soft_noise(width, height):
    small = rng.integers(0, 256, (height, width), dtype=np.uint8)
    return np.asarray(Image.fromarray(small).resize((W, H), Image.Resampling.BICUBIC), dtype=np.float32) / 255.0


grain = rng.normal(0, 1, (H, W)).astype(np.float32)
broad = soft_noise(12, 7) - .5
medium = soft_noise(81, 37) - .5
fine = soft_noise(290, 130) - .5
xx = np.arange(W)[None, :]
yy = np.arange(H)[:, None]


def finish(name, rgb, severity, lower_dirt=False, steel=False):
    base = np.array(rgb, np.float32)
    variation = broad * (10 + severity * 12) + medium * (7 + severity * 10) + fine * 5 + grain * 1.3
    image = np.ones((H, W, 3), np.float32) * base + variation[..., None]
    if lower_dirt:
        # UV v grows upward: grime catches on the rocker and under work surfaces.
        bottom = np.clip((H * .36 - yy) / (H * .36), 0, 1) ** 1.9
        image -= bottom[..., None] * (6 + severity * 23)
    if steel:
        image += np.sin(yy / 3.2)[..., None] * 1.5
    # Faint water/grease runs begin at actual, sparse points rather than a pattern.
    for _ in range(int(9 + severity * 11)):
        x = int(rng.integers(25, W - 25)); top = int(rng.integers(H // 5, H - 35))
        length = int(rng.integers(24, 140)); width = int(rng.integers(2, 8))
        run = np.maximum(0, 1 - np.abs(xx - x) / width) * np.maximum(0, 1 - np.abs(yy - top) / length)
        image -= run[..., None] * (4 + severity * 17)
    image = Image.fromarray(np.uint8(np.clip(image, 0, 255)), 'RGB')
    draw = ImageDraw.Draw(image)
    # Edge scuffs and chips expose grey primer, with occasional rusty points.
    if severity:
        for _ in range(int(severity * 75)):
            x = int(rng.integers(4, W - 6)); y = int(rng.choice([rng.integers(8, 62), rng.integers(H - 98, H - 5), rng.integers(25, H - 25)]))
            length = int(rng.integers(3, 22)); color = tuple(int(c) for c in (91, 94, 88) if True)
            if rng.random() < .32: color = (116, 75, 55)
            draw.line((x, y, x + length, y + int(rng.integers(-2, 3))), fill=color, width=int(rng.integers(1, 3)))
            if rng.random() < .55: draw.line((x + 1, y + 2, x + length // 2, y + 2), fill=(180, 162, 135), width=1)
    image.save(OUT / f'{name}.png', optimize=True)


finish('cream', (205, 197, 175), .85, True)
finish('teal', (54, 112, 107), 1.0, True)
finish('coral', (161, 75, 51), .9, True)
finish('inner', (187, 181, 163), .42, True)
finish('innerlow', (80, 112, 104), .65, True)
finish('floor', (97, 89, 73), 1.0, False)
finish('steel', (155, 160, 151), .45, False, True)

# The narrow sign is painted on an old board. A legible name, smaller secondary
# line, and selective erosion make it feel like real branding at game distance.
sign = Image.new('RGB', (1200, 270), '#263d40')
d = ImageDraw.Draw(sign)
for y in range(270):
    n = int(5 * np.sin(y / 13) + 3 * np.sin(y / 3.7))
    d.line((0, y, 1200, y), fill=(38 + n, 60 + n, 59 + n))
d.rectangle((13, 12, 1185, 255), outline='#b9916c', width=8)
d.line((35, 220, 1164, 220), fill='#ba7951', width=4)
fonts = [Path('C:/Windows/Fonts/arialbd.ttf'), Path('C:/Windows/Fonts/arial.ttf')]
large = ImageFont.truetype(str(fonts[0]), 140)
small = ImageFont.truetype(str(fonts[1]), 42)
d.text((600, 112), 'LITTLE FLAME', font=large, anchor='mm', fill='#efdfb8', stroke_width=1, stroke_fill='#f6e8c4')
d.text((600, 207), 'HOT FOOD  •  LATE NIGHTS', font=small, anchor='mm', fill='#d89164')
for i in range(450):
    x = int(rng.integers(18, 1181)); y = int(rng.integers(14, 258))
    if rng.random() < .72: d.line((x, y, x + int(rng.integers(1, 7)), y), fill='#52625d', width=1)
sign.save(OUT / 'nameboard.png', optimize=True)

board = Image.new('RGB', (700, 520), '#283734')
bd = ImageDraw.Draw(board)
bd.rectangle((14, 14, 686, 506), outline='#9d815f', width=13)
bd.rectangle((36, 36, 664, 484), outline='#697068', width=3)
heading = ImageFont.truetype(str(fonts[0]), 61)
notes = ImageFont.truetype(str(fonts[1]), 42)
bd.text((350, 105), 'TODAY / PREP', font=heading, anchor='mm', fill='#f0dfae')
bd.line((78, 150, 622, 150), fill='#c38963', width=4)
for y, note in [(204, '1  STOCK THE SHELF'), (277, '2  HEAT THE GRILL'), (350, '3  KEEP IT CLEAN')]:
    bd.text((93, y), note, font=notes, fill='#dad0ac')
bd.text((350, 445), 'WE GET THROUGH IT TOGETHER', font=ImageFont.truetype(str(fonts[1]), 30), anchor='mm', fill='#bd9574')
board = board.filter(ImageFilter.GaussianBlur(.25))
board.save(OUT / 'shiftboard.png', optimize=True)

# The original user-owned OBJ has no UVs. Give its existing triangles planar
# metre-based UVs in a separate preview asset; never overwrite the source mesh.
source = Path('Assets/Art/LittleFlame/LittleFlame.obj').read_text().splitlines()
positions = [None]
for line in source:
    if line.startswith('v '):
        positions.append(tuple(float(v) for v in line.split()[1:4]))
uv = [None] * len(positions)
for line in source:
    if not line.startswith('f '):
        continue
    ids = [int(v) for v in line.split()[1:4]]
    a, b, c = (np.array(positions[i]) for i in ids)
    n = np.cross(b - a, c - a)
    axis = np.argmax(np.abs(n))
    for i in ids:
        x, y, z = positions[i]
        if axis == 1:
            uv[i] = ((x + 4.2) / 8.4, (z + 2.1) / 4.2)
        elif axis == 0:
            uv[i] = ((z + 2.1) / 4.2, y / 4.4)
        else:
            uv[i] = ((x + 4.2) / 8.4, y / 4.4)
out = OUT / 'LittleFlamePatina.obj'
with out.open('w', newline='\n') as f:
    f.write('# Preview copy of LittleFlame.obj with projected UVs; generated, not hand-edited.\n')
    f.write('mtllib LittleFlamePatina.mtl\no LittleFlamePatina\n')
    for p in positions[1:]:
        f.write(f'v {p[0]:.5f} {p[1]:.5f} {p[2]:.5f}\n')
    for u, v in uv[1:]:
        f.write(f'vt {u:.6f} {v:.6f}\n')
    material = None
    floor_repair_count = 0
    for line in source:
        if line.startswith(('v ', 'mtllib ', 'o ')):
            continue
        if line.startswith('usemtl '):
            material = line.split()[1]
        if line.startswith('f '):
            ids = [int(v) for v in line.split()[1:4]]
            a, b, c = (np.array(positions[i]) for i in ids)
            n = np.cross(b - a, c - a)
            center = (a + b + c) / 3
            # The source assigns a few upward-facing cab floor shoulders its teal
            # lower-wall/underbody material. Keep their geometry but finish them
            # as the same worn floor, leaving the exterior paint untouched.
            floor_repair = (material in ('LF_InnerLow', 'LF_Under')
                            and np.linalg.norm(n) > 0
                            and n[1] / np.linalg.norm(n) > .75
                            and 1.1 < center[0] < 3.85
                            and .75 < center[1] < 1.4
                            and -1.36 < center[2] < 1.78)
            if floor_repair:
                f.write('usemtl LF_Floor\n')
                floor_repair_count += 1
            f.write('f ' + ' '.join(f'{i}/{i}' for i in ids) + '\n')
            if floor_repair:
                f.write(f'usemtl {material}\n')
        else:
            f.write(line + '\n')
(OUT / 'LittleFlamePatina.mtl').write_text(Path('Assets/Art/LittleFlame/LittleFlame.mtl').read_text())
print(f'Cab floor finish applied to {floor_repair_count} original faces')
