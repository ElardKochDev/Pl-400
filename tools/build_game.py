# -*- coding: utf-8 -*-
"""Orquestador del pipeline docx -> datos del juego PL-400 (una sola orden).

  1) split_pl400.py            27 docx -> Conocimiento/p#/MD#/{texto.txt,img/}
  2) build_pl400_data.py       textos + questions/*.json + config -> ab900data.json
  3) build_pl400_supertomes.py textos -> supertomes.json

NO genera las preguntas (se escriben en questions/p#_md#.json, en INGLES,
4 por tomo). Ver ADAPT-NEW-CERT.md (proyecto AB-410) para el flujo completo.

Uso:
    python tools/build_game.py            # pipeline completo
    python tools/build_game.py --data     # solo reconstruye datos (pasos 2-3)
"""
import os, sys, subprocess

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

STEPS_FULL = [
    ("Trocear los 27 docx por modulo", "split_pl400.py"),
    ("Construir ab900data.json (preguntas, zonas, areas, meta)", "build_pl400_data.py"),
    ("Construir supertomes.json (Super Tomos por parte)", "build_pl400_supertomes.py"),
]
STEPS_DATA = STEPS_FULL[1:]


def run(desc, script):
    path = os.path.join(ROOT, script)
    if not os.path.exists(path):
        print("  [SALTADO] no existe %s" % script)
        return
    print("\n=== %s ===\n    (%s)" % (desc, script))
    r = subprocess.run([sys.executable, path], cwd=ROOT)
    if r.returncode != 0:
        print("\n!! Fallo en %s (codigo %d). Pipeline detenido." % (script, r.returncode))
        sys.exit(r.returncode)


def main():
    steps = STEPS_DATA if "--data" in sys.argv else STEPS_FULL
    print("Pipeline de datos del juego PL-400  (raiz: %s)" % ROOT)
    for desc, script in steps:
        run(desc, script)
    print("\nListo. Compila con build-pl400-apk.ps1 / build-pl400-windows.ps1.")


if __name__ == "__main__":
    main()
