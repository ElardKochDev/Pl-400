# PROMPTS de ChatGPT para el ARTE de PL-400

> Genera cada hoja como **pixel-art JRPG 16-bit** (estilo SNES), contornos limpios, colores vivos, paleta tecnológica (azul, cian, verde esmeralda, violeta, ámbar). **Fondo COMPLETAMENTE plano y uniforme gris-azulado MUY OSCURO** (para que el recortador separe las figuras por sí solo). **Una figura por celda**, centrada, sin texto, sin sombras que toquen otras celdas, márgenes claros entre celdas. Cada figura apoyada en la base de su celda. Temática: desarrollador de Microsoft Power Platform (Dataverse, Power Apps, Power Automate, plug-ins, conectores, Azure, ALM).

Cuando me entregues las imágenes, yo las troceo con `slice_pl400_art.py` y las nombro con las **claves** de las tablas de abajo (el PNG con esa clave MANDA sobre el dibujo por código).

---


## 1) HOJAS DE ENEMIGOS (una por mundo)


### Mundo 1 — Canvas Data Keep (P1)  →  archivo `enemies_p1.png`

Hoja pixel-art, cuadrícula **3 columnas × 5 filas** (15 enemigos, cada FILA = un módulo). Fondo plano gris-azulado oscuro. Enemigos temáticos, tamaño mediano, distintos entre sí:


**Fila 1 (Módulo 1 · Hall of Variables):**
1) **Stray Global** — Global variables (Set) hold a value app-wide for the session.
2) **Context Wisp** — Context variables (UpdateContext) are scoped to a single screen.
3) **Collection Swarm** — Collections store tables of records in memory via Collect/ClearCollect.

**Fila 2 (Módulo 2 · Forge of Patch):**
4) **Broken Form** — Patch creates or edits records without needing a form control.
5) **Duplicate Row** — Patch with Defaults() creates a new record; passing a record edits it.
6) **Ghost Record** — Remove and RemoveIf delete records; Remove does not ask to confirm.

**Fila 3 (Módulo 3 · Gallery of Choices):**
7) **Local Choice** — Local choices belong to one column; global choices are reusable across tables.
8) **Lookup Mirage** — A choice stores a fixed option set; a lookup points to a row in another table.
9) **Filter Phantom** — Filter Dataverse choice columns in Power Fx using the option value/record.

**Fila 4 (Módulo 4 · Keep of Relations):**
10) **Orphan Row** — A one-to-many (1:N) relationship links a parent row to many child rows.
11) **Tangled Web** — Many-to-many (N:N) relationships connect rows on both sides freely.
12) **Lost Lookup** — A lookup column on the child materializes its relationship to the parent.

**Fila 5 (Módulo 5 · Sanctum of Expressions):**
13) **Syntax Imp** — The formula bar offers auto-suggest, hints and links as you type Power Fx.
14) **Type Mismatch** — Conversion functions (Value, Text, DateValue) change a value's type.
15) **Nested Tangle** — Combine functions (If, With, ForAll) to write complex expressions.

_Claves (celda por celda, izq→der / arriba→abajo):_ `e_p1m1_0`=Stray Global, `e_p1m1_1`=Context Wisp, `e_p1m1_2`=Collection Swarm, `e_p1m2_0`=Broken Form, `e_p1m2_1`=Duplicate Row, `e_p1m2_2`=Ghost Record, `e_p1m3_0`=Local Choice, `e_p1m3_1`=Lookup Mirage, `e_p1m3_2`=Filter Phantom, `e_p1m4_0`=Orphan Row, `e_p1m4_1`=Tangled Web, `e_p1m4_2`=Lost Lookup, `e_p1m5_0`=Syntax Imp, `e_p1m5_1`=Type Mismatch, `e_p1m5_2`=Nested Tangle


### Mundo 2 — Performance Bastion (P2)  →  archivo `enemies_p2.png`

Hoja pixel-art, cuadrícula **3 columnas × 5 filas** (15 enemigos, cada FILA = un módulo). Fondo plano gris-azulado oscuro. Enemigos temáticos, tamaño mediano, distintos entre sí:


**Fila 1 (Módulo 6 · Bastion of Delegation):**
1) **Non-Delegable Blob** — Delegation pushes processing to the source; non-delegable functions run locally up to the limit.
2) **Refresh Glutton** — Too many refreshes and lookups are the most common performance bottleneck.
3) **Warning Sprite** — A delegation warning means the query may not return all rows.

