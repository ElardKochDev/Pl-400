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
