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
