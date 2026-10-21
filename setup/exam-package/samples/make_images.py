"""Draw the pictures used by the synthetic sample papers (notices, story pictures, Listening options).

The texts are our own, written for the samples; nothing here comes from Cambridge papers.
Run from the repository root after changing a sample:
    python -I setup/exam-package/samples/make_images.py
"""

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).resolve().parent
FONTS = Path("C:/Windows/Fonts")
INK = (20, 20, 20)
PAPER = (255, 255, 255)


def font(name: str, size: int) -> ImageFont.FreeTypeFont:
    try:
        return ImageFont.truetype(str(FONTS / name), size)
    except OSError:
        return ImageFont.load_default(size)


def centred_lines(draw: ImageDraw.ImageDraw, lines, box, fonts):
    """Draw lines centred horizontally and vertically in box; fonts[i] is used for line i."""
    left, top, right, bottom = box
    heights = [draw.textbbox((0, 0), text, font=f)[3] + 10 for text, f in zip(lines, fonts)]
    y = top + (bottom - top - sum(heights)) / 2
    for text, f, h in zip(lines, fonts, heights):
        width = draw.textlength(text, font=f)
        draw.text((left + (right - left - width) / 2, y), text, font=f, fill=INK)
        y += h


def notice(path: Path, title: str, lines, width=420, height=170):
    """A framed notice like the A2 Reading Part 1 signs: bold italic title, italic lines."""
    image = Image.new("RGB", (width, height), PAPER)
    draw = ImageDraw.Draw(image)
    draw.rectangle((3, 3, width - 4, height - 4), outline=INK, width=3)
    draw.rectangle((10, 10, width - 11, height - 11), outline=INK, width=1)
    fonts = [font("georgiaz.ttf", 26)] + [font("georgiai.ttf", 21)] * len(lines)
    centred_lines(draw, [title, *lines], (14, 14, width - 14, height - 14), fonts)
    image.save(path)


def message(path: Path, sender: str, text_lines, width=420, height=170):
    """A phone message or email: grey header with the sender, plain text below."""
    image = Image.new("RGB", (width, height), PAPER)
    draw = ImageDraw.Draw(image)
    draw.rounded_rectangle((2, 2, width - 3, height - 3), radius=14, outline=INK, width=2)
    draw.rounded_rectangle((2, 2, width - 3, 44), radius=14, fill=(225, 230, 232), outline=INK, width=2)
    draw.rectangle((4, 30, width - 5, 44), fill=(225, 230, 232))
    draw.line((2, 44, width - 3, 44), fill=INK, width=2)
    draw.text((18, 10), sender, font=font("arialbd.ttf", 20), fill=INK)
    y = 58
    for line in text_lines:
        draw.text((18, y), line, font=font("arial.ttf", 19), fill=INK)
        y += 27
    image.save(path)


def card(path: Path, caption: str, painter=None, width=240, height=180):
    """A picture option: a simple drawing (if any) with a caption underneath."""
    image = Image.new("RGB", (width, height), PAPER)
    draw = ImageDraw.Draw(image)
    draw.rectangle((1, 1, width - 2, height - 2), outline=(150, 150, 150), width=2)
    if painter:
        painter(draw, width, height - 40)
    f = font("arialbd.ttf", 22)
    draw.text(((width - draw.textlength(caption, font=f)) / 2, height - 36), caption, font=f, fill=INK)
    image.save(path)


def sun(draw, w, h):
    cx, cy, r = w / 2, h / 2 + 5, 32
    for i in range(12):
        import math
        a = math.tau * i / 12
        draw.line((cx + math.cos(a) * (r + 8), cy + math.sin(a) * (r + 8), cx + math.cos(a) * (r + 22), cy + math.sin(a) * (r + 22)), fill=(230, 160, 0), width=5)
    draw.ellipse((cx - r, cy - r, cx + r, cy + r), fill=(255, 200, 0), outline=(230, 160, 0), width=3)


def cloud(draw, w, h, y=None):
    cy = y if y is not None else h / 2
    for dx, dy, r in ((-40, 5, 28), (0, -12, 36), (40, 5, 28)):
        draw.ellipse((w / 2 + dx - r, cy + dy - r, w / 2 + dx + r, cy + dy + r), fill=(200, 205, 210), outline=(120, 120, 120), width=2)


def rain(draw, w, h):
    cloud(draw, w, h, y=h / 2 - 12)
    for x in range(int(w / 2) - 50, int(w / 2) + 60, 22):
        draw.line((x, h / 2 + 30, x - 8, h / 2 + 55), fill=(40, 110, 200), width=4)


def wind(draw, w, h):
    for i, y in enumerate((h / 2 - 25, h / 2, h / 2 + 25)):
        draw.arc((40 + i * 15, y - 20, w - 40, y + 20), 200, 340, fill=(80, 80, 80), width=5)


def clock(hours: int, minutes: int):
    def paint(draw, w, h):
        import math
        cx, cy, r = w / 2, h / 2 + 5, 52
        draw.ellipse((cx - r, cy - r, cx + r, cy + r), outline=INK, width=4)
        for i in range(12):
            a = math.tau * i / 12
            draw.line((cx + math.sin(a) * (r - 8), cy - math.cos(a) * (r - 8), cx + math.sin(a) * (r - 2), cy - math.cos(a) * (r - 2)), fill=INK, width=3)
        hour_angle = math.tau * ((hours % 12) + minutes / 60) / 12
        minute_angle = math.tau * minutes / 60
        draw.line((cx, cy, cx + math.sin(hour_angle) * 28, cy - math.cos(hour_angle) * 28), fill=INK, width=6)
        draw.line((cx, cy, cx + math.sin(minute_angle) * 42, cy - math.cos(minute_angle) * 42), fill=INK, width=4)
    return paint


