"""Repair the dull, grey-blue backs of Tripo character textures (v2: region-matched).

Tripo only sees the front of a concept and invents the back, which comes out darker, colder and greyer.
v1 matched the whole back to the whole front's average colour, which pulled skin toward the clothes' colour
and over-brightened pale arms. v2 works region by region:
  1. read the rigged mesh in bind pose (T-pose) and project it to a front view,
  2. cut that view into a grid of small cells (arm, sleeve, hand, leg, torso...),
  3. in each cell, shift the back-facing texels' colour statistics (brightness, colour, saturation, contrast)
     to match the front-facing texels of the same cell (Reinhard colour transfer),
  4. for the back of the head, match the crown of the hair seen from the front (not the face),
  5. lift the whole texture's saturation slightly so it sits with the city's bright palette.
Usage: python fix_tripo_backs.py <in.fbx> <in.png> <out.png> [strength 0..1] [saturation boost]
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
    y = c @ np.array([.299, .587, .114]); cb = (c[..., 2] - y) * .564; cr = (c[..., 0] - y) * .713
    return y, cb, cr
def from_ycc(y, cb, cr):
    r = y + 1.403 * cr; b = y + 1.773 * cb; g = (y - .299 * r - .114 * b) / .587
    return np.clip(np.stack([r, g, b], -1), 0, 1)

def fix(fbx, png, out, strength=.9, sat_boost=1.12, cell=.07, head=.2):
    V, T, TUV = load_fbx(fbx)
    img = Image.open(png).convert('RGB'); W, H = img.size
    rgb = np.asarray(img).astype(np.float32) / 255.
    tr = V[T]; n = np.cross(tr[:, 1] - tr[:, 0], tr[:, 2] - tr[:, 0]); nz = n[:, 2] / (np.linalg.norm(n, axis=1) + 1e-12)
    c = tr.mean(1); lo, hi = V.min(0), V.max(0); height = hi[1] - lo[1]
    # Grid cell of each triangle in the front view (x across, y up), in units of the character's height.
    cx = np.floor((c[:, 0] - lo[0]) / (cell * height)).astype(int); cy = np.floor((c[:, 1] - lo[1]) / (cell * height)).astype(int)
    cell_id = cx * 1000 + cy
    is_head = c[:, 1] > hi[1] - head * height
    # Back of the head is matched to the hair at the top of the head seen from the front, never to the face.
    crown = c[:, 1] > hi[1] - .07 * height
    cell_id = np.where(is_head & (nz > .25), -2, cell_id)          # the face: never a reference
    cell_id = np.where(is_head & (nz <= .25), -1, cell_id)        # back and sides of the head
    cell_id = np.where(crown & (nz > .25), -1, cell_id)           # front of the crown: the hair to match
    # Triangle index per texel.
    idmap = Image.new('I', (W, H), -1); d = ImageDraw.Draw(idmap)
    for i, uv in enumerate(TUV): d.polygon([(u * W, (1 - v) * H) for u, v in uv], fill=i)
    ids = np.asarray(idmap)
    valid = ids >= 0; tid = np.where(valid, ids, 0)
    y, cb, cr = to_ycc(rgb)
    front_t = nz > .25; back_w_t = np.clip((-nz - .05) / .45, 0, 1)
    texel_cell = cell_id[tid]; texel_front = valid & front_t[tid]; texel_backw = np.where(valid, back_w_t[tid], 0)
    # Per-cell statistics, front texels vs back texels.
    ny, ncb, ncr = y.copy(), cb.copy(), cr.copy()
    flat_cell = texel_cell[valid]; cells = np.unique(cell_id[(back_w_t > 0)])
    for cid in cells:
        fm = texel_front & (texel_cell == cid); bm = (texel_backw > 0) & (texel_cell == cid)
        if fm.sum() < 60 or bm.sum() < 30: continue
        for ch, out_ch in ((y, ny), (cb, ncb), (cr, ncr)):
            mF, sF = ch[fm].mean(), ch[fm].std() + 1e-3; mB, sB = ch[bm].mean(), ch[bm].std() + 1e-3
            k = np.clip(sF / sB, .6, 1.8)
            out_ch[bm] = (ch[bm] - mB) * k + mF
    # Blend by back weight (feathered so no seam at the sides), then a gentle saturation lift everywhere.
    wimg = Image.fromarray((texel_backw * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(4))
    w = np.asarray(wimg).astype(np.float32) / 255. * strength
    y2 = y + (ny - y) * w; cb2 = (cb + (ncb - cb) * w) * sat_boost; cr2 = (cr + (ncr - cr) * w) * sat_boost
    Image.fromarray((from_ycc(y2, cb2, cr2) * 255 + .5).astype(np.uint8), 'RGB').save(out, optimize=True)
    return dict(cells=len(cells), back_texels=int((texel_backw > 0).sum()))

if __name__ == '__main__':
    a = sys.argv
    print(fix(a[1], a[2], a[3], float(a[4]) if len(a) > 4 else .9, float(a[5]) if len(a) > 5 else 1.12))
