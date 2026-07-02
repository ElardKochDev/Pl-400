# -*- coding: utf-8 -*-
"""Trocea las hojas de ChatGPT de PL-400 (Chatgpt resources/*.png) en sprites sueltos
con las CLAVES que el motor busca, y coloca fondos e icono en su sitio.

  enemies_p#.png  -> e_p#m{md}_{0,1,2}   (rejilla 3 col x N filas; fila = módulo; SIN rótulo)
  bosses_p#.png   -> b_p#m{md}            (1 fila x N col; rótulo ABAJO -> se recorta)
  guardians.png   -> guard_d1..guard_d6 + boss_final  (4x2, 7 usados; rótulo ABAJO)
  *.png fondos    -> Resources/Art/External/*   (TitleVista, TownPlaza, Battle*)
  AppIcon.png     -> Assets/Art/Icon/AppIcon.png

Fondo plano oscuro -> alfa por relleno desde los bordes (remove_bg_flood). Reutiliza la
técnica de slice_ab410_art.py.
"""
import os
from collections import deque
from PIL import Image

HERE  = os.path.dirname(os.path.abspath(__file__))
SRC   = os.path.join(HERE, "Chatgpt resources")
RES   = os.path.join(HERE, "PL400Unity", "Assets", "Resources")
TILES = os.path.join(RES, "Art", "Tiles")
EXT   = os.path.join(RES, "Art", "External")
ICON  = os.path.join(HERE, "PL400Unity", "Assets", "Art", "Icon", "AppIcon.png")

# Módulos por parte (para nombrar e_p#m#_/b_p#m#)
PART_MODS = {1: [1, 2, 3, 4, 5], 2: [1, 2, 3, 4, 5], 3: [1, 2, 3, 4],
             4: [1, 2, 3], 5: [1, 2, 3, 4, 5], 6: [1, 2, 3, 4, 5]}


def cell(img, r, c, rows, cols, inset=0.0):
    W, H = img.size
    cw, ch = W // cols, H // rows
    x0, y0, x1, y1 = c*cw, r*ch, (c+1)*cw, (r+1)*ch
    if inset:
        dx, dy = int(cw*inset), int(ch*inset)
        x0, y0, x1, y1 = x0+dx, y0+dy, x1-dx, y1-dy
    return img.crop((x0, y0, x1, y1))