**Fila 2 (Módulo 7 · Observatory of Monitor):**
4) **Slow Start** — Optimize App.OnStart; move work to OnVisible or Concurrent to speed startup.
5) **Throttle Wraith** — Connector limits and throttling can slow or block data calls.
6) **Blind Spot** — Monitor traces every event; Application Insights logs telemetry for diagnostics.

**Fila 3 (Módulo 8 · Nexus of the Platform):**
7) **Silent Connector** — Connectors link Power Platform to external services and data.
8) **Schema Drifter** — The Common Data Model gives Dataverse standard tables and semantics.
9) **Code Temptation** — Write code only when configuration cannot meet the requirement.

**Fila 4 (Módulo 9 · Workshop of Tools):**
10) **CLI Gremlin** — The Power Platform CLI (pac) scaffolds PCF, manages solutions and auth.
11) **Layer Specter** — Solutions layer; managed layers sit above the unmanaged base.
12) **Dependency Knot** — Solution dependencies must exist in the target environment to import.

**Fila 5 (Módulo 10 · Forge of Extensibility):**
13) **Pipeline Echo** — The event pipeline (pre-validation, pre/post-operation) is where plug-ins run.
14) **Custom API Shard** — A Custom API defines a reusable, bound or unbound Dataverse operation.
15) **Config-or-Code Sphinx** — Prefer business rules/flows over client script/plug-ins when they suffice.

_Claves (celda por celda, izq→der / arriba→abajo):_ `e_p2m1_0`=Non-Delegable Blob, `e_p2m1_1`=Refresh Glutton, `e_p2m1_2`=Warning Sprite, `e_p2m2_0`=Slow Start, `e_p2m2_1`=Throttle Wraith, `e_p2m2_2`=Blind Spot, `e_p2m3_0`=Silent Connector, `e_p2m3_1`=Schema Drifter, `e_p2m3_2`=Code Temptation, `e_p2m4_0`=CLI Gremlin, `e_p2m4_1`=Layer Specter, `e_p2m4_2`=Dependency Knot, `e_p2m5_0`=Pipeline Echo, `e_p2m5_1`=Custom API Shard, `e_p2m5_2`=Config-or-Code Sphinx


### Mundo 3 — UI Extensibility Spire (P3)  →  archivo `enemies_p3.png`

Hoja pixel-art, cuadrícula **3 columnas × 4 filas** (12 enemigos, cada FILA = un módulo). Fondo plano gris-azulado oscuro. Enemigos temáticos, tamaño mediano, distintos entre sí:


**Fila 1 (Módulo 11 · Atrium of Client Script):**
1) **Unregistered Handler** — Register client scripts on form/field events via form properties or code.
2) **Context Cipher** — The execution context exposes the form context to access data and UI.
3) **Hidden Section** — getControl/setVisible show or hide form controls, tabs and sections.

**Fila 2 (Módulo 12 · Spire of the Xrm Object):**
4) **WebApi Wraith** — Xrm.WebApi performs CRUD against Dataverse from client script.
5) **Deprecated Revenant** — Avoid unsupported/deprecated methods; run the Solution Checker.
6) **Naming Collision** — Define unique function names to avoid clashes across libraries.

**Fila 3 (Módulo 13 · Foundry of Components):**
7) **Manifest Gap** — The manifest (ControlManifest) declares a component's properties and resources.
8) **Lifecycle Loop** — PCF lifecycle: init, updateView, getOutputs, destroy.
9) **Kit Mimic** — Creator Kit and community components speed up building UIs.

**Fila 4 (Módulo 14 · Laboratory of Code Components):**
10) **Build Error** — pac pcf init plus npm build compiles a code component project.
11) **Style Glitch** — CSS web resources style a code component's rendered output.
12) **Harness Shade** — Test and debug components in the Power Apps component test harness.

_Claves (celda por celda, izq→der / arriba→abajo):_ `e_p3m1_0`=Unregistered Handler, `e_p3m1_1`=Context Cipher, `e_p3m1_2`=Hidden Section, `e_p3m2_0`=WebApi Wraith, `e_p3m2_1`=Deprecated Revenant, `e_p3m2_2`=Naming Collision, `e_p3m3_0`=Manifest Gap, `e_p3m3_1`=Lifecycle Loop, `e_p3m3_2`=Kit Mimic, `e_p3m4_0`=Build Error, `e_p3m4_1`=Style Glitch, `e_p3m4_2`=Harness Shade


### Mundo 4 — Server-Side Crypt (P4)  →  archivo `enemies_p4.png`

