# Transcripción del examen real (MeasureUp) — PL-400

Fuente: `Exam Questions/*.png` (209 capturas = **103 preguntas**, "Question X of 103").
Objetivo: transcribir las 103 FIEL (enunciado + opciones + respuesta correcta + explicación),
repartirlas en **6 mundos por área del examen**, casos de estudio en un área **Case Files** con
lector, y tomos resumidos a notas de examen.

## Esquema por pregunta (un archivo `q/qNNN.json`)
```json
{
  "n": 1,                         // nº 1..103
  "area": "extend-platform",      // clave de mundo (ver abajo); "" si aún sin asignar
  "type": "single|multi|order|match|hotspot|yesno",
  "scenario": "contexto...",      // párrafos de escenario (puede faltar)
  "q": "pregunta...",
  "options": ["...", "..."],      // single/multi/order/match: pool de opciones
  "answer": ...,                  // single: "texto correcto"
                                  // multi: ["texto1","texto2"]
                                  // order: ["p1","p2",...] en el ORDEN correcto
                                  // match: {"target/frase": "opción correcta", ...}
                                  // hotspot: {"blank1":"valor", ...} (código/desplegables)
                                  // yesno: [true/false por afirmación] con "rows"
  "rows": [...],                  // yesno/hotspot: afirmaciones o huecos
  "why": "explicación oficial...",
  "cs": null,                     // id de caso de estudio si pertenece a uno (agrupa varias)
  "src": ["Screenshot ... .png"]  // capturas de origen
}
```

## Mundos (6 áreas skills-measured PL-400)
- `design-dataverse` — Diseño técnico + Configurar Dataverse.
- `apps-powerfx` — Crear/Configurar Power Apps + Power Fx.
- `automation` — Automatización de procesos (Power Automate, business rules).
- `extend-ux` — Extender la experiencia (client scripting, PCF).
- `extend-platform` — Extender la plataforma (plug-ins, Custom API, Web API, Dataverse SDK).
- `integrations-alm` — Integraciones (Azure, conectores) + ALM + seguridad.
- Casos de estudio → área `casefiles` (lector de contexto + sub-preguntas), `cs` agrupa.

## Progreso
Ver `progress.md`. Cada pregunta hecha = archivo `q/qNNN.json`.
```
