from PIL import Image, ImageDraw

SIZE = 1024
LIP_COLOR = (0xD3, 0xC7, 0xAE)
TILE_COLOR = (0xE8, 0xDC, 0xC8)
BEZEL_COLOR = (0x55, 0x50, 0x4C)
ACCENT_COLOR = (0xD9, 0x3B, 0x18)
BEZEL = (130, 140, 894, 884)
BEZEL_R = 56
HOLE_X = [170, 810]
HOLE_YS = [194, 291, 389, 486, 583, 681, 778]
HOLE_W, HOLE_H, HOLE_R = 44, 52, 11
TRIANGLE = [(474, 424), (590, 512), (474, 600)]
ICO_SIZES = [256, 128, 64, 48, 32, 24, 16]


def render(size):
    s = size / SIZE
    icon = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(icon)

    d.rectangle([0, 16 * s, size, size], fill=LIP_COLOR + (255,))
    d.rectangle([0, 0, size, size], fill=TILE_COLOR + (255,))
    d.rounded_rectangle([BEZEL[0] * s, BEZEL[1] * s, BEZEL[2] * s, BEZEL[3] * s], radius=BEZEL_R * s, fill=BEZEL_COLOR + (255,))

    film = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    fd = ImageDraw.Draw(film)
    fd.rectangle([BEZEL[0] * s, BEZEL[1] * s, BEZEL[2] * s, BEZEL[3] * s], fill=TILE_COLOR + (255,))
    for x in HOLE_X:
        for y in HOLE_YS:
            fd.rounded_rectangle([x * s, y * s, (x + HOLE_W) * s, (y + HOLE_H) * s], radius=HOLE_R * s, fill=BEZEL_COLOR + (255,))
    fd.polygon([(x * s, y * s) for x, y in TRIANGLE], fill=ACCENT_COLOR + (255,))

    clip = Image.new("L", (size, size), 0)
    cd = ImageDraw.Draw(clip)
    cd.rounded_rectangle([BEZEL[0] * s, BEZEL[1] * s, BEZEL[2] * s, BEZEL[3] * s], radius=BEZEL_R * s, fill=255)
    icon.paste(film, mask=clip)
    return icon


def main():
    base = render(SIZE)
    base.save("icon_1024.png")
    frames = [base.resize((n, n), Image.LANCZOS) for n in ICO_SIZES]
    frames[0].save("icon.ico", format="ICO", sizes=[(n, n) for n in ICO_SIZES], append_images=frames[1:])
    base.resize((256, 256), Image.LANCZOS).save("icon_256.png")


if __name__ == "__main__":
    main()
