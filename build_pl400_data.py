# -*- coding: utf-8 -*-
"""Construye ab900data.json (nombre opaco heredado) para el RPG PL-400.

Estructura (igual que AB-410, pero con 6 mundos):
  Ciudad -> 6 hubs (P1..P6) -> 27 mazmorras (una por MD).
Cada mazmorra (MD) tiene: tomos (1-10, escalados por tamano del modulo, con el
TEXTO REAL del modulo + diagramas del docx), enemigos, cofres, carruaje y UN jefe.

Fuentes:
  - Conocimiento/p#/MD#/texto.txt  (texto del modulo -> tomos)   [genera split_pl400.py]
  - Conocimiento/p#/MD#/img/*.png  (diagramas -> ilustraciones de tomos)
  - questions/p#_md#.json          (preguntas por MD, en INGLES)

Todo el contenido de PL-400 va en INGLES (peticion del usuario).
NO se implementa aun la Torre del Gran Sabio (meta.studyTower=false, towerbq vacio).
Reejecutar tras tocar contenido o preguntas.
"""
import os, re, json, glob, shutil

# Stopwords ES+EN (para emparejar cada tomo con la pregunta mas afin a su lectura).
_STOP = set((
    "para con los las una que del este esta como más sobre cada según cuando donde entre "
    "the and for with that this from your you are can will has have into over each when where "
    "which what how why use used using uses does not but also only then than them they their "
    "a an of to in on is it as be by or at we do if so no").split())

def _kw(s):
    """Conjunto de palabras significativas (>=4 letras, sin stopwords)."""
    return set(w for w in re.findall(r"[a-záéíóúñü]{4,}", (s or "").lower()) if w not in _STOP)

HERE   = os.path.dirname(os.path.abspath(__file__))
CONO   = os.path.join(HERE, "Conocimiento")
QDIR   = os.path.join(HERE, "questions")
UNITY  = os.path.join(HERE, "PL400Unity", "Assets", "Resources")
DATA   = os.path.join(UNITY, "Data", "ab900data.json")
ARTDIR = os.path.join(UNITY, "Art", "PL400")

# ---------------------------------------------------------------------------
# CONFIGURACION DEL EXAMEN (lo unico especifico de la certificacion a nivel global).
# El motor lee este bloque "meta" desde ab900data.json (Meta()/PartOfDom()/TowerFloors).
# ---------------------------------------------------------------------------
EXAM_ID         = "PL-400"
EXAM_NAME       = "Power Platform Developer Saga PL-400"
EXAM_NAME_SHORT = "PL-400"
PART_NAMES = {
    1: "Canvas Data Keep (P1)",
    2: "Performance Bastion (P2)",
    3: "UI Extensibility Spire (P3)",
    4: "Server-Side Crypt (P4)",
    5: "Azure Nexus (P5)",
    6: "Connectors & ALM Citadel (P6)",
}