def scene(path: Path, number: int, lines, colour):
    """A story picture for Writing Part 7: a coloured panel with the scene described in words."""
    width, height = 300, 220
    image = Image.new("RGB", (width, height), colour)
    draw = ImageDraw.Draw(image)
    draw.rectangle((1, 1, width - 2, height - 2), outline=INK, width=2)
    draw.ellipse((12, 12, 48, 48), fill=PAPER, outline=INK, width=2)
    f = font("arialbd.ttf", 20)
    draw.text((30 - draw.textlength(str(number), font=f) / 2, 18), str(number), font=f, fill=INK)
    centred_lines(draw, lines, (10, 50, width - 10, height - 10), [font("comic.ttf", 22)] * len(lines))
    image.save(path)


def reading_writing():
    images = HERE / "a2-key-fs-reading-writing" / "images"
    images.mkdir(exist_ok=True)
    notice(images / "rw-p1-q1.png", "SCHOOL LIBRARY", ["Now open on Saturdays, 9 am – 12 pm", "Borrow books for the weekend!"])
    message(images / "rw-p1-q2.png", "Mia", ["Hi Tom, I can't meet you at 3.", "Can we meet at the park at 4.30", "instead? See you there!"])
    notice(images / "rw-p1-q3.png", "CANTEEN", ["When you finish your lunch,", "please leave your tray", "on the table by the door."])
    message(images / "rw-p1-q4.png", "From: Coach Lin", ["Thursday's swimming lesson starts", "at 3.15, not 3.45.", "Please don't be late!"])
    notice(images / "rw-p1-q5.png", "SCHOOL CONCERT – 20 MAY", ["We need singers!", "Interested? See Mr Lee in Room 12."])
    notice(images / "rw-p1-q6.png", "Science test tomorrow!", ["Bring a pen –", "pencils are not allowed."])
    scene(images / "rw-p7-1.png", 1, ["Leo finds a small dog", "alone in the park."], (210, 235, 210))
    scene(images / "rw-p7-2.png", 2, ["He reads the phone", "number on its collar", "and calls."], (215, 225, 245))
    scene(images / "rw-p7-3.png", 3, ["A happy girl gets", "her dog back and", "gives Leo a cake."], (250, 230, 200))


def listening():
    images = HERE / "a2-key-fs-listening" / "images"
    images.mkdir(exist_ok=True)
    pictures = {
        "q1": [("Sunny", sun), ("Rainy", rain), ("Windy", wind)],
        "q2": [("A cap", None), ("A T-shirt", None), ("A bag", None)],
        "q3": [("On the table", None), ("In her coat", None), ("Under the sofa", None)],
        "q4": [("6.15", clock(6, 15)), ("6.45", clock(6, 45)), ("7.30", clock(7, 30))],
        "q5": [("Tennis", None), ("Basketball", None), ("Volleyball", None)],
    }
    for question, options in pictures.items():
        for key, (caption, painter) in zip("abc", options):
            card(images / f"l-p1-{question}-{key}.png", caption, painter)


def portrait(path: Path, colour, width=150, height=170):
    """A simple head-and-shoulders drawing standing in for the photo of a person (B1 Reading Part 2)."""
    image = Image.new("RGB", (width, height), (238, 238, 236))
    draw = ImageDraw.Draw(image)
    draw.ellipse((width / 2 - 70, height - 70, width / 2 + 70, height + 70), fill=colour, outline=INK, width=2)
    draw.ellipse((width / 2 - 34, 30, width / 2 + 34, 104), fill=(244, 214, 188), outline=INK, width=2)
    draw.arc((width / 2 - 14, 70, width / 2 + 14, 88), 20, 160, fill=INK, width=2)
    draw.ellipse((width / 2 - 16, 56, width / 2 - 10, 62), fill=INK)
    draw.ellipse((width / 2 + 10, 56, width / 2 + 16, 62), fill=INK)
    image.save(path)


def b1_reading():
    images = HERE / "b1-pfs-reading" / "images"
    images.mkdir(exist_ok=True)
    notice(images / "b1-r-p1-q1.png", "SPORTS HALL", ["Students may only use the gym", "when a teacher is present.", "Opening times are not changing."], height=190)
    message(images / "b1-r-p1-q2.png", "Ella", ["Hi Sam! The party starts at 7, but could", "you come at 5? I need help putting up", "decorations and making the food."])
    notice(images / "b1-r-p1-q3.png", "SCIENCE MUSEUM TRIP – ALL YEARS", ["Cost: £12, as in the letter home.", "Give your form to Ms Kay by Friday", "or you will not be able to come."], width=460, height=190)
    notice(images / "b1-r-p1-q4.png", "WHEELS BIKE SHOP", ["Buy any new bike this month:", "free repairs for a whole year!", "Test rides every Saturday – £5"], height=190)
    message(images / "b1-r-p1-q5.png", "Jack", ["Max, can I borrow your history notes?", "I missed Monday's lesson and the test", "is on Thursday. Back tomorrow, promise!"])
    for name, colour in (("dina", (120, 170, 220)), ("omar", (210, 120, 90)), ("lena", (150, 200, 140)), ("tom", (230, 190, 90)), ("maya", (180, 140, 210))):
        portrait(images / f"b1-r-p2-{name}.png", colour)


if __name__ == "__main__":
    reading_writing()
    listening()
    b1_reading()
    print("Pictures written under", HERE)
