# Progreso de transcripción (103 preguntas)

Estado: EN CURSO por lotes. Cada pregunta hecha = archivo `q/qNNN.json`.
Reanudar: mirar el último qNNN.json presente y seguir por el nº siguiente, mapeando capturas por
el header "Question X of 103". Cada pregunta ocupa 2-3 capturas consecutivas.

## Hechas
- [x] Q1  order   integrations-alm   (Azure function -> canvas via custom connector, 5 pasos)  src 101615/101629/101635
- [x] Q2  order   extend-ux          (PCF en portal, 4 pasos)                                    src 101659/101712/101722
- [x] Q3  match   design-dataverse   (exchange rates: Virtual Table / Custom Connector)          src 101742/101754

## Siguientes (pendientes)
- [x] Q4  multi   design-dataverse   (restrict duplicates: Alternate key + Plug-in + Real-time workflow)  src 101811
- [x] Q5  yesno   extend-platform    (plug-in code contactColl RetrieveMultiple/ColumnSet)  src 101834/101846
- [x] Q6  match   extend-platform    (pipeline stages: Pre-validation/Pre-operation/Post-operation)  src 101903/101918
- [ ] ... hasta Q103

## Pistas ya vistas (para cuando lleguemos)
- Q18 single  design-dataverse/apps  (evitar duplicados sin código -> Alternate keys)
- Q32 multi   integrations-alm/ALM   (solution checker roles -> Export Customizations + Solution Checker)
- Q41 order   extend-ux              (PCF CLI: mkdir / cd / pac pcf init / npm install)
- Q83 order   apps-powerfx           (header como component library, 6 pasos)
- Q103 match  automation             (SharePoint/Dataverse trigger: Trigger condition/Column filter/Row filter)

## Tipos y su UI de examen
- single (radio, 1 correcta) · multi (checkbox, "elige N") · order (arrastrar a secuencia)
- match (arrastrar opción -> fila/hueco; pool reutilizable) · hotspot/code (desplegables en código)
- yesno (Sí/No por afirmación) · casestudy (contexto compartido + varias preguntas, campo `cs`)