# ---------------------------------------------------------------------------
# 1) METADATOS DE LOS 27 MODULOS (parte, md, dom global 1..27, nombre de mazmorra,
#    subtitulo, 3 enemigos y 1 jefe con su "lore" de aprendizaje en INGLES).
# ---------------------------------------------------------------------------
MODULES = [
 # ----- P1: Canvas apps: data & Power Fx (5) -----
 dict(part=1, md=1, dom=1, name="Hall of Variables",
   sub="Imperative vs declarative - global/context variables - collections",
   enemies=[("Stray Global","Global variables (Set) hold a value app-wide for the session."),
            ("Context Wisp","Context variables (UpdateContext) are scoped to a single screen."),
            ("Collection Swarm","Collections store tables of records in memory via Collect/ClearCollect.")],
   boss=("The Imperative Warden","Imperative logic defines HOW step by step (Set, Navigate); declarative defines WHAT.")),
 dict(part=1, md=2, dom=2, name="Forge of Patch",
   sub="Patch to create/edit records - Remove/RemoveIf - editing beyond forms",
   enemies=[("Broken Form","Patch creates or edits records without needing a form control."),
            ("Duplicate Row","Patch with Defaults() creates a new record; passing a record edits it."),
            ("Ghost Record","Remove and RemoveIf delete records; Remove does not ask to confirm.")],
   boss=("Master of Patch","Patch(DataSource, BaseRecord, ChangeRecord) writes precise changes to data.")),
 dict(part=1, md=3, dom=3, name="Gallery of Choices",
   sub="Local vs global choices - choice vs lookup - filtering choices with Power Fx",
   enemies=[("Local Choice","Local choices belong to one column; global choices are reusable across tables."),
            ("Lookup Mirage","A choice stores a fixed option set; a lookup points to a row in another table."),
            ("Filter Phantom","Filter Dataverse choice columns in Power Fx using the option value/record.")],
   boss=("Choicemaster","Use global choices for reuse and lookups for real relationships between tables.")),
 dict(part=1, md=4, dom=4, name="Keep of Relations",
   sub="1:N and N:N relationships - related rows - relational data in Power Apps",
   enemies=[("Orphan Row","A one-to-many (1:N) relationship links a parent row to many child rows."),
            ("Tangled Web","Many-to-many (N:N) relationships connect rows on both sides freely."),
            ("Lost Lookup","A lookup column on the child materializes its relationship to the parent.")],
   boss=("Warden of Relations","Dataverse relationships (1:N, N:N) model related data; Power Apps navigates them.")),
 dict(part=1, md=5, dom=5, name="Sanctum of Expressions",
   sub="Power Fx functions: string, logical, math, date/time, collection",
   enemies=[("Syntax Imp","The formula bar offers auto-suggest, hints and links as you type Power Fx."),
            ("Type Mismatch","Conversion functions (Value, Text, DateValue) change a value's type."),
            ("Nested Tangle","Combine functions (If, With, ForAll) to write complex expressions.")],
   boss=("Formula Sage","Power Fx is the declarative, Excel-like typed expression language of Power Apps.")),

 # ----- P2: Canvas performance & dev fundamentals (5) -----
 dict(part=2, md=1, dom=6, name="Bastion of Delegation",
   sub="Delegation - warnings/limits - performance bottlenecks",
   enemies=[("Non-Delegable Blob","Delegation pushes processing to the source; non-delegable functions run locally up to the limit."),
            ("Refresh Glutton","Too many refreshes and lookups are the most common performance bottleneck."),
            ("Warning Sprite","A delegation warning means the query may not return all rows.")],
   boss=("Delegation Overlord","Prefer delegable functions and data sources so the server filters and sorts data.")),
 dict(part=2, md=2, dom=7, name="Observatory of Monitor",
   sub="App load time - Monitor - OnStart optimization - Application Insights",
   enemies=[("Slow Start","Optimize App.OnStart; move work to OnVisible or Concurrent to speed startup."),
            ("Throttle Wraith","Connector limits and throttling can slow or block data calls."),
            ("Blind Spot","Monitor traces every event; Application Insights logs telemetry for diagnostics.")],
   boss=("The All-Seeing Monitor","Use Monitor and Application Insights to find and fix performance and errors.")),
 dict(part=2, md=3, dom=8, name="Nexus of the Platform",
   sub="Power Apps/Automate/BI/Pages/Copilot - connectors - Dataverse - CDM - when to code",
   enemies=[("Silent Connector","Connectors link Power Platform to external services and data."),
            ("Schema Drifter","The Common Data Model gives Dataverse standard tables and semantics."),
            ("Code Temptation","Write code only when configuration cannot meet the requirement.")],
   boss=("Herald of the Platform","Power Platform = Power Apps + Automate + BI + Pages + Copilot Studio over Dataverse.")),
 dict(part=2, md=4, dom=9, name="Workshop of Tools",
   sub="Power Platform CLI - VS tools - ALM - solutions (managed/unmanaged, layering)",
   enemies=[("CLI Gremlin","The Power Platform CLI (pac) scaffolds PCF, manages solutions and auth."),
            ("Layer Specter","Solutions layer; managed layers sit above the unmanaged base."),
            ("Dependency Knot","Solution dependencies must exist in the target environment to import.")],
   boss=("Toolsmith of ALM","Package with SolutionPackager/CLI; managed to deploy, unmanaged to develop.")),
 dict(part=2, md=5, dom=10, name="Forge of Extensibility",
   sub="UX extensibility (PCF, client script) - Dataverse API/plug-ins - Custom API - configure vs code",
   enemies=[("Pipeline Echo","The event pipeline (pre-validation, pre/post-operation) is where plug-ins run."),
            ("Custom API Shard","A Custom API defines a reusable, bound or unbound Dataverse operation."),
            ("Config-or-Code Sphinx","Prefer business rules/flows over client script/plug-ins when they suffice.")],
   boss=("Extensibility Archmage","Extend Dataverse via Web API, Organization Service, plug-ins and Custom APIs.")),

 # ----- P3: UI extensibility: client scripting & PCF (4) -----
 dict(part=3, md=1, dom=11, name="Atrium of Client Script",
   sub="Web resources - event handlers - execution/form context - common tasks",
   enemies=[("Unregistered Handler","Register client scripts on form/field events via form properties or code."),
            ("Context Cipher","The execution context exposes the form context to access data and UI."),
            ("Hidden Section","getControl/setVisible show or hide form controls, tabs and sections.")],
   boss=("Scriptbinder","Client scripting (JavaScript) reacts to form events using the Client API context.")),
 dict(part=3, md=2, dom=12, name="Spire of the Xrm Object",
   sub="Xrm global objects (Navigation, Utility, WebApi) - best practices - standards",
   enemies=[("WebApi Wraith","Xrm.WebApi performs CRUD against Dataverse from client script."),
            ("Deprecated Revenant","Avoid unsupported/deprecated methods; run the Solution Checker."),
            ("Naming Collision","Define unique function names to avoid clashes across libraries.")],
   boss=("Xrm Overlord","The Xrm object exposes App, Navigation, Utility and WebApi for global operations.")),
 dict(part=3, md=3, dom=13, name="Foundry of Components",
   sub="PCF advantages - component types - lifecycle - manifest - tooling",
   enemies=[("Manifest Gap","The manifest (ControlManifest) declares a component's properties and resources."),
            ("Lifecycle Loop","PCF lifecycle: init, updateView, getOutputs, destroy."),
            ("Kit Mimic","Creator Kit and community components speed up building UIs.")],
   boss=("Component Architect","The Power Apps component framework builds reusable code components for apps.")),
 dict(part=3, md=4, dom=14, name="Laboratory of Code Components",
   sub="Create/build a PCF - manifest - styling - logic - package - test harness",
   enemies=[("Build Error","pac pcf init plus npm build compiles a code component project."),
            ("Style Glitch","CSS web resources style a code component's rendered output."),
            ("Harness Shade","Test and debug components in the Power Apps component test harness.")],
   boss=("The Codewright","Implement updateView/getOutputs, package into a solution and deploy the component.")),

 # ----- P4: Server extensibility: Dataverse, plug-ins & Web API (3) -----
 dict(part=4, md=1, dom=15, name="Crypt of Dataverse",
   sub="Extensibility model - plug-ins (.NET assemblies) - event framework/pipeline - sync vs async",
   enemies=[("Metadata Guardian","Dataverse is metadata-driven and solution-aware."),
            ("Event Echo","Event messages (Create, Update...) flow through the event pipeline stages."),
            ("Async Phantom","Steps run synchronously (immediate) or asynchronously (background).")],
   boss=("Keeper of Dataverse","Extend Dataverse with .NET plug-ins registered on pipeline events and messages.")),
 dict(part=4, md=2, dom=16, name="Vault of Plug-ins",
   sub="When to use plug-ins - execution context - pre/post images - shared variables",
   enemies=[("Input Specter","InputParameters/OutputParameters carry data in the plug-in context."),
            ("Image Wraith","Pre/PostEntityImages capture row values before and after the operation."),
            ("Alternative Imp","Prefer business rules or flows when a plug-in is not required.")],
   boss=("Plug-in Overlord","A plug-in implements IPlugin.Execute and reads IPluginExecutionContext.")),
 dict(part=4, md=3, dom=17, name="Gateway of the Web API",
   sub="Web API vs Organization Service - OData 4 - Entra ID auth (OAuth) - CRUD",
   enemies=[("OData Sentinel","The Dataverse Web API is an OData v4 RESTful endpoint."),
            ("Token Gatekeeper","Register an app in Microsoft Entra ID and use OAuth to get an access token."),
            ("FetchXML Shade","FetchXML expresses complex queries against Dataverse.")],
   boss=("Warden of the Web API","Call Dataverse over HTTP with OData and OAuth bearer tokens from Entra ID.")),

 # ----- P5: Azure integration & connectors (5) -----
 dict(part=5, md=1, dom=18, name="Nexus of Azure",
   sub="Azure integration options - Service Bus - registering endpoints/steps - listeners",
   enemies=[("Bus Phantom","Azure Service Bus queues and relays Dataverse messages to external listeners."),
            ("Grid Wisp","Event Grid and Event Hubs handle event routing at scale."),
            ("Endpoint Cipher","Register a Service Bus endpoint and a step to publish Dataverse events.")],
   boss=("Integration Overlord","Publish Dataverse data to Azure via Service Bus, Functions and messaging.")),
 dict(part=5, md=2, dom=19, name="Reactor of Functions",
   sub="Functions vs Logic Apps/WebJobs - hosting plans - scaling - timeout",
   enemies=[("Cold Start","The Consumption plan scales to zero; Premium avoids cold starts."),
            ("Plan Drifter","Choose Consumption, Flex, Premium or Dedicated hosting per need."),
            ("Timeout Wraith","Function apps have a time-out duration that varies by plan.")],
   boss=("Reactor Core","Azure Functions run event-driven serverless code that scales on demand.")),
 dict(part=5, md=3, dom=20, name="Assembly of Bindings",
   sub="Local dev/test - triggers and bindings - identity-based connections",
   enemies=[("Trigger Ghost","A trigger starts a function; bindings connect input/output data."),
            ("Binding Knot","Binding direction is in, out or inout."),
            ("Secretless Shade","Use identity-based (managed identity) connections instead of secrets.")],
   boss=("Binding Artificer","Define triggers and bindings to wire functions to Azure services.")),
 dict(part=5, md=4, dom=21, name="Portal of Connectors",
   sub="Maker portal - general info - actions/triggers - request/response - validation",
   enemies=[("Nameless Action","Action and trigger naming shapes the connector's operations."),
            ("Hidden Op","Action visibility (none/advanced/internal/important) controls exposure."),
            ("Schema Gap","Request and response definitions describe the operation payloads.")],
   boss=("Portal Warden","Build a custom connector in a solution via the maker portal and an OpenAPI definition.")),
 dict(part=5, md=5, dom=22, name="Workshop of the Connector",
   sub="Connected Services in VS - APIM - connector vs connection - OpenAPI import",
   enemies=[("Service Binder","Add Power Platform as a connected service in Visual Studio."),
            ("APIM Gate","Export a Web API from Azure API Management to a custom connector."),
            ("Connection Split","A connector is the definition; a connection is an authenticated instance.")],
   boss=("Connector Smith","Create custom connectors from VS, APIM or an OpenAPI (Swagger) definition.")),

 # ----- P6: Advanced connectors & ALM (5) -----
 dict(part=6, md=1, dom=23, name="Citadel of Authentication",
   sub="APIM gateway - sharing - auth options (Basic, API key, OAuth2, Windows) - Entra ID",
   enemies=[("Anonymous Shade","No-auth connectors expose the API without credentials."),
            ("Key Keeper","API key and Basic authentication pass simple credentials."),
            ("OAuth Sentinel","OAuth 2.0 with Microsoft Entra ID delegates secure access.")],
   boss=("Gatekeeper of Auth","Secure connectors with OAuth 2.0/Entra ID over an API Management gateway.")),
 dict(part=6, md=2, dom=24, name="Hall of Policies",
   sub="Apply policies - expressions/runtime values - data conversion - host URL/routing",
   enemies=[("Policy Wraith","Policy templates transform requests and responses without code."),
            ("Expression Imp","Policy expressions read runtime values (headers, query, body)."),
            ("Route Drifter","Set host URL and route request policies redirect the call.")],
   boss=("Policy Magistrate","Connector policies reshape traffic: set values, convert data, route requests.")),
 dict(part=6, md=3, dom=25, name="Archive of OpenAPI",
   sub="Swagger editor - chunk transfer - test connection - dynamic values/schema",
   enemies=[("Static List","The dynamic list of values extension populates choices at runtime."),
            ("Rigid Schema","Dynamic schema adapts the payload shape to the operation."),
            ("Chunk Blocker","Enable chunk transfer for large payloads.")],
   boss=("Swagger Sage","OpenAPI extensions add dynamic values/schema, test connection and chunking.")),
 dict(part=6, md=4, dom=26, name="Foundry of Solutions",
   sub="Connection references - environment variables - managed/unmanaged - deploy - DevOps",
   enemies=[("Hardcoded Ref","Connection references and environment variables externalize configuration."),
            ("Unmanaged Sprawl","Unmanaged solutions are for dev; managed for downstream environments."),
            ("Manual Deploy","Power Platform Build Tools automate solution export/import in DevOps.")],
   boss=("Solution Steward","Manage solutions with connection references, env variables and ALM pipelines.")),
 dict(part=6, md=5, dom=27, name="Citadel of ALM",
   sub="Solution layering - managed layers - updates/upgrades - source control - CLI unpack",
   enemies=[("Layer Phantom","Managed layers stack over the unmanaged base; the top layer wins."),
            ("Upgrade Knot","Managed updates patch; upgrades replace and remove deleted components."),
            ("Binary Blob","pac solution unpack turns a solution zip into source-control-friendly files.")],
   boss=("Grand Architect of ALM","Architect solutions with layering, version control and CLI unpack for healthy ALM.")),
]

