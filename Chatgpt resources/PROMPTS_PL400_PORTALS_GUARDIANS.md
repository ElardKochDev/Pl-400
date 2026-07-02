# PROMPTS PL-400 — Portales por color + Guardianes v3 (sin aura)

> Mismo estilo que el resto: **pixel-art JRPG 16-bit (SNES)**, contornos limpios, colores vivos,
> **una figura/objeto por celda sobre FONDO PLANO uniforme MUY OSCURO** (gris-azulado casi negro),
> con márgenes claros sin tocar los bordes. **SIN texto, sin rótulos, sin marcos de escena, sin suelo.**
> Yo troceo cada hoja con transparencia (como los enemigos).


---

## 1) PORTALES POR MUNDO  →  archivo `portals.png`

Necesito **7 portales mágicos**, uno por reino, **cada uno de SU color**. Son puertas/portales
verticales tipo arco de piedra con un **vórtice de energía giratorio** en el centro del color indicado
(el brillo del vórtice NO cuenta como fondo: el fondo sigue plano y oscuro). Ornamentados pero legibles
a tamaño pequeño (~64 px). Todos con la MISMA forma de arco para que se vean como hermanos; **solo cambia
el COLOR del vórtice y de las gemas del marco**.

Cuadrícula **4 columnas × 2 filas** (7 usados + 1 vacío), fondo plano oscuro uniforme:

1. **Mundo 1 – Canvas Data Keep** → vórtice **azul acero** (steel blue), gemas azul claro.
2. **Mundo 2 – Performance Bastion** → vórtice **verde-cian esmeralda**, gemas verde menta.
3. **Mundo 3 – UI Extensibility Spire** → vórtice **violeta/púrpura**, gemas lila.
4. **Mundo 4 – Server-Side Crypt** → vórtice **ámbar/rojo lava**, gemas naranja.
5. **Mundo 5 – Azure Nexus** → vórtice **azul-cian eléctrico** (cyan neón brillante), gemas celeste.
6. **Mundo 6 – Connectors & ALM Citadel** → vórtice **púrpura-dorado** (púrpura con oro), gemas doradas.
7. **Torre del Examen (final)** → vórtice **dorado-fuego intenso** con toques carmesí, marco de oro
   coronado; el más grande e imponente (es el portal final).

_Claves (celda por celda, izq→der / arriba→abajo):_ `portal_p1`, `portal_p2`, `portal_p3`,
`portal_p4`, `portal_p5`, `portal_p6`, `portal_final`, (8ª celda vacía).


---

## 2) GUARDIANES v3 — SIN AURA  →  archivo `guardians_v3.png`

Repite los 6 guardianes colosales + el jefe final, PERO esta vez **SIN aura de energía, SIN llamas,
SIN rayos, SIN humo y SIN NINGÚN elemento de fondo** (nada de ventanas de código, marcos de puerta,
medidores, servidores ni cajas flotantes). **Solo la figura**, de cuerpo entero, centrada, con silueta
limpia y recortable, sobre fondo plano oscuro uniforme. Deben verse imponentes por su tamaño, armadura
y pose — no por efectos alrededor.

Cuadrícula **4 columnas × 2 filas** (7 usados + 1 vacío), una figura por celda:

1. **Sentinel of Canvas Data** — guardián colosal, armadura **azul acero**, hacha/lanza.
2. **Custodian of Performance** — guardián colosal, armadura **verde-cian**.
3. **Warden of UI Extensibility** — guardián demoníaco colosal, tonos **violeta/púrpura**.
4. **Keeper of the Server Crypt** — guardián colosal, armadura **ámbar/rojo** (bronce oscuro).
5. **Overseer of the Azure Nexus** — guardián colosal, armadura **azul-cian eléctrico** (metal frío).
6. **Arbiter of Connectors & ALM** — guardián colosal con cuernos, armadura **púrpura-dorada**.
7. **THE PL-400 EXAM** — rey demonio final con **corona + birrete de graduación**, armadura dorada
   épica; imponente por sí mismo, **sin fuego alrededor**. Puede sostener un tomo/cetro, SIN texto.

_Claves:_ `guard_d1`..`guard_d6` (celdas 1-6) y `boss_final` (celda 7).

> Recordatorio: fondo plano + sin aura + sin props flotantes = recorte con transparencia perfecto,
> igual que quedaron los enemigos.