Hoja pixel-art, cuadrícula **3 columnas × 3 filas** (9 enemigos, cada FILA = un módulo). Fondo plano gris-azulado oscuro. Enemigos temáticos, tamaño mediano, distintos entre sí:


**Fila 1 (Módulo 15 · Crypt of Dataverse):**
1) **Metadata Guardian** — Dataverse is metadata-driven and solution-aware.
2) **Event Echo** — Event messages (Create, Update...) flow through the event pipeline stages.
3) **Async Phantom** — Steps run synchronously (immediate) or asynchronously (background).

**Fila 2 (Módulo 16 · Vault of Plug-ins):**
4) **Input Specter** — InputParameters/OutputParameters carry data in the plug-in context.
5) **Image Wraith** — Pre/PostEntityImages capture row values before and after the operation.
6) **Alternative Imp** — Prefer business rules or flows when a plug-in is not required.

**Fila 3 (Módulo 17 · Gateway of the Web API):**
7) **OData Sentinel** — The Dataverse Web API is an OData v4 RESTful endpoint.
8) **Token Gatekeeper** — Register an app in Microsoft Entra ID and use OAuth to get an access token.
9) **FetchXML Shade** — FetchXML expresses complex queries against Dataverse.

_Claves (celda por celda, izq→der / arriba→abajo):_ `e_p4m1_0`=Metadata Guardian, `e_p4m1_1`=Event Echo, `e_p4m1_2`=Async Phantom, `e_p4m2_0`=Input Specter, `e_p4m2_1`=Image Wraith, `e_p4m2_2`=Alternative Imp, `e_p4m3_0`=OData Sentinel, `e_p4m3_1`=Token Gatekeeper, `e_p4m3_2`=FetchXML Shade


### Mundo 5 — Azure Nexus (P5)  →  archivo `enemies_p5.png`

Hoja pixel-art, cuadrícula **3 columnas × 5 filas** (15 enemigos, cada FILA = un módulo). Fondo plano gris-azulado oscuro. Enemigos temáticos, tamaño mediano, distintos entre sí:


**Fila 1 (Módulo 18 · Nexus of Azure):**
1) **Bus Phantom** — Azure Service Bus queues and relays Dataverse messages to external listeners.
2) **Grid Wisp** — Event Grid and Event Hubs handle event routing at scale.
3) **Endpoint Cipher** — Register a Service Bus endpoint and a step to publish Dataverse events.

**Fila 2 (Módulo 19 · Reactor of Functions):**
4) **Cold Start** — The Consumption plan scales to zero; Premium avoids cold starts.
5) **Plan Drifter** — Choose Consumption, Flex, Premium or Dedicated hosting per need.
6) **Timeout Wraith** — Function apps have a time-out duration that varies by plan.

**Fila 3 (Módulo 20 · Assembly of Bindings):**
7) **Trigger Ghost** — A trigger starts a function; bindings connect input/output data.
8) **Binding Knot** — Binding direction is in, out or inout.
9) **Secretless Shade** — Use identity-based (managed identity) connections instead of secrets.

**Fila 4 (Módulo 21 · Portal of Connectors):**
10) **Nameless Action** — Action and trigger naming shapes the connector's operations.
11) **Hidden Op** — Action visibility (none/advanced/internal/important) controls exposure.
12) **Schema Gap** — Request and response definitions describe the operation payloads.

**Fila 5 (Módulo 22 · Workshop of the Connector):**
13) **Service Binder** — Add Power Platform as a connected service in Visual Studio.
14) **APIM Gate** — Export a Web API from Azure API Management to a custom connector.
15) **Connection Split** — A connector is the definition; a connection is an authenticated instance.

_Claves (celda por celda, izq→der / arriba→abajo):_ `e_p5m1_0`=Bus Phantom, `e_p5m1_1`=Grid Wisp, `e_p5m1_2`=Endpoint Cipher, `e_p5m2_0`=Cold Start, `e_p5m2_1`=Plan Drifter, `e_p5m2_2`=Timeout Wraith, `e_p5m3_0`=Trigger Ghost, `e_p5m3_1`=Binding Knot, `e_p5m3_2`=Secretless Shade, `e_p5m4_0`=Nameless Action, `e_p5m4_1`=Hidden Op, `e_p5m4_2`=Schema Gap, `e_p5m5_0`=Service Binder, `e_p5m5_1`=APIM Gate, `e_p5m5_2`=Connection Split


