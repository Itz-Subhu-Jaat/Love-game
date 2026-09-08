#!/usr/bin/env python3
"""Generates UI sprites (white, tinted at runtime) for Love Game via Pillow."""
import os
from PIL import Image, ImageDraw, ImageFilter

ROOT = "/home/z/my-project/Love-game"
OUT = os.path.join(ROOT, "Assets/Resources/Sprites")

W = (255, 255, 255, 255)

def canvas(size):
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    return img, ImageDraw.Draw(img)

def save(img, name):
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + ".png")
    img.save(path)
    print(f"wrote {path}")

def panel():
    # 512, 9-slice borders 64
    img, d = canvas(512)
    d.rounded_rectangle([8, 8, 504, 504], radius=48, fill=(255, 255, 255, 235))
    save(img, "ui_panel")

def button():
    img, d = canvas(256)
    d.rounded_rectangle([6, 6, 250, 122], radius=28, fill=(255, 255, 255, 240))
    img = img.crop((0, 0, 256, 128))
    save(img, "ui_button")

def button_round():
    img, d = canvas(256)
    d.ellipse([6, 6, 250, 250], fill=(255, 255, 255, 235))
    save(img, "ui_button_round")

def joystick():
    img, d = canvas(512)
    d.ellipse([16, 16, 496, 496], outline=(255, 255, 255, 220), width=18)
    d.ellipse([90, 90, 422, 422], outline=(255, 255, 255, 70), width=6)
    save(img, "ui_joystick_ring")

    img2, d2 = canvas(256)
    d2.ellipse([20, 20, 236, 236], fill=(255, 255, 255, 200))
    img2 = img2.filter(ImageFilter.GaussianBlur(2))
    save(img2, "ui_joystick_thumb")

def heart():
    img, d = canvas(128)
    # two circles + triangle
    d.ellipse([14, 20, 62, 68], fill=W)
    d.ellipse([64, 20, 112, 68], fill=W)
    d.polygon([(22, 54), (104, 54), (63, 112)], fill=W)
    save(img, "ui_heart")

def sliders():
    img, d = canvas(256)
    img = Image.new("RGBA", (256, 32), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([2, 2, 254, 30], radius=15, fill=(255, 255, 255, 220))
    save(img, "ui_slider_track")

    img = Image.new("RGBA", (256, 32), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([2, 2, 254, 30], radius=15, fill=(255, 255, 255, 235))
    save(img, "ui_slider_fill")

def pin():
    img, d = canvas(128)
    d.ellipse([28, 14, 100, 86], fill=W)
    d.polygon([(48, 74), (80, 74), (64, 118)], fill=W)
    d.ellipse([52, 38, 76, 62], fill=(255, 255, 255, 0))
    save(img, "map_pin")

def main():
    panel()
    button()
    button_round()
    joystick()
    heart()
    sliders()
    pin()

if __name__ == "__main__":
    main()
