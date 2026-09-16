#!/usr/bin/env python3
"""生成「我的」页 iOS 设置风格的彩色分组图标：96x96 彩色圆角方块 + 白色线性图形。
输出到 src/static/tabbar/（与 tabBar 图标同目录便于统一管理）。
用法：python3 scripts/gen-cell-icons.py（需 Pillow）"""
from PIL import Image, ImageDraw
from pathlib import Path

OUT = Path(__file__).resolve().parent.parent / "src" / "static" / "tabbar"
OUT.mkdir(parents=True, exist_ok=True)

W = 7  # 白色图形线宽


def base(color):
    img = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((0, 0, 95, 95), radius=24, fill=color)
    return img, d


def draw_order(d):
    """单据：圆角矩形 + 三条横线"""
    d.rounded_rectangle((30, 20, 66, 76), radius=7, width=W, outline="white")
    for y in (38, 50, 62):
        d.line([(39, y), (57 if y != 62 else 51, y)], fill="white", width=6)


def draw_pin(d):
    """定位：实心圆头 + 三角尾"""
    d.ellipse((35, 14, 61, 40), fill="white")
    d.polygon([(41, 36), (55, 36), (48, 60)], fill="white")


def draw_cart(d):
    """购物车线性图形"""
    d.line([(12, 26), (23, 26)], fill="white", width=W)
    d.line([(23, 26), (33, 60)], fill="white", width=W, joint="curve")
    d.line([(33, 60), (72, 60), (79, 31), (29, 31)], fill="white", width=W, joint="curve")
    d.ellipse((37, 68, 49, 80), fill="white")
    d.ellipse((61, 68, 73, 80), fill="white")


def draw_sync(d):
    """双弧循环箭头"""
    d.arc((24, 24, 72, 72), start=190, end=335, fill="white", width=W)
    d.polygon([(62, 20), (76, 30), (60, 38)], fill="white")
    d.arc((24, 24, 72, 72), start=10, end=155, fill="white", width=W)
    d.polygon([(34, 76), (20, 66), (36, 58)], fill="white")


def draw_coupon(d):
    """优惠券：票券轮廓 + 中间虚线 + 两侧缺口"""
    d.rounded_rectangle((14, 30, 82, 68), radius=10, width=W, outline="white")
    d.line([(48, 32), (48, 66)], fill="white", width=4)
    d.ellipse((44, 24, 52, 32), fill=(0, 0, 0, 0))
    d.ellipse((44, 66, 52, 74), fill=(0, 0, 0, 0))


ICONS = {
    "order": ("#0071e3", draw_order),   # 蓝
    "pin": ("#ff3b30", draw_pin),       # 红（类地图）
    "cart": ("#ff9500", draw_cart),     # 橙
    "sync": ("#34c759", draw_sync),     # 绿
    "coupon": ("#5e5ce6", draw_coupon), # 紫（营销）
}

for name, (color, painter) in ICONS.items():
    img, d = base(color)
    painter(d)
    img.save(OUT / f"cell-{name}.png")
print("cell icons written to", OUT)
