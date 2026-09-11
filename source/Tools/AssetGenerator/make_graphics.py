#!/usr/bin/env python3
"""
make_graphics.py - procedural art generator for "Monster Maze" (a MonoGame remake of 3D Monster Maze).

Running  `python3 make_graphics.py`  (from any directory) regenerates every bitmap the game ships with:

  * Content/Textures  - seamless 1024x1024 wall, floor, ceiling and exit textures
  * Content/Sprites   - the Rex sprite sheet (8 frames), the "eaten" jaws and particle sprites
  * Content/UI        - nine-slice button/panel, icon atlas, vignette, logo and full-screen backgrounds
  * App icons for iOS and Android, the Android splash, the iOS launch logo and store artwork

Everything is drawn with Pillow (no numpy) using fixed random seeds, so the output is identical on
every run.  Most artwork is drawn at 2x resolution and downsampled with LANCZOS for anti-aliasing.
Tileable textures wrap their noise, blurs and shapes around the edges so they repeat without seams.
"""
import colorsys
import json
import math
import os
import random
import time

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont, ImageOps

# --------------------------------------------------------------------------------------------------
# Paths
# --------------------------------------------------------------------------------------------------
HERE = os.path.dirname(os.path.abspath(__file__))
B = os.path.normpath(os.path.join(HERE, "..", "Template"))         # solution folder
C = os.path.join(B, "MonsterMaze.Core", "Content")                  # MonoGame content folder
FONT_BOLD = "/Applications/LibreOffice.app/Contents/Resources/fonts/truetype/DejaVuSans-Bold.ttf"


def save(img, path):
    """Save a PNG, creating folders as needed, and report its size.  Fully transparent pixels get
    black RGB so nothing leaks when MonoGame premultiplies alpha (white-only sprites stay white)."""
    os.makedirs(os.path.dirname(path), exist_ok=True)
    if img.mode == "RGBA" and img.getextrema()[:3] != ((255, 255), (255, 255), (255, 255)):
        img = Image.composite(img, Image.new("RGBA", img.size, (0, 0, 0, 0)), img.getchannel("A").point(lambda v: 255 if v else 0))
    img.save(path)
    print("  %-70s %dx%d %s" % (os.path.relpath(path, B), img.size[0], img.size[1], img.mode))


# --------------------------------------------------------------------------------------------------
# Small maths / colour helpers
# --------------------------------------------------------------------------------------------------
def clamp(v, lo=0, hi=255):
    return lo if v < lo else hi if v > hi else v


def lerp(a, b, t):
    return a + (b - a) * t


def mix(c1, c2, t):
    """Linear blend of two RGB(A) tuples."""
    return tuple(int(round(lerp(a, b, t))) for a, b in zip(c1, c2))


def jitter_colour(rng, rgb, dh=0.02, ds=0.08, dv=0.10):
    """Randomly nudge hue, saturation and value of an RGB colour."""
    h, s, v = colorsys.rgb_to_hsv(*[c / 255.0 for c in rgb])
    h = (h + rng.uniform(-dh, dh)) % 1.0
    s = min(1, max(0, s + rng.uniform(-ds, ds)))
    v = min(1, max(0, v * (1 + rng.uniform(-dv, dv))))
    return tuple(int(c * 255) for c in colorsys.hsv_to_rgb(h, s, v))


def lut_from_stops(stops):
    """Build 256-entry R, G, B lookup tables from [(position 0..1, (r,g,b)), ...] colour stops."""
    tables = ([], [], [])
    for i in range(256):
        t = i / 255.0
        for (p0, c0), (p1, c1) in zip(stops, stops[1:]):
            if t <= p1 or (p1, c1) == stops[-1]:
                k = 0 if p1 == p0 else min(1, max(0, (t - p0) / (p1 - p0)))
                col = mix(c0, c1, k)
                break
        for ch in range(3):
            tables[ch].append(col[ch])
    return tables


def colorize(gray, stops):
    """Map a greyscale image through a multi-stop colour ramp (a gradient map)."""
    r, g, b = lut_from_stops(stops)
    return Image.merge("RGB", (gray.point(r), gray.point(g), gray.point(b)))


def vgradient(w, h, stops):
    """Vertical multi-stop gradient, top (0) to bottom (1)."""
    ramp = Image.linear_gradient("L").resize((1, h), Image.BILINEAR)
    return colorize(ramp, stops).resize((w, h))


def radial(w, h, cx=0.5, cy=0.5, rx=0.5, ry=0.5):
    """Greyscale radial distance field: 0 at (cx,cy), 255 at the ellipse of radii (rx,ry) (fractions)."""
    # Image.radial_gradient is 256x256 with 0 in the centre and 255 at the corners (radius ~181),
    # so at radius 128 it reads ~180: stretch radius 128 onto the ellipse and rescale the values.
    size = (max(1, int(w * rx * 2)), max(1, int(h * ry * 2)))
    g = Image.radial_gradient("L").point(lambda v: min(255, int(v * 255 / 180.3))).resize(size, Image.BILINEAR)
    out = Image.new("L", (w, h), 255)
    out.paste(g, (int(w * cx - size[0] / 2), int(h * cy - size[1] / 2)))
    return out


def contrast_layer(img, amount):
    """Remap 0..255 around mid-grey to 128 +/- amount (a neutral layer for hard-light blending)."""
    return img.point(lambda v: int(128 + (v - 128) * amount / 128.0))


def light(base, layer):
    """Hard-light a greyscale layer onto an RGB image: 128 = no change, brighter lightens, darker darkens."""
    return ImageChops.hard_light(base, layer.convert("RGB"))


def add_glow(base, colour, mask, strength=1.0):
    """Additively blend a coloured glow (mask * colour) onto an RGB image."""
    if strength != 1.0:
        mask = mask.point(lambda v: clamp(int(v * strength)))
    glow = ImageChops.multiply(Image.new("RGB", base.size, colour), mask.convert("RGB"))
    return ImageChops.add(base, glow)


def scale_mask(mask, k):
    return mask.point(lambda v: clamp(int(v * k)))


# --------------------------------------------------------------------------------------------------
# Seamless (wrap-around) noise and filters
# --------------------------------------------------------------------------------------------------
def rand_image(rng, w, h):
    """Uniform white noise from a seeded RNG (reproducible, unlike Image.effect_noise)."""
    return Image.frombytes("L", (w, h), rng.randbytes(w * h))


def value_noise(size, cells, rng):
    """Smooth, tileable value noise: a cells x cells random grid enlarged bicubically with wrapping."""
    pad = 2
    grid = wrap_pad(rand_image(rng, cells, cells), pad)
    step = size / cells
    full = grid.resize((round((cells + 2 * pad) * step),) * 2, Image.BICUBIC)
    o = round(pad * step)
    return full.crop((o, o, o + size, o + size))


def fractal_noise(size, rng, cells=4, octaves=5, persistence=0.55):
    """Sum several octaves of value noise (fBm), then stretch to the full 0..255 range."""
    acc, total, amp = value_noise(size, cells, rng), 1.0, 1.0
    for k in range(1, octaves):
        c = cells * 2 ** k
        if c > size // 2:
            break
        amp *= persistence
        acc = Image.blend(acc, value_noise(size, c, rng), amp / (total + amp))
        total += amp
    return ImageOps.autocontrast(acc, cutoff=0.5)


def wrap_pad(img, pad):
    """Surround an image with `pad` pixels copied from the opposite edges (as if it tiled)."""
    w, h = img.size
    big = Image.new(img.mode, (w + 2 * pad, h + 2 * pad))
    for dx in (-w, 0, w):
        for dy in (-h, 0, h):
            big.paste(img, (pad + dx, pad + dy))
    return big


def wrap_filter(img, flt, pad):
    """Apply a filter as if the image repeated forever (no seams at the borders)."""
    w, h = img.size
    pad = min(pad, w, h)
    return wrap_pad(img, pad).filter(flt).crop((pad, pad, pad + w, pad + h))


def wrap_blur(img, radius):
    return wrap_filter(img, ImageFilter.GaussianBlur(radius), int(radius * 3) + 4)


def wrap_resize(img, size):
    """Downsample a tileable image with LANCZOS without introducing edge artefacts."""
    w, h = img.size
    k = w / size
    pad = int(8 * k)
    big = wrap_pad(img, pad)
    big = big.resize((round(big.size[0] / k), round(big.size[1] / k)), Image.LANCZOS)
    p = round(pad / k)
    return big.crop((p, p, p + size, p + size))


def wrapped(size, fn):
    """Call fn(dx, dy) for the 9 wrap offsets so shapes crossing an edge reappear on the other side."""
    for dx in (-size, 0, size):
        for dy in (-size, 0, size):
            fn(dx, dy)


def random_walk(rng, x, y, angle, steps, step_len, wiggle):
    """A jagged polyline, used for cracks, roots and scratches."""
    pts = [(x, y)]
    for _ in range(steps):
        angle += rng.uniform(-wiggle, wiggle)
        x += math.cos(angle) * step_len
        y += math.sin(angle) * step_len
        pts.append((x, y))
    return pts


def shift(pts, dx, dy):
    return [(x + dx, y + dy) for x, y in pts]


def relief_shading(height, depth=2.0, d=3, limit=70):
    """Emboss-style lighting from a height map, lit from the top-left.
    Returns a neutral-128 layer: bright on slopes facing the light, dark on the far side.
    Values are soft-clipped to 128 +/- limit so steep edges don't burn out to pure white/black."""
    soft = [int(128 + limit * math.tanh((v - 128) / float(limit))) for v in range(256)]
    return ImageChops.subtract(height, ImageChops.offset(height, d, d), scale=1.0 / depth, offset=128).point(soft)


# ==================================================================================================
# TEXTURES
# ==================================================================================================
TEX = 1024          # final texture size
HI = TEX * 2        # working resolution

WALL_PALETTE = [    # (weight, colour) - slate blues and teals with a few warm and violet stones
    (5, (102, 128, 166)), (4, (78, 140, 146)), (3, (94, 116, 154)), (2, (68, 122, 138)),
    (1.2, (178, 138, 78)), (1.0, (128, 106, 162)), (0.6, (156, 112, 94)),
]


def pick(rng, palette):
    total = sum(w for w, _ in palette)
    r = rng.uniform(0, total)
    for w, c in palette:
        r -= w
        if r <= 0:
            return c
    return palette[-1][1]


def block_layout(rng, courses, wmin, wmax, split=0.2):
    """Irregular running-bond layout at working resolution: returns a list of (x, y, w, h) cells.
    Each course is filled with random widths and started at a random offset (wrapping)."""
    cells = []
    ch = HI / courses
    for r in range(courses):
        widths, remaining = [], HI
        while remaining > 0:
            w = rng.uniform(wmin, wmax) * 2
            if remaining - w < wmin * 2:
                w = remaining
            widths.append(w)
            remaining -= w
        x = rng.uniform(0, HI)
        for w in widths:
            if rng.random() < split and w > wmin * 2:       # occasionally two stacked smaller stones
                k = rng.uniform(0.4, 0.6)
                cells.append((x, r * ch, w, ch * k))
                cells.append((x, r * ch + ch * k, w, ch * (1 - k)))
            else:
                cells.append((x, r * ch, w, ch))
            x += w
    return cells


def rough_rect(rng, x, y, w, h, gap, rough):
    """Polygon for one stone: a rectangle inset by the mortar gap, with chipped corners and bumpy edges."""
    g = [gap + rng.uniform(-0.3, 0.6) * gap for _ in range(4)]         # uneven joints on each side
    x0, y0, x1, y1 = x + g[0], y + g[1], x + w - g[2], y + h - g[3]
    corners = [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]
    pts = []
    for i in range(4):
        (ax, ay), (bx, by) = corners[i], corners[(i + 1) % 4]
        cut = rng.uniform(6, 30)                                        # chipped corner
        length = math.hypot(bx - ax, by - ay)
        n = max(3, int(length / 45))
        for k in range(n + 1):
            t = cut / length + (1 - 2 * cut / length) * k / n
            pts.append((lerp(ax, bx, t) + rng.uniform(-rough, rough), lerp(ay, by, t) + rng.uniform(-rough, rough)))
    return pts