# --- Contoso developer cases (uno por parte + gran caso final), en INGLES ---
def _q(q, o, a, why):
    return {"t": "single", "q": q, "o": o, "a": a, "why": why}

CONTOSO = [
 {"part": 1, "d": 1, "titulo": "Contoso builds a canvas app",
  "caso": "Contoso needs a canvas app where staff can create and edit orders, remember the selected customer per screen, and keep a working list of line items before saving. Formulas should stay simple and delegable-friendly.",
  "qs": [
    _q("How would you edit a record WITHOUT a form control?",
       ["Use the Patch function", "Use Navigate", "Use a Label", "Use Notify"], 0,
       "Patch creates or edits records directly against a data source or collection."),
    _q("How do you keep the selected customer only on the current screen?",
       ["A context variable via UpdateContext", "A global variable via Set", "A collection", "An environment variable"], 0,
       "Context variables are scoped to a single screen."),
    _q("How do you keep a temporary in-memory list of line items?",
       ["A collection (Collect/ClearCollect)", "A single global variable", "A Label", "A gallery only"], 0,
       "Collections store tables of records in memory."),
    _q("Which development style defines WHAT you want rather than HOW?",
       ["Declarative (Power Fx)", "Imperative step-by-step", "Assembly code", "FetchXML"], 0,
       "Power Fx is declarative: you describe the result, not each step."),
  ]},
 {"part": 2, "d": 6, "titulo": "Contoso tunes app performance",
  "caso": "A Contoso canvas app is slow to start and shows delegation warnings on a large table. They want fast startup and accurate results across thousands of rows.",
  "qs": [
    _q("What does a delegation warning indicate?",
       ["The query may not return all matching rows", "The app will crash", "The data source is offline", "The license expired"], 0,
       "Non-delegable queries only process up to the delegation limit locally."),
    _q("How do you speed up a slow App.OnStart?",
       ["Move work to OnVisible/Concurrent and load less", "Add more screens", "Disable Power Fx", "Increase the limit to 2000"], 0,
       "Reduce OnStart work; defer and parallelize data loads."),
    _q("Which tool traces events to diagnose performance live?",
       ["Monitor", "Solution Checker", "SolutionPackager", "FetchXML Builder"], 0,
       "Monitor records each event to troubleshoot performance and errors."),
    _q("What logs long-term telemetry for a Power Apps app?",
       ["Application Insights", "A collection", "A context variable", "The formula bar"], 0,
       "Application Insights captures telemetry for diagnostics over time."),
  ]},
 {"part": 3, "d": 11, "titulo": "Contoso extends a form",
  "caso": "Contoso wants a model-driven form to hide a section until a field is set, validate on save, and reuse a custom UI control across apps.",
  "qs": [
    _q("How do you react to a form field change?",
       ["Register a client script on the field OnChange event", "Edit the sitemap", "Write a plug-in on retrieve", "Add a Power BI tile"], 0,
       "Client scripts register on form/field events like OnChange and OnSave."),
    _q("What gives client script access to the form's data and UI?",
       ["The form context from the execution context", "The Xrm.Utility only", "A connection reference", "An environment variable"], 0,
       "The execution context exposes the form context."),
    _q("How do you build a reusable custom UI control?",
       ["A PCF code component", "A business rule", "A canvas collection", "A FetchXML view"], 0,
       "The Power Apps component framework builds reusable code components."),
    _q("How do you hide a section until a field is set?",
       ["setVisible(false) on the section via client script", "Delete the section", "Use a plug-in", "Use a cloud flow"], 0,
       "Client script toggles control/section visibility."),
  ]},
 {"part": 4, "d": 15, "titulo": "Contoso codes server-side logic",
  "caso": "Contoso must run validation the instant an order row is created, capture the row's prior values, and expose a reusable operation other apps can call.",
  "qs": [
    _q("What runs custom .NET logic on a Dataverse Create event?",
       ["A plug-in registered on the event pipeline", "A canvas app", "A Power BI report", "A sitemap"], 0,
       "Plug-ins are .NET assemblies registered on pipeline messages/stages."),
    _q("How do you capture a row's values before the change?",
       ["A PreEntityImage", "A PostEntityImage only", "A context variable", "A collection"], 0,
       "Pre/PostEntityImages capture row values before/after the operation."),
    _q("How do you validate immediately and block the save?",
       ["A synchronous plug-in that throws on invalid data", "An asynchronous background job", "A scheduled flow", "A Power BI alert"], 0,
       "Synchronous steps run immediately and can stop the operation."),
    _q("How do you expose a reusable Dataverse operation to callers?",
       ["Define a Custom API", "Export a solution", "Create a canvas app", "Write a business rule"], 0,
       "A Custom API defines a reusable bound/unbound operation."),
  ]},
 {"part": 5, "d": 18, "titulo": "Contoso integrates with Azure",
  "caso": "Contoso wants Dataverse events to reach an external system reliably, run serverless code on demand, and connect to a partner REST API from Power Platform.",
  "qs": [
    _q("How do you relay Dataverse events to an external listener?",
       ["Publish to Azure Service Bus via a registered step", "Email a CSV", "A canvas collection", "A Power BI dataset"], 0,
       "Register a Service Bus endpoint/step to publish Dataverse messages."),
    _q("What runs event-driven serverless code that scales on demand?",
       ["Azure Functions", "A model-driven app", "A business rule", "A sitemap"], 0,
       "Azure Functions run serverless, event-driven code."),
    _q("What starts a function and connects its data?",
       ["Triggers and bindings", "A form context", "A managed layer", "A choice column"], 0,
       "A trigger starts the function; bindings wire input/output data."),
    _q("How does Power Platform call a partner REST API?",
       ["Through a custom connector", "Through a plug-in image", "Through FetchXML", "Through a business rule"], 0,
       "Custom connectors expose external REST APIs to Power Platform."),
  ]},
 {"part": 6, "d": 23, "titulo": "Contoso secures and ships connectors",
  "caso": "Contoso secures a custom connector with delegated identity, transforms requests without code, and moves the solution from development to production reliably.",
  "qs": [
    _q("Which auth delegates secure access via Microsoft Entra ID?",
       ["OAuth 2.0", "No authentication", "API key", "Basic authentication"], 0,
       "OAuth 2.0 with Entra ID delegates secure, token-based access."),
    _q("How do you transform a connector request without code?",
       ["Apply a connector policy", "Write a plug-in", "Edit the sitemap", "Add a Power BI tile"], 0,
       "Policy templates reshape requests/responses declaratively."),
    _q("What externalizes connection details for deployment?",
       ["Connection references and environment variables", "Hardcoded secrets", "A local collection", "A context variable"], 0,
       "Connection references/environment variables externalize config across environments."),
    _q("How do you ship from dev to production reliably?",
       ["Export/import a managed solution (ideally via pipelines)", "Copy screenshots", "Recreate by hand", "Share the app by email"], 0,
       "Managed solutions transport components; Build Tools automate it."),
  ]},
 {"part": 0, "d": 0, "titulo": "THE GRAND CONTOSO CASE (final exam)",
  "caso": "Contoso ships an end-to-end developer solution: a canvas app to edit orders, a model-driven form extended with client script and a PCF control, server-side plug-ins for validation, a Dataverse Web API integration, Azure Service Bus + Functions, a secured custom connector, and a managed ALM pipeline to production. Design it combining the whole PL-400 syllabus.",
  "qs": [
    _q("Edit records in a canvas app without a form?",
       ["Patch", "Navigate", "Notify", "Collect only"], 0, "Patch writes precise changes to a data source."),
    _q("Model related orders and line items?",
       ["A one-to-many (1:N) relationship", "N:N only", "No relationship", "A choice column"], 0,
       "One parent with many children is a 1:N relationship."),
    _q("Run validation the instant a row is created?",
       ["A synchronous plug-in on the event pipeline", "A scheduled flow", "A Power BI alert", "A canvas timer"], 0,
       "Synchronous plug-ins run immediately and can block the save."),
    _q("Add a reusable custom control to the form?",
       ["A PCF code component", "A business rule", "A sitemap area", "A choice column"], 0,
       "PCF builds reusable code components."),
    _q("Reach an external system reliably from Dataverse events?",
       ["Azure Service Bus via a registered step", "An email attachment", "A canvas collection", "A Power BI dataset"], 0,
       "Service Bus relays Dataverse messages to listeners."),
    _q("Query Dataverse over HTTP from an external app?",
       ["The Dataverse Web API (OData v4) with OAuth", "A canvas gallery", "A business rule", "A sitemap"], 0,
       "The Web API is an OData v4 endpoint secured with Entra ID OAuth."),
    _q("Secure the custom connector with delegated identity?",
       ["OAuth 2.0 with Entra ID", "No authentication", "API key in the URL", "Basic auth over HTTP"], 0,
       "OAuth 2.0/Entra ID delegates secure access."),
    _q("Externalize connection details across environments?",
       ["Connection references and environment variables", "Hardcoded values", "A local variable", "Screenshots"], 0,
       "They decouple config from the solution for ALM."),
    _q("Ship the whole solution to production?",
       ["Export/import a managed solution via pipelines", "Copy files manually", "Email the app", "Rebuild in prod by hand"], 0,
       "Managed solutions plus Build Tools deliver reliable ALM."),
    _q("Speed up a slow-starting canvas app?",
       ["Optimize OnStart, use OnVisible/Concurrent and delegation", "Add more screens", "Disable Power Fx", "Raise the row limit"], 0,
       "Reduce startup work, parallelize loads and keep queries delegable."),
  ]},
]

