# -*- coding: utf-8 -*-
"""Extrae el texto y las imagenes de los 27 modulos PL-400 (docx separados).

A diferencia de AB-410 (un unico docx con marcadores P#/MD#), PL-400 ya viene
troceado: Pl-400/Conocimiento/Modulos/p<part>_md<md>.docx (uno por modulo).

Por cada docx:
  1. Recorre el cuerpo EN ORDEN DE LECTURA (parrafos + imagenes).
  2. Usa los estilos Heading 1/2/3 para prefijar '#','##','###' en texto.txt
     (mismo formato que consume build_pl400_data.build_module_tomes).
  3. Vuelca las imagenes embebidas a  Conocimiento/p<part>/MD<md>/img/imageNN.png
  4. Escribe texto.txt por modulo y un image_map.txt global.

El contenido de Microsoft Learn ya esta en INGLES. Las 2 primeras lineas de cada
docx son marcadores en espanol ("Modulo N - ...", "Parte N - ...") y se descartan.
"""
import os, re, io, glob, sys
import docx
from docx import Document
from docx.oxml.ns import qn
from docx.text.paragraph import Paragraph

HERE = os.path.dirname(os.path.abspath(__file__))
SRC  = os.path.join(HERE, "Pl-400", "Conocimiento", "Modulos")
OUT  = os.path.join(HERE, "Conocimiento")
MAP  = os.path.join(HERE, "image_map.txt")

BLIP  = qn("a:blip")
EMBED = qn("r:embed")

RE_MARK = re.compile(r"^(modulo|parte)\s+\d+\b", re.I)   # lineas marcador (espanol) a descartar


def heading_level(style_name):
    if not style_name:
        return 0
    if "Heading 1" in style_name or style_name == "Title":
        return 1
    if "Heading 2" in style_name:
        return 2
    if "Heading 3" in style_name:
        return 3
    return 0


def para_image_rids(p_elem):
    """rIds de las imagenes embebidas en un parrafo, en orden."""
    rids = []
    for blip in p_elem.iter(BLIP):
        rid = blip.get(EMBED)
        if rid:
            rids.append(rid)
    return rids


def process(path, part, md, map_lines):
    doc = Document(path)
    rel_parts = doc.part.related_parts
    folder = os.path.join(OUT, "p%d" % part, "MD%d" % md)
    img_dir = os.path.join(folder, "img")
    os.makedirs(img_dir, exist_ok=True)
    # limpiar salidas previas
    for d in (folder, img_dir):
        for f in os.listdir(d):
            fp = os.path.join(d, f)
            if os.path.isfile(fp):
                os.remove(fp)

    txt_lines = []
    n_img = 0
    last_text = ""
    body = doc.element.body
    for child in body.iterchildren():
        if child.tag != qn("w:p"):
            continue
        para = Paragraph(child, doc)
        text = para.text.strip()
        rids = para_image_rids(child)
        if text and RE_MARK.match(text):
            continue                      # marcador "Modulo N"/"Parte N" (espanol)
        lvl = heading_level(para.style.name)
        if text:
            txt_lines.append(("#" * lvl) + " " + text if lvl else text)
            last_text = text
        for rid in rids:
            part_obj = rel_parts.get(rid)
            if part_obj is None:
                continue
            blob = part_obj.blob
            pname = os.path.basename(part_obj.partname)     # image12.png
            ext = pname.rsplit(".", 1)[-1].lower()
            if ext not in ("png", "jpg", "jpeg", "emf", "wmf", "gif"):
                continue
            save_name = pname[:-4] + "jpg" if ext == "jpeg" else pname
            with open(os.path.join(img_dir, save_name), "wb") as fh:
                fh.write(blob)
            n_img += 1
            map_lines.append("p%d/MD%d  %s  <- %s" % (part, md, save_name, last_text[:70]))

    with open(os.path.join(folder, "texto.txt"), "w", encoding="utf-8") as fh:
        fh.write("\n".join(txt_lines))
    n_head = sum(1 for l in txt_lines if l.startswith("#"))
    n_chars = sum(len(l) for l in txt_lines)
    print("p%d/MD%d  %3d lineas  %2d headings  %6d ch  %2d img" % (
        part, md, len(txt_lines), n_head, n_chars, n_img))
    return n_img


def main():
    if not os.path.isdir(SRC):
        print("No existe la carpeta de modulos:", SRC); sys.exit(1)
    files = sorted(glob.glob(os.path.join(SRC, "p*_md*.docx")))
    map_lines, total_imgs, n = [], 0, 0
    for f in files:
        m = re.match(r"p(\d+)_md(\d+)\.docx$", os.path.basename(f), re.I)
        if not m:
            continue
        part, md = int(m.group(1)), int(m.group(2))
        total_imgs += process(f, part, md, map_lines)
        n += 1
    with open(MAP, "w", encoding="utf-8") as fh:
        fh.write("\n".join(map_lines))
    print("\nTotal modulos:", n, "| imagenes guardadas:", total_imgs)
    print("Mapa de imagenes ->", MAP)


if __name__ == "__main__":
    main()