def stone_surface(seed, palette, courses=4, wmin=170, wmax=400, gap=14, rough=4, mortar=(44, 40, 46),
                  split=0.12, bevel=12, bump=0.6, cracks=6):
    """Build a stone-block texture at working resolution.
    Returns (albedo RGB, stone mask L, height L) so variants can decorate the pieces before lighting."""
    rng = random.Random(seed)
    colours = Image.new("RGB", (HI, HI), mortar)
    mask = Image.new("L", (HI, HI), 0)
    dc, dm = ImageDraw.Draw(colours), ImageDraw.Draw(mask)
    for (x, y, w, h) in block_layout(rng, courses, wmin, wmax, split):
        col = jitter_colour(rng, pick(rng, palette), dv=0.16)
        poly = rough_rect(rng, x, y, w, h, gap, rough)
        full = [(x, y), (x + w, y), (x + w, y + h), (x, y + h)]

        def draw(dx, dy):
            dc.polygon(shift(full, dx, dy), fill=col)
            dm.polygon(shift(poly, dx, dy), fill=255)
        wrapped(HI, draw)

    # Round the stone corners: blur then re-threshold the mask.
    mask = wrap_blur(mask, 6).point(lambda v: 255 if v > 128 else 0)
    # Gritty mortar between the stones.
    grit = contrast_layer(fractal_noise(HI, rng, cells=128, octaves=2), 50)
    colours = Image.composite(colours, light(Image.new("RGB", (HI, HI), mortar), grit), mask)

    # Height: rounded bevels + gently domed faces + surface roughness (fine and coarse).
    rounded = [clamp(int(255 * math.sqrt(max(0, v - 128) / 127.0))) for v in range(256)]
    bev = wrap_blur(mask, bevel).point(rounded)
    dome = wrap_blur(mask, bevel * 6)
    height = Image.blend(bev, dome, 0.3)
    ridged = ImageOps.invert(ImageChops.difference(fractal_noise(HI, rng, cells=8, octaves=6, persistence=0.6),
                                                   Image.new("L", (HI, HI), 128)))
    rough_n = Image.blend(fractal_noise(HI, rng, cells=4, octaves=7, persistence=0.62), ridged, 0.4)
    rough_n = Image.blend(rough_n, value_noise(HI, 512, rng), 0.18)          # fine granular texture
    height = ImageChops.multiply(height, rough_n.point(lambda v: int(255 - bump * (255 - v))))
    mortar_n = fractal_noise(HI, rng, cells=64, octaves=3).point(lambda v: v // 6)
    height = ImageChops.add(height, mortar_n)

    # Hairline cracks, kept inside the stones, carved into the height map and darkened in colour.
    crack = Image.new("L", (HI, HI), 0)
    dk = ImageDraw.Draw(crack)
    for _ in range(cracks):
        pts = random_walk(rng, rng.uniform(0, HI), rng.uniform(0, HI), rng.uniform(0, 6.28),
                          rng.randint(8, 18), rng.uniform(10, 16), 0.55)
        wrapped(HI, lambda dx, dy: dk.line(shift(pts, dx, dy), fill=255, width=4, joint="curve"))
    crack = ImageChops.multiply(crack, mask.filter(ImageFilter.MinFilter(9)))
    height = ImageChops.subtract(height, crack)
    colours = Image.composite(Image.new("RGB", (HI, HI), mortar), colours, scale_mask(crack, 0.7))

    # Colour variation inside each stone: blotchy light/dark plus a slight warm/cool drift,
    # and darker "ambient occlusion" towards the joints.
    blotch = contrast_layer(fractal_noise(HI, rng, cells=8, octaves=6, persistence=0.65), 30)
    colours = light(colours, blotch)
    drift = Image.merge("RGB", [contrast_layer(value_noise(HI, 16, rng), 9) for _ in range(3)])
    colours = ImageChops.hard_light(colours, drift)
    ao = wrap_blur(mask, 10).point(lambda v: 150 + int(105 * v / 255))
    colours = ImageChops.multiply(colours, ao.convert("RGB"))
    return colours, mask, height


def shade_surface(albedo, height, depth=1.6, grain_seed=1, grain=22, final=True):
    """Light the albedo with the height map, add fine grain and (optionally) downsample to the final size."""
    img = light(albedo, relief_shading(height, depth))
    img = light(img, relief_shading(wrap_blur(height, 10), depth * 1.5, d=10))  # broad soft shading
    g = wrap_blur(rand_image(random.Random(grain_seed), HI, HI), 0.7)
    img = light(img, contrast_layer(ImageOps.autocontrast(g), grain))
    return wrap_resize(img, TEX) if final else img


def make_wall_stone():
    albedo, mask, height = stone_surface(101, WALL_PALETTE)
    return shade_surface(albedo, height)


def make_wall_moss():
    """Stone wall with moss creeping up from the bottom and roots hanging from the top."""
    rng = random.Random(202)
    albedo, mask, height = stone_surface(203, WALL_PALETTE, split=0.3)
    # Moss density: strong near the bottom, fading upwards.  A thin fringe just below the top edge
    # continues the moss of the tile above, so the texture still wraps vertically.
    rows = []
    for y in range(HI):
        up = (HI - y) / HI                        # 0 at bottom edge .. 1 at top edge
        rows.append(clamp(int(255 * max(math.exp(-up * 2.2), math.exp(-(1 - up) * 22)))))
    density = Image.new("L", (1, HI))
    density.putdata(rows)
    density = density.resize((HI, HI))
    blobs = fractal_noise(HI, rng, cells=8, octaves=6, persistence=0.6)
    moss = ImageChops.add(ImageChops.multiply(blobs, density), density.point(lambda v: v // 4))
    moss_m = moss.point(lambda v: clamp((v - 110) * 4))
    # Moss clings more to the mortar joints: grow the mask a little where there is no stone.
    joints = ImageChops.multiply(ImageOps.invert(mask), moss.point(lambda v: clamp((v - 45) * 3)))
    moss_m = wrap_blur(ImageChops.lighter(moss_m, joints), 2)
    fine = fractal_noise(HI, rng, cells=64, octaves=3)
    moss_col = colorize(fine, [(0, (18, 44, 16)), (0.45, (42, 94, 28)), (0.8, (84, 140, 40)), (1, (150, 180, 60))])
    albedo = Image.composite(moss_col, albedo, moss_m)
    height = ImageChops.lighter(height, ImageChops.multiply(moss_m, fine.point(lambda v: 150 + v // 3)))

    # Hanging roots and vines: wiggly lines from the top edge downwards, with a few leaves.
    d = ImageDraw.Draw(albedo)
    dh = ImageDraw.Draw(height)
    for i in range(11):
        x = rng.uniform(0, HI)
        pts = random_walk(rng, x, -10, math.pi / 2, rng.randint(10, 30), rng.uniform(18, 30), 0.3)
        w = rng.randint(9, 17)
        col = rng.choice([(70, 50, 34), (88, 64, 42), (52, 72, 34)])
        leaves = []
        for (lx, ly) in pts[2::2]:
            sz = rng.uniform(12, 22)
            side = rng.choice((-1, 1))
            leaf = [(lx, ly), (lx + side * sz * 1.4, ly - sz * 0.6), (lx + side * sz * 2.4, ly + sz * 0.3),
                    (lx + side * sz * 0.8, ly + sz * 0.6)]
            leaves.append((leaf, jitter_colour(rng, (70, 132, 44), dv=0.2)))
        for dx in (-HI, 0, HI):
            p = shift(pts, dx, 0)
            d.line(p, fill=col, width=w, joint="curve")
            d.line(shift(p, -w // 4, 0), fill=mix(col, (200, 170, 120), 0.35), width=max(2, w // 4), joint="curve")
            dh.line(p, fill=255, width=w, joint="curve")
            for leaf, lc in leaves:
                d.polygon(shift(leaf, dx, 0), fill=lc)
                dh.polygon(shift(leaf, dx, 0), fill=235)
    return shade_surface(albedo, height, grain_seed=2)


def crystal_cluster(rng, draw_col, draw_mask, cx, cy, colour, n, spread_angle):
    """A fan of faceted crystal prisms growing out of a crack: back shards first (darker), front last."""
    shards = []
    for i in range(n):
        a = spread_angle + rng.uniform(-0.9, 0.9)
        shards.append((rng.uniform(0, 1), a, rng.uniform(140, 360), rng.uniform(38, 78)))
    shards.sort()                                            # depth order
    for depth, a, length, width in shards:
        ox, oy = cx + rng.uniform(-40, 40), cy + rng.uniform(-30, 30)
        ux, uy = math.cos(a), math.sin(a)                    # along the shard
        px, py = -uy, ux                                     # across the shard
        body = length * 0.74
        p = lambda u, v: (ox + ux * u + px * v, oy + uy * u + py * v)
        left = [p(0, -width / 2), p(body, -width / 2), p(length, 0), p(body, 0), p(0, 0)]
        right = [p(0, 0), p(body, 0), p(length, 0), p(body, width / 2), p(0, width / 2)]
        k = 0.55 + 0.45 * depth                               # front shards are brighter
        lit, dark = mix((10, 5, 30), mix(colour, (255, 255, 255), 0.45), k), mix((10, 5, 30), colour, k * 0.7)
        # the face turned towards the top-left light is the lit one
        a_face, b_face = (left, right) if (px + py) > 0 else (right, left)
        draw_col.polygon(a_face, fill=lit)
        draw_col.polygon(b_face, fill=dark)
        draw_col.line([p(0, 0), p(body, 0), p(length, 0)], fill=mix(colour, (255, 255, 255), 0.8), width=4)
        draw_col.line([p(body, -width / 2), p(length, 0), p(body, width / 2)], fill=mix(lit, (255, 255, 255), 0.5), width=3)
        draw_mask.polygon(left + right, fill=int(120 + 135 * depth))


def make_wall_crystal():
    """Stone wall with glowing crystal clusters (magenta, cyan and amber) growing out of it."""
    albedo, mask, height = stone_surface(304, WALL_PALETTE)
    img = shade_surface(albedo.point(lambda v: int(v * 0.9)), height, grain_seed=3, final=False)
    clusters = [((430, 1560), (255, 50, 200), -1.9), ((1500, 620), (40, 225, 255), -1.2),
                ((1250, 1760), (255, 170, 30), -1.6), ((300, 380), (40, 225, 255), -0.6)]
    for (cx, cy), col, ang in clusters:
        gmask = Image.new("L", (HI, HI), 0)
        # dark cavity the crystals grow from
        cav = Image.new("L", (HI, HI), 0)
        ImageDraw.Draw(cav).ellipse((cx - 110, cy - 60, cx + 110, cy + 60), fill=200)
        img = Image.composite(Image.new("RGB", (HI, HI), (12, 8, 20)), img, wrap_blur(cav, 25))
        dc, dg = ImageDraw.Draw(img), ImageDraw.Draw(gmask)
        wrapped(HI, lambda dx, dy: crystal_cluster(random.Random(cx * 7 + cy), dc, dg, cx + dx, cy + dy, col, 9, ang))
        img = add_glow(img, col, wrap_blur(gmask, 200), 3.5)       # wide coloured light on the stones
        img = add_glow(img, col, wrap_blur(gmask, 60), 1.6)
        img = add_glow(img, col, wrap_blur(gmask, 16), 0.9)        # tight halo
        core = wrap_blur(gmask.point(lambda v: 255 if v > 220 else 0).filter(ImageFilter.MinFilter(15)), 8)
        img = add_glow(img, (255, 255, 255), core, 0.7)            # bright hot core
    return wrap_resize(img, TEX)


def make_wall_exit():
    """Stone wall with a carved arch and a glowing green EXIT sign pointing right."""
    rng = random.Random(404)
    albedo, mask, height = stone_surface(101, WALL_PALETTE)          # same stones as the plain wall
    cx, base, r_out, r_in = HI // 2, 640, 500, 350                  # arch centre x, spring line y, radii
    band = Image.new("L", (HI, HI), 0)
    inner = Image.new("L", (HI, HI), 0)
    db, di = ImageDraw.Draw(band), ImageDraw.Draw(inner)
    db.ellipse((cx - r_out, base - r_out, cx + r_out, base + r_out), fill=255)
    db.rectangle((cx - r_out, base, cx + r_out, HI), fill=255)
    di.ellipse((cx - r_in, base - r_in, cx + r_in, base + r_in), fill=255)
    di.rectangle((cx - r_in, base, cx + r_in, HI), fill=255)
    band = ImageChops.subtract(band, inner)

    # Voussoirs (wedge stones) around the arch and blocks down the two jambs, each its own colour.
    stones = Image.new("RGB", (HI, HI))
    ds = ImageDraw.Draw(stones)
    grooves = Image.new("L", (HI, HI), 255)
    dg = ImageDraw.Draw(grooves)
    n = 11
    for i in range(n):
        a0, a1 = math.pi + math.pi * i / n, math.pi + math.pi * (i + 1) / n
        col = jitter_colour(rng, (170, 146, 110) if i != n // 2 else (190, 160, 110), dv=0.08)
        ds.pieslice((cx - r_out - 5, base - r_out - 5, cx + r_out + 5, base + r_out + 5),
                    math.degrees(a0), math.degrees(a1), fill=col)
        dg.line([(cx + math.cos(a0) * (r_in - 10), base + math.sin(a0) * (r_in - 10)),
                 (cx + math.cos(a0) * (r_out + 10), base + math.sin(a0) * (r_out + 10))], fill=0, width=8)
    for side in (-1, 1):
        y = base
        while y < HI:
            h = rng.uniform(230, 330)
            x0 = cx + side * r_in if side > 0 else cx - r_out
            ds.rectangle((x0, y, x0 + (r_out - r_in), y + h), fill=jitter_colour(rng, (160, 138, 104), dv=0.08))
            dg.line([(x0 - 10, y), (x0 + r_out - r_in + 10, y)], fill=0, width=8)
            y += h
    stones = light(stones, contrast_layer(fractal_noise(HI, rng, cells=16, octaves=4), 40))
    albedo = Image.composite(stones, albedo, band)

    # The doorway interior is recessed and in shadow.
    albedo = Image.composite(albedo.point(lambda v: int(v * 0.42)), albedo, inner)
    band_h = ImageChops.multiply(wrap_blur(band, 10).point(lambda v: clamp((v - 100) * 2)), grooves)
    height = Image.composite(band_h, height, band)
    height = Image.composite(height.point(lambda v: v // 2), height, inner)
    img = light(albedo, relief_shading(height, 2.2))
    img = light(img, relief_shading(wrap_blur(height, 10), 3.3, d=10))

    # Glowing sign: EXIT lettering above a fat arrow pointing right.
    sign = Image.new("L", (HI, HI), 0)
    d = ImageDraw.Draw(sign)
    font = ImageFont.truetype(FONT_BOLD, 230)
    d.text((cx, 900), "EXIT", font=font, fill=255, anchor="mm")
    ay = 1320
    d.polygon([(cx - 300, ay - 55), (cx + 90, ay - 55), (cx + 90, ay - 150), (cx + 320, ay),
               (cx + 90, ay + 150), (cx + 90, ay + 55), (cx - 300, ay + 55)], fill=255)
    green = (70, 255, 110)
    img = add_glow(img, green, wrap_blur(sign.filter(ImageFilter.MaxFilter(31)), 200), 2.2)  # wash of light
    img = add_glow(img, green, wrap_blur(sign, 40), 1.3)
    img = add_glow(img, green, wrap_blur(sign, 12), 1.0)
    img = Image.composite(Image.new("RGB", (HI, HI), (150, 255, 170)), img, wrap_blur(sign, 1.5))
    core = wrap_blur(sign.filter(ImageFilter.MinFilter(15)), 4)
    img = add_glow(img, (255, 255, 255), core, 0.8)
    g = wrap_blur(rand_image(random.Random(4), HI, HI), 0.7)
    img = light(img, contrast_layer(ImageOps.autocontrast(g), 18))
    return wrap_resize(img, TEX)


FLOOR_PALETTE = [(4, (156, 88, 60)), (3, (132, 76, 54)), (3, (166, 106, 72)), (2, (120, 66, 56)),
                 (2, (146, 98, 70)), (1, (108, 70, 52))]


def make_floor():
    """Worn terracotta flagstones with cracks, grit and dark puddles."""
    rng = random.Random(505)
    albedo, mask, height = stone_surface(507, FLOOR_PALETTE, courses=4, wmin=230, wmax=460, gap=10, rough=6,
                                         mortar=(46, 34, 30), split=0.25, bevel=16, bump=0.7, cracks=16)
    # Puddles: dark, flat, slightly blue-tinted patches with a faint sheen.
    pud = fractal_noise(HI, rng, cells=4, octaves=3).point(lambda v: clamp((v - 200) * 8))
    pud = wrap_blur(pud, 12)
    albedo = Image.composite(ImageChops.multiply(albedo, Image.new("RGB", (HI, HI), (80, 78, 100))), albedo, pud)
    height = Image.composite(height.point(lambda v: 90 + v // 6), height, pud)
    # Grit: scattered light and dark specks.
    d = ImageDraw.Draw(albedo)
    for _ in range(4000):
        x, y, r = rng.uniform(0, HI), rng.uniform(0, HI), rng.uniform(1.5, 4)
        col = rng.choice([(40, 28, 24), (70, 52, 40), (190, 150, 110), (120, 100, 90)])
        wrapped(HI, lambda dx, dy: d.ellipse((x + dx - r, y + dy - r, x + dx + r, y + dy + r), fill=col))
    img = light(albedo, relief_shading(height, 1.3))
    img = light(img, relief_shading(wrap_blur(height, 10), 2.0, d=10))
    sheen = ImageChops.multiply(pud, fractal_noise(HI, rng, cells=8, octaves=3).point(lambda v: clamp((v - 130) * 2)))
    img = add_glow(img, (60, 70, 90), wrap_blur(sheen, 6))
    g = wrap_blur(rand_image(random.Random(5), HI, HI), 0.7)
    img = light(img, contrast_layer(ImageOps.autocontrast(g), 26))
    return wrap_resize(img, TEX)


def ridged(size, rng, cells, octaves):
    """Ridged noise: 1 - |noise - 0.5|, giving sharp crests like broken rock."""
    n = fractal_noise(size, rng, cells=cells, octaves=octaves, persistence=0.55)
    return ImageOps.autocontrast(ImageOps.invert(ImageChops.difference(n, Image.new("L", (size, size), 128))))


def make_ceiling():
    """Rough cave rock in deep indigo and purple with bluish highlights and dark cracks."""
    rng = random.Random(606)
    big = ridged(HI, rng, 4, 6)
    height = Image.blend(big, fractal_noise(HI, rng, cells=8, octaves=8, persistence=0.6), 0.45)
    height = Image.blend(height, ridged(HI, rng, 16, 4), 0.2)
    colour_n = Image.blend(fractal_noise(HI, rng, cells=4, octaves=5), height, 0.5)
    albedo = colorize(colour_n, [(0, (16, 12, 36)), (0.35, (44, 30, 82)), (0.6, (76, 52, 124)),
                                 (0.85, (96, 92, 170)), (1, (130, 150, 210))])
    d, dh = ImageDraw.Draw(albedo), ImageDraw.Draw(height)
    for _ in range(6):
        pts = random_walk(rng, rng.uniform(0, HI), rng.uniform(0, HI), rng.uniform(0, 6.28), rng.randint(14, 30), 26, 0.3)
        branch = random_walk(rng, *pts[len(pts) // 2], rng.uniform(0, 6.28), rng.randint(5, 10), 22, 0.4)
        for line, w in ((pts, rng.randint(5, 8)), (branch, 4)):
            wrapped(HI, lambda dx, dy: (d.line(shift(line, dx, dy), fill=(8, 5, 16), width=w, joint="curve"),
                                        dh.line(shift(line, dx, dy), fill=0, width=w + 6, joint="curve")))
    img = light(albedo, relief_shading(height, 3.0, d=3))
    img = light(img, relief_shading(wrap_blur(height, 14), 5.0, d=14, limit=90))
    g = wrap_blur(rand_image(random.Random(6), HI, HI), 0.7)
    img = light(img, contrast_layer(ImageOps.autocontrast(g), 22))
    return wrap_resize(img, TEX)


def make_exit_light():
    """The blinding daylight seen through the exit: white-gold centre, god rays, pale sky at the edges."""
    S = TEX
    rng = random.Random(707)
    dist = radial(S, S, rx=0.8, ry=0.8)
    img = colorize(dist, [(0, (255, 255, 252)), (0.2, (255, 251, 228)), (0.42, (255, 228, 160)),
                          (0.62, (252, 208, 140)), (0.78, (236, 216, 190)), (0.9, (184, 206, 236)),
                          (1, (150, 190, 240))])
    rays = Image.new("L", (S, S), 0)
    d = ImageDraw.Draw(rays)
    for _ in range(70):
        a = rng.uniform(0, 2 * math.pi)
        w = rng.uniform(0.01, 0.05)
        v = rng.randint(60, 200)
        d.polygon([(S / 2, S / 2), (S / 2 + math.cos(a - w) * S, S / 2 + math.sin(a - w) * S),
                   (S / 2 + math.cos(a + w) * S, S / 2 + math.sin(a + w) * S)], fill=v)
    rays = rays.filter(ImageFilter.GaussianBlur(10))
    fade = ImageOps.invert(dist).point(lambda v: int(255 * (v / 255.0) ** 1.4))
    rays = ImageChops.multiply(rays, fade)
    img = ImageChops.screen(img, ImageChops.multiply(Image.new("RGB", (S, S), (255, 236, 180)), rays.convert("RGB")))
    return img


# ==================================================================================================
# SHAPE AND SHADING TOOLKIT (used by Rex, the jaws, icons and illustrations)
# ==================================================================================================
def spline(pts, closed=True, n=12):
    """Catmull-Rom spline through control points -> smooth polyline (closed by default)."""
    out, N = [], len(pts)
    segs = range(N) if closed else range(N - 1)
    for i in segs:
        p0 = pts[(i - 1) % N] if closed else pts[max(i - 1, 0)]
        p1, p2 = pts[i], pts[(i + 1) % N]
        p3 = pts[(i + 2) % N] if closed else pts[min(i + 2, N - 1)]
        for k in range(n):
            t = k / n
            t2, t3 = t * t, t * t * t
            out.append(tuple(0.5 * (2 * b + (-a + c) * t + (2 * a - 5 * b + 4 * c - d) * t2 + (-a + 3 * b - 3 * c + d) * t3)
                             for a, b, c, d in zip(p0, p1, p2, p3)))
    if not closed:
        out.append(pts[-1])
    return out


def sym(half):
    """Mirror a list of left-half points (x relative to the centre line) into a closed outline."""
    right = [(-x, y) for (x, y) in reversed(half) if abs(x) > 1e-6]
    return list(half) + right


def tapered(path, w0, w1):
    """Polygon for a stroke along `path` whose width tapers from w0 to w1 (tails, arms, claws)."""
    left, right, n = [], [], len(path)
    for i, (x, y) in enumerate(path):
        (ax, ay), (bx, by) = path[max(i - 1, 0)], path[min(i + 1, n - 1)]
        dx, dy = bx - ax, by - ay
        ln = math.hypot(dx, dy) or 1
        w = lerp(w0, w1, i / (n - 1)) / 2
        left.append((x - dy / ln * w, y + dx / ln * w))
        right.append((x + dy / ln * w, y - dx / ln * w))
    return left + right[::-1]


def capsule_mask(size, path, w0, w1, n=8):
    """Mask of a tapered stroke with rounded ends (limbs, fingers, toes, tails)."""
    pts = spline(path, closed=False, n=n) if len(path) > 2 else path
    m = poly_mask(size, tapered(pts, w0, w1))
    d = ImageDraw.Draw(m)
    for (x, y), w in ((pts[0], w0), (pts[-1], w1)):
        d.ellipse((x - w / 2, y - w / 2, x + w / 2, y + w / 2), fill=255)
    return m


def poly_mask(size, pts, fill=255):
    m = Image.new("L", size, 0)
    ImageDraw.Draw(m).polygon(pts, fill=fill)
    return m


def move(img, dx, dy):
    """Shift an image without wrapping (vacated area is black / transparent)."""
    out = Image.new(img.mode, img.size, 0)
    out.paste(img, (int(dx), int(dy)))
    return out


def dilate(mask, px):
    """Grow a mask by roughly `px` pixels (blur + threshold, cheaper than a big MaxFilter)."""
    return mask.filter(ImageFilter.GaussianBlur(px / 2.0)).point(lambda v: 255 if v > 24 else int(v * 10))


def scale_texture(size, spacing, seed):
    """Neutral-grey relief layer of pebbly reptile scales: randomly scattered, overlapping bumps."""
    rng = random.Random(seed)
    w, h = size
    hm = Image.new("L", size, 0)
    d = ImageDraw.Draw(hm)
    for _ in range(int(w * h / (spacing * spacing) * 1.6)):
        x, y = rng.uniform(0, w), rng.uniform(0, h)
        r = spacing * rng.uniform(0.3, 0.62)
        v = rng.randint(140, 255)
        d.ellipse((x - r, y - r * 0.85, x + r, y + r * 0.85), fill=v)
        d.ellipse((x - r * 0.5, y - r * 0.5, x + r * 0.45, y + r * 0.3), fill=min(255, v + 45))
    hm = hm.filter(ImageFilter.GaussianBlur(spacing * 0.1))
    return ImageChops.subtract(hm, move(hm, 0, 2), scale=0.5, offset=128)


def shade_part(mask, top, bottom, rim=0.35, soft_frac=0.14, top_light=60, tex=None, tex_amount=1.0,
               light_dy=None):
    """Turn a flat mask into a rounded, lit form:
    * vertical colour gradient (top -> bottom colour) across the part's bounding box
    * 'rim darkness': edges fall off towards rim*colour, the middle faces the viewer and stays bright
    * a highlight along upward-facing edges (light from above)
    * an optional scale texture.  Returns an RGB image (only meaningful inside `mask`)."""
    box = mask.getbbox()
    if not box:
        return Image.new("RGB", mask.size)
    x0, y0, x1, y1 = box
    col = Image.new("RGB", mask.size, top)
    col.paste(vgradient(x1 - x0, y1 - y0, [(0, top), (1, bottom)]), (x0, y0))
    size = max(x1 - x0, y1 - y0)
    soft = mask.filter(ImageFilter.GaussianBlur(max(2, size * soft_frac)))
    rim_lut = [int(255 * (rim + (1 - rim) * min(1.0, v / 190.0) ** 0.8)) for v in range(256)]
    col = ImageChops.multiply(col, soft.point(rim_lut).convert("RGB"))
    dy = light_dy or max(2, int(size * 0.05))
    hl = ImageChops.subtract(soft, move(soft, 0, dy), scale=1.0, offset=0)       # rises towards the top edge
    col = ImageChops.add(col, hl.point(lambda v: int(min(255, v * 2.2) * top_light / 255)).convert("RGB"))
    sh = ImageChops.subtract(soft, move(soft, 0, -dy), scale=1.0, offset=0)      # rises towards the bottom edge
    col = ImageChops.subtract(col, sh.point(lambda v: int(min(255, v * 2.0) * 0.5)).convert("RGB"))
    if tex is not None:
        t = tex if tex_amount == 1.0 else contrast_layer(tex, 127 * tex_amount)
        col = light(col, t)
    return col


class Layers:
    """An RGBA canvas that parts are painted onto back-to-front, each with a dark outline."""

    def __init__(self, size, outline=(12, 16, 8), outline_px=5):
        self.img = Image.new("RGBA", size, (0, 0, 0, 0))
        self.size = size
        self.outline = outline
        self.outline_px = outline_px

    def shadow(self, mask, blur, dy, strength=0.6):
        """Soft ambient shadow cast by a part onto what is already painted (depth separation)."""
        a = ImageChops.multiply(move(mask, 0, dy).filter(ImageFilter.GaussianBlur(blur)), self.img.getchannel("A"))
        a = a.point(lambda v: int(v * strength))
        self.img = Image.alpha_composite(self.img, Image.merge("RGBA", (*Image.new("RGB", self.size, (0, 0, 0)).split(), a)))

    def paint(self, rgb, mask, outline=True, shadow=0, shadow_strength=0.6):
        if shadow:
            self.shadow(mask, shadow, shadow // 2, shadow_strength)
        if outline and self.outline_px:
            ring = dilate(mask, self.outline_px)
            self.img = Image.alpha_composite(self.img, Image.merge("RGBA", (*Image.new("RGB", self.size, self.outline).split(), ring)))
        if isinstance(rgb, tuple):
            rgb = Image.new("RGB", self.size, rgb)
        self.img = Image.alpha_composite(self.img, Image.merge("RGBA", (*rgb.split(), mask)))

    def silhouette(self):
        return self.img.getchannel("A")




# ==================================================================================================
# REX - the Tyrannosaurus, seen from the front, built from layered spline shapes
# ==================================================================================================
FW, FH = 512, 640                 # final frame size
RW, RH = FW * 2, FH * 2           # working frame size (2x)
RCX = RW // 2                     # centre line

HIDE_TOP, HIDE_BOT = (104, 134, 48), (48, 74, 24)
HEAD_TOP, HEAD_BOT = (104, 132, 46), (54, 80, 26)
STRIPE_MUL = (100, 112, 92)        # multiplied into the hide for the dark stripes
BELLY_TOP, BELLY_BOT = (242, 196, 100), (206, 124, 48)
IVORY, IVORY_SHADE = (248, 240, 214), (192, 174, 134)
MOUTH_DARK, MOUTH_RED = (36, 3, 6), (156, 24, 32)
EYE_GLOW = (255, 176, 30)
HEAD_Y, HEAD_S = 296, 1.3          # rest position and size of the head


class RexPose:
    """Everything that changes between animation frames."""

    def __init__(self, **kw):
        self.dx = 0.0            # sideways sway of the upper body
        self.dy = 0.0            # vertical bob of the upper body
        self.breath = 0.0        # chest expansion 0..1
        self.head_x = 0.0        # head offset from its rest position
        self.head_y = 0.0
        self.head_s = 1.0        # extra head scale (lunge = much bigger, i.e. closer)
        self.jaw = 0.0           # 0 closed .. 1 wide open
        self.head_tilt = 0.0     # roar: head thrown back
        self.squint = 0.0
        self.lift = (0.0, 0.0)   # leg lift, (viewer-left, viewer-right)
        self.near = (1.0, 1.0)   # leg scale about the foot (the forward leg is closer)
        self.arms = 0.0          # 0 hanging .. 1 flexed / raised
        self.tail = 1.0          # side the tail swings out to (-1..1)
        self.tilt = 0.0          # whole-body roll in degrees (pivot = planted foot)
        self.pivot = 0.0         # x (relative to centre) of the pivot foot
        self.__dict__.update(kw)


def P(x, y):
    """Rex-space point (x relative to the centre line) -> canvas pixel."""
    return (RCX + x, y)


def rex_mask(pts):
    return poly_mask((RW, RH), spline(pts))


def striped(col, mask, stripes, blur=4):
    """Darken the hide along a stripe mask (restricted to the part's own mask)."""
    stripes = ImageChops.multiply(stripes.filter(ImageFilter.GaussianBlur(blur)), mask)
    return Image.composite(ImageChops.multiply(col, Image.new("RGB", col.size, STRIPE_MUL)), col, stripes)


def stripe_band(draw, pts, width, rng):
    """One irregular tiger-stripe: a tapered band with a wobbly width."""
    draw.polygon(tapered(spline(pts, closed=False, n=6), width * rng.uniform(0.8, 1.2), 2), fill=230)


def leg_point(side, pose, x, y, part):
    """Map a leg-space point to the canvas.  Lifted legs fold up; the planted, forward leg is
    scaled up slightly about its foot (it is closer to the camera)."""
    i = 0 if side < 0 else 1
    lift, near = pose.lift[i], pose.near[i]
    fx = side * 210
    x, y = fx + (x - fx) * near, RH + (y - RH) * near
    if part == "thigh":
        y -= lift * 50 * max(0, y - 700) / 300.0
        x += pose.dx * 0.8
        y += pose.dy
    elif part == "shin":
        y = 950 + (y - 950) * (1 - 0.25 * lift) - lift * 60
        x += pose.dx * 0.4
    else:
        y -= lift * 120
    return P(x, y)


def draw_leg(L, side, pose, tex, part):
    """part = "lower" (shin, foot, toes) or "thigh" (drawn later, over the torso)."""
    o = side
    lift = pose.lift[0 if side < 0 else 1]
    T = lambda x, y, part: leg_point(side, pose, o * x, y, part)
    if part == "thigh":
        draw_thigh(L, side, pose, tex, T)
        return
    # Shin
    m = rex_mask([T(x, y, "shin") for x, y in [(150, 930), (272, 930), (268, 1030), (244, 1160), (170, 1160), (152, 1040)]])
    L.paint(shade_part(m, (92, 118, 42), (44, 68, 22), tex=tex, rim=0.3), m)
    # Foot: a heavy pad and three forward-pointing toes, each with a hooked claw
    m = rex_mask([T(x, y, "foot") for x, y in [(145, 1135), (270, 1135), (292, 1200), (210, 1228), (126, 1200)]])
    L.paint(shade_part(m, HIDE_TOP, HIDE_BOT, tex=tex, rim=0.35), m)
    droop = lift * 20
    for (bx, by), (tx, ty), w in (((270, 1172), (306, 1234), 52), ((148, 1172), (112, 1232), 52), ((210, 1170), (210, 1238), 66)):
        path = [T(bx, by, "foot"), T(lerp(bx, tx, 0.55), lerp(by, ty, 0.55) + droop, "foot"), T(tx, ty + droop, "foot")]
        m = capsule_mask((RW, RH), path, w, w * 0.8)
        L.paint(shade_part(m, (136, 156, 62), (64, 84, 30), tex=tex, rim=0.3), m)
        cx, cy = path[-1]
        cw = w * 0.36
        bottom = RH - 1 - lift * 120
        claw = [(cx - cw, cy - 6), (cx + cw, cy - 6), (cx + cw * 0.35, (cy + bottom) / 2), (cx, bottom),
                (cx - cw * 0.5, (cy + bottom) / 2 - 4)]
        m = poly_mask((RW, RH), spline(claw, n=6))
        L.paint(shade_part(m, (236, 226, 196), (110, 96, 74), rim=0.45, top_light=40), m)


def draw_thigh(L, side, pose, tex, T):
    """Big muscular drumstick with stripes."""
    m = rex_mask([T(x, y, "thigh") for x, y in [(116, 700), (240, 676), (334, 758), (350, 870), (310, 970),
                                                (232, 1012), (150, 996), (104, 906), (96, 800)]])
    col = shade_part(m, HIDE_TOP, HIDE_BOT, tex=tex, rim=0.28, soft_frac=0.2)
    st = Image.new("L", (RW, RH), 0)
    ds, rng = ImageDraw.Draw(st), random.Random(5 + side)
    for k in range(4):
        y0 = 720 + k * 70
        stripe_band(ds, [T(370, y0, "thigh"), T(290, y0 + 30, "thigh"), T(210 - k * 10, y0 + 70, "thigh")], 44, rng)
    L.paint(striped(col, m, st), m, shadow=26)


def draw_tail(L, pose, tex):
    s = pose.tail
    path = [P(pose.dx, 820), P(s * 210 + pose.dx, 930), P(s * 340, 950), P(s * 420, 895), P(s * 446, 800)]
    m = capsule_mask((RW, RH), path, 220, 14, n=10)
    col = shade_part(m, HIDE_TOP, HIDE_BOT, tex=tex, rim=0.25)
    st = Image.new("L", (RW, RH), 0)
    ds = ImageDraw.Draw(st)
    pts = spline(path, closed=False, n=10)
    for i in range(8, len(pts) - 3, 5):
        (x0, y0), (x1, y1) = pts[i - 1], pts[i + 1]
        a = math.atan2(y1 - y0, x1 - x0) + math.pi / 2
        x, y = pts[i]
        ds.polygon(tapered([(x - math.cos(a) * 120, y - math.sin(a) * 120), (x, y), (x + math.cos(a) * 120, y + math.sin(a) * 120)],
                           26, 26), fill=220)
    L.paint(striped(col, m, st), m)


def draw_torso(L, pose, tex, belly_tex):
    b = 1 + 0.045 * pose.breath
    B = lambda pts: [P(x + pose.dx, y + pose.dy) for x, y in pts]
    half = [(0, 220), (-110, 216), (-196, 260), (-244 * b, 350), (-300 * b, 480), (-306 * b, 610), (-272, 724),
            (-232, 820), (-150, 900), (0, 930)]
    m = rex_mask(B(sym(half)))
    col = shade_part(m, mix(HIDE_TOP, (0, 0, 0), 0.4), mix(HIDE_BOT, (0, 0, 0), 0.1), tex=tex, rim=0.25, soft_frac=0.16)
    belly_half = [(0, 440), (-126, 452), (-196 * b, 570), (-206 * b, 700), (-166, 830), (-104, 896), (0, 912)]
    bm = ImageChops.multiply(rex_mask(B(sym(belly_half))).filter(ImageFilter.GaussianBlur(12)), m)
    # Dark stripes wrapping round the shoulders and flanks (not over the belly)
    st = Image.new("L", (RW, RH), 0)
    ds, rng = ImageDraw.Draw(st), random.Random(3)
    for side in (-1, 1):
        for k in range(7):
            y0 = 230 + k * 88
            stripe_band(ds, B([(side * 340, y0), (side * 270, y0 + 26), (side * (190 - k * 3), y0 + 64)]), 56 - k * 3, rng)
    st = ImageChops.multiply(st, ImageOps.invert(bm))
    col = striped(col, m, st)
    # Belly: warm yellow-orange with crocodile-like ventral bands
    bcol = shade_part(bm.point(lambda v: 255 if v > 100 else 0), BELLY_TOP, BELLY_BOT, rim=0.55, top_light=30,
                      tex=belly_tex, tex_amount=0.5)
    d = ImageDraw.Draw(bcol)
    for y in range(470, 910, 32):
        t = (y - 440) / 472.0
        w = 205 * math.sin(math.pi * min(0.98, 0.25 + t * 0.8)) + 10
        pts = [P(x + pose.dx, y + pose.dy + 18 * (1 - (x / w) ** 2)) for x in [w * (i / 10.0 - 1) for i in range(21)]]
        d.line(pts, fill=(146, 80, 34), width=5)
        d.line([(x, y + 5) for x, y in pts], fill=(252, 218, 146), width=2)
    L.paint(Image.composite(bcol, col, bm), m)


def draw_arm(L, side, pose, tex):
    o, f = side, pose.arms
    sh = (o * 178 + pose.dx, 590 + pose.dy)
    elbow = (lerp(o * 222, o * 272, f) + pose.dx, lerp(650, 610, f) + pose.dy)
    hand = (lerp(o * 176, o * 280, f) + pose.dx, lerp(726, 530, f) + pose.dy)
    m = capsule_mask((RW, RH), [P(*sh), P(*elbow)], 72, 50)
    L.paint(shade_part(m, HIDE_TOP, HIDE_BOT, tex=tex, rim=0.35), m, shadow=16)
    m = capsule_mask((RW, RH), [P(*elbow), P(*hand)], 50, 40)
    L.paint(shade_part(m, HIDE_TOP, HIDE_BOT, tex=tex, rim=0.35), m)
    base = math.atan2(hand[1] - elbow[1], hand[0] - elbow[0])
    for spread in (-0.45, 0.45):                                  # two clawed fingers
        ang = base + spread * (1 + f)
        pts, (x, y) = [P(*hand)], hand
        for j in range(3):
            ang -= 0.28 * spread * 2
            x += math.cos(ang) * 22
            y += math.sin(ang) * 22
            pts.append(P(x, y))
        m = capsule_mask((RW, RH), pts, 22, 13, n=5)
        L.paint(shade_part(m, HIDE_TOP, HIDE_BOT, rim=0.45), m)
        tx, ty = pts[-1]
        claw = [(tx - math.sin(ang) * 7, ty + math.cos(ang) * 7), (tx + math.sin(ang) * 7, ty - math.cos(ang) * 7),
                (tx + math.cos(ang - spread) * 26, ty + math.sin(ang - spread) * 26)]
        L.paint(IVORY, poly_mask((RW, RH), claw))


def tooth(draw, base, tip, width, curl=0.0):
    """One curved conical tooth: dark outline, ivory body, shaded side and a glint."""
    bx, by = base
    tx, ty = tip
    ux, uy = tx - bx, ty - by
    ln = math.hypot(ux, uy) or 1
    px, py = -uy / ln * width / 2, ux / ln * width / 2
    mx, my = bx + ux * 0.55 + px * curl, by + uy * 0.55 + py * curl

    def outline(k, tip_back):
        return spline([(bx + px * k, by + py * k), (mx + px * 0.55 * k, my + py * 0.55 * k),
                       (tx - ux * tip_back, ty - uy * tip_back), (mx - px * 0.55 * k, my - py * 0.55 * k),
                       (bx - px * k, by - py * k)], n=6)
    draw.polygon(outline(1.0, -0.04), fill=(44, 26, 12))
    draw.polygon(outline(0.8, 0.02), fill=IVORY)
    draw.polygon(spline([(mx, my), (tx - ux * 0.02, ty - uy * 0.02), (mx - px * 0.44, my - py * 0.44),
                         (bx - px * 0.8, by - py * 0.8), (bx, by)], n=4), fill=IVORY_SHADE)
    draw.line([(bx + px * 0.4 + ux * 0.12, by + py * 0.4 + uy * 0.12), (mx + px * 0.25, my + py * 0.25)],
              fill=(255, 255, 250), width=max(1, int(width / 7)))


def curve_points(ctrl, count, t0, t1):
    """`count` evenly spaced (point, t) samples along an open spline between parameters t0 and t1."""
    pts = spline(ctrl, closed=False, n=16)
    return [(pts[int(lerp(t0, t1, k / max(1, count - 1)) * (len(pts) - 1))], lerp(t0, t1, k / max(1, count - 1)))
            for k in range(count)]


def draw_head(L, pose, tex, size=(RW, RH), centre=None, scale=None):
    """Rex's head, facing the camera.  Used for the sprite frames and (bigger) for the app icon.
    Head space: origin at the head centre, about 560 wide and 380 tall at scale 1."""
    s = scale if scale is not None else HEAD_S * pose.head_s
    hx, hy = centre if centre is not None else (RCX + pose.dx * 0.9 + pose.head_x, HEAD_Y + pose.dy + pose.head_y)

    def H(x, y):
        if y < 0:
            y *= 1 - 0.22 * pose.head_tilt          # head thrown back: the snout foreshortens
        return (hx + x * s, hy + y * s)

    def mask(pts):
        return poly_mask(size, spline([H(x, y) for x, y in pts]))

    J = 230 * pose.jaw
    w1 = max(1, int(3 * s))
    old_outline = L.outline_px
    L.outline_px = max(4, int(old_outline * s / HEAD_S))

    # Skull and massive jaw-muscle cheeks: wide at the top, narrowing to the jaw corners (a shield shape)
    cm = mask(sym([(0, -150), (-80, -156), (-160, -150), (-228, -120), (-270, -60), (-284, 0), (-272, 60),
                   (-240, 112), (-190, 150), (0, 150)]))
    col = shade_part(cm, HEAD_TOP, HEAD_BOT, tex=tex, rim=0.28, soft_frac=0.2, top_light=70)
    st = Image.new("L", size, 0)
    ds, rng = ImageDraw.Draw(st), random.Random(9)
    for side in (-1, 1):
        for (y0, y1, w) in ((-140, -112, 40), (-70, -56, 36), (0, -12, 40), (64, 40, 32)):
            stripe_band(ds, [H(side * 300, y0), H(side * 240, (y0 + y1) / 2), H(side * 170, y1)], w * s, rng)
    L.paint(striped(col, cm, st, blur=3 * s), cm, shadow=int(44 * s), shadow_strength=0.9)

    # Lower jaw: two jaw bones meeting at the chin, forming a V that swings down (and narrows) by J
    k = J / 230.0
    lip = [(-224, 124), (-168 * (1 - 0.08 * k), 138 + 0.4 * J), (-88 * (1 - 0.15 * k), 152 + 0.78 * J), (0, 158 + J)]
    chin = [(-246, 138), (-204 * (1 - 0.06 * k), 180 + 0.4 * J), (-112 * (1 - 0.12 * k), 214 + 0.78 * J), (0, 226 + J)]
    jm = mask(lip + [(-x, y) for x, y in reversed(lip[:-1])] + [(-x, y) for x, y in chin[:-1]] + list(reversed(chin)))
    L.paint(shade_part(jm, (92, 120, 42), (170, 146, 74), tex=tex, rim=0.35), jm, shadow=int(20 * s))

    upper_lip = [(-224, 124), (-160, 138), (-80, 150), (0, 156), (80, 150), (160, 138), (224, 124)]
    if pose.jaw > 0.02:
        # Mouth: red gums fading into a black throat, a tongue and upward-pointing lower teeth
        mm = mask(upper_lip + [(-x, y) for x, y in lip[1:-1]] + [lip[-1]] + list(reversed(lip[1:-1])))
        x0, y0, x1, y1 = mm.getbbox()
        throat = Image.new("L", size, 255)
        g = Image.radial_gradient("L").resize((max(1, int((x1 - x0) * 0.9)), max(1, int((y1 - y0) * 1.1))))
        throat.paste(g, (x0 + int((x1 - x0) * 0.05), y0 - int((y1 - y0) * 0.25)))
        mcol = colorize(throat, [(0, MOUTH_DARK), (0.4, (84, 6, 14)), (0.75, MOUTH_RED), (1, (200, 60, 64))])
        L.paint(mcol, mm, outline=False)
        tm = ImageChops.multiply(mask(sym([(0, 156 + J * 0.55), (-60, 156 + J * 0.56), (-112, 150 + J * 0.62),
                                           (-60, 156 + J * 0.92), (0, 158 + J * 0.97)])), mm)
        tcol = shade_part(tm, (232, 104, 116), (146, 36, 50), rim=0.45, top_light=80)
        ImageDraw.Draw(tcol).line([H(0, 156 + J * 0.6), H(0, 156 + J * 0.9)], fill=(130, 26, 40), width=int(6 * s))
        L.paint(tcol, tm, outline=False)
        d = ImageDraw.Draw(L.img)
        for side in (-1, 1):
            for (pt, t) in curve_points([(side * x, y) for x, y in lip], 8, 0.1, 0.92):
                ln = (20 + 34 * math.sin(math.pi * min(1.0, t * 1.1))) * (0.6 + 0.4 * pose.jaw)
                bx, by = H(*pt)
                tooth(d, (bx, by + 4 * s), (bx + side * ln * 0.2 * s, by - ln * s), ln * 0.56 * s, curl=-side * 0.25)
        # strings of saliva between the jaws
        if pose.jaw > 0.5:
            for side, xa, xb in ((-1, 120, 104), (1, 60, 50), (1, 150, 130)):
                ya, yb = 150 - xa * 0.1, 150 + J * (1 - xb / 230.0)
                d.line(spline([H(side * xa, ya), H(side * (xa + xb) / 2 + side * 6, (ya + yb) / 2 + 20), H(side * xb, yb)],
                              closed=False, n=8), fill=(236, 220, 214, 255), width=w1)

    # Snout / upper jaw: a narrower muzzle pointing at the camera, widening to the tooth row
    sm = mask(sym([(0, -162), (-44, -158), (-76, -122), (-98, -50), (-116, 20), (-146, 72), (-196, 108),
                   (-226, 122), (-160, 140), (-80, 152), (0, 158)]))
    scol = shade_part(sm, (170, 192, 82), (88, 116, 40), tex=tex, rim=0.38, soft_frac=0.2, top_light=110)
    d = ImageDraw.Draw(scol)
    for side in (-1, 1):
        cx, cy = H(side * 44, 58)                                     # nostrils near the snout tip
        d.ellipse((cx - 18 * s, cy - 9 * s, cx + 18 * s, cy + 9 * s), fill=(16, 20, 6))
        d.arc((cx - 21 * s, cy - 14 * s, cx + 21 * s, cy + 7 * s), 200, 340, fill=(184, 204, 110), width=w1)
        for ax, ay in ((96, 108), (124, 100), (70, 116)):             # snarl wrinkles above the lip
            p0, p1, p2 = H(side * (ax - 22), ay - 14), H(side * ax, ay - 2), H(side * (ax + 20), ay + 8)
            d.line([p0, p1, p2], fill=(44, 62, 20), width=w1 + 1, joint="curve")
    rng = random.Random(77)
    for _ in range(34):                                               # rough bumps along the snout ridge
        x, y = rng.uniform(-90, 90), rng.uniform(-150, 40)
        if abs(x) < 50 + (y + 160) * 0.25:
            cx, cy = H(x, y)
            r = rng.uniform(5, 10) * s
            d.arc((cx - r, cy - r * 0.8, cx + r, cy + r * 0.8), 200, 340, fill=(190, 206, 120), width=w1)
            d.arc((cx - r, cy - r * 0.8, cx + r, cy + r * 0.8), 20, 160, fill=(56, 76, 24), width=w1)
    band = Image.new("L", size, 0)                                    # darker lip band
    ImageDraw.Draw(band).line([H(x, y - 10) for x, y in spline(upper_lip, closed=False)], fill=170, width=int(24 * s))
    scol = Image.composite(ImageChops.multiply(scol, Image.new("RGB", size, (150, 130, 110))), scol,
                           band.filter(ImageFilter.GaussianBlur(7 * s)))
    L.paint(scol, sm, shadow=int(26 * s), shadow_strength=0.8)

    # Upper teeth hanging from the lip line (uneven lengths), and the grim crease at each mouth corner
    d = ImageDraw.Draw(L.img)
    trng = random.Random(21)
    for side in (-1, 1):
        d.line([H(side * 222, 124), H(side * 244, 104), H(side * 256, 74)], fill=(20, 26, 8), width=int(6 * s), joint="curve")
        half = [(side * x, y) for x, y in upper_lip[:4]]
        for (pt, t) in reversed(curve_points(half, 8, 0.06, 0.95)):
            ln = (26 + 38 * math.sin(math.pi * min(1.0, t * 1.2))) * trng.uniform(0.8, 1.15)
            bx, by = H(pt[0], pt[1] - 6)
            tooth(d, (bx, by), (bx - side * ln * 0.14 * s, by + ln * s), ln * 0.54 * s, curl=side * 0.3)

    # Eyes: sunk in dark sockets, glowing amber with slit pupils, under scowling brow ridges
    glow = Image.new("L", size, 0)
    dg = ImageDraw.Draw(glow)
    for side in (-1, 1):
        d = ImageDraw.Draw(L.img)                   # (L.paint below replaces L.img, so re-bind each time)
        ex, ey = H(side * 152, -66)
        rx, ry = 27 * s, 19 * (1 - 0.4 * pose.squint) * s
        d.ellipse((ex - rx * 1.5, ey - ry * 1.7, ex + rx * 1.5, ey + ry * 1.6), fill=(14, 16, 4))
        eye = Image.radial_gradient("L").resize((int(rx * 2), int(ry * 2)))
        eye_col = colorize(eye, [(0, (255, 252, 180)), (0.35, (255, 216, 60)), (0.75, (240, 134, 16)), (1, (140, 50, 4))])
        em = Image.new("L", eye.size, 0)
        ImageDraw.Draw(em).ellipse((0, 0, eye.size[0] - 1, eye.size[1] - 1), fill=255)
        L.img.paste(eye_col, (int(ex - rx), int(ey - ry)), em)
        d.ellipse((ex - 5 * s, ey - ry * 0.92, ex + 5 * s, ey + ry * 0.92), fill=(8, 2, 0))
        gx = ex - side * 11 * s
        d.ellipse((gx - 5 * s, ey - ry * 0.5 - 4 * s, gx + 5 * s, ey - ry * 0.5 + 4 * s), fill=(255, 255, 240))
        dg.ellipse((ex - rx, ey - ry, ex + rx, ey + ry), fill=255)
        lo = 8 * pose.squint
        hm = mask([(side * x, y + lo) for x, y in [(160, -118), (192, -154), (210, -158), (236, -116)]])
        L.paint(shade_part(hm, (128, 116, 70), (52, 46, 24), rim=0.45, top_light=90, tex=tex), hm)
        bm = mask([(side * x, y + lo) for x, y in [(50, -76), (104, -114), (196, -134), (254, -104), (212, -86), (124, -78)]])
        L.paint(shade_part(bm, (122, 138, 60), (34, 50, 16), rim=0.3, top_light=120, tex=tex), bm,
                shadow=int(12 * s), shadow_strength=0.8)
    halo = ImageChops.multiply(glow.filter(ImageFilter.GaussianBlur(24 * s)), L.silhouette())
    rgb = add_glow(L.img.convert("RGB"), EYE_GLOW, halo, 1.5)
    L.img = Image.merge("RGBA", (*rgb.split(), L.img.getchannel("A")))
    L.outline_px = old_outline


def draw_rex(pose, tex, head_tex, belly_tex):
    """Render one 512x640 RGBA frame of Rex for the given pose."""
    L = Layers((RW, RH), outline=(12, 18, 6), outline_px=5)
    tex = move(tex, pose.dx, pose.dy)
    draw_tail(L, pose, tex)
    stepping = [side for side in (-1, 1) if pose.lift[0 if side < 0 else 1] > 0.5]
    for side in (-1, 1):
        if side not in stepping:
            draw_leg(L, side, pose, tex, "lower")
    draw_torso(L, pose, tex, belly_tex)
    for side in (-1, 1):
        draw_leg(L, side, pose, tex, "thigh")
    for side in stepping:                      # a leg swinging forward: knee and shin in front of the thigh
        draw_leg(L, side, pose, tex, "lower")
    for side in (-1, 1):
        draw_arm(L, side, pose, tex)
    draw_head(L, pose, move(head_tex, pose.dx, pose.dy))
    img = L.img
    # Light from above: the legs sink into the gloom of the corridor floor.
    fall = vgradient(RW, RH, [(0, (255, 255, 255)), (0.55, (255, 255, 255)), (1, (140, 140, 150))])
    rgb = ImageChops.multiply(img.convert("RGB"), fall)
    img = Image.merge("RGBA", (*rgb.split(), img.getchannel("A")))
    if pose.tilt:
        img = img.rotate(pose.tilt, resample=Image.BICUBIC, center=(RCX + pose.pivot, RH))
    return img.resize((FW, FH), Image.LANCZOS)


REX_POSES = [
    RexPose(jaw=0.1),                                                                       # 0 idle A
    RexPose(jaw=0.1, breath=1.0, head_y=-12, dy=-3),                                        # 1 idle B
    RexPose(jaw=0.12, lift=(0.0, 1.0), near=(1.06, 0.95), dx=-18, tilt=2.5, pivot=-210, tail=1),   # 2 walk A
    RexPose(jaw=0.1, lift=(0.0, 0.3), near=(1.02, 1.0), dx=-6, dy=-10, head_y=-6, tail=0.6),       # 3 walk B
    RexPose(jaw=0.12, lift=(1.0, 0.0), near=(0.95, 1.06), dx=18, tilt=-2.5, pivot=210, tail=-1),   # 4 walk C
    RexPose(jaw=0.16, lift=(0.3, 0.0), near=(1.0, 1.02), dx=6, dy=-10, head_y=-4, tail=-0.6),      # 5 walk D
    RexPose(jaw=1.0, head_y=-70, head_tilt=1.0, squint=1.0, arms=1.0, breath=1.0),           # 6 roar
    RexPose(jaw=0.9, head_s=1.33, head_y=40, squint=0.6, arms=0.5, dy=8),                   # 7 lunge
]


def rex_textures():
    """Scale relief for body, head and belly, with large dark/light mottling mixed into the hide."""
    rng = random.Random(14)
    mottle = contrast_layer(fractal_noise(RH, rng, cells=8, octaves=5).crop((0, 0, RW, RH)), 60)
    body = ImageChops.hard_light(contrast_layer(scale_texture((RW, RH), 14, 11), 26), mottle)
    head = ImageChops.hard_light(contrast_layer(scale_texture((RW, RH), 11, 12), 26), mottle)
    return body, head, scale_texture((RW, RH), 26, 13)


def make_rex_sheet():
    tex, head_tex, belly_tex = rex_textures()
    sheet = Image.new("RGBA", (FW * 4, FH * 2), (0, 0, 0, 0))
    for i, pose in enumerate(REX_POSES):
        sheet.paste(draw_rex(pose, tex, head_tex, belly_tex),
                    ((i % 4) * FW, (i // 4) * FH))
    return sheet


# ==================================================================================================
# "EATEN" JAWS - full-screen close-ups of Rex's upper and lower jaw
# ==================================================================================================
def big_tooth_mask(size, base_l, base_r, tip, bend):
    """Mask of a large, slightly curved tooth: a fat cone that tapers quickly near its point,
    with small serration notches along its edges."""
    (lx, ly), (rx, ry), (tx, ty) = base_l, base_r, tip
    half = (rx - lx) / 2
    cx0, cy0 = (lx + rx) / 2, (ly + ry) / 2
    left, right = [], []
    for k in range(13):
        t = k / 12.0
        w = half * (1 - t) ** 0.65
        cx = lerp(cx0, tx, t) + bend * math.sin(math.pi * t)
        cy = lerp(cy0, ty, t)
        notch = 5 if (k % 2 and 0 < k < 11) else 0
        left.append((cx - w + notch, cy))
        right.append((cx + w - notch, cy))
    return poly_mask(size, spline(left + right[::-1][1:], n=4))


def make_jaw(upper):
    """One jaw for the eaten sequence, drawn at 2x in the 'upper' orientation (teeth hanging down to
    the bottom edge).  The lower jaw uses a different tooth layout, gets a tongue, and is flipped."""
    W, Hh = 2160, 1920
    rng = random.Random(801 if upper else 802)
    gum_y = 980 if upper else 1060                      # gum line at the tooth centres
    n_teeth = 6
    spacing = W / n_teeth
    offset = spacing / 2 if upper else 0                 # upper and lower teeth interlock
    L = Layers((W, Hh), outline=(34, 6, 8), outline_px=8)

    # Flesh: palate (upper) or floor of the mouth (lower): dark at the back, redder towards the teeth,
    # with wobbly chevron-shaped ridges and wet highlights.
    flesh = colorize(fractal_noise(2048, rng, cells=4, octaves=6).resize((W, Hh)),
                     [(0, (46, 4, 10)), (0.5, (112, 16, 26)), (1, (170, 42, 50))])
    flesh = ImageChops.multiply(flesh, vgradient(W, Hh, [(0, (80, 70, 70)), (0.5, (255, 255, 255)), (1, (255, 255, 255))]))
    ridges = Image.new("L", (W, Hh), 128)
    dr = ImageDraw.Draw(ridges)
    for k in range(6):
        y = 80 + k * 150
        pts = [(x, y + 120 * (1 - abs(x / W * 2 - 1)) + rng.uniform(-30, 30)) for x in range(-60, W + 160, 160)]
        dr.line(spline(pts, closed=False, n=6), fill=190, width=rng.randint(40, 70), joint="curve")
    ridges = Image.blend(ridges, fractal_noise(2048, rng, cells=8, octaves=4).resize((W, Hh)), 0.35)
    ridges = ridges.filter(ImageFilter.GaussianBlur(14))
    flesh = light(flesh, relief_shading(ridges, 2.0, d=10, limit=36))
    shine = Image.new("L", (W, Hh), 0)
    ds = ImageDraw.Draw(shine)
    for _ in range(40):
        x, y, r = rng.uniform(0, W), rng.uniform(40, gum_y), rng.uniform(20, 70)
        ds.ellipse((x - r, y - r * 0.18, x + r, y + r * 0.18), fill=rng.randint(60, 150))
    flesh = add_glow(flesh, (255, 190, 190), shine.filter(ImageFilter.GaussianBlur(7)))
    if not upper:
        # the tongue lies in the floor of the mouth (this is flipped to the bottom later)
        t = Image.new("L", (W, Hh), 0)
        ImageDraw.Draw(t).ellipse((60, -900, W - 60, 600), fill=255)
        tcol = shade_part(t, (140, 30, 44), (230, 100, 112), rim=0.35, soft_frac=0.25, top_light=0)
        dt = ImageDraw.Draw(tcol)
        for _ in range(500):                                          # papillae
            x, y = rng.uniform(180, W - 180), rng.uniform(0, 560)
            dt.ellipse((x - 4, y - 3, x + 4, y + 3), fill=(222, 110, 122) if rng.random() < 0.5 else (160, 46, 60))
        groove = Image.new("L", (W, Hh), 0)
        ImageDraw.Draw(groove).line([(W / 2, 0), (W / 2, 480)], fill=255, width=36)
        tcol = Image.composite(Image.new("RGB", (W, Hh), (110, 20, 34)), tcol, groove.filter(ImageFilter.GaussianBlur(10)))
        flesh = Image.composite(tcol.filter(ImageFilter.GaussianBlur(1)), flesh, t.filter(ImageFilter.GaussianBlur(3)))

    # Scalloped gum edge: high over each tooth, dipping between them.
    edge = [(0, 0), (W, 0)]
    for x in range(W, -1, -24):
        edge.append((x, gum_y - 44 * math.cos(2 * math.pi * (x - offset) / spacing) + 10 * math.sin(x / 97.0)))
    gm = poly_mask((W, Hh), edge)
    L.paint(flesh, gm, outline=False)

    # Teeth: big jagged ivory cones.  Roots start above the gum line, so the gums overlap them.
    teeth = []
    for i in range(-1, n_teeth + 1):
        cx = offset + i * spacing + rng.uniform(-24, 24)
        size = rng.uniform(0.8, 1.1)
        tip_y = Hh - rng.uniform(0, 60) if size > 0.92 else Hh - rng.uniform(220, 420)
        teeth.append((size, cx, tip_y))
    teeth.sort()                                                     # smaller ones behind
    for size, cx, tip_y in teeth:
        half = spacing * 0.42 * size
        tm = big_tooth_mask((W, Hh), (cx - half, gum_y - 70), (cx + half, gum_y - 70),
                            (cx + rng.uniform(-50, 50), tip_y), rng.uniform(-50, 50))
        col = shade_part(tm, (220, 190, 130), (255, 252, 240), rim=0.62, soft_frac=0.25, top_light=0)
        hl = Image.new("L", (W, Hh), 0)
        ImageDraw.Draw(hl).line([(cx - half * 0.45, gum_y), (cx - half * 0.22, lerp(gum_y, tip_y, 0.75))], fill=120, width=40)
        col = add_glow(col, (255, 255, 255), ImageChops.multiply(hl.filter(ImageFilter.GaussianBlur(18)), tm))
        L.paint(col, tm)

    # Gum band over the tooth roots: pinker, glossy edge.
    band = ImageChops.subtract(gm, move(gm, 0, -110)).filter(ImageFilter.GaussianBlur(2))
    gcol = shade_part(gm, (110, 14, 24), (200, 72, 80), rim=0.6, soft_frac=0.02, top_light=0)
    L.shadow(band, 16, 12, 0.7)
    fade = ImageChops.multiply(gm, vgradient(W, Hh, [(0, (0, 0, 0)), (max(0, (gum_y - 200) / Hh), (0, 0, 0)),
                                                     ((gum_y - 60) / Hh, (255, 255, 255)), (1, (255, 255, 255))]).convert("L"))
    L.img.paste(gcol, (0, 0), fade)
    edge_line = ImageChops.subtract(dilate(gm, 8), gm)
    L.img.paste((40, 6, 10), (0, 0), edge_line)
    img = L.img
    if not upper:
        img = img.transpose(Image.FLIP_TOP_BOTTOM)
    return img.resize((1080, 960), Image.LANCZOS)


# ==================================================================================================
# PARTICLES
# ==================================================================================================
def white_rgba(alpha):
    """White RGB everywhere with the given alpha channel."""
    return Image.merge("RGBA", (*Image.new("RGB", alpha.size, (255, 255, 255)).split(), alpha))


def make_particle_soft():
    g = Image.radial_gradient("L").resize((64, 64), Image.BICUBIC)       # 0 centre, 181 at the edge centres
    return white_rgba(g.point(lambda v: int(255 * math.exp(-((v / 181.0) * 2.4) ** 2) * min(1.0, max(0.0, (181 - v) / 30.0)))))


def make_particle_dust():
    rng = random.Random(901)
    m = Image.new("L", (256, 256), 0)
    pts = []
    for k in range(9):
        a = k / 9.0 * 2 * math.pi
        r = rng.uniform(40, 80)
        pts.append((128 + math.cos(a) * r, 128 + math.sin(a) * r * 0.7))
    ImageDraw.Draw(m).polygon(spline(pts), fill=230)
    for _ in range(5):                                                   # a few satellite specks
        x, y, r = rng.uniform(40, 216), rng.uniform(50, 206), rng.uniform(6, 14)
        ImageDraw.Draw(m).ellipse((x - r, y - r, x + r, y + r), fill=200)
    return white_rgba(m.filter(ImageFilter.GaussianBlur(12)).resize((64, 64), Image.LANCZOS))


def make_particle_spark():
    S = 256
    m = Image.new("L", (S, S), 0)
    d = ImageDraw.Draw(m)
    c = S / 2
    for (ax, ay) in ((1, 0), (0, 1)):                                    # the four long points
        L_, w = 120, 16
        d.polygon([(c - ax * L_, c - ay * L_), (c + ay * w, c + ax * w), (c + ax * L_, c + ay * L_), (c - ay * w, c - ax * w)], fill=255)
    for (ax, ay) in ((1, 1), (1, -1)):                                   # four short diagonal glints
        L_, w = 50, 8
        d.polygon([(c - ax * L_, c - ay * L_), (c + ay * w, c - ax * w), (c + ax * L_, c + ay * L_), (c - ay * w, c + ax * w)], fill=150)
    star = m.filter(ImageFilter.GaussianBlur(3))
    glow = Image.radial_gradient("L").resize((S, S)).point(lambda v: int(170 * math.exp(-((v / 255.0) * 3.2) ** 2)))
    return white_rgba(ImageChops.lighter(star, glow).resize((64, 64), Image.LANCZOS))


# ==================================================================================================
# UI ELEMENTS
# ==================================================================================================
def make_button():
    """256x128 nine-slice button (48 px border): light, tintable, rounded, with a top highlight."""
    k = 4
    W, Hh, r = 256 * k, 128 * k, 40 * k
    body = Image.new("L", (W, Hh), 0)
    ImageDraw.Draw(body).rounded_rectangle((2 * k, 2 * k, W - 2 * k - 1, Hh - 2 * k - 1), r, fill=255)
    border = Image.new("L", (W, Hh), 0)
    ImageDraw.Draw(border).rounded_rectangle((0, 0, W - 1, Hh - 1), r + 2 * k, fill=255)
    fill = vgradient(W, Hh, [(0, (250, 250, 250)), (0.5, (232, 232, 234)), (1, (196, 196, 200))])
    img = Image.new("RGBA", (W, Hh), (0, 0, 0, 0))
    img.paste((120, 120, 126, 255), (0, 0), border)                      # thin darker border
    img.paste(fill, (0, 0), body)
    hl = Image.new("L", (W, Hh), 0)                                      # 6 px inner highlight on the top edge
    ImageDraw.Draw(hl).rounded_rectangle((4 * k, 4 * k, W - 4 * k - 1, Hh * 0.62), r - 2 * k, fill=255)
    ImageDraw.Draw(hl).rounded_rectangle((4 * k, 10 * k, W - 4 * k - 1, Hh * 0.62 + 6 * k), r - 2 * k, fill=0)
    img.paste((255, 255, 255, 255), (0, 0), ImageChops.multiply(hl, body))
    return img.resize((256, 128), Image.LANCZOS)


def make_panel():
    """256x256 nine-slice panel (64 px border): translucent near-black fill, 4 px white border."""
    k = 4
    S, r = 256 * k, 48 * k
    outer = Image.new("L", (S, S), 0)
    ImageDraw.Draw(outer).rounded_rectangle((0, 0, S - 1, S - 1), r, fill=255)
    inner = Image.new("L", (S, S), 0)
    ImageDraw.Draw(inner).rounded_rectangle((4 * k, 4 * k, S - 4 * k - 1, S - 4 * k - 1), r - 4 * k, fill=255)
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    img.paste((255, 255, 255, 255), (0, 0), outer)
    img.paste((12, 12, 16, 200), (0, 0), inner)
    return img.resize((256, 256), Image.LANCZOS)


def make_vignette():
    """512x512 white vignette: alpha 0 in the middle, rising from 45% radius to 255 at the corners."""
    S = 512
    g = Image.radial_gradient("L").resize((S, S), Image.BICUBIC)        # 255 at the corners, 181 at edge centres
    start = 0.45 / 1.4142

    def ramp(v):
        t = min(1.0, max(0.0, (v / 255.0 - start) / (1 - start)))
        return int(255 * t * t * (3 - 2 * t))
    return white_rgba(g.point(ramp))


# --------------------------------------------------------------------------------------------------
# Icon atlas: 8x2 cells of 128 px, white glyphs.  Glyphs are designed on a 96-unit grid (the cell
# minus 16 px padding) and drawn at 4x.
# --------------------------------------------------------------------------------------------------
ICON_K = 4                         # supersampling
ICON_CELL = 128 * ICON_K


def icon_canvas():
    img = Image.new("L", (ICON_CELL, ICON_CELL), 0)
    return img, ImageDraw.Draw(img)


def u(*pts):
    """Design units (0..96 inside the padding) -> supersampled cell pixels."""
    return [((16 + x) * ICON_K, (16 + y) * ICON_K) for x, y in pts]


def ubox(x0, y0, x1, y1):
    (a, b), (c, d) = u((x0, y0), (x1, y1))
    return (a, b, c, d)


def stroke(d, pts, width, fill=255):
    """Thick polyline with round caps and joints."""
    p = u(*pts)
    w = width * ICON_K
    d.line(p, fill=fill, width=int(w), joint="curve")
    for x, y in (p[0], p[-1]):
        d.ellipse((x - w / 2, y - w / 2, x + w / 2, y + w / 2), fill=fill)


def glyph_arrow_up(d):
    d.polygon(u((48, 2), (92, 46), (64, 46), (64, 94), (32, 94), (32, 46), (4, 46)), fill=255)


def glyph_turn(d):
    """Arrow that runs up and curves round to the right (mirrored for turn-left)."""
    d.rectangle(ubox(12, 58, 32, 96), fill=255)                              # upright stem
    d.arc(ubox(12, 22, 84, 94), 180, 270, fill=255, width=20 * ICON_K)       # quarter turn
    d.polygon(u((50, 2), (94, 32), (50, 62)), fill=255)                      # head pointing right


def glyph_pause(d):
    d.rounded_rectangle(ubox(14, 8, 40, 88), 6 * ICON_K, fill=255)
    d.rounded_rectangle(ubox(56, 8, 82, 88), 6 * ICON_K, fill=255)


def glyph_play(d):
    d.polygon(u((18, 6), (90, 48), (18, 90)), fill=255)


def speaker(d):
    d.rounded_rectangle(ubox(2, 32, 26, 64), 3 * ICON_K, fill=255)
    d.polygon(u((20, 32), (48, 10), (48, 86), (20, 64)), fill=255)


def glyph_sound(d):
    speaker(d)
    for r in (16, 32):
        d.arc(ubox(48 - r, 48 - r, 48 + r, 48 + r), -55, 55, fill=255, width=9 * ICON_K)


def glyph_mute(d):
    speaker(d)
    stroke(d, [(62, 34), (90, 62)], 10)
    stroke(d, [(62, 62), (90, 34)], 10)


def glyph_gear(d):
    cx, cy = u((48, 48))[0]
    for k in range(8):
        a = k * math.pi / 4
        pts = []
        for da, r in ((-0.2, 30), (-0.14, 46), (0.14, 46), (0.2, 30)):
            pts.append((cx + math.cos(a + da) * r * ICON_K, cy + math.sin(a + da) * r * ICON_K))
        d.polygon(pts, fill=255)
    d.ellipse(ubox(12, 12, 84, 84), fill=255)
    d.ellipse(ubox(34, 34, 62, 62), fill=0)


def glyph_trophy(d):
    d.polygon(spline(u((20, 6), (76, 6), (74, 30), (62, 52), (48, 58), (34, 52), (22, 30)), n=8), fill=255)
    d.rectangle(ubox(20, 4, 76, 12), fill=255)
    d.arc(ubox(2, 12, 30, 44), 90, 270, fill=255, width=7 * ICON_K)
    d.arc(ubox(66, 12, 94, 44), -90, 90, fill=255, width=7 * ICON_K)
    d.rectangle(ubox(42, 54, 54, 74), fill=255)
    d.rounded_rectangle(ubox(24, 72, 72, 90), 4 * ICON_K, fill=255)


def glyph_skull(d):
    d.ellipse(ubox(12, 2, 84, 70), fill=255)
    d.rounded_rectangle(ubox(26, 52, 70, 90), 8 * ICON_K, fill=255)
    d.ellipse(ubox(24, 28, 44, 50), fill=0)
    d.ellipse(ubox(52, 28, 72, 50), fill=0)
    d.polygon(u((48, 52), (42, 64), (54, 64)), fill=0)
    for x in (40, 48, 56):
        d.line(u((x, 74), (x, 90)), fill=0, width=3 * ICON_K)


def glyph_exit(d):
    # The familiar "exit" pictogram: a figure running out through a doorway. It stays readable
    # at the small sizes used on the high score table, where a plain door looked like bars.
    stroke(d, [(9, 92), (9, 9), (40, 9), (40, 92)], 13)        # the doorway
    d.ellipse(ubox(56, 2, 80, 26), fill=255)                    # head
    stroke(d, [(65, 32), (56, 60)], 17)                         # body, leaning forward
    stroke(d, [(46, 48), (63, 37), (82, 48)], 12)               # arms
    stroke(d, [(86, 92), (75, 74), (56, 60), (46, 77), (30, 81)], 14)   # legs mid-stride


def glyph_back(d):
    stroke(d, [(64, 8), (26, 48), (64, 88)], 18)


def glyph_check(d):
    stroke(d, [(8, 52), (36, 80), (88, 20)], 16)


def glyph_star(d):
    pts = []
    for k in range(10):
        r = 48 if k % 2 == 0 else 20
        a = -math.pi / 2 + k * math.pi / 5
        pts.append((48 + math.cos(a) * r, 52 + math.sin(a) * r))
    d.polygon(u(*pts), fill=255)


def glyph_keyboard(d):
    d.rounded_rectangle(ubox(0, 18, 96, 80), 8 * ICON_K, fill=255)
    for row, (y, n, x0) in enumerate(((26, 7, 8), (40, 6, 14))):
        for i in range(n):
            x = x0 + i * 12.5
            d.rounded_rectangle(ubox(x, y, x + 8, y + 8), 2 * ICON_K, fill=0)
    d.rounded_rectangle(ubox(24, 56, 72, 66), 2 * ICON_K, fill=0)


def make_icons():
    """1024x256 atlas of 16 white glyphs (row 0 then row 1, see ICON_ORDER)."""
    def flip_v(fn):
        return lambda: fn().transpose(Image.FLIP_TOP_BOTTOM)

    def flip_h(fn):
        return lambda: fn().transpose(Image.FLIP_LEFT_RIGHT)

    def cell(fn):
        def build():
            img, d = icon_canvas()
            fn(d)
            return img
        return build
    order = [cell(glyph_arrow_up), flip_v(cell(glyph_arrow_up)), flip_h(cell(glyph_turn)), cell(glyph_turn),
             cell(glyph_pause), cell(glyph_play), cell(glyph_sound), cell(glyph_mute),
             cell(glyph_gear), cell(glyph_trophy), cell(glyph_skull), cell(glyph_exit),
             cell(glyph_back), cell(glyph_check), cell(glyph_star), cell(glyph_keyboard)]
    atlas = Image.new("L", (1024, 256), 0)
    for i, build in enumerate(order):
        g = build()
        g = g.filter(ImageFilter.GaussianBlur(ICON_K * 0.8)).point(lambda v: 255 if v > 127 else 0)  # soften corners
        atlas.paste(g.resize((128, 128), Image.LANCZOS), ((i % 8) * 128, (i // 8) * 128))
    return white_rgba(atlas)


# --------------------------------------------------------------------------------------------------
# Title logo
# --------------------------------------------------------------------------------------------------
def text_mask(size, text, font_px, centre, shear=-0.14, max_w=None):
    """White text on black, centred at `centre`, optionally squeezed to max_w and sheared (italic lean)."""
    font = ImageFont.truetype(FONT_BOLD, font_px)
    x0, y0, x1, y1 = font.getbbox(text)
    tw, th = x1 - x0, y1 - y0
    pad = int(th * 0.4)
    m = Image.new("L", (tw + 2 * pad, th + 2 * pad), 0)
    ImageDraw.Draw(m).text((pad - x0, pad - y0), text, font=font, fill=255)
    if max_w and tw > max_w:
        m = m.resize((int(m.size[0] * max_w / tw), m.size[1]), Image.LANCZOS)
    w, h = m.size
    m = m.transform((w + int(abs(shear) * h), h), Image.AFFINE, (1, shear, shear * h if shear < 0 else 0, 0, 1, 0),
                    resample=Image.BICUBIC)
    out = Image.new("L", size, 0)
    out.paste(m, (int(centre[0] - m.size[0] / 2), int(centre[1] - m.size[1] / 2)))
    return out


def add_drips(mask, rng, count, min_len, max_len, width):
    """Hang slimy drips off the bottom edges of the letters in `mask`."""
    w, h = mask.size
    px = mask.load()
    d = ImageDraw.Draw(mask)
    candidates = []
    for x in range(0, w, 6):                     # find the lowest inked pixel in each column
        for y in range(h - 1, 0, -2):
            if px[x, y] > 200:
                candidates.append((x, y))
                break
    candidates.sort(key=lambda p: -p[1])
    chosen = []
    for x, y in candidates:
        if len(chosen) >= count:
            break
        if all(abs(x - cx) > width * 5 for cx, _ in chosen) and rng.random() < 0.5:
            chosen.append((x, y))
    for x, y in chosen:
        ln = rng.uniform(min_len, max_len)
        wd = width * rng.uniform(0.7, 1.2)
        d.rounded_rectangle((x - wd / 2, y - wd, x + wd / 2, y + ln), wd / 2, fill=255)
        r = wd * 0.8                             # bulb at the end of the drip
        d.ellipse((x - r, y + ln - r * 1.2, x + r, y + ln + r * 0.8), fill=255)
        d.polygon([(x - wd * 1.4, y - 2), (x + wd * 1.4, y - 2), (x + wd / 2, y + wd), (x - wd / 2, y + wd)], fill=255)
    return mask


def make_logo():
    """1024x512 transparent title logo: MONSTER / MAZE with a 3D badge."""
    W, Hh = 2048, 1024
    rng = random.Random(1001)
    top = add_drips(text_mask((W, Hh), "MONSTER", 390, (W / 2 + 60, 270), max_w=1720), rng, 5, 50, 120, 34)
    bottom = add_drips(text_mask((W, Hh), "MAZE", 440, (W / 2 + 20, 650), max_w=1100), rng, 5, 40, 110, 34)
    letters = ImageChops.lighter(top, bottom)
    # Fill: vivid acid-green -> yellow -> orange gradient running down each line (drips stay orange)
    fill = Image.new("RGB", (W, Hh))
    ramp = [(0, (170, 255, 50)), (0.45, (250, 240, 50)), (0.8, (255, 150, 20)), (1, (230, 80, 10))]
    for m, (y0, y1) in ((top, (90, 470)), (bottom, (430, 880))):
        band = Image.new("RGB", (W, Hh), ramp[-1][1])
        band.paste(vgradient(W, y1 - y0, ramp), (0, y0))
        fill.paste(band, (0, 0), m)
    # Bevel: light the top-left edges of the letters and darken the bottom-right
    bevel = letters.filter(ImageFilter.GaussianBlur(9))
    fill = light(fill, relief_shading(bevel, 3.0, d=6, limit=80))
    gloss = ImageChops.multiply(letters, vgradient(W, Hh, [(0, (90, 90, 90)), (0.28, (0, 0, 0)), (1, (0, 0, 0))]).convert("L"))
    fill = ImageChops.add(fill, gloss.convert("RGB"))
    outline = dilate(letters, 36)
    img = Image.new("RGBA", (W, Hh), (0, 0, 0, 0))
    glow = outline.filter(ImageFilter.GaussianBlur(34)).point(lambda v: int(v * 0.6))
    img.paste((110, 255, 70, 255), (0, 0), glow)                                # outer glow
    img = Image.alpha_composite(img, Image.merge("RGBA", (*Image.new("RGB", (W, Hh)).split(),
                                                         move(outline, 14, 22).filter(ImageFilter.GaussianBlur(8)).point(lambda v: int(v * 0.8)))))
    img.paste((22, 10, 34, 255), (0, 0), outline)                               # thick dark outline
    inner = dilate(letters, 8)
    img.paste((60, 30, 70, 255), (0, 0), ImageChops.subtract(inner, letters))    # thin inner rim
    img.paste(fill, (0, 0), letters)
    # "3D" badge: a tilted red roundel stuck on the top-left corner
    b = Image.new("RGBA", (400, 400), (0, 0, 0, 0))
    db = ImageDraw.Draw(b)
    db.ellipse((10, 10, 390, 390), fill=(22, 10, 34, 255))
    db.ellipse((34, 34, 366, 366), fill=(226, 30, 40, 255))
    db.ellipse((56, 44, 344, 250), fill=(245, 80, 80, 255))
    db.ellipse((60, 60, 340, 340), fill=(214, 24, 36, 255))
    db.text((200, 204), "3D", font=ImageFont.truetype(FONT_BOLD, 170), fill=(255, 255, 255, 255), anchor="mm",
            stroke_width=10, stroke_fill=(22, 10, 34, 255))
    b = b.rotate(14, resample=Image.BICUBIC).resize((300, 300), Image.LANCZOS)
    img.alpha_composite(b, (4, 26))
    return img.resize((1024, 512), Image.LANCZOS)


# ==================================================================================================
# FULL-SCREEN ILLUSTRATIONS
# ==================================================================================================
def solve_linear(A, b):
    """Tiny Gaussian elimination (for the 8x8 perspective system)."""
    n = len(b)
    M = [row[:] + [b[i]] for i, row in enumerate(A)]
    for c in range(n):
        p = max(range(c, n), key=lambda r: abs(M[r][c]))
        M[c], M[p] = M[p], M[c]
        for r in range(n):
            if r != c:
                f = M[r][c] / M[c][c]
                M[r] = [a - f * bb for a, bb in zip(M[r], M[c])]
    return [M[i][n] / M[i][i] for i in range(n)]


def perspective_coeffs(dst, src):
    """Coefficients for Image.transform(PERSPECTIVE) mapping output quad `dst` onto input quad `src`."""
    A, b = [], []
    for (x, y), (X, Y) in zip(dst, src):
        A.append([x, y, 1, 0, 0, 0, -X * x, -X * y])
        b.append(X)
        A.append([0, 0, 0, x, y, 1, -Y * x, -Y * y])
        b.append(Y)
    return solve_linear(A, b)


def paste_quad(canvas, tex, src_rect, dst_quad):
    """Texture-map the rectangle `src_rect` of `tex` onto the screen quadrilateral `dst_quad`."""
    x0, y0, x1, y1 = src_rect
    coeffs = perspective_coeffs(dst_quad, [(x0, y0), (x1, y0), (x1, y1), (x0, y1)])
    warped = tex.transform(canvas.size, Image.PERSPECTIVE, coeffs, Image.BICUBIC)
    m = Image.new("L", canvas.size, 0)
    ImageDraw.Draw(m).polygon(dst_quad, fill=255, outline=255, width=2)       # slight overlap hides seams
    canvas.paste(warped, (0, 0), m)


def render_corridor(W, Hh, vp, f, tex, openings=(), end_z=9.0, fog_top=(96, 44, 150), fog_bottom=(20, 116, 124),
                    fog_density=0.22, falloff=0.55, eyes=True):
    """One-point-perspective maze corridor, painted far-to-near with the game's own textures, then
    fogged by depth.  `openings` lists (segment index, side) where a side passage branches off."""
    cx, cy = vp
    img = Image.new("RGB", (W, Hh), (0, 0, 0))

    def S(x, y, z):                                  # world -> screen (corridor is 2 wide, 2 high)
        return (cx + x * f / z, cy + y * f / z)
    wall, moss, floor = tex["wall_stone"], tex["wall_moss"], tex["floor"]
    ceil = tex["ceiling"].point(lambda v: int(v * 0.6))
    z_near = 0.35
    zs = [z_near] + [float(k) for k in range(1, int(end_z) + 1)]
    paste_quad(img, wall, (0, 0, 1024, 1024), [S(-1, -1, end_z), S(1, -1, end_z), S(1, 1, end_z), S(-1, 1, end_z)])
    for i in range(len(zs) - 2, -1, -1):             # far to near
        z0, z1 = zs[i], zs[i + 1]
        u0, u1 = (0, 512) if i % 2 == 0 else (512, 1024)
        paste_quad(img, floor, (0, u0, 1024, u1), [S(-1, 1, z1), S(1, 1, z1), S(1, 1, z0), S(-1, 1, z0)])
        paste_quad(img, ceil, (0, u0, 1024, u1), [S(-1, -1, z0), S(1, -1, z0), S(1, -1, z1), S(-1, -1, z1)])
        for side in (-1, 1):
            if (i, side) in openings:
                # side passage: its far wall faces us, with floor and ceiling running off sideways
                paste_quad(img, floor, (0, 0, 1024, 512), [S(side * 7, 1, z1), S(side, 1, z1), S(side, 1, z0), S(side * 7, 1, z0)])
                paste_quad(img, ceil, (0, 0, 1024, 512), [S(side * 7, -1, z0), S(side, -1, z0), S(side, -1, z1), S(side * 7, -1, z1)])
                for k in range(3):
                    xa, xb = side * (1 + 2 * k), side * (3 + 2 * k)
                    paste_quad(img, wall, (0, 0, 1024, 1024), [S(min(xa, xb), -1, z1), S(max(xa, xb), -1, z1),
                                                               S(max(xa, xb), 1, z1), S(min(xa, xb), 1, z1)])
            else:
                t = moss if (i * 7 + side) % 5 == 0 else wall
                q = [S(side, -1, z0), S(side, -1, z1), S(side, 1, z1), S(side, 1, z0)]
                paste_quad(img, t, (u0, 0, u1, 1024), q)
    # Depth from the screen position: for walls z = f/|dx|, for floor/ceiling z = f/|dy|.
    dx = Image.new("L", (W, 1))
    dx.putdata([clamp(int(255 * abs(x - cx) / f)) for x in range(W)])
    dy = Image.new("L", (1, Hh))
    dy.putdata([clamp(int(255 * abs(y - cy) / f)) for y in range(Hh)])
    inv_depth = ImageChops.lighter(dx.resize((W, Hh)), dy.resize((W, Hh)))        # 255/z
    fog = inv_depth.point(lambda v: int(255 * (1 - math.exp(-fog_density * 255.0 / max(v, 1)))))
    shade = inv_depth.point(lambda v: int(255 / (1 + falloff * 255.0 / max(v, 1))))
    img = ImageChops.multiply(img, shade.convert("RGB"))
    fog_col = vgradient(W, Hh, [(0, fog_top), (cy / Hh, mix(fog_top, fog_bottom, 0.5)), (1, fog_bottom)])
    img = Image.composite(fog_col, img, fog)
    if eyes:                                                      # Rex, lurking in the dark
        glow = Image.new("L", (W, Hh), 0)
        dg = ImageDraw.Draw(glow)
        z = end_z - 1.5
        for side in (-1, 1):
            ex, ey = S(side * 0.14, -0.28, z)
            r = f * 0.045 / z
            dg.ellipse((ex - r * 1.3, ey - r * 0.7, ex + r * 1.3, ey + r * 0.7), fill=255)
        img = add_glow(img, (255, 150, 30), glow.filter(ImageFilter.GaussianBlur(f * 0.08 / z)), 1.6)
        img = add_glow(img, (255, 200, 80), glow, 0.9)
    return img


def vignette_dark(img, strength=0.8, inner=0.35):
    """Darken towards the edges and corners."""
    W, Hh = img.size
    g = radial(W, Hh, rx=1.0, ry=1.0)
    lut = [int(255 * (1 - strength * max(0.0, (v / 255.0 - inner) / (1 - inner)) ** 1.6)) for v in range(256)]
    return ImageChops.multiply(img, g.point(lut).convert("RGB"))


def make_menu_bg(tex):
    W, Hh = 1080, 1920
    img = render_corridor(W, Hh, (540, 860), 560, tex, openings={(1, -1), (3, 1), (5, -1)}, falloff=0.9)
    img = img.point(lambda v: int(v * 0.62))
    return vignette_dark(img, 0.9, inner=0.2)


def hills(W, Hh, rng, base_y, amp, waves):
    """Mask of a rolling hill line (sum of sines) filled down to the bottom."""
    phases = [(rng.uniform(0, 6.28), rng.uniform(0.6, 1.4) * k) for k in range(1, waves + 1)]
    pts = [(0, Hh)]
    for x in range(0, W + 9, 8):
        y = base_y + sum(math.sin(x / W * math.pi * f + p) * amp / (i + 1) for i, (p, f) in enumerate(phases))
        pts.append((x, y))
    pts.append((W, Hh))
    return poly_mask((W, Hh), pts)


def make_escape_sky():
    """Joyful sunrise: deep blue sky to gold, a big sun with rays, rolling hills and the maze exit arch."""
    W, Hh = 1080, 1920
    rng = random.Random(1101)
    horizon = 1180
    img = vgradient(W, Hh, [(0, (18, 34, 110)), (0.22, (60, 60, 170)), (0.42, (170, 80, 170)),
                            (0.54, (250, 120, 120)), (0.6, (255, 170, 90)), (horizon / Hh, (255, 222, 130)),
                            (1, (255, 200, 120))])
    sun = (540, horizon - 40)
    # Rays and glow around the sun
    rays = Image.new("L", (W, Hh), 0)
    d = ImageDraw.Draw(rays)
    for k in range(28):
        a = k / 28.0 * 2 * math.pi + rng.uniform(-0.05, 0.05)
        w = rng.uniform(0.03, 0.07)
        d.polygon([sun, (sun[0] + math.cos(a - w) * 2200, sun[1] + math.sin(a - w) * 2200),
                   (sun[0] + math.cos(a + w) * 2200, sun[1] + math.sin(a + w) * 2200)], fill=rng.randint(25, 70))
    rays = ImageChops.multiply(rays.filter(ImageFilter.GaussianBlur(8)), ImageOps.invert(radial(W, Hh, 0.5, sun[1] / Hh, 0.9, 0.5)))
    img = ImageChops.screen(img, ImageChops.multiply(Image.new("RGB", (W, Hh), (255, 230, 170)), rays.convert("RGB")))
    glow = ImageOps.invert(radial(W, Hh, 0.5, sun[1] / Hh, 0.8, 0.45)).point(lambda v: int(255 * (v / 255.0) ** 2.2))
    img = add_glow(img, (255, 190, 110), glow, 0.9)
    # Soft clouds lit pink and gold from below
    cl = fractal_noise(512, rng, cells=4, octaves=6).resize((W * 2, 700)).crop((W // 2, 0, W // 2 + W, 700))
    cl = cl.point(lambda v: clamp((v - 150) * 3)).filter(ImageFilter.GaussianBlur(6))
    cloud_band = Image.new("L", (W, Hh), 0)
    cloud_band.paste(cl, (0, 330))
    cloud_band = ImageChops.multiply(cloud_band, vgradient(W, Hh, [(0, (0,) * 3), (0.2, (0,) * 3), (0.35, (255,) * 3),
                                                                  (0.5, (255,) * 3), (0.58, (0,) * 3), (1, (0,) * 3)]).convert("L"))
    ccol = vgradient(W, Hh, [(0, (255, 170, 200)), (0.3, (255, 150, 190)), (0.55, (255, 210, 150)), (1, (255, 220, 160))])
    img = Image.composite(ccol, img, cloud_band.point(lambda v: int(v * 0.75)))
    # Sun disc
    disc = Image.new("L", (W, Hh), 0)
    ImageDraw.Draw(disc).ellipse((sun[0] - 150, sun[1] - 150, sun[0] + 150, sun[1] + 150), fill=255)
    img = add_glow(img, (255, 200, 120), disc.filter(ImageFilter.GaussianBlur(60)), 1.0)
    img = Image.composite(Image.new("RGB", (W, Hh), (255, 252, 226)), img, disc.filter(ImageFilter.GaussianBlur(3)))
    # Birds
    d = ImageDraw.Draw(img)
    for _ in range(7):
        x, y, s = rng.uniform(150, 950), rng.uniform(560, 900), rng.uniform(10, 20)
        d.line([(x - s, y - s * 0.4), (x - s * 0.4, y - s * 0.55), (x, y)], fill=(70, 40, 90), width=3, joint="curve")
        d.line([(x, y), (x + s * 0.4, y - s * 0.55), (x + s, y - s * 0.4)], fill=(70, 40, 90), width=3, joint="curve")
    # Rolling hills in layers, hazier and pinker with distance, each with a sunlit rim
    layers = [(horizon + 10, 50, (226, 140, 150), (200, 110, 140)), (horizon + 90, 80, (170, 90, 140), (120, 60, 120)),
              (horizon + 220, 110, (96, 60, 120), (60, 40, 90)), (horizon + 400, 120, (46, 34, 76), (26, 20, 50))]
    for base_y, amp, c_top, c_bot in layers:
        m = hills(W, Hh, rng, base_y, amp, 3)
        col = vgradient(W, Hh, [(0, c_top), (base_y / Hh, c_top), (1, c_bot)])
        rim = ImageChops.subtract(m, move(m, 0, 6)).filter(ImageFilter.GaussianBlur(2))
        img = Image.composite(col, img, m)
        img = add_glow(img, (255, 200, 120), rim, 0.6)
    # The maze exit: a dark stone arch standing on the near hill, backlit by the sunrise
    arch = Image.new("L", (W, Hh), 0)
    da = ImageDraw.Draw(arch)
    ax, top, bottom, r_out, r_in = 540, 1400, 1920, 330, 220
    da.ellipse((ax - r_out, top, ax + r_out, top + 2 * r_out), fill=255)
    da.rectangle((ax - r_out, top + r_out, ax + r_out, bottom), fill=255)
    opening = Image.new("L", (W, Hh), 0)
    do = ImageDraw.Draw(opening)
    do.ellipse((ax - r_in, top + (r_out - r_in), ax + r_in, top + r_out + r_in), fill=255)
    do.rectangle((ax - r_in, top + r_out, ax + r_in, bottom), fill=255)
    stone = colorize(fractal_noise(1024, rng, cells=8, octaves=5).resize((W, W)).crop((0, 0, W, W)).resize((W, Hh)),
                     [(0, (26, 20, 44)), (1, (70, 56, 96))])
    ds = ImageDraw.Draw(stone)
    for k in range(9):                                            # voussoir joints
        a = math.pi + math.pi * (k + 0.5) / 9
        ds.line([(ax + math.cos(a) * r_in, top + r_out + math.sin(a) * r_in),
                 (ax + math.cos(a) * r_out, top + r_out + math.sin(a) * r_out)], fill=(12, 8, 22), width=5)
    for y in range(top + r_out, bottom, 90):
        for side in (-1, 1):
            ds.line([(ax + side * r_in, y), (ax + side * r_out, y)], fill=(12, 8, 22), width=5)
    rim = ImageChops.subtract(arch, move(arch, 3, 8)).filter(ImageFilter.GaussianBlur(3))
    img = Image.composite(stone, img, arch)
    img = add_glow(img, (255, 190, 110), rim, 1.0)
    inside = vgradient(W, Hh, [(0, (6, 6, 12)), (0.8, (8, 10, 16)), (1, (14, 22, 24))])
    img = Image.composite(inside, img, opening)
    return img


def make_eaten_bg():
    """Dark blood-red backdrop with claw scratches and Rex's jaws looming dimly at the top."""
    W, Hh = 1080, 1920
    rng = random.Random(1201)
    img = vgradient(W, Hh, [(0, (80, 6, 10)), (0.45, (40, 2, 6)), (1, (6, 0, 2))])
    blot = fractal_noise(1024, rng, cells=4, octaves=5).resize((W, Hh)).point(lambda v: 150 + int(v * 105 / 255))
    img = ImageChops.multiply(img, blot.convert("RGB"))                  # blotchy, uneven darkness
    # Rex's head, huge and dim, jaws open over the top of the screen
    L = Layers((W, Hh), outline=(0, 0, 0), outline_px=6)
    tex = contrast_layer(scale_texture((W, Hh), 16, 5), 30)
    pose = RexPose(jaw=1.0, squint=0.6)
    draw_head(L, pose, tex, size=(W, Hh), centre=(540, 330), scale=2.2)
    head = L.img
    gray = head.convert("L")
    tinted = ImageChops.multiply(Image.merge("RGB", (gray, gray, gray)), Image.new("RGB", (W, Hh), (150, 40, 40)))
    img = Image.composite(tinted, img, head.getchannel("A").point(lambda v: int(v * 0.55)))
    # the eyes still burn
    eg = Image.new("L", (W, Hh), 0)
    de = ImageDraw.Draw(eg)
    for side in (-1, 1):
        ex, ey = 540 + side * 152 * 2.2, 330 - 66 * 2.2
        de.ellipse((ex - 50, ey - 26, ex + 50, ey + 26), fill=255)
    img = add_glow(img, (255, 90, 20), eg.filter(ImageFilter.GaussianBlur(40)), 1.2)
    img = add_glow(img, (255, 160, 60), eg.filter(ImageFilter.GaussianBlur(6)), 0.6)
    # Three claw scratches slashed across the screen
    sc = Image.new("L", (W, Hh), 0)
    ds = ImageDraw.Draw(sc)
    for k in range(3):
        x0, y0 = 120 + k * 150, 1000 + k * 40
        path = random_walk(rng, x0, y0, 0.62, 10, 70, 0.04)
        ds.polygon(tapered(spline(path, closed=False, n=4), 4, 4), fill=255)
        mid = tapered(spline(path, closed=False, n=4)[4:-4], 26, 26)
        ds.polygon(mid, fill=200)
    sc = sc.filter(ImageFilter.GaussianBlur(3))
    img = add_glow(img, (120, 20, 20), sc, 0.8)
    img = ImageChops.subtract(img, ImageChops.multiply(sc.filter(ImageFilter.GaussianBlur(1)).point(lambda v: v // 3).convert("RGB"),
                                                       Image.new("RGB", (W, Hh), (255, 255, 255))))
    return vignette_dark(img, 0.9, inner=0.3)


# ==================================================================================================
# APP ICONS, SPLASH AND STORE ART
# ==================================================================================================
def maze_cells(n, rng):
    """Perfect maze on an n x n grid (iterative depth-first search).  Returns the set of open walls."""
    seen, stack, opened = {(0, 0)}, [(0, 0)], set()
    while stack:
        x, y = stack[-1]
        nbrs = [(x + dx, y + dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))
                if 0 <= x + dx < n and 0 <= y + dy < n and (x + dx, y + dy) not in seen]
        if not nbrs:
            stack.pop()
            continue
        nxt = rng.choice(nbrs)
        opened.add(frozenset(((x, y), nxt)))
        seen.add(nxt)
        stack.append(nxt)
    return opened


def maze_background(S, seed=1301, n=9, vignette=0.85):
    """Glowing green maze pattern on dark green, darkened towards the edges (icon background)."""
    rng = random.Random(seed)
    k = 2
    W = S * k
    img = Image.new("RGB", (W, W))
    img.paste(colorize(radial(W, W, rx=0.75, ry=0.75), [(0, (14, 50, 34)), (1, (2, 12, 10))]), (0, 0))
    walls = Image.new("L", (W, W), 0)
    d = ImageDraw.Draw(walls)
    opened = maze_cells(n, rng)
    cell = W / (n + 0.6)
    o = cell * 0.3
    lw = int(cell * 0.2)
    for x in range(n):
        for y in range(n):
            x0, y0 = o + x * cell, o + y * cell
            if x + 1 < n and frozenset(((x, y), (x + 1, y))) not in opened or x + 1 == n:
                d.line([(x0 + cell, y0), (x0 + cell, y0 + cell)], fill=255, width=lw)
            if y + 1 < n and frozenset(((x, y), (x, y + 1))) not in opened or y + 1 == n:
                d.line([(x0, y0 + cell), (x0 + cell, y0 + cell)], fill=255, width=lw)
            if x == 0:
                d.line([(x0, y0), (x0, y0 + cell)], fill=255, width=lw)
            if y == 0:
                d.line([(x0, y0), (x0 + cell, y0)], fill=255, width=lw)
    for x in range(n + 1):                                          # round the wall joints
        for y in range(n + 1):
            cx, cy = o + x * cell, o + y * cell
            d.ellipse((cx - lw / 2, cy - lw / 2, cx + lw / 2, cy + lw / 2), fill=255)
    walls = ImageChops.multiply(walls, radial(W, W, rx=0.9, ry=0.9).point(lambda v: 255 if v < 250 else 0))
    col = vgradient(W, W, [(0, (210, 255, 80)), (0.5, (70, 240, 110)), (1, (20, 200, 170))])
    img = add_glow(img, (40, 255, 120), walls.filter(ImageFilter.GaussianBlur(cell * 0.3)), 0.9)
    bevel = light(col, relief_shading(walls.filter(ImageFilter.GaussianBlur(lw * 0.3)), 2.0, d=max(2, lw // 6)))
    img = Image.composite(bevel, img, walls)
    img = vignette_dark(img, vignette, inner=0.25)
    return img.resize((S, S), Image.LANCZOS)


def icon_head(canvas=1400):
    """Rex's head for the icons: jaws slightly open, turned a little (a front view given a 3/4 twist
    with a perspective warp), cropped to its bounding box.  RGBA."""
    L = Layers((canvas, canvas), outline=(8, 12, 4), outline_px=6)
    tex = contrast_layer(scale_texture((canvas, canvas), 12, 21), 26)
    draw_head(L, RexPose(jaw=0.38, squint=0.25), tex, size=(canvas, canvas), centre=(canvas / 2, canvas * 0.46), scale=2.0)
    img = L.img
    c = canvas
    # right side further away -> shorter; the whole head shifts left a little
    coeffs = perspective_coeffs([(0, 0), (c * 0.94, c * 0.06), (c * 0.94, c * 0.94), (0, c)],
                                [(0, 0), (c, 0), (c, c), (0, c)])
    img = img.transform((c, c), Image.PERSPECTIVE, coeffs, Image.BICUBIC)
    return img.crop(img.getbbox())


def fit_into(img, box_w, box_h):
    k = min(box_w / img.size[0], box_h / img.size[1])
    return img.resize((max(1, int(img.size[0] * k)), max(1, int(img.size[1] * k))), Image.LANCZOS)


def make_master_icon(head):
    """1024x1024 opaque master icon: Rex's head over the glowing maze."""
    S = 1024
    bg = maze_background(S)
    h = fit_into(head, 900, 860)
    x, y = (S - h.size[0]) // 2 + 10, S - h.size[1] - 40
    a = h.getchannel("A")
    shadow = Image.new("L", (S, S), 0)
    shadow.paste(a, (x + 16, y + 26))
    bg = ImageChops.multiply(bg, ImageOps.invert(shadow.filter(ImageFilter.GaussianBlur(24)).point(lambda v: int(v * 0.8))).convert("RGB"))
    halo = Image.new("L", (S, S), 0)
    halo.paste(a, (x, y))
    bg = add_glow(bg, (90, 255, 120), ImageChops.subtract(halo.filter(ImageFilter.GaussianBlur(18)), halo), 0.9)
    bg.paste(h.convert("RGB"), (x, y), a)
    return vignette_dark(bg, 0.55, inner=0.55)


def rounded_icon(master, size, radius_frac=0.18, circle=False):
    """Scale the master icon down and cut it to a rounded square (or circle) with a smooth edge."""
    k = 4
    m = Image.new("L", (size * k, size * k), 0)
    d = ImageDraw.Draw(m)
    if circle:
        d.ellipse((0, 0, size * k - 1, size * k - 1), fill=255)
    else:
        d.rounded_rectangle((0, 0, size * k - 1, size * k - 1), int(size * k * radius_frac), fill=255)
    img = master.resize((size, size), Image.LANCZOS).convert("RGBA")
    img.putalpha(m.resize((size, size), Image.LANCZOS))
    return img


def make_adaptive_foreground(head):
    """432x432 adaptive-icon foreground: the head inside the central 288 px safe zone."""
    img = Image.new("RGBA", (432, 432), (0, 0, 0, 0))
    h = fit_into(head, 276, 276)
    img.alpha_composite(h, ((432 - h.size[0]) // 2, (432 - h.size[1]) // 2 + 4))
    return img


def make_splash(logo):
    W, Hh = 1080, 1920
    img = Image.new("RGB", (W, Hh), (0, 0, 0))
    glow = ImageOps.invert(radial(W, Hh, 0.5, 0.5, 0.55, 0.3)).point(lambda v: int(v * 0.35))
    img = add_glow(img, (60, 255, 90), glow)
    lg = fit_into(logo, 940, 470)
    img.paste(lg.convert("RGB"), ((W - lg.size[0]) // 2, (Hh - lg.size[1]) // 2), lg.getchannel("A"))
    return img


def make_feature_graphic(tex, logo, rex_frame):
    """1024x500 Google Play feature graphic: corridor art, logo left, Rex looming on the right."""
    W, Hh = 1024, 500
    img = render_corridor(W, Hh, (700, 250), 300, tex, openings={(1, -1), (2, 1)}, falloff=0.7, eyes=False)
    img = img.point(lambda v: int(v * 0.8))
    img = ImageChops.multiply(img, Image.linear_gradient("L").rotate(90).resize((W, Hh)).point(
        lambda v: int(255 - 150 * (v / 255.0) ** 1.5)).transpose(Image.FLIP_LEFT_RIGHT).convert("RGB"))
    rex = fit_into(rex_frame, 460, 470)
    glow = Image.new("L", (W, Hh), 0)
    glow.paste(rex.getchannel("A"), (W - rex.size[0] - 30, Hh - rex.size[1]))
    img = add_glow(img, (255, 120, 40), glow.filter(ImageFilter.GaussianBlur(30)), 0.35)
    img.paste(rex.convert("RGB"), (W - rex.size[0] - 30, Hh - rex.size[1]), rex.getchannel("A"))
    lg = fit_into(logo, 560, 300)
    img.paste(lg.convert("RGB"), (24, (Hh - lg.size[1]) // 2), lg.getchannel("A"))
    return vignette_dark(img, 0.6, inner=0.4)


# ==================================================================================================
# MAIN - generate everything
# ==================================================================================================
def write_text(path, text):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w") as f:
        f.write(text)
    print("  %s" % os.path.relpath(path, B))


ADAPTIVE_ICON_XML = """<?xml version="1.0" encoding="utf-8"?>
<adaptive-icon xmlns:android="http://schemas.android.com/apk/res/android">
    <background android:drawable="@mipmap/ic_launcher_background" />
    <foreground android:drawable="@mipmap/ic_launcher_foreground" />
</adaptive-icon>
"""


def main():
    start = time.time()
    print("Monster Maze asset generator -> %s" % B)
    tex_dir, spr_dir, ui_dir = (os.path.join(C, d) for d in ("Textures", "Sprites", "UI"))

    print("Textures")
    tex = {}
    for name, fn in (("wall_stone", make_wall_stone), ("wall_moss", make_wall_moss), ("wall_crystal", make_wall_crystal),
                     ("wall_exit", make_wall_exit), ("floor", make_floor), ("ceiling", make_ceiling),
                     ("exit_light", make_exit_light)):
        tex[name] = fn()
        save(tex[name], os.path.join(tex_dir, name + ".png"))

    print("Sprites")
    sheet = make_rex_sheet()
    save(sheet, os.path.join(spr_dir, "rex_sheet.png"))
    save(make_jaw(True), os.path.join(spr_dir, "jaws_top.png"))
    save(make_jaw(False), os.path.join(spr_dir, "jaws_bottom.png"))
    save(make_particle_soft(), os.path.join(spr_dir, "particle_soft.png"))
    save(make_particle_dust(), os.path.join(spr_dir, "particle_dust.png"))
    save(make_particle_spark(), os.path.join(spr_dir, "particle_spark.png"))

    print("UI")
    logo = make_logo()
    for name, img in (("button", make_button()), ("panel", make_panel()), ("icons", make_icons()),
                      ("vignette", make_vignette()), ("logo", logo), ("escape_sky", make_escape_sky()),
                      ("eaten_bg", make_eaten_bg()), ("menu_bg", make_menu_bg(tex))):
        save(img, os.path.join(ui_dir, name + ".png"))

    print("App icons")
    head = icon_head()
    master = make_master_icon(head)
    ios_icons = os.path.join(B, "MonsterMaze.iOS", "Assets.xcassets")
    save(master.convert("RGB"), os.path.join(ios_icons, "AppIcon.appiconset", "AppIcon.png"))
    write_text(os.path.join(ios_icons, "AppIcon.appiconset", "Contents.json"), json.dumps(
        {"images": [{"filename": "AppIcon.png", "idiom": "universal", "platform": "ios", "size": "1024x1024"}],
         "info": {"author": "xcode", "version": 1}}, indent=2) + "\n")
    write_text(os.path.join(ios_icons, "Contents.json"), json.dumps({"info": {"author": "xcode", "version": 1}}, indent=2) + "\n")
    res = os.path.join(B, "MonsterMaze.Android", "Resources")
    for folder, size in (("mdpi", 48), ("hdpi", 72), ("xhdpi", 96), ("xxhdpi", 144), ("xxxhdpi", 192)):
        save(rounded_icon(master, size), os.path.join(res, "mipmap-" + folder, "ic_launcher.png"))
        save(rounded_icon(master, size, circle=True), os.path.join(res, "mipmap-" + folder, "ic_launcher_round.png"))
    save(make_adaptive_foreground(head), os.path.join(res, "mipmap-xxxhdpi", "ic_launcher_foreground.png"))
    save(maze_background(432, vignette=0.6), os.path.join(res, "mipmap-xxxhdpi", "ic_launcher_background.png"))
    for name in ("ic_launcher.xml", "ic_launcher_round.xml"):
        write_text(os.path.join(res, "mipmap-anydpi-v26", name), ADAPTIVE_ICON_XML)

    print("Splash and launch screens")
    save(make_splash(logo), os.path.join(res, "drawable", "splash.png"))
    save(logo.copy(), os.path.join(B, "MonsterMaze.iOS", "Resources", "LaunchLogo.png"))

    print("Store artwork")
    roar = sheet.crop((2 * FW, FH, 3 * FW, 2 * FH))
    save(make_feature_graphic(tex, logo, roar), os.path.join(B, "Store", "GooglePlay", "feature_graphic.png"))
    save(master.resize((512, 512), Image.LANCZOS).convert("RGB"), os.path.join(B, "Store", "GooglePlay", "icon_512.png"))
    save(master.convert("RGB"), os.path.join(B, "Store", "AppStore", "icon_1024.png"))
    print("Done in %.1f s" % (time.time() - start))


if __name__ == "__main__":
    main()