CONTOSO_EXTRA = {}   # (sin niveles extra por ahora; cada caso queda en nivel unico "Easy")

ENEMY_SPR = ["mon_slime","mon_ogre","mon_ghost","mon_crab","mon_demon","mon_spider",
             "mon_eye","mon_goblin","mon_spectre","mon_rat","mon_bug"]
ENEMY_EMO = ["🦠","🪨","👻","🦀","😈","🕷️","👁️","👺","💀","🐀","🐛"]

# ---------------------------------------------------------------------------
# 2) TOMOS desde el texto real de cada modulo (INGLES).
# ---------------------------------------------------------------------------
CRUFT = re.compile(r"^(Completed\d*|\d+\s*(XP|minutes?|min)\b|Next|Previous|Continue|Save|Edit|"
                   r"Print|Feedback|Need help\??|Unit\b|Module\b|Start\b|Completado)", re.I)
SKIP_SECTION = re.compile(r"(check your knowledge|knowledge check|test your|module assessment|"
                          r"summary quiz|comprueba|prueba de conocimiento|evaluaci)", re.I)

def clean_line(t):
    t = t.strip()
    if not t: return ""
    if CRUFT.match(t): return ""
    if re.fullmatch(r"[\d\W]{0,4}", t): return ""
    return t

def chunk(text, lim=520):
    """Parte un texto largo en paginas <= lim respetando frases."""
    text = re.sub(r"\s+", " ", text).strip()
    if len(text) <= lim: return [text] if text else []
    out, cur = [], ""
    for sent in re.split(r"(?<=[.;:])\s+", text):
        if len(cur) + len(sent) + 1 <= lim:
            cur = (cur + " " + sent).strip()
        else:
            if cur: out.append(cur)
            cur = sent
            while len(cur) > lim:
                out.append(cur[:lim]); cur = cur[lim:]
    if cur: out.append(cur)
    return out

