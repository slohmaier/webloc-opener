"""Generate an Office-365-style icon for webloc-opener.

Produces:
  app.png  – 256x256 master
  app.ico  – multi-resolution Windows icon (16,24,32,48,64,128,256)

Design language: Microsoft Fluent / Office 365.
  - Rounded blue gradient tile (Outlook-blue family)
  - Centred white globe with latitude/longitude meridians
  - Small "open external" badge in the upper-right corner
"""
from __future__ import annotations
import os
from PIL import Image, ImageDraw

SIZE = 1024  # supersampled, downsampled later for crisp edges


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(len(a)))


def make_tile() -> Image.Image:
    img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))

    # Vertical gradient: top lighter, bottom darker (Outlook/Edge blue family)
    top = (0x2B, 0x88, 0xD8)
    bot = (0x10, 0x3F, 0x91)
    grad = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    g = ImageDraw.Draw(grad)
    for y in range(SIZE):
        t = y / (SIZE - 1)
        g.line([(0, y), (SIZE, y)], fill=lerp(top, bot, t) + (255,))

    # Rounded-rect mask (Office tiles have ~18% corner radius)
    mask = Image.new("L", (SIZE, SIZE), 0)
    m = ImageDraw.Draw(mask)
    radius = int(SIZE * 0.18)
    m.rounded_rectangle([(0, 0), (SIZE - 1, SIZE - 1)], radius=radius, fill=255)

    img.paste(grad, (0, 0), mask)

    # Subtle top highlight
    highlight = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    h = ImageDraw.Draw(highlight)
    for y in range(int(SIZE * 0.35)):
        a = int(55 * (1 - y / (SIZE * 0.35)))
        h.line([(0, y), (SIZE, y)], fill=(255, 255, 255, a))
    empty = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    img.alpha_composite(Image.composite(highlight, empty, mask))

    return img


def draw_globe(img: Image.Image, cx: int, cy: int, r: int, stroke: int):
    draw = ImageDraw.Draw(img, "RGBA")
    white = (255, 255, 255, 255)

    # Outer circle
    draw.ellipse(
        [(cx - r, cy - r), (cx + r, cy + r)],
        outline=white, width=stroke,
    )

    # Equator
    draw.line([(cx - r, cy), (cx + r, cy)], fill=white, width=stroke)

    # Vertical meridian
    draw.line([(cx, cy - r), (cx, cy + r)], fill=white, width=stroke)

    # Curved meridians via narrow ellipses → suggest sphere
    for ex_ratio in (0.48,):
        ew = int(r * ex_ratio)
        bbox = [(cx - ew, cy - r), (cx + ew, cy + r)]
        draw.ellipse(bbox, outline=white, width=stroke)

    # Tropics (curved horizontal arcs)
    for dy_ratio in (-0.50, 0.50):
        dy = int(r * dy_ratio)
        eh = int(abs(dy) * 2.0)
        bbox = [(cx - r, cy + dy - eh // 2), (cx + r, cy + dy + eh // 2)]
        if dy < 0:
            draw.arc(bbox, start=0, end=180, fill=white, width=stroke)
        else:
            draw.arc(bbox, start=180, end=360, fill=white, width=stroke)


def draw_external_badge(img: Image.Image, x1: int, y1: int, size: int, stroke: int,
                        bg_color):
    """Small 'open external' badge: filled rounded square containing an arrow."""
    draw = ImageDraw.Draw(img, "RGBA")
    white = (255, 255, 255, 255)

    x2 = x1 + size
    y2 = y1 + size

    # Filled white badge with rounded corners
    badge_radius = int(size * 0.22)
    draw.rounded_rectangle(
        [(x1, y1), (x2, y2)],
        radius=badge_radius, fill=white,
    )

    # Diagonal arrow drawn in the tile's blue, pointing up-right
    pad = int(size * 0.24)
    tip_x = x2 - pad
    tip_y = y1 + pad
    tail_x = x1 + pad
    tail_y = y2 - pad

    draw.line([(tail_x, tail_y), (tip_x, tip_y)], fill=bg_color, width=stroke)

    # Arrowhead: short horizontal + vertical strokes from the tip
    head = int(size * 0.36)
    draw.line([(tip_x, tip_y), (tip_x - head, tip_y)], fill=bg_color, width=stroke)
    draw.line([(tip_x, tip_y), (tip_x, tip_y + head)], fill=bg_color, width=stroke)


def main():
    img = make_tile()

    # Globe – centred, taking ~55% of canvas
    cx, cy = SIZE // 2, int(SIZE * 0.55)
    r = int(SIZE * 0.30)
    stroke = max(10, SIZE // 70)
    draw_globe(img, cx, cy, r, stroke)

    # Badge – top-right corner, clearly separated from globe
    badge_size = int(SIZE * 0.28)
    pad = int(SIZE * 0.08)
    badge_x = SIZE - pad - badge_size
    badge_y = pad
    arrow_stroke = max(10, SIZE // 80)
    draw_external_badge(
        img, badge_x, badge_y, badge_size, arrow_stroke,
        bg_color=(0x10, 0x3F, 0x91, 255),  # match deep navy
    )

    # Downsample to 256 master, then export multi-res .ico
    master = img.resize((256, 256), Image.LANCZOS)

    out_dir = os.path.dirname(os.path.abspath(__file__))
    png_path = os.path.join(out_dir, "app.png")
    ico_path = os.path.join(out_dir, "app.ico")

    master.save(png_path, "PNG")
    sizes = [(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]
    master.save(ico_path, format="ICO", sizes=sizes)

    print(f"Wrote {png_path}")
    print(f"Wrote {ico_path}")


if __name__ == "__main__":
    main()
