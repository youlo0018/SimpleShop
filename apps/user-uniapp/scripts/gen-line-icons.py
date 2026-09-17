#!/usr/bin/env python3
"""生成参考图风格的线性图标（96x96 透明 PNG，深灰描边，无底色）。
输出到 src/static/line/，供"我的服务/权益/金刚区"使用（后台可配置 icon 为图片路径或 emoji）。
用法：python3 scripts/gen-line-icons.py（需 Pillow）"""
from PIL import Image, ImageDraw
from pathlib import Path

OUT = Path(__file__).resolve().parent.parent / "src" / "static" / "line"
OUT.mkdir(parents=True, exist_ok=True)
S = 96          # 画布
W = 6           # 线宽
INK = (29, 29, 31, 255)


def canvas():
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    return img, ImageDraw.Draw(img)


def draw_order(d):
    d.rounded_rectangle((26, 14, 70, 82), radius=8, outline=INK, width=W)
    for y in (34, 46, 58):
        d.line([(38, y), (58 if y != 58 else 50, y)], fill=INK, width=5)


def draw_cart(d):
    d.line([(12, 24), (24, 24)], fill=INK, width=W)
    d.line([(24, 24), (34, 60)], fill=INK, width=W, joint="curve")
    d.line([(34, 60), (74, 60), (82, 30), (30, 30)], fill=INK, width=W, joint="curve")
    d.ellipse((40, 70, 52, 82), outline=INK, width=5)
    d.ellipse((64, 70, 76, 82), outline=INK, width=5)


def draw_record(d):
    d.rounded_rectangle((20, 14, 76, 82), radius=8, outline=INK, width=W)
    for x, h in ((32, 16), (46, 26), (60, 36)):
        d.line([(x, 68), (x, 68 - h)], fill=INK, width=6)
    d.line([(30, 68), (66, 68)], fill=INK, width=5)


def draw_gift(d):
    d.rounded_rectangle((22, 40, 74, 78), radius=6, outline=INK, width=W)
    d.rounded_rectangle((18, 24, 78, 44), radius=6, outline=INK, width=W)
    d.line([(48, 24), (48, 78)], fill=INK, width=W)
    d.arc((32, 10, 48, 28), start=0, end=250, fill=INK, width=W)
    d.arc((48, 10, 64, 28), start=290, end=180, fill=INK, width=W)


def draw_service(d):
    # 耳机客服：头梁 + 耳罩
    d.arc((22, 18, 74, 70), start=180, end=360, fill=INK, width=W)
    d.rounded_rectangle((18, 46, 32, 70), radius=6, outline=INK, width=5)
    d.rounded_rectangle((64, 46, 78, 70), radius=6, outline=INK, width=5)
    d.arc((54, 58, 78, 84), start=270, end=90, fill=INK, width=5)
    d.ellipse((48, 72, 58, 82), fill=INK)


def draw_review(d):
    d.rounded_rectangle((16, 18, 80, 66), radius=12, outline=INK, width=W)
    d.polygon([(34, 66), (30, 82), (48, 66)], fill=INK)
    # 气泡内星星
    d.polygon([(48, 28), (53, 40), (66, 42), (56, 51), (59, 64), (48, 57), (37, 64), (40, 51), (30, 42), (43, 40)], fill=INK)


def draw_points(d):
    d.ellipse((16, 16, 80, 80), outline=INK, width=W)
    d.ellipse((30, 30, 66, 66), outline=INK, width=4)
    d.line([(40, 38), (56, 38)], fill=INK, width=5)
    d.line([(48, 38), (48, 60)], fill=INK, width=5)
    d.line([(40, 48), (56, 48)], fill=INK, width=5)


def draw_pin(d):
    d.ellipse((28, 12, 68, 52), outline=INK, width=W)
    d.line([(34, 44), (48, 84)], fill=INK, width=W)
    d.line([(62, 44), (48, 84)], fill=INK, width=W)
    d.ellipse((43, 27, 53, 37), outline=INK, width=4)


