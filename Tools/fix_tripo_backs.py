"""Repair the dull, grey-blue backs of Tripo character textures.

Tripo only sees the front of a concept and invents the back, which comes out darker and colder.
For each character this script:
  1. reads the rigged mesh (bind pose) and finds which texels are painted on back-facing surfaces,
  2. shifts those texels' brightness, colour cast and saturation toward the front's averages
     (local detail is kept: dark hair stays darker than skin),
  3. gives the whole texture a small saturation lift so it sits with the city's bright palette.
Usage: python fix_backs.py <in.fbx> <in.png> <out.png> [strength 0..1] [saturation boost]
"""
import struct, zlib, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

def load_fbx(path):
    f = open(path, 'rb').read(); big = struct.unpack('<I', f[23:27])[0] >= 7500
    def arr(p):
        t = chr(f[p]); cnt, enc, cl = struct.unpack('<III', f[p+1:p+13]); raw = f[p+13:p+13+cl]
        if enc: raw = zlib.decompress(raw)
        return np.frombuffer(raw, {'d': '<f8', 'i': '<i4', 'f': '<f4', 'l': '<i8'}[t])
    out = {}
    def walk(off):
        while True:
            if big: end, n, plen = struct.unpack('<QQQ', f[off:off+24]); h = 24
            else: end, n, plen = struct.unpack('<III', f[off:off+12]); h = 12
            if end == 0: return
            nl = f[off+h]; name = f[off+h+1:off+h+1+nl].decode(); p = off+h+1+nl
            if name in ('Vertices', 'PolygonVertexIndex', 'UV', 'UVIndex') and name not in out: out[name] = arr(p)
            if p + plen < end: walk(p + plen)
            off = end
    walk(27)
    V = out['Vertices'].reshape(-1, 3); I = out['PolygonVertexIndex']; UV = out['UV'].reshape(-1, 2); UVI = out['UVIndex']
    tris, uvt, poly, pidx = [], [], [], []
    for j, ix in enumerate(I):
        poly.append(ix if ix >= 0 else ~ix); pidx.append(j)
        if ix < 0:
            for a in range(1, len(poly) - 1): tris.append((poly[0], poly[a], poly[a+1])); uvt.append((pidx[0], pidx[a], pidx[a+1]))
            poly, pidx = [], []
    return V, np.array(tris), UV[UVI[np.array(uvt)]]

def to_ycc(c):
    y = c @ [.299, .587, .114]; cb = (c[..., 2] - y) * .564; cr = (c[..., 0] - y) * .713
    return y, cb, cr
def from_ycc(y, cb, cr):
    r = y + 1.403 * cr; b = y + 1.773 * cb; g = (y - .299 * r - .114 * b) / .587
    return np.clip(np.stack([r, g, b], -1), 0, 1)

def fix(fbx, png, out, strength=1.0, sat_boost=1.15):
    V, T, TUV = load_fbx(fbx)
    img = Image.open(png).convert('RGBA'); W, H = img.size
    tex = np.asarray(img).astype(np.float32) / 255.; rgb = tex[..., :3]
    tr = V[T]; n = np.cross(tr[:, 1] - tr[:, 0], tr[:, 2] - tr[:, 0]); nz = n[:, 2] / (np.linalg.norm(n, axis=1) + 1e-12)
    # 0 on the front and sides, 1 squarely on the back, smooth in between.
    w_tri = np.clip((-nz - .05) / .45, 0, 1)
    def paint(values):
        m = Image.new('F', (W, H), 0.0); d = ImageDraw.Draw(m)
        for uv, v in zip(TUV, values):
            d.polygon([(u * W, (1 - vv) * H) for u, vv in uv], fill=float(v))
        return np.asarray(m)
    back = paint(w_tri); covered = paint(np.ones(len(T)))
    front_mask = (paint((nz > .3).astype(float)) > .5) & (covered > .5)
    back_mask = (back > .5) & (covered > .5)
    y, cb, cr = to_ycc(rgb)
    def stats(m): return y[m].mean(), cb[m].mean(), cr[m].mean(), np.hypot(cb[m], cr[m]).mean()
    yF, cbF, crF, sF = stats(front_mask); yB, cbB, crB, sB = stats(back_mask)
    gY = float(np.clip(yF / max(yB, 1e-3), 1, 1.6)); gS = float(np.clip(sF / max(sB, 1e-3), 1, 1.8))
    # Feather the back weight so there is no seam where front meets back, and let it bleed into UV gutters.
    wimg = Image.fromarray((back * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(5)).filter(ImageFilter.GaussianBlur(6))
    w = np.asarray(wimg).astype(np.float32) / 255. * strength
    y2 = y * (1 + (gY - 1) * w)
    cb2 = cb + w * ((cb - cbB) * gS + cbF - cb)
    cr2 = cr + w * ((cr - crB) * gS + crF - cr)
    # Whole-texture saturation lift.
    cb2 *= sat_boost; cr2 *= sat_boost
    res = from_ycc(y2, cb2, cr2)
    outimg = np.concatenate([res, tex[..., 3:]], -1)
    Image.fromarray((outimg * 255 + .5).astype(np.uint8), 'RGBA').convert('RGB').save(out, optimize=True)
    return dict(front_lum=round(yF, 3), back_lum=round(yB, 3), lum_gain=round(gY, 2), sat_gain=round(gS, 2), cast_cb=round(cbF - cbB, 3), cast_cr=round(crF - crB, 3))

if __name__ == '__main__':
    a = sys.argv
    print(fix(a[1], a[2], a[3], float(a[4]) if len(a) > 4 else 1.0, float(a[5]) if len(a) > 5 else 1.15))
