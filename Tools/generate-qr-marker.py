#!/usr/bin/env python3
"""Gera o Image Target alternativo do SafetyLens baseado em QR Code.

O QR NAO e decodificado pelo app: ele e rastreado como imagem (Image Target)
pelo ARKit/ARCore. QR puro e visualmente repetitivo e tende a receber nota
baixa de rastreamento no `arcoreimg`; por isso a arte adiciona moldura, titulo,
regua e cantos assimetricos (mesma linguagem visual do marcador WL03), que dao
pontos de referencia unicos para o tracking.

Uso:
    pip install qrcode pillow
    python3 Tools/generate-qr-marker.py

Saida:
    Assets/Markers/safetylens-qr-target.png   (1200x1200, imprimir com 12 cm)
    docs/images/marcador-qr-target.png        (copia para documentacao)

Conferencia de qualidade (opcional, requer a ferramenta do ARCore):
    arcoreimg eval-img --input_image_path=Assets/Markers/safetylens-qr-target.png
    Meta: nota >= 75.
"""

from __future__ import annotations

import shutil
from pathlib import Path

import qrcode
from PIL import Image, ImageDraw, ImageFont

SIZE = 1200
QR_CONTENT = "safetylens:demo-industrial-risk"

# Paleta do marcador WL03 (mesma familia visual).
INK = (15, 27, 45)        # azul-marinho dos textos e molduras
TEAL = (23, 90, 110)      # circulo do canto superior esquerdo
RED = (217, 83, 56)       # triangulo do canto superior direito
GREEN = (90, 115, 84)     # quadrado com X do canto inferior esquerdo
PURPLE = (108, 66, 150)   # quadrado com triangulo do canto inferior direito
WHITE = (255, 255, 255)
BG = (248, 249, 251)


def load_font(points: int) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    for candidate in (
        "/System/Library/Fonts/Helvetica.ttc",
        "/System/Library/Fonts/Supplemental/Arial Bold.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
    ):
        try:
            return ImageFont.truetype(candidate, points)
        except OSError:
            continue
    return ImageFont.load_default()


def centered_text(draw: ImageDraw.ImageDraw, y: int, text: str, font, fill) -> None:
    box = draw.textbbox((0, 0), text, font=font)
    width = box[2] - box[0]
    draw.text(((SIZE - width) / 2 - box[0], y), text, font=font, fill=fill)


def build_qr() -> Image.Image:
    qr = qrcode.QRCode(
        error_correction=qrcode.constants.ERROR_CORRECT_M,
        box_size=16,
        border=0,
    )
    qr.add_data(QR_CONTENT)
    qr.make(fit=True)
    return qr.make_image(fill_color=INK, back_color=WHITE).convert("RGB")


def main() -> None:
    image = Image.new("RGB", (SIZE, SIZE), BG)
    draw = ImageDraw.Draw(image)

    # Molduras duplas, como no WL03.
    draw.rectangle([12, 12, SIZE - 12, SIZE - 12], outline=INK, width=14)
    draw.rectangle([44, 44, SIZE - 44, SIZE - 44], outline=INK, width=4)

    # Titulo e subtitulo (fonte dimensionada para nao invadir os cantos assimetricos).
    centered_text(draw, 84, "SAFETYLENS  QR TARGET", load_font(54), INK)
    centered_text(draw, 165, "ALVO ALTERNATIVO - RASTREADO COMO IMAGEM", load_font(28), INK)

    # QR central.
    qr = build_qr()
    qr_size = 640
    qr = qr.resize((qr_size, qr_size), Image.NEAREST)
    qr_x = (SIZE - qr_size) // 2
    qr_y = 250
    draw.rectangle(
        [qr_x - 18, qr_y - 18, qr_x + qr_size + 18, qr_y + qr_size + 18],
        fill=WHITE,
        outline=INK,
        width=6,
    )
    image.paste(qr, (qr_x, qr_y))

    # Cantos assimetricos: cada canto e diferente para dar pontos unicos ao tracking.
    draw.ellipse([70, 70, 190, 190], fill=TEAL)
    draw.rectangle([105, 105, 155, 155], fill=WHITE)

    draw.polygon([(SIZE - 70, 70), (SIZE - 190, 190), (SIZE - 70, 190)], fill=RED)
    draw.ellipse([SIZE - 150, 120, SIZE - 100, 170], fill=WHITE)

    draw.rectangle([70, SIZE - 190, 190, SIZE - 70], fill=GREEN)
    draw.line([76, SIZE - 184, 184, SIZE - 76], fill=WHITE, width=12)
    draw.line([76, SIZE - 76, 184, SIZE - 184], fill=WHITE, width=12)

    draw.rectangle([SIZE - 190, SIZE - 190, SIZE - 70, SIZE - 70], fill=PURPLE)
    draw.polygon(
        [(SIZE - 130, SIZE - 170), (SIZE - 170, SIZE - 95), (SIZE - 90, SIZE - 95)],
        fill=WHITE,
    )

    # Regua inferior (quebra a simetria vertical e marca a escala fisica).
    ruler_y = SIZE - 230
    draw.line([230, ruler_y, SIZE - 230, ruler_y], fill=INK, width=4)
    for i in range(25):
        x = 230 + i * (SIZE - 460) // 24
        tick = 26 if i % 4 == 0 else 14
        draw.line([x, ruler_y - tick, x, ruler_y], fill=INK, width=4)
    centered_text(draw, SIZE - 190, "ALVO FISICO: largura recomendada 12 cm", load_font(34), INK)
    centered_text(draw, SIZE - 140, QR_CONTENT, load_font(26), INK)

    root = Path(__file__).resolve().parent.parent
    target = root / "Assets" / "Markers" / "safetylens-qr-target.png"
    target.parent.mkdir(parents=True, exist_ok=True)
    image.save(target, "PNG")

    docs_copy = root / "docs" / "images" / "marcador-qr-target.png"
    docs_copy.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(target, docs_copy)

    print(f"OK: {target}")
    print(f"OK: {docs_copy}")


if __name__ == "__main__":
    main()
