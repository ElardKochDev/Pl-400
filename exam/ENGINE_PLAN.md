# Plan de construcción: UI nueva + reparto de mundos (PL-400 examen real)

Objetivo: reemplazar el banco viejo por las 103 preguntas reales, repartidas en **6 mundos por
área** + **Case Files**, con **UI nueva** para los tipos del examen y un **lector de casos de estudio**.

## 1) Pipeline de datos — `build_pl400_exam.py` (nuevo)
- Lee `exam/q/*.json` (preguntas sueltas) y `exam/cs/*.json` (casos de estudio).
- Agrupa por `area` -> 6 mundos: design-dataverse, apps-powerfx, automation, extend-ux,
  extend-platform, integrations-alm. Casos de estudio -> área `casefiles`.
- Escribe el banco en `ab900data.json` (campo `bq` u otro que lea el motor), con el ESQUEMA nuevo
  por tipo (abajo). VACÍA el `bq` viejo (reemplazo total, el usuario lo pidió).
- Reparte cada pregunta al calabozo de su mundo (como tomo-check o pregunta de combate).
- Ajusta `meta.parts` a 6 áreas; nombres de mundo en inglés.

## 2) Esquema por tipo (lo que ya escribí en exam/q)
- `single`  : options[], answer="texto"
- `multi`   : options[], answer=["t1","t2"] (elige k)
- `yesno`   : rows[{s,v:bool}] (+ opcional `code`)
- `order`   : options[] (pool), answer=[secuencia correcta]
- `match`   : options[] (pool), rows[] (frases), answer={frase:opción}
- `hotspot` : rows[{s,a,options?}] (+ opcional `choices`)  (desplegables)
- `code`    : code (con `[[1]]`,`[[2]]`… huecos), blanks[{id,a,options[]}]
- caso: cs/csN.json = {sections[{title,text}], questions[{q,type,options,answer,why}]}

## 3) Motor C# (AB900Game.cs) — `BuildQuestionBody(col, q)` nuevos tipos
Hoy soporta single/multi/yesno/drop. AÑADIR render + evaluador (Func<bool?>) para:
- **order**: lista de opciones barajadas; el jugador las ORDENA (botones ↑/↓ o tap secuencial que
  numera). Correcto si la secuencia == answer. Mostrar como "toca en orden".
- **match**: por cada `row` (frase) un desplegable/botón que elige una `option` del pool. Correcto
  si todas las asignaciones == answer.
- **hotspot**: por cada `row` un desplegable con sus `options` (o `choices`); correcto si todas == a.
- **code**: render del `code` en fuente monoespaciada, SCROLLEABLE, con los `[[n]]` como
  desplegables inline; correcto si todos los blanks == a. (Reutilizar el combo desplegable que ya
  existe para `drop`.)
- Reutilizar `BuildQuestionWidget` (aviso ⚠ si incompleto, todo-o-nada) y el flujo de combate/tomo.

## 4) Lector de casos de estudio (area casefiles) — pantalla nueva
- `RenderCaseStudy(cs)`: panel con PESTAÑAS de `sections` (Background/Current Environment/
  Requirements…) arriba o a un lado, texto scrolleable; abajo navegación de sub-preguntas
  (Question 1..N) que usan `BuildQuestionBody`. Al acertar todas -> caso superado (XP/medalla).
- Área `casefiles` en la ciudad o como zona propia (biblioteca "Case Files").

## 5) Tomos -> notas de examen (resumen por mundo)
- Reescribir tomos de cada mundo como resumen conciso enfocado al examen (skills measured del área),
  alineado con sus preguntas. (Fase posterior a la UI.)

## 6) Orden de ejecución sugerido
1. `build_pl400_exam.py` con las preguntas YA transcritas (16 + cs1) -> ab900data.json (banco nuevo).
2. C#: nuevos tipos en BuildQuestionBody + RenderCaseStudy + gating area casefiles.
3. Compilar (Windows smoke) -> APK. Probar tipos nuevos con el slice actual.
4. Seguir transcribiendo (q018→ y csN) hasta 103; el pipeline los recoge sin tocar C#.
5. Resumir tomos.

## Estado transcripción
16 preguntas (q001..q016) + cs1 (IN_PROGRESS). Faltan Q17(cs1 completar)..Q103. Ver progress.md.