### Mundo 6 — Connectors & ALM Citadel (P6)  →  archivo `enemies_p6.png`

Hoja pixel-art, cuadrícula **3 columnas × 5 filas** (15 enemigos, cada FILA = un módulo). Fondo plano gris-azulado oscuro. Enemigos temáticos, tamaño mediano, distintos entre sí:


**Fila 1 (Módulo 23 · Citadel of Authentication):**
1) **Anonymous Shade** — No-auth connectors expose the API without credentials.
2) **Key Keeper** — API key and Basic authentication pass simple credentials.
3) **OAuth Sentinel** — OAuth 2.0 with Microsoft Entra ID delegates secure access.

**Fila 2 (Módulo 24 · Hall of Policies):**
4) **Policy Wraith** — Policy templates transform requests and responses without code.
5) **Expression Imp** — Policy expressions read runtime values (headers, query, body).
6) **Route Drifter** — Set host URL and route request policies redirect the call.

**Fila 3 (Módulo 25 · Archive of OpenAPI):**
7) **Static List** — The dynamic list of values extension populates choices at runtime.
8) **Rigid Schema** — Dynamic schema adapts the payload shape to the operation.
9) **Chunk Blocker** — Enable chunk transfer for large payloads.

**Fila 4 (Módulo 26 · Foundry of Solutions):**
10) **Hardcoded Ref** — Connection references and environment variables externalize configuration.
11) **Unmanaged Sprawl** — Unmanaged solutions are for dev; managed for downstream environments.
12) **Manual Deploy** — Power Platform Build Tools automate solution export/import in DevOps.

**Fila 5 (Módulo 27 · Citadel of ALM):**
13) **Layer Phantom** — Managed layers stack over the unmanaged base; the top layer wins.
14) **Upgrade Knot** — Managed updates patch; upgrades replace and remove deleted components.
15) **Binary Blob** — pac solution unpack turns a solution zip into source-control-friendly files.

_Claves (celda por celda, izq→der / arriba→abajo):_ `e_p6m1_0`=Anonymous Shade, `e_p6m1_1`=Key Keeper, `e_p6m1_2`=OAuth Sentinel, `e_p6m2_0`=Policy Wraith, `e_p6m2_1`=Expression Imp, `e_p6m2_2`=Route Drifter, `e_p6m3_0`=Static List, `e_p6m3_1`=Rigid Schema, `e_p6m3_2`=Chunk Blocker, `e_p6m4_0`=Hardcoded Ref, `e_p6m4_1`=Unmanaged Sprawl, `e_p6m4_2`=Manual Deploy, `e_p6m5_0`=Layer Phantom, `e_p6m5_1`=Upgrade Knot, `e_p6m5_2`=Binary Blob


---

## 2) HOJAS DE JEFES (una por mundo)


### Mundo 1 — Canvas Data Keep (P1)  →  archivo `bosses_p1.png`

Hoja pixel-art, **1 fila × 5 columnas** (5 jefes). Figuras GRANDES e imponentes, más detalladas que los enemigos, con aura/pose de jefe. Fondo plano oscuro:

1) **The Imperative Warden** (jefe del módulo 1 · Hall of Variables) — Imperative logic defines HOW step by step (Set, Navigate); declarative defines WHAT.
2) **Master of Patch** (jefe del módulo 2 · Forge of Patch) — Patch(DataSource, BaseRecord, ChangeRecord) writes precise changes to data.
3) **Choicemaster** (jefe del módulo 3 · Gallery of Choices) — Use global choices for reuse and lookups for real relationships between tables.
4) **Warden of Relations** (jefe del módulo 4 · Keep of Relations) — Dataverse relationships (1:N, N:N) model related data; Power Apps navigates them.
5) **Formula Sage** (jefe del módulo 5 · Sanctum of Expressions) — Power Fx is the declarative, Excel-like typed expression language of Power Apps.

_Claves:_ `b_p1m1`=The Imperative Warden, `b_p1m2`=Master of Patch, `b_p1m3`=Choicemaster, `b_p1m4`=Warden of Relations, `b_p1m5`=Formula Sage


### Mundo 2 — Performance Bastion (P2)  →  archivo `bosses_p2.png`

Hoja pixel-art, **1 fila × 5 columnas** (5 jefes). Figuras GRANDES e imponentes, más detalladas que los enemigos, con aura/pose de jefe. Fondo plano oscuro:

