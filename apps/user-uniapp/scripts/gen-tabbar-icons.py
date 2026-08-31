#!/usr/bin/env python3
"""生成 tabBar 图标：81x81 PNG，SF Symbols 风格线性图标。
未选中 #8e8e93（iOS 灰），选中 #0071e3（Apple 蓝）。输出到 src/static/tabbar/。
用法：python3 scripts/gen-tabbar-icons.py（需 Pillow）"""
from PIL import Image, ImageDraw
from pathlib import Path

OUT = Path(__file__).resolve().parent.parent / "src" / "static" / "tabbar"
OUT.mkdir(parents=True, exist_ok=True)

GRAY = (142, 142, 147, 255)   # iOS systemGray
BLUE = (0, 113, 227, 255)     # Apple blue
W = 5  # 线宽：81px 画布上对应 tab 显示尺寸约 1.5px 视觉线宽


def draw_home(d, c):
    d.line([(14, 40), (40, 16), (66, 40)], fill=c, width=W, joint="curve")
    d.line([(21, 35), (21, 66)], fill=c, width=W)
    d.line([(59, 35), (59, 66)], fill=c, width=W)
    d.line([(21, 66), (59, 66)], fill=c, width=W)
    d.line([(34, 66), (34, 51), (46, 51), (46, 66)], fill=c, width=W, joint="curve")


def draw_grid(d, c):
    for x in (11, 44):
        for y in (11, 44):
            d.rounded_rectangle((x, y, x + 26, y + 26), radius=8, width=W, outline=c)


def draw_cart(d, c):
    d.line([(10, 18), (19, 18)], fill=c, width=W)
    d.line([(19, 18), (28, 50)], fill=c, width=W, joint="curve")
    d.line([(28, 50), (60, 50), (66, 26), (24, 26)], fill=c, width=W, joint="curve")
    d.ellipse((30, 56, 42, 68), width=W, outline=c)
    d.ellipse((52, 56, 64, 68), width=W, outline=c)


def draw_user(d, c):
    d.ellipse((28, 11, 52, 35), width=W, outline=c)
    d.arc((16, 44, 64, 92), start=180, end=360, width=W, fill=c)


ICONS = {"home": draw_home, "category": draw_grid, "cart": draw_cart, "user": draw_user}

for name, painter in ICONS.items():
    for suffix, color in (("gray", GRAY), ("blue", BLUE)):
        img = Image.new("RGBA", (81, 81), (0, 0, 0, 0))
        painter(ImageDraw.Draw(img), color)
        img.save(OUT / f"{name}-{suffix}.png")
print("icons written to", OUT)