# Escalado de tomos por volumen (1..10). Se reparte de forma COMPACTA: el modulo mas
# pequeno tiene ~1 tomo y el mas grande ~8, con un min-max lineal sobre el nº de paginas
# de TODOS los modulos (dos pasadas). Asi hay pocos tomos pero sustanciosos (el usuario
# pidio 4 preguntas por tomo, luego menos tomos = preguntas de mas calidad).
TOME_LO, TOME_HI = 1, 8

def module_pages(part, md):
    """Lista de (titulo_seccion, texto_pagina) del modulo, o [] si no hay texto."""
    path = os.path.join(CONO, "p%d" % part, "MD%d" % md, "texto.txt")
    if not os.path.exists(path): return []
    lines = open(path, encoding="utf-8").read().splitlines()
    sections, cur_head, cur_body, skipping = [], None, [], False
    def flush():
        if cur_head and not skipping:
            body = " ".join(cur_body).strip()
            if body: sections.append((cur_head, body))
    for l in lines:
        if l.startswith("#"):
            flush()
            cur_head = l.lstrip("#").strip()
            cur_body = []
            skipping = bool(SKIP_SECTION.search(cur_head))
        else:
            c = clean_line(l)
            if c and not skipping: cur_body.append(c)
    flush()
    pages = []
    for head, body in sections:
        for i, pg in enumerate(chunk(body)):
            pages.append((head, (head + " — " + pg) if i == 0 else pg))
    return pages