1) **Delegation Overlord** (jefe del módulo 6 · Bastion of Delegation) — Prefer delegable functions and data sources so the server filters and sorts data.
2) **The All-Seeing Monitor** (jefe del módulo 7 · Observatory of Monitor) — Use Monitor and Application Insights to find and fix performance and errors.
3) **Herald of the Platform** (jefe del módulo 8 · Nexus of the Platform) — Power Platform = Power Apps + Automate + BI + Pages + Copilot Studio over Dataverse.
4) **Toolsmith of ALM** (jefe del módulo 9 · Workshop of Tools) — Package with SolutionPackager/CLI; managed to deploy, unmanaged to develop.
5) **Extensibility Archmage** (jefe del módulo 10 · Forge of Extensibility) — Extend Dataverse via Web API, Organization Service, plug-ins and Custom APIs.

_Claves:_ `b_p2m1`=Delegation Overlord, `b_p2m2`=The All-Seeing Monitor, `b_p2m3`=Herald of the Platform, `b_p2m4`=Toolsmith of ALM, `b_p2m5`=Extensibility Archmage


### Mundo 3 — UI Extensibility Spire (P3)  →  archivo `bosses_p3.png`

Hoja pixel-art, **1 fila × 4 columnas** (4 jefes). Figuras GRANDES e imponentes, más detalladas que los enemigos, con aura/pose de jefe. Fondo plano oscuro:

1) **Scriptbinder** (jefe del módulo 11 · Atrium of Client Script) — Client scripting (JavaScript) reacts to form events using the Client API context.
2) **Xrm Overlord** (jefe del módulo 12 · Spire of the Xrm Object) — The Xrm object exposes App, Navigation, Utility and WebApi for global operations.
3) **Component Architect** (jefe del módulo 13 · Foundry of Components) — The Power Apps component framework builds reusable code components for apps.
4) **The Codewright** (jefe del módulo 14 · Laboratory of Code Components) — Implement updateView/getOutputs, package into a solution and deploy the component.

_Claves:_ `b_p3m1`=Scriptbinder, `b_p3m2`=Xrm Overlord, `b_p3m3`=Component Architect, `b_p3m4`=The Codewright


### Mundo 4 — Server-Side Crypt (P4)  →  archivo `bosses_p4.png`

Hoja pixel-art, **1 fila × 3 columnas** (3 jefes). Figuras GRANDES e imponentes, más detalladas que los enemigos, con aura/pose de jefe. Fondo plano oscuro:

1) **Keeper of Dataverse** (jefe del módulo 15 · Crypt of Dataverse) — Extend Dataverse with .NET plug-ins registered on pipeline events and messages.
2) **Plug-in Overlord** (jefe del módulo 16 · Vault of Plug-ins) — A plug-in implements IPlugin.Execute and reads IPluginExecutionContext.
3) **Warden of the Web API** (jefe del módulo 17 · Gateway of the Web API) — Call Dataverse over HTTP with OData and OAuth bearer tokens from Entra ID.

_Claves:_ `b_p4m1`=Keeper of Dataverse, `b_p4m2`=Plug-in Overlord, `b_p4m3`=Warden of the Web API


### Mundo 5 — Azure Nexus (P5)  →  archivo `bosses_p5.png`

Hoja pixel-art, **1 fila × 5 columnas** (5 jefes). Figuras GRANDES e imponentes, más detalladas que los enemigos, con aura/pose de jefe. Fondo plano oscuro:

1) **Integration Overlord** (jefe del módulo 18 · Nexus of Azure) — Publish Dataverse data to Azure via Service Bus, Functions and messaging.
2) **Reactor Core** (jefe del módulo 19 · Reactor of Functions) — Azure Functions run event-driven serverless code that scales on demand.
3) **Binding Artificer** (jefe del módulo 20 · Assembly of Bindings) — Define triggers and bindings to wire functions to Azure services.
4) **Portal Warden** (jefe del módulo 21 · Portal of Connectors) — Build a custom connector in a solution via the maker portal and an OpenAPI definition.
5) **Connector Smith** (jefe del módulo 22 · Workshop of the Connector) — Create custom connectors from VS, APIM or an OpenAPI (Swagger) definition.

_Claves:_ `b_p5m1`=Integration Overlord, `b_p5m2`=Reactor Core, `b_p5m3`=Binding Artificer, `b_p5m4`=Portal Warden, `b_p5m5`=Connector Smith


### Mundo 6 — Connectors & ALM Citadel (P6)  →  archivo `bosses_p6.png`

