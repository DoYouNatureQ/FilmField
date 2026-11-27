from PIL import Image, ImageDraw

SIZE = 1024
SHELL_R = 200
BEZEL = (130, 80, 894, 944)
BEZEL_R = 60
HOLE_X = [184, 712]
HOLE_YS = [133, 336, 538, 741]
HOLE_W, HOLE_H, HOLE_R = 114, 135, 28
TRIANGLE = [(474, 418), (594, 512), (474, 606)]
TRI_ROUND_R = 22
TRIANGLE_SIMPLE = [(430, 372), (652, 512), (430, 652)]
TRI_ROUND_R_SIMPLE = 44
ICO_SIZES = [256, 128, 64, 48, 32, 24, 16]
SIMPLE_MAX = 32

VARIANTS = {
    "orange": ((0xE8, 0xDC, 0xC8), (0xD3, 0xC7, 0xAE), (0x55, 0x50, 0x4C), (0xE8, 0xDC, 0xC8)),
}


def render(size, colors, simple=False):
    shell, lip, interior, accent = colors
    s = size / SIZE
    icon = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(icon)

    d.rectangle([0, 0, size, size], fill=shell + (255,))
    d.rounded_rectangle([BEZEL[0] * s, BEZEL[1] * s, BEZEL[2] * s, BEZEL[3] * s], radius=BEZEL_R * s, fill=interior + (255,))
    if not simple:
        for x in HOLE_X:
            for y in HOLE_YS:
                d.rounded_rectangle([x * s, y * s, (x + HOLE_W) * s, (y + HOLE_H) * s], radius=HOLE_R * s, fill=accent + (255,))
    tri = TRIANGLE_SIMPLE if simple else TRIANGLE
    tri_r = TRI_ROUND_R_SIMPLE if simple else TRI_ROUND_R
    mask = Image.new("L", (size * 4, size * 4), 0)
    md = ImageDraw.Draw(mask)
    k = 4 * s
    P = [(x * k, y * k) for x, y in tri]
    R = tri_r * k
    md.polygon(P, fill=255)
    for i in range(3):
        x0, y0 = P[i]
        x1, y1 = P[(i + 1) % 3]
        md.line([x0, y0, x1, y1], fill=255, width=int(round(2 * R)))
        md.ellipse([x0 - R, y0 - R, x0 + R, y0 + R], fill=255)
    mask = mask.resize((size, size), Image.LANCZOS)
    solid = Image.new("RGBA", (size, size), accent + (255,))
    icon.paste(solid, mask=mask)
    shell_mask = Image.new("L", (size * 4, size * 4), 0)
    ImageDraw.Draw(shell_mask).rounded_rectangle([0, 0, size * 4 - 1, size * 4 - 1], radius=SHELL_R * 4 * s, fill=255)
    icon.putalpha(shell_mask.resize((size, size), Image.LANCZOS))
    return icon


def main():
    for name, colors in VARIANTS.items():
        base = render(SIZE, colors)
        base.save("icon_1024.png")
        render(256, colors).save("icon_256.png")
        frames = [render(n, colors, simple=n <= SIMPLE_MAX) for n in ICO_SIZES]
        frames[0].save("icon.ico", format="ICO", sizes=[(n, n) for n in ICO_SIZES], append_images=frames[1:])


if __name__ == "__main__":
    main()
