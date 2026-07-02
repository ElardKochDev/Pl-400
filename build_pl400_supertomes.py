# -*- coding: utf-8 -*-
"""Construye supertomes.json: 6 Super Tomos (uno por parte) para PL-400.
Se desbloquean al completar TODOS los modulos de su parte. Cada uno resume el
contenido de la parte en ~12 paginas {section, text, image} tomadas de los tomos
ya generados en ab900data.json + diagramas de Resources/Art/PL400. Todo en INGLES.
Reejecutar tras build_pl400_data.py."""
import os, json, re

HERE  = os.path.dirname(os.path.abspath(__file__))
UNITY = os.path.join(HERE, "PL400Unity", "Assets", "Resources", "Data")
DATA  = os.path.join(UNITY, "ab900data.json")
OUT   = os.path.join(UNITY, "supertomes.json")

PART_TITLE = {
 1: "Super Tome I - Canvas apps: data & Power Fx",
 2: "Super Tome II - Performance & developer fundamentals",
 3: "Super Tome III - UI extensibility: client scripting & PCF",
 4: "Super Tome IV - Server-side: Dataverse, plug-ins & Web API",
 5: "Super Tome V - Azure integration & connectors",
 6: "Super Tome VI - Advanced connectors & ALM",
}
NPART = 6


def trim(text, lim=900):
    text = re.sub(r"\s+", " ", text).strip()
    if len(text) <= lim:
        return text
    cut = text[:lim]
    dot = cut.rfind(". ")
    return (cut[:dot + 1] if dot > lim * 0.5 else cut).strip()


def build():
    data = json.load(open(DATA, encoding="utf-8"))
    tomes = data["tomes"]
    out_tomes = []
    for part in range(1, NPART + 1):
        keys = sorted([k for k in tomes if re.match(r"p%dm\d+_\d+$" % part, k)],
                      key=lambda k: (int(re.search(r"m(\d+)_", k).group(1)), int(k.rsplit("_", 1)[1])))
        pages = []
        for k in keys:
            t = tomes[k]
            if not t["pages"]:
                continue
            raw = t["pages"][0]
            pages.append({"section": t["t"], "text": trim(raw),
                          "image": (t["imgs"][0] if t.get("imgs") else "")})
        if len(pages) > 12:
            step = len(pages) / 12.0
            pages = [pages[int(i * step)] for i in range(12)]
        pages.append({"section": "Exam quick keys",
                      "text": "Review the key components and verbs of this part: pick the RIGHT tool "
                              "(canvas/model-driven app, Power Automate, plug-in, Web API, PCF, custom "
                              "connector, Azure Function/Service Bus, solution/ALM), the TASK verb "
                              "(create, edit, delegate, extend, integrate, secure, deploy) and WHO owns "
                              "the data. Map each scenario to the exact option. Good luck on PL-400!",
                      "image": pages[0]["image"] if pages else ""})
        out_tomes.append({"id": "st_p%d" % part, "title": PART_TITLE[part],
                          "unlock": "p%d" % part, "pages": pages})
        print("st_p%d: %d paginas" % (part, len(pages)))

    out = {"source": "PL-400 module content (per-part summaries)", "tomes": out_tomes}
    with open(OUT, "w", encoding="utf-8") as f:
        json.dump(out, f, ensure_ascii=False)
    print("Escrito:", OUT)


if __name__ == "__main__":
    build()