Hoja pixel-art, **1 fila × 5 columnas** (5 jefes). Figuras GRANDES e imponentes, más detalladas que los enemigos, con aura/pose de jefe. Fondo plano oscuro:

1) **Gatekeeper of Auth** (jefe del módulo 23 · Citadel of Authentication) — Secure connectors with OAuth 2.0/Entra ID over an API Management gateway.
2) **Policy Magistrate** (jefe del módulo 24 · Hall of Policies) — Connector policies reshape traffic: set values, convert data, route requests.
3) **Swagger Sage** (jefe del módulo 25 · Archive of OpenAPI) — OpenAPI extensions add dynamic values/schema, test connection and chunking.
4) **Solution Steward** (jefe del módulo 26 · Foundry of Solutions) — Manage solutions with connection references, env variables and ALM pipelines.
5) **Grand Architect of ALM** (jefe del módulo 27 · Citadel of ALM) — Architect solutions with layering, version control and CLI unpack for healthy ALM.

_Claves:_ `b_p6m1`=Gatekeeper of Auth, `b_p6m2`=Policy Magistrate, `b_p6m3`=Swagger Sage, `b_p6m4`=Solution Steward, `b_p6m5`=Grand Architect of ALM


---

## 3) GUARDIANES DE LA TORRE DEL EXAMEN + REY DEMONIO  →  `guardians.png`

Hoja pixel-art, **cuadrícula 4×2 (7 celdas usadas, 1 vacía)**. Fondo plano oscuro. 6 GUARDIANES colosales (uno por mundo) + el JEFE FINAL. Estilo épico:

1) **Sentinel of Canvas Data (P1)** — guardián que domina TODO el temario del Mundo 1 (Canvas Data Keep).
2) **Custodian of Performance (P2)** — guardián que domina TODO el temario del Mundo 2 (Performance Bastion).
3) **Warden of UI Extensibility (P3)** — guardián que domina TODO el temario del Mundo 3 (UI Extensibility Spire).
4) **Keeper of the Server Crypt (P4)** — guardián que domina TODO el temario del Mundo 4 (Server-Side Crypt).
5) **Overseer of the Azure Nexus (P5)** — guardián que domina TODO el temario del Mundo 5 (Azure Nexus).
6) **Arbiter of Connectors & ALM (P6)** — guardián que domina TODO el temario del Mundo 6 (Connectors & ALM Citadel).
7) **THE PL-400 EXAM** — rey demonio final con corona y birrete de graduación, aura de fuego ámbar, épico e intimidante.

_Claves:_ `guard_d1`..`guard_d6` (celdas 1-6) y `boss_final` (celda 7).


---

## 4) FONDOS DE ZONA (16:9 apaisado, pixel-art JRPG, sin texto)

- **`BattleBastion.png`** — Mundo 1 · Canvas Data Keep: fortaleza de datos con galerías/formularios flotantes, tonos azul.
- **`BattleCrypt.png`** — Mundo 2 · Performance Bastion: sala de máquinas/telemetría con gráficas y engranajes, verde-cian.
- **`BattleFactory.png`** — Mundo 3 · UI Extensibility Spire: taller de componentes/código, hologramas de controles, violeta.
- **`BattleThrone.png`** — Mundo 4/final · Server-Side Crypt + Torre del Examen: cripta-servidor con trono imponente, ámbar/rojo.
- **`TownPlaza.png`** — Ciudad: plaza tecnológica con fuente central y 6 portales de reino.
- **`TitleVista.png`** — Pantalla de título: paisaje épico Power Platform, heroico.

(Se guardan en `PL400Unity/Assets/Resources/Art/External/`.)


## 5) ICONO ANDROID  →  `AppIcon.png` (512×512)

Icono cuadrado, escudo/emblema heroico RPG con los colores de Power Platform (azul, verde, violeta, ámbar) y un toque de fantasía (espada + tomo mágico con «PL-400»). Pixel-art limpio, legible en pequeño, fondo sólido. Va en `PL400Unity/Assets/Art/Icon/AppIcon.png`.


---

## Resumen de claves de arte

- Enemigos: `e_p{mundo}m{modulo}_{0,1,2}` (81 en total)
- Jefes de módulo: `b_p{mundo}m{modulo}` (27)
- Guardianes: `guard_d1`..`guard_d6` · Jefe final: `boss_final`
- Fondos: `Art/External/*` · Icono: `Art/Icon/AppIcon.png`
- Mientras no haya PNG, el juego usa sprites dibujados por código (siempre jugable).