def draw_invoice(d):
    d.rounded_rectangle((24, 12, 72, 84), radius=6, outline=INK, width=W)
    d.line([(34, 30), (62, 30)], fill=INK, width=5)
    d.line([(34, 44), (62, 44)], fill=INK, width=5)
    d.line([(34, 58), (50, 58)], fill=INK, width=5)


def draw_car(d):
    d.rounded_rectangle((14, 38, 82, 66), radius=10, outline=INK, width=W)
    d.line([(24, 38), (36, 22), (60, 22), (72, 38)], fill=INK, width=W, joint="curve")
    d.line([(24, 52), (72, 52)], fill=INK, width=4)
    d.ellipse((24, 62, 36, 74), outline=INK, width=5)
    d.ellipse((60, 62, 72, 74), outline=INK, width=5)


def draw_info(d):
    d.ellipse((14, 14, 82, 82), outline=INK, width=W)
    d.ellipse((44, 28, 52, 36), fill=INK)
    d.line([(48, 44), (48, 66)], fill=INK, width=W)


def draw_pay(d):
    d.rounded_rectangle((18, 18, 46, 46), radius=6, outline=INK, width=W)
    d.rounded_rectangle((50, 18, 78, 46), radius=6, outline=INK, width=W)
    d.rounded_rectangle((18, 50, 46, 78), radius=6, outline=INK, width=W)
    d.rectangle((54, 54, 62, 62), fill=INK)
    d.rectangle((68, 54, 74, 60), fill=INK)
    d.rectangle((54, 68, 60, 74), fill=INK)
    d.rectangle((68, 68, 74, 74), fill=INK)


def draw_card(d):
    d.rounded_rectangle((14, 26, 82, 70), radius=10, outline=INK, width=W)
    d.line([(14, 42), (82, 42)], fill=INK, width=W)
    d.line([(26, 56), (48, 56)], fill=INK, width=5)


def draw_heart(d):
    d.polygon([(48, 78), (20, 50), (26, 32), (42, 30), (48, 40), (54, 30), (70, 32), (76, 50)], outline=INK, width=W)


def draw_refresh(d):
    d.arc((22, 22, 74, 74), start=200, end=330, fill=INK, width=W)
    d.polygon([(66, 18), (80, 28), (64, 38)], fill=INK)
    d.arc((22, 22, 74, 74), start=20, end=150, fill=INK, width=W)
    d.polygon([(30, 78), (16, 68), (32, 58)], fill=INK)


def draw_mall(d):
    d.line([(16, 40), (16, 82), (80, 82), (80, 40)], fill=INK, width=W, joint="curve")
    d.polygon([(12, 40), (24, 20), (72, 20), (84, 40)], outline=INK, width=W)
    d.line([(36, 82), (36, 58), (60, 58), (60, 82)], fill=INK, width=W)


def draw_benefit(d):
    # 权益：钻石
    d.polygon([(48, 14), (78, 40), (48, 84), (18, 40)], outline=INK, width=W)
    d.line([(18, 40), (78, 40)], fill=INK, width=4)
    d.line([(36, 40), (48, 14), (60, 40)], fill=INK, width=4)


def draw_star(d):
    d.polygon([(48, 12), (60, 38), (88, 42), (68, 62), (72, 88), (48, 76), (24, 88), (28, 62), (8, 42), (36, 38)], outline=INK, width=W)


ICONS = {
    "order": draw_order, "cart": draw_cart, "record": draw_record, "gift": draw_gift,
    "service": draw_service, "review": draw_review, "points": draw_points, "pin": draw_pin,
    "invoice": draw_invoice, "car": draw_car, "info": draw_info, "pay": draw_pay,
    "card": draw_card, "heart": draw_heart, "refresh": draw_refresh, "mall": draw_mall,
    "benefit": draw_benefit, "star": draw_star,
}

for name, painter in ICONS.items():
    img, d = canvas()
    painter(d)
    img.save(OUT / f"{name}.png")
print("line icons written to", OUT, "total:", len(ICONS))