def plan_tome_counts():
    """Numero de tomos por (part,md), min-max lineal [TOME_LO..TOME_HI] sobre paginas."""
    counts = {(m["part"], m["md"]): len(module_pages(m["part"], m["md"])) for m in MODULES}
    vals = [v for v in counts.values() if v > 0]
    pmin, pmax = (min(vals), max(vals)) if vals else (0, 0)
    T = {}
    for k, p in counts.items():
        if p <= 0:
            T[k] = 0
        elif pmax > pmin:
            T[k] = int(round(TOME_LO + (TOME_HI - TOME_LO) * (p - pmin) / (pmax - pmin)))
        else:
            T[k] = TOME_LO
        T[k] = max(1, min(T[k], p)) if p > 0 else 0
    return T

def build_module_tomes(part, md, dom, imgs, T):
    """Reparte las paginas del modulo en T tomos contiguos."""
    pages = module_pages(part, md)
    if not pages or T <= 0: return []
    T = min(T, len(pages))
    per = max(1, -(-len(pages) // T))   # techo
    tomes = []
    for ti in range(T):
        slice_pages = pages[ti * per:(ti + 1) * per]
        if not slice_pages: break
        title = slice_pages[0][0][:60]
        body_pages = [p[1] for p in slice_pages][:9]
        tid = "p%dm%d_%d" % (part, md, ti)
        tome_imgs = []
        if imgs:
            tome_imgs = [imgs[(ti * 2) % len(imgs)]]
            if len(imgs) > 1: tome_imgs.append(imgs[(ti * 2 + 1) % len(imgs)])
        tomes.append({"id": tid, "t": title, "pages": body_pages, "imgs": tome_imgs})
    return tomes

# ---------------------------------------------------------------------------
# 3) IMAGENES: copiar los PNG de cada MD a Resources/Art/PL400 y devolver refs.
# ---------------------------------------------------------------------------
def copy_images(part, md):
    src = os.path.join(CONO, "p%d" % part, "MD%d" % md, "img")
    refs = []
    if not os.path.isdir(src): return refs
    os.makedirs(ARTDIR, exist_ok=True)
    for f in sorted(glob.glob(os.path.join(src, "*.png"))) + sorted(glob.glob(os.path.join(src, "*.jpg"))):
        name = os.path.basename(f)
        # prefijar por modulo para evitar colisiones entre modulos (image1.png repetido)
        pref = "p%dm%d_%s" % (part, md, name)
        dst = os.path.join(ARTDIR, pref)
        if not os.path.exists(dst):
            try: shutil.copyfile(f, dst)
            except OSError: continue
        refs.append("Art/PL400/" + pref.rsplit(".", 1)[0])
    return refs

# ---------------------------------------------------------------------------
# 4) PREGUNTAS: cargar questions/p#_md#.json (lista). Tolera ausencia/parcial.
# ---------------------------------------------------------------------------
def _placeholder(dom, name, i):
    return {"d": dom, "t": "single", "q": "[PENDING] Q%d for %s." % (i + 1, name),
            "o": ["Content in preparation", "-", "--", "---"], "a": 0,
            "why": "This question is being authored (4 per tome)."}

def load_questions(part, md, dom, ntomes):
    """Carga questions/p#_md#.json y garantiza al menos 4*ntomes (4 por tomo)."""
    path = os.path.join(QDIR, "p%d_md%d.json" % (part, md))
    qs = []
    if os.path.exists(path):
        qs = json.load(open(path, encoding="utf-8"))
    for q in qs:
        q["d"] = dom
        q.setdefault("t", "single")
    need = max(4 * ntomes, 4)
    name = MODULES[dom - 1]["name"]
    while len(qs) < need:
        qs.append(_placeholder(dom, name, len(qs)))
    return qs

def load_tower_questions():
    # Torre del Gran Sabio DESACTIVADA por ahora (torre de 100 pisos: se disena despues).
    path = os.path.join(QDIR, "tower.json")
    if os.path.exists(path):
        qs = json.load(open(path, encoding="utf-8"))
        for q in qs:
            q.setdefault("t", "single"); q.setdefault("d", 0)
        return qs
    return []

# ---------------------------------------------------------------------------
# 5) ENSAMBLAR
# ---------------------------------------------------------------------------
NPART = 6

def estats(dom):
    return dict(hp=26 + dom, atk=5 + dom // 5, xp=12 + dom)

def bstats(dom):
    return dict(hp=90 + dom * 4, atk=9 + dom // 5, xp=50 + dom * 3)

def hexcol(part):
    return ["#5fa8ff", "#57c98a", "#b07cff", "#ffb454", "#ff6ec7", "#4fd6d6"][part - 1]

def build():
    zones, tomes, areas = [], {}, {}
    bq = []
    eidx = 0
    TPLAN = plan_tome_counts()   # nº de tomos por modulo (min-max 1..8)

    for m in MODULES:
        dom, part, md = m["dom"], m["part"], m["md"]
        enemies = []
        for ei, (n, lore) in enumerate(m["enemies"]):
            st = estats(dom)
            enemies.append({"n": n, "e": ENEMY_EMO[eidx % len(ENEMY_EMO)],
                            "hp": st["hp"], "atk": st["atk"], "xp": st["xp"],
                            "lore": lore, "spr": "e_p%dm%d_%d" % (part, md, ei)})
            eidx += 1
        bn, blore = m["boss"]
        bs = bstats(dom)
        boss = {"n": bn, "e": "🐉", "hp": bs["hp"], "atk": bs["atk"], "xp": bs["xp"],
                "lore": blore, "spr": "b_p%dm%d" % (part, md)}
        zones.append({"name": m["name"], "sub": m["sub"], "dom": dom,
                      "color": hexcol(part), "bg": "#0b1530", "enemies": enemies, "boss": boss})

        imgs = copy_images(part, md)
        T = TPLAN[(part, md)]
        mtomes = build_module_tomes(part, md, dom, imgs, T)
        qs = load_questions(part, md, dom, len(mtomes))
        bq.extend(qs)
        # 4 PREGUNTAS POR TOMO (quiz al cerrar el tomo, todo-o-nada). Las preguntas del
        # modulo van en orden de tomo: las 4 primeras -> tomo 0, las 4 siguientes -> tomo 1...
        for ti, t in enumerate(mtomes):
            grp = qs[ti * 4:(ti + 1) * 4]
            while len(grp) < 4:
                grp.append(_placeholder(dom, m["name"], ti * 4 + len(grp)))
            checks = [{k: q[k] for k in ("q", "o", "a", "why", "t", "k", "rows", "drops",
                                          "q_en", "o_en", "why_en") if k in q} for q in grp]
            tomes[t["id"]] = {"t": t["t"], "pages": t["pages"], "imgs": t["imgs"],
                              "checks": checks, "check": checks[0]}

        md_things = [{"x": 3, "y": 1, "kind": "tome", "id": t["id"]} for t in mtomes]
        areas["p%d_md%d" % (part, md)] = {
            "dom": dom, "zi": dom - 1, "part": part,
            "name": "%s (P%d·MD%d)" % (m["name"], part, md),
            "things": md_things, "start": [1, 1]}

    # --- zona final (indice 27) ---
    zones.append({"name": "Tower of the Exam", "sub": "Full PL-400 question bank", "dom": 0,
                  "color": "#ff6b6b", "bg": "#1a0b14",
                  "enemies": [{"n": "Echo of the Syllabus", "e": "👁️", "hp": 60, "atk": 12, "xp": 30,
                               "lore": "The exam covers the WHOLE PL-400 syllabus.", "spr": "mon_eye"}],
                  "boss": {"n": "THE PL-400 EXAM", "e": "👑", "hp": 300, "atk": 16, "xp": 300,
                           "lore": "The final exam mixes questions from all 6 parts. Prove your mastery!",
                           "spr": "boss_final"}})

    # --- hubs P1..P6 ---
    PART_MDS = {p: [m for m in MODULES if m["part"] == p] for p in range(1, NPART + 1)}
    door_slots = [(2, 2), (5, 2), (8, 2), (11, 2), (2, 4), (5, 4), (8, 4)]
    for p in range(1, NPART + 1):
        things = []
        for i, m in enumerate(PART_MDS[p]):
            x, y = door_slots[i]
            things.append({"x": x, "y": y, "kind": "portal", "to": "p%d_md%d" % (p, m["md"])})
        things.append({"x": 13, "y": 7, "kind": "portal", "to": "town"})
        areas["p%d" % p] = {"hub": True, "part": p, "name": PART_NAMES[p],
                            "things": things, "start": [7, 7]}

    # --- ciudad: 6 portales de reino + torre del examen + granja ---
    areas["town"] = {"name": "City of Power Platform", "start": [10, 10], "things": [
        {"x": 2,  "y": 4,  "kind": "portal", "to": "p1"},
        {"x": 2,  "y": 10, "kind": "portal", "to": "p2"},
        {"x": 6,  "y": 1,  "kind": "portal", "to": "p3"},
        {"x": 14, "y": 1,  "kind": "portal", "to": "p4"},
        {"x": 18, "y": 4,  "kind": "portal", "to": "p5"},
        {"x": 18, "y": 10, "kind": "portal", "to": "p6"},
        {"x": 6,  "y": 13, "kind": "portal", "to": "final"},
        {"x": 14, "y": 13, "kind": "portal", "to": "farm"},
        {"x": 8,  "y": 10, "kind": "npc", "who": "recepcion"},
        {"x": 13, "y": 9,  "kind": "dashboard"},
        {"x": 17, "y": 12, "kind": "inn"},
    ]}

    areas["farm"] = {"name": "Monster Farm", "farm": True, "start": [10, 12], "things": [
        {"x": 17, "y": 12, "kind": "portal", "to": "town"},
    ]}

    # --- torre final: 6 guardianes (uno por parte) + Rey Demonio ---
    GNAMES = {1: "Sentinel of Canvas Data (P1)", 2: "Custodian of Performance (P2)",
              3: "Warden of UI Extensibility (P3)", 4: "Keeper of the Server Crypt (P4)",
              5: "Overseer of the Azure Nexus (P5)", 6: "Arbiter of Connectors & ALM (P6)"}
    gx = [3, 6, 9, 12, 15, 18]
    guardians = []
    for p in range(1, NPART + 1):
        guardians.append({"x": gx[p-1], "y": 5, "kind": "guardian", "part": p,
                          "spr": "guard_d%d" % p, "n": GNAMES[p],
                          "hp": 130 + p * 3, "atk": 12 + p // 3, "xp": 70 + p * 5})
    areas["final"] = {"name": "Tower of the PL-400 Exam", "dom": 0, "start": [10, 13], "things":
        [{"x": 10, "y": 1, "kind": "npc", "who": "sabio"}] + guardians +
        [{"x": 10, "y": 11, "kind": "boss", "zi": len(MODULES)},
         {"x": 18, "y": 1, "kind": "exit"}]}

    # --- layouts ---
    hub_layout = ["###############"] + ["#.............#"] * 7 + ["###############"]
    farm_layout = ["#########################"] + ["#.......................#"] * 17 + ["#########################"]

    npcs = {
        "recepcion": {"e": "💁", "name": "City Guide", "color": "#9fd0ff", "lines": [
            "Welcome, future PL-400 champion!",
            "Move with the D-pad (or arrows/WASD) and press A (or Space) to interact.",
            "There are 6 kingdoms: P1 Canvas Data, P2 Performance, P3 UI Extensibility,",
            "P4 Server-Side, P5 Azure Nexus and P6 Connectors & ALM.",
            "Each kingdom has DOORS (one per module). Cross one to enter its dungeon.",
            "Read every TOME in a dungeon: the boss stays SEALED until you read them all.",
            "Finish all modules of a kingdom to unlock its SUPER TOME in the module select.",
            "A carriage in each dungeon takes you back to the city anytime."]},
        "sabio": {"e": "🧙", "name": "Sage of the Exam", "color": "#ffd86b", "lines": [
            "Once you complete all 6 parts, I will test you on the WHOLE syllabus.",
            "A single wrong answer resets the trial. Are you ready?"]},
    }

    LVL_NAMES = ["Easy", "Medium", "Hard", "Expert"]
    for c in CONTOSO:
        if c["part"] == 0:
            c["levels"] = [{"n": "Final exam", "qs": c["qs"]}]
            continue
        levels = [{"n": "Easy", "qs": c["qs"]}]
        for j, extra in enumerate(CONTOSO_EXTRA.get(c["part"], [])):
            levels.append({"n": LVL_NAMES[min(j + 1, len(LVL_NAMES) - 1)], "qs": extra})
        c["levels"] = levels

    meta = {
        "examId": EXAM_ID,
        "examName": EXAM_NAME,
        "examNameShort": EXAM_NAME_SHORT,
        "moduleCount": len(MODULES),
        "partCount": NPART,
        "studyTower": False,   # Torre del Gran Sabio (100 pisos) desactivada por ahora
        "parts": [{"part": p, "name": PART_NAMES[p],
                   "modules": [m["dom"] for m in PART_MDS[p]]} for p in range(1, NPART + 1)],
    }

    towerbq = load_tower_questions()

    data = {"meta": meta, "bq": bq, "towerbq": towerbq, "zones": zones, "tomes": tomes, "npcs": npcs,
            "areas": areas, "town": TOWN_LAYOUT, "dun": DUN_LAYOUT,
            "hub": hub_layout, "farmmap": farm_layout, "contoso": CONTOSO}

    os.makedirs(os.path.dirname(DATA), exist_ok=True)
    with open(DATA, "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False)

    pend = sum(1 for q in bq if q["q"].startswith("[PENDING]"))
    print("Escrito:", DATA)
    print("  zonas:", len(zones), "| tomos:", len(tomes), "| areas:", len(areas),
          "| preguntas:", len(bq), "(pendientes:", pend, ")", "| towerbq:", len(towerbq))
    by_part = {}
    for m in MODULES:
        nt = sum(1 for k in tomes if k.startswith("p%dm%d_" % (m["part"], m["md"])))
        nq = sum(1 for q in bq if q["d"] == m["dom"])
        by_part.setdefault(m["part"], []).append((m["md"], nt, nq))
    for p in sorted(by_part):
        print("  P%d:" % p, ", ".join("MD%d(%dt/%dq)" % (md, nt, nq) for md, nt, nq in by_part[p]))


TOWN_LAYOUT = [
 "#####################","#T,,,,,,,,+,,,,,,,,T#","#,,,,,,,,,+,,,,,,,,,#",
 "#,T,,,,,,,+,,,,,,,T,#","#,,,,,,,,,+,,,,,,,,,#","#,,,,,,,+++++,,,,,,,#",
 "#,,,,,,,+~~~+,,,,,,,#","#++++++++~~~++++++++#","#,,,,,,,+~~~+,,,,,,,#",
 "#,,,,,,,+++++,,,,,,,#","#,,,,,,,,,+,,,,,,,,,#","#,T,,,,,,,+,,,,,,,T,#",
 "#,,,,,,,,,+,,,,,,,,,#","#T,,,,,,,,+,,,,,,,,T#","#####################"]

DUN_LAYOUT = [
 "#####################","#...................#","#...#...#...#...#...#",
 "#...................#","#.###...........###.#","#...................#",
 "#...#...#...#...#...#","#...................#","#.###...........###.#",
 "#...................#","#...#...#...#...#...#","#...................#",
 "#...................#","#...................#","#####################"]

if __name__ == "__main__":
    build()