- [x] Q7  single  extend-ux          (PCF: call RetrieveMultipleRecords desde updateView)  src 101948
- [x] Q8  order   integrations-alm   (webhook a Azure function: Register New Web Hook -> ... -> step con filtering)  src 102019/102027
- [x] Q9  hotspot integrations-alm   (App Insights: Azure portal / Maker portal / Azure portal)  src 102044/102050
- [x] Q10 single  extend-platform    (validar email en create real-time -> pre-operation synchronous plug-in)  src 102107
- [x] Q11 code    extend-platform    (Organization Service optimistic concurrency: UpdateRequest/IfRowVersionMatches/FaultException)  src 102129/102141/102146
- [x] Q12 yesno   design-dataverse   (rollup + field security: Recalculate/read rollup value)  src 102204/102214/102221
- [x] Q13 hotspot extend-platform    (Custom API: Is Function false / Entity binding / None processing step)  src 102241/102248
- [x] Q14 order   design-dataverse   (dar acceso a tabla via security role, 5 pasos PPAC)  src 102308/102314
- [x] Q15 order   integrations-alm   (S2S auth app user + MSAL para Web API, 5 pasos)  src 102339/102347
- [x] Q16 multi   integrations-alm   (custom connector dynamic params: x-ms-dynamic-schema + x-ms-dynamic-properties)  src 102418/102424
- [x] Q17 CASE STUDY cs1 casefiles  (CompanyA retail D365 Sales; 3 secciones + 5 sub-preguntas: EntraID / WebHook+AzureFn / ConsoleC#+WebAPI+SharePoint / order Graph custom connector / Copilot Studio+Power Pages)  -> exam/cs/cs1.json DONE  src 102440..102644

## OJO casos de estudio
Q17 es el primer CASO DE ESTUDIO: cabecera "Case Study", panel de Secciones (Background/Current
Environment/Requirements) + lista Questions 1..5. En el contador avanza como "Question 17 of 103".
Se guardan en exam/cs/csN.json (no en q/). El contador global salta segun cuantas sub-preguntas
consuma; hay que ir viendo el header. Diseno de UI: LECTOR con pestanas de seccion + navegacion de
sub-preguntas (area 'casefiles').
- [x] Q18 single  design-dataverse   (evitar duplicados sin codigo -> Alternate keys)  src 102736
- [x] Q19 single  apps-powerfx       (componente low-code reusable en MDA/Teams/Power Pages -> Canvas app)  src 102747
- [x] Q20 single  integrations-alm   (cambios de form no salen -> Delete active unmanaged layer)  src 102804/102815
- [x] Q21 match   integrations-alm   (solution actions: new component=unmanaged / deploy to test=managed / edit=unmanaged)  src 102822
- [x] Q22 single  automation         (long-running sequential enrichment con crecimiento -> Durable Functions)  src 102837
- [x] Q23 match   automation         (thank-you email=Workflow / validar quote=Business Process flow / error input=Business rule)  src 102903/102912
- [x] Q24 single  integrations-alm   (feed nightly delta a ERP -> Track changes / change tracking)  src 102924
- [x] Q25 CASE STUDY cs2 casefiles  (Environmental Assessments MDA/PCF/client scripting; 5 secciones + 7 sub-preguntas: openErrorDialog / yesno JS exhibit No-No-Si / feature-usage / CountRows>0 / web resource dependency / code manifest AssessmentMapControl-standard-read-only / OnLoad)  -> exam/cs/cs2.json DONE  src 102952..103221
- [x] Q26 single  automation         (CRUD desde boton canvas via Power Automate -> Instant cloud flow)  src 103421
- [x] Q27 match   integrations-alm   (sync 3rd-party API=Webhook / mensajes a web Azure=Service Bus / real-time Dataverse=Plug-in)  src 103434/103441
- [x] Q28 single  automation         (RPA+organizar archivos+scrape a Excel+UI testing -> Desktop flow)  src 103459
- [x] Q29 multi   integrations-alm   (auth Web API SPA: client ID+secret / Dataverse user impersonation+admin consent / application user)  src 103537/103546
- [x] Q30 CASE STUDY cs3 casefiles  (Delivery companies / custom connectors; 4 secciones + 4 sub: Auth model+OpenAPI / API definition+import RESTful Logic Apps / Policy template / Postman)  -> exam/cs/cs3.json DONE  src 103601..103710
- [x] Q31 SOLUTION-EVAL cs4 casefiles  (event handler func no en library; 2 soluciones: JS web resource a Form library=Yes / editar command bar EnableRule=No)  -> exam/cs/cs4.json DONE  src 103733/103744
- [x] Q32 multi   integrations-alm   (solution checker missing roles -> Export Customizations + Solution Checker)  src 111944
- [x] Q33 single  extend-ux          (bloquear UI y avisar durante verificacion -> Xrm.Utility.showProgressIndicator)  src 112005
- [x] Q34 order   extend-ux          (build/test/deploy PCF: npm run build -> npm start -> pac solution init -> pac solution add-reference -> msbuild /t:restore)  src 112021/112028
- [x] Q35 multi   integrations-alm   (custom connector exchange rates -> auth type + OpenAPI definition + HTTP verb)  src 112046
- [x] Q36 yesno   extend-platform    (console app crea/actualiza contacto; No/No/Si/No - Mark Butler, key not present, no siempre ColumnSet(true))  src 112104
- [x] Q37 code    extend-platform    (plug-in Execute: GetService(typeof(ITracingService)) + context=(IPluginExecutionContext))  src 112122/112131
- [x] Q38 multi   integrations-alm   (Developer environment -> Microsoft Dataverse + Region)  src 112149
- [x] Q39 single  extend-ux          (Monitor form onload script error; flags DisableFormHandlers/DisableFormLibraries -> culpable travel_AccountJS)  src 112200..112228
- [x] Q40 yesno   extend-platform    (Web API crear savedquery; Si/No/Si - Prefer return=representation, no @odata.context, query option en URL)  src 112249
- [x] Q41 order   extend-ux          (empezar PCF: mkdir -> cd -> pac pcf init -template field -> npm install)  src 112348/112354
- [x] Q42 single  integrations-alm   (custom connector test error 500 -> revisar Path and Host fields en swagger)  src 112436
- [x] Q43 single  integrations-alm   (managed solution import falla, workflow en Loan table -> Published dependency)  src 112447
- [x] Q44 multi   automation         (dividir flow en parent/child -> crear en solucion + instant trigger child + Response action child)  src 112458
- [x] Q45 single  extend-platform    (plug-in sync timeout muchas ops -> Register pre and post entity images)  src 112510
- [x] Q46 single  extend-platform    (cientos GET/POST concurrentes error inmediato -> Number of concurrent requests)  src 112521
- [x] Q47 match   apps-powerfx       (SharePoint upload=Standard / Dataverse Notes=Premium / Entra guest user=Custom)  src 112532/112538
- [x] Q48 code    integrations-alm   (custom connector transform code: class Script : ScriptBase override ExecuteAsync)  src 112555
- [x] Q49 hotspot integrations-alm   (Dataverse->Service Bus: Register service endpoint / SAS / JSON message format / Register new step)  src 112613  [OJO opciones de dropdown inferidas]
- [x] Q50 yesno   extend-platform    (Custom API Action; No response prop obligatoria / aparece en $metadata=Si / user sin permiso=error Si)  src 112626
- [x] Q51 yesno   extend-platform    (plug-in PreOperation con pre/post images; sync=Si / cols en ambas images=Si / PreEntityImages['telephone1']=No es alias)  src 112638
- [x] Q52 single  integrations-alm   (Logic App gobernada por Managed Identity -> Application user)  src 112648
- [x] Q53 hotspot automation         (canvas llama cloud flow: trigger When Power Apps calls a flow V2 / Respond action / Collect function)  src 112701/112708
- [x] Q54 multi   integrations-alm   (app admin crea/busca/update users Entra -> Azure Functions + Custom connector + Microsoft Graph API)  src 112725/112730
- [x] Q55 multi   design-dataverse   (Is Eligible solo sales manager + error en Qualify sin code -> Column-level security + Real-time workflow)  src 112742
- [x] Q56 multi   extend-ux          (pasar executionContext desde Ribbon Workbench -> CRM parameter + PrimaryControl)  src 112754
- [x] Q57 match   integrations-alm   (Dataverse eventos Azure: sync=Azure Function / load balance externo=Service Bus / on-prem seguro=Azure Relay)  src 112806
- [x] Q58 single  design-dataverse   (validar tax number desde forms+portal -> business rule scope Entity)  src 112846
- [x] Q59 single  apps-powerfx       (varios ClearCollect en OnStart lento -> Concurrent)  src 112857
- [x] Q60 match   apps-powerfx       (config=Collection / offline=SaveData / Google Maps=Launch / notas CRM=Patch / email adjuntos=Power Automate)  src 112909/112914
- [x] Q61 single  extend-ux          (RibbonDiffXml mensaje en idioma del usuario -> CrmParameter = UserLcid)  src 112926
- [x] Q62 order   extend-platform    (retrieve contacts C#: IOrganizationService -> QueryExpression -> ConditionExpression -> FilterExpression -> EntityCollection)  src 112938/112943
- [x] Q63 multi   integrations-alm   (canvas solo Global admin gestiona users Entra -> Microsoft Graph API + Custom connector)  src 112955
- [x] Q64 single  automation         (Power Automate create lead falla -> revisar columna Lookup)  src 113007
- [x] Q65 hotspot integrations-alm   (external read/write=Custom API / sync Azure Function on update=Webhook / on-prem seguro=Azure Relay)  src 113018
- [x] Q66 yesno   automation         (ocultar datos confidenciales en logs: mark sensitive=No / masking rule=No / Secure Outputs=Si)  src 113030
- [x] Q67 yesno   integrations-alm   (DLP bloquea connector: correr app=No / flows suspendidos=Si / otra DLP Business permite=No)  src 113041
- [x] Q68 SOLUTION-EVAL cs5 casefiles  (Dataverse+website 3rd-party API, dev local+App Insights; Logic Apps=No / Power Automate=No / Azure Functions=Yes)  -> exam/cs/cs5.json DONE  src 113123..113207
- [x] Q69 code    automation         (flow ExportEmail: apis/shared_office365 / ['body/body'] / @parameters('$authentication'))  src 113223/113229
- [x] Q70 multi   design-dataverse   (support no actualiza cols transaccionales -> Column-level security + Role-based form)  src 113241
- [x] Q71 single  extend-ux          (PCF avisa cambio de dato al form -> notifyOutputChanged)  src 113253
- [x] Q72 hotspot integrations-alm   (JSON sync a Azure Function on create: Dataverse Plug-in / Webhook / IServiceEndpointNotificationService)  src 113307
- [x] Q73 single  integrations-alm   (SPA auth Dataverse Web API OAuth -> MSAL)  src 113325
- [x] Q74 single  extend-platform    (custom API Is Function=Yes -> GET)  src 113337
- [x] Q75 multi   integrations-alm   (429 errors: prevenir requests solapadas UI + agrupar requests en batches)  src 113353/113358
- [x] Q76 single  integrations-alm   (custom connector Teams/Graph -> OAuth 2.0)  src 113410
- [x] Q77 order   extend-platform    (Azure aware plugin: create function app+publish profile -> VS project import -> modify code+publish -> register webhook -> register step)  src 113425/113431
- [x] Q78 order   integrations-alm   (S2S OAuth Node: register app Entra -> add Dataverse permissions -> add key/secret -> create application user con App ID)  src 113445/113450
- [x] Q79 single  extend-platform    (privilegios workflow activity -> Create+Read+Write+Delete Activity)  src 113509
- [x] Q80 order   integrations-alm   (external app sin MFA a custom tables: App registration Entra -> API permissions -> application user -> custom security role)  src 113521/113527
- [x] Q81 single  automation         (generar PDF invoice + email desde command button, min custom, monitoreable -> Power Automate cloud flow triggered con JavaScript)  src 113543