def _median_bg(im):
    w, h = im.size
    pts = [(4, 4), (w-5, 4), (4, h-5), (w-5, h-5),
           (w//2, 4), (w//2, h-5), (4, h//2), (w-5, h//2), (8, 8), (w-9, h-9)]
    rs, gs, bs = [], [], []
    px = im.load()
    for x, y in pts:
        p = px[x, y]; rs.append(p[0]); gs.append(p[1]); bs.append(p[2])
    rs.sort(); gs.sort(); bs.sort(); m = len(rs)//2
    return rs[m], gs[m], bs[m]


def remove_bg_flood(im, tol=86):
    im = im.convert("RGBA")
    w, h = im.size
    px = im.load()
    br, bg, bb = _median_bg(im)
    def isbg(p): return abs(p[0]-br)+abs(p[1]-bg)+abs(p[2]-bb) <= tol
    visited = bytearray(w*h)
    q = deque()
    for x in range(w):
        for yy in (0, h-1):
            i = yy*w+x
            if not visited[i] and isbg(px[x, yy]): visited[i] = 1; q.append((x, yy))
    for y in range(h):
        for xx in (0, w-1):
            i = y*w+xx
            if not visited[i] and isbg(px[xx, y]): visited[i] = 1; q.append((xx, y))
    while q:
        x, y = q.popleft()
        p = px[x, y]; px[x, y] = (p[0], p[1], p[2], 0)
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x+dx, y+dy
            if 0 <= nx < w and 0 <= ny < h:
                i = ny*w+nx
                if not visited[i] and isbg(px[nx, ny]): visited[i] = 1; q.append((nx, ny))
    return im


def autocrop(im, top_frac=0.0, bottom_frac=0.0, margin=0.0, thr=16):
    im = im.convert("RGBA")
    w, h = im.size
    a = im.split()[3].point(lambda p: 255 if p > thr else 0)
    left = int(w*margin); top = int(h*max(top_frac, margin))
    right = w-int(w*margin); bottom = h-int(h*max(bottom_frac, margin))
    bbx = a.crop((left, top, right, bottom)).getbbox()
    if not bbx:
        return None
    x0, y0, x1, y1 = bbx
    return im.crop((left+x0, top+y0, left+x1, top+y1))


def fit_square(im, size):
    im = im.convert("RGBA")
    w, h = im.size
    s = max(w, h)
    canvas = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    canvas.paste(im, ((s-w)//2, (s-h)//2), im)
    return canvas.resize((size, size), Image.LANCZOS)


def save(im, folder, name):
    os.makedirs(folder, exist_ok=True)
    im.save(os.path.join(folder, name))


def sheet(fn):
    p = os.path.join(SRC, fn)
    return Image.open(p).convert("RGBA") if os.path.exists(p) else None


def slice_grid(fn, rows, cols, names, out_size, bottom_label=False):
    img = sheet(fn)
    if img is None:
        print("  (falta %s)" % fn); return 0
    bot = 0.15 if bottom_label else 0.02
    n = 0
    for idx, name in enumerate(names):
        if not name:
            continue
        r, c = idx // cols, idx % cols
        cl = remove_bg_flood(cell(img, r, c, rows, cols, inset=0.03))
        cr = autocrop(cl, top_frac=0.02, bottom_frac=bot, margin=0.01)
        if cr:
            save(fit_square(cr, out_size), TILES, name + ".png"); n += 1
    print("  %s -> %d sprites" % (fn, n))
    return n


def slice_grid_portrait(fn, rows, cols, names, out_size, bottom_trim=0.15, top_trim=0.02):
    """Para jefes/guardianes: su fondo es una ESCENA (no plano), así que se recorta la
    celda como retrato OPACO y se elimina la franja del rótulo inferior (no se quita el fondo)."""
    img = sheet(fn)
    if img is None:
        print("  (falta %s)" % fn); return 0
    n = 0
    for idx, name in enumerate(names):
        if not name:
            continue
        r, c = idx // cols, idx % cols
        cl = cell(img, r, c, rows, cols, inset=0.02).convert("RGBA")
        w, h = cl.size
        cl = cl.crop((int(w*0.02), int(h*top_trim), int(w*0.98), int(h*(1-bottom_trim))))
        save(fit_square(cl, out_size), TILES, name + ".png"); n += 1
    print("  %s -> %d sprites" % (fn, n))
    return n


def enemy_names(part):
    out = []
    for md in PART_MODS[part]:
        for i in range(3):
            out.append("e_p%dm%d_%d" % (part, md, i))
    return out


def boss_names(part):
    return ["b_p%dm%d" % (part, md) for md in PART_MODS[part]]


def do_enemies():
    print("Enemigos:")
    total = 0
    for p in range(1, 7):
        rows = len(PART_MODS[p])
        total += slice_grid("enemies_p%d.png" % p, rows, 3, enemy_names(p), 112, bottom_label=False)
    print("  TOTAL enemigos:", total)


def do_bosses():
    print("Jefes:")
    total = 0
    for p in range(1, 7):
        cols = len(PART_MODS[p])
        total += slice_grid_portrait("bosses_p%d.png" % p, 1, cols, boss_names(p), 200, bottom_trim=0.14)
    print("  TOTAL jefes:", total)


def do_guardians():
    print("Guardianes + jefe final:")
    names = ["guard_d1", "guard_d2", "guard_d3", "guard_d4",
             "guard_d5", "guard_d6", "boss_final", None]
    slice_grid_portrait("guardians.png", 2, 4, names, 220, bottom_trim=0.13)


def do_backdrops():
    print("Fondos:")
    for name in ["TitleVista", "TownPlaza", "BattleBastion", "BattleCrypt", "BattleFactory", "BattleThrone"]:
        p = os.path.join(SRC, name + ".png")
        if not os.path.exists(p):
            print("  (falta %s.png)" % name); continue
        im = Image.open(p).convert("RGB")
        w, h = im.size
        nw = 1280; nh = int(h * nw / w)
        save(im.resize((nw, nh), Image.LANCZOS), EXT, name + ".png")
        print("  ->", name + ".png")


def do_icon():
    p = os.path.join(SRC, "AppIcon.png")
    if not os.path.exists(p):
        print("  (falta AppIcon.png)"); return
    os.makedirs(os.path.dirname(ICON), exist_ok=True)
    Image.open(p).convert("RGB").resize((512, 512), Image.LANCZOS).save(ICON)
    print("Icono -> Assets/Art/Icon/AppIcon.png")


if __name__ == "__main__":
    do_enemies()
    do_bosses()
    do_guardians()
    do_backdrops()
    do_icon()
    print("Listo.")
