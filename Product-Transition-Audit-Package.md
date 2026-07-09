# Product Transition Audit - com.immersive.framework

Data: 2026-07-09

Escopo auditado: `Packages/com.immersive.framework` / package standalone em `C:\Projetos\ImmersivePackages\com.immersive.framework`.

Fora de escopo: implementação, correção de código, novos sistemas, execução de Unity, build, playmode, smoke ou batchmode.

## 1. Resumo executivo

O package já não é apenas "componentes soltos + validators + smokes" no eixo de `GameApplication -> Route -> Activity -> Scene Lifecycle`: existe runtime real em `FrameworkRuntimeHost`, `GameFlowRuntime`, `RouteLifecycleRuntime`, `ActivityFlowRuntime`, `RuntimeContentRuntime`, gates, loading, transition, pause e reset registry. Essa base técnica é a parte mais forte do package.

O problema de transição para produto está na experiência de criação e composição. O usuário ainda cria boa parte das features adicionando componentes manualmente em cena, preenchendo IDs/owners/references técnicos e depois validando em Inspector, Project Settings ou `FrameworkQaCanvas`. Há poucos assets de intenção reutilizável: `GameApplicationAsset`, `RouteAsset`, `ActivityAsset`, `RouteContentProfileAsset` e `ActivityContentProfileAsset`. Não há `Samples~`, `Templates~`, prefabs oficiais, recipes oficiais ou composers idempotentes no package.

O eixo Player/Actor/Slot é deliberadamente passivo: bons contratos e validação, mas sem runtime authority de player session, join, spawn, movement, input binding ou camera binding. Camera tem runtime prático via `FrameworkCameraDirector`, mas ainda é montada manualmente. Reset/Restart é a superfície de gameplay mais madura. Content/Anchors/RuntimeContent têm contratos e partes de runtime, mas a UX exposta ainda parece uma ponte técnica. Save/Preferences/Progression ainda é contrato/store explícito, não produto authorável.

Severidade geral: **Alta para UX/produto**, **Média para runtime authority**, **Baixa para contratos técnicos centrais**.

## 2. Matriz por sistema

| Sistema | Classificação principal | Como o usuário cria hoje | Onde o designer edita a intenção | Recipe/Profile/Template | Composer/Authoring component | Apply/Rebuild idempotente | Inspector | Contratos técnicos | Runtime authority | Validators/smokes como UX principal | Superfície de produto necessária |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Player / Actor / Slot | Contrato pronto, UX fraca; Authoring incompleto; Candidato a Recipe/Composer; Candidato a Runtime Context/Session | Manualmente com `PlayerSlotDeclaration`, `PlayerActorDeclaration`, `ActorReadinessBehaviour`, `PlayerEntryBehaviour`, `PlayerViewBehaviour`, `PlayerControlBehaviour`, `UnityPlayerInputGateAdapter` | Campos técnicos de componentes em cena; validação por `PlayerBindingAuthoringValidationWindow` | Não há recipe/template oficial; há somente declarações e evidência passiva | Existem authoring components passivos, não composer de Player | `RebuildEntry`, `RebuildView`, `RebuildControl` em componentes; não há Apply/Rebuild de player completo | Técnico demais: fala em evidence, fallback ids, states, readiness | `PlayerSlotId`, `ActorId`, `IActor`, `IPlayerEntry`, `IPlayerView`, `IPlayerControl`, topology/readiness validators | Não para player como produto; explicitamente não faz join/spawn/movement/input/camera binding | Sim, validação/readiness é o fluxo principal | `Player Recipe`, `Player Rig Composer`, `Player Runtime Context`, linguagem designer-first para slot, actor, view, control |
| Camera | Contrato pronto, UX fraca; Authoring incompleto; Produto parcialmente utilizável | Manualmente com `FrameworkCameraDirector`, `FrameworkRouteCameraBinding`, `FrameworkActivityCameraBinding`, optional `FrameworkCinemachineRigApplier` | Componentes de cena e bindings em Route/Activity content | Não há camera profile/template; guide descreve montagem | Existem bindings, mas não composer de rig/camera route/activity | `Refresh()` no director; lifecycle callbacks aplicam; não há rebuild idempotente de rig | Melhor que Player, mas ainda técnico: director, rig applier, anchors, priorities | `IFrameworkCameraRigApplier`, descriptors, priorities, route/activity binding contracts | Sim, `FrameworkCameraDirector` decide rig efetivo e aplica prioridade/active state | Não é validator-only; guia e montagem manual são fluxo principal | `Camera Setup Recipe`, `Route Camera Composer`, `Activity Camera Composer`, prefab/sample de director + adapters |
| Route / Activity | Produto utilizável; Candidato a Runtime Context/Session | Assets por `CreateAssetMenu` e botões nos custom editors/settings: `GameApplicationAsset`, `RouteAsset`, `ActivityAsset`, content profiles | `GameApplicationAssetEditor`, `RouteAssetEditor`, `ActivityAssetEditor`, content profile editors, Project Settings | Sim para profiles de conteúdo; não há template completo de aplicação/rota/atividade | Parcial: assets são authoring roots; bindings de cena complementam | Parcial: criação de assets e scene path management; sem composer idempotente de cenas/bindings | Relativamente designer-first nos assets principais, técnico em profiles e diagnostics | Scene lifecycle, route/activity states, operation planner/executor, events, route/activity runtime scope | Sim, `GameFlowRuntime`, `RouteLifecycleRuntime`, `ActivityFlowRuntime` | Model readiness valida, mas não substitui completamente a criação | `Game Application Recipe`, `Route Composer`, `Activity Composer`, template de startup route/activity + scenes |
| Reset / Objects | Produto utilizável para Reset; ObjectEntry passivo; Authoring incompleto | Reset via `UnityResetSubjectAdapter`, participants, `ObjectResetTrigger`, `ObjectResetGroupTrigger`, `ActivityRestartTrigger`; ObjectEntry via `ObjectEntryDeclaration` | Componentes de cena; custom editors para triggers/declarations; reset selection inline | Não há reset recipe/template; docs descrevem shape canônico | Existem authoring components reais; não há composer de objeto resetável | Registro runtime on enable/retry; reset request idempotente por in-flight; sem Apply/Rebuild de prefab oficial | Reset é aceitável, mas `ResetSelectionConfig`, scopes, subject refs ainda são técnicos | `ResetRegistry`, `ResetExecutor`, `ResetSubject`, `IResetParticipant`, `IUnityResettable`, `ResetSelectionConfig` | Sim, reset/restart executam em Play Mode contra `FrameworkRuntimeHost` | QA é forte, mas reset tem uso real; ObjectEntry ainda é validation/passive | `Resettable Object Recipe`, `Activity Restart Recipe`, `Runtime Object Recipe`, composer para subject + participants + trigger |
| Content / Anchors | Runtime incompleto; Authoring incompleto; Candidato a Recipe/Composer; Candidato a Runtime Context/Session | `RouteContentAnchor`, `ActivityContentAnchor`, `RouteContentBinding`, `ActivityLocalVisibilityAdapter`, `UnityContentAnchorMaterializationBridge`/Set | Componentes com IDs, owner, scope, runtime content id, resource key, release policy | Content profiles existem para scenes; não há content/anchor recipe/template | Existem authoring components e bridges, mas bridges são técnicos | Explicit bridge tem context menus Materialize/Release; não há composer idempotente oficial | Técnico demais: runtime owner id, anchor owner id, resource key, scope root flags | `ContentAnchorSet`, discovery runtime, logical binding, `RuntimeContentRuntime`, materialization/release adapters | Parcial: logical runtime/binding existe; physical placement/materialization ainda aparece como bridge/adapter | Sim para vários casos; smokes e diagnostics validam caminho lógico | `Content Placement Recipe`, `Anchor Composer`, `Runtime Content Composer`, superfície de prefab-to-anchor designer-first |
| Pause / Loading / Transition | Contrato pronto, UX fraca; Runtime real; Candidato a Recipe/Composer | `GameApplicationAsset` aponta UIGlobal scene; cena contém `UnityFadeCurtainEffectAdapter`, `UnityLoadingSurfaceAdapter`, `UnityPauseResidentSurfaceAdapter`, triggers de Pause/Input | GameApplication global UI fields + componentes na UIGlobal scene | Não há Global UI Surface recipe/template oficial | Surface adapters existem; não há composer que cria UIGlobal | Runtime aplica snapshots/requests; adapters têm context menus QA; sem rebuild de surface | Misto: conceitos bons, mas setup manual e adapter-heavy | `PauseRuntime`, `PauseSurfaceRuntime`, `LoadingSurfaceRuntime`, `ITransitionOrchestrator`, gates, adapters | Sim, host aplica pause, timescale, gates, loading surface e transition effects | Parte da validação/QA é principal para provar wiring; uso real existe | `Global UI Surface Recipe`, `Pause/Loading/Transition Surface Composer`, sample/prefab UIGlobal |
| Save / Preferences / Progression | Contrato pronto, UX fraca; Runtime incompleto; Candidato a Runtime Context/Session | Por código: `PlayerPrefsPreferencesStore`, `ProgressionSaveRuntime` com store injetado, `ISnapshotParticipant` implementado por consumidor | Não há asset/editor de save slots, schema, autosave, progression profile | Não há save/profile/template authorável | Não há composer/authoring component | Não há apply/rebuild | Não há Inspector de produto | Preferences store, progression store/runtime, snapshot participant contracts | Parcial: runtime explícito de progression save existe, mas não observa lifecycle, snapshot discovery, autosave ou UI | Sim, smokes validam contratos; produto não existe | `Save Profile`, `Progression Slot Recipe`, `Snapshot/Save Session Context`, editor de schema/participants |
| Validators / QA tools | Validator-only; DiagnosticsOnly; tecnicamente forte | `Project Settings > Immersive Framework`, custom inspectors, `PlayerBindingAuthoringValidationWindow`, `FrameworkQaCanvas` | Em janelas/botões de validação e IMGUI QA | Não são recipes | Não são composers | Não aplicam configuração; reportam issues | Técnico, voltado a QA/dev | `FrameworkAuthoringValidator`, `FrameworkAuthoringModelReadinessValidator`, muitos `QaSmokeRunner` | QA exercita runtime, mas não é autoridade de produto | Sim, em várias áreas é a UX mais concreta | Rebaixar para suporte: readiness/diagnostics depois de recipes/composers |
| Editor tooling | Contrato pronto, UX fraca | Project Settings cria settings, GameApplication e LoggingConfig; inspectors criam Route/Activity/profiles associados | Project Settings e custom inspectors | Profiles existem; templates ausentes | Não há composer multi-objeto/cena | Criação de assets parcial, sem rebuild de cena/prefab | Melhor nos assets principais; técnico em bridges/anchors/reset | Settings, boot validator, authoring validator, inspectors | Editor-only, não runtime | Sim, Model Readiness check ganha destaque | Wizards/composers idempotentes por fluxo de produto |
| Docs / Samples / Templates | Docs fortes tecnicamente; Samples/Templates ausentes | Guia HTML via Project Settings; docs em `Documentation~`; sem samples/templates | `Documentation~/Guides/Usage/index.html` e ADRs | Não há `Samples~` ou `Templates~` | Não há composer documentado como produto | Não aplicável | Docs misturam usuário e arquitetura; guide atual diz preview.12 enquanto package é preview.14 | ADRs extensos e úteis | Não aplicável | Docs/QA compensam ausência de produto authorável | Samples oficiais, templates mínimos e guia atualizado para preview atual |

## 3. Áreas mais problemáticas

### Player / Actor / Slot

Severidade: **Alta**.

Evidência:

- `PlayerSlotDeclaration`, `PlayerActorDeclaration`, `PlayerEntryBehaviour`, `PlayerViewBehaviour` e `PlayerControlBehaviour` declaram explicitamente que não fazem join, spawn, movement, input binding, camera activation ou control binding.
- `Documentation~/Current/00-Current-State.md` registra `viewBinding = false`, `controlBinding = false`, `cameraActivation = false`, `inputActivation = false`, `movement = false`, `actorSpawning = false`.
- `PlayerBindingAuthoringValidationWindow` é uma janela de validação, não um fluxo de criação.

Diagnóstico: é uma fundação técnica correta, mas ainda não é produto authorável. O usuário monta evidência passiva e valida topologia. O próximo produto precisa decidir o conceito público de player antes de virar runtime.

### Content / Anchors / RuntimeContent

Severidade: **Alta**.

Evidência:

- `UnityContentAnchorMaterializationBridgeEditor` expõe `Runtime Scope`, `Runtime Owner Id`, `Anchor Owner Id`, `Runtime Content Id`, `Resource Key`, `Release Policy` e `Create Scope Root If Missing`.
- ADRs F8R/F9R registram que logical runtime/content anchor existe, mas physical placement/materialization foi historicamente restrito ou reavaliado.
- Existem context menus para `Materialize Prefab At Anchor`, `Release Bridge Scope`, `Materialize All Bridges`, mas não recipe/composer.

Diagnóstico: a feature tem linguagem técnica demais para designer e ainda parece uma ponte de validação/materialização explícita. Precisa virar uma superfície de intenção: "colocar este conteúdo neste ponto desta Route/Activity".

### Save / Preferences / Progression

Severidade: **Alta**.

Evidência:

- `ProgressionSaveRuntime` exige `IProgressionSaveStore` injetado e declara que não descobre participants, não captura snapshots, não agenda autosave, não observa Route/Activity lifecycle e não possui UI.
- `ISnapshotParticipant` é contrato local de capture/restore e não persiste nem descobre participantes.
- Não há Editor/Authoring específico para save/progression.

Diagnóstico: contratos estão bem delimitados, mas a feature ainda não é produto. Não existe "Save System" authorável no framework.

### Validators / QA como UX

Severidade: **Alta**.

Evidência:

- `FrameworkQaCanvas` tem uma grande superfície IMGUI com botões para smokes, route requests, activity requests, reset diagnostics, content anchor diagnostics, pause/input mode smokes.
- `Project Settings` destaca `Run Model Readiness Check`, cujo texto diz que reporta issues e não cria assets, modifica settings ou aplica fallback.
- Há 44 arquivos em `Runtime/Diagnostics` e múltiplos `QaSmokeRunner` em áreas de runtime.

Diagnóstico: QA está forte, mas em várias áreas o caminho mais concreto para o usuário entender a feature é rodar validação/smoke. Isso confirma a transição pendente para recipes/composers.

## 4. Áreas já fortes tecnicamente

- **Boot e Game Flow:** `ImmersiveFrameworkBootstrap` valida settings, cria `FrameworkRuntimeHost` e inicia `GameFlowRuntime`.
- **Route / Activity runtime:** `RouteLifecycleRuntime` e `ActivityFlowRuntime` carregam cenas, criam runtime scopes, disparam lifecycle content, operam route/activity transitions e mantêm estado.
- **Global UI runtime:** `GlobalUiSceneRuntime` carrega/persiste UIGlobal e coleta adapters de transition/loading/pause.
- **Pause / Loading / Transition:** há gates, snapshots, result objects, transition orchestrator, loading progress reporter, pause time scale e surface adapters.
- **Reset / Restart:** `ResetRegistry`, `ResetExecutor`, `UnityResetSubjectAdapter`, `ObjectResetTrigger`, `ObjectResetGroupTrigger` e `ActivityRestartTrigger` formam um caminho real de Play Mode.
- **Identidade:** há ids tipados e validações explícitas, com rejeição de fallback silencioso em várias áreas.
- **Editor validation:** `FrameworkAuthoringValidator` cobre GameApplication, Route, Activity, content profiles, anchors, materialization bridges e reset/restart triggers.

## 5. Lacunas de UX/produto

1. Falta um fluxo de criação completo para "novo jogo com framework": Project Settings cria `GameApplication`, mas Route, Activity, content scenes, UIGlobal, surfaces e reset/player/camera setups ainda são compostos em etapas dispersas.
2. Falta linguagem de Inspector mais designer-first em Player, ContentAnchor, RuntimeContent, ResetSelection e bridge/materialization.
3. Falta separar "Basic" de "Advanced/Debug" nos componentes mais técnicos.
4. Falta um conjunto oficial de prefabs/samples/templates. O package não possui `Samples~` nem `Templates~`.
5. Falta uma noção de Recipe/Profile reutilizável para Player, Camera, Resettable Object, Runtime Object, Global UI Surface e Save.
6. Falta Composer/Apply/Rebuild idempotente que materialize bindings, adapters e containers técnicos sem o designer preencher cada detalhe.
7. O guia de uso em `Documentation~/Guides/Usage/README.md` declara conteúdo de `v1.0.0-preview.12`, enquanto `package.json` está em `1.0.0-preview.14`.

## 6. Lacunas de runtime authority

- **Player/Actor/Slot:** não há autoridade runtime de player session. O package ainda não possui owner para join/spawn/input/camera/control/movement.
- **Content/Anchors:** existe runtime lógico e algumas pontes Unity, mas a autoridade de produto para materializar conteúdo por intenção ainda não está clara.
- **Save/Progression:** não há save session, autosave, snapshot aggregation, lifecycle observation ou UI runtime.
- **Camera:** há autoridade local no `FrameworkCameraDirector`, mas não há contexto oficial que resolva camera por player/activity/route como produto.
- **Editor tooling:** valida e cria assets isolados, mas não é autoridade de composição.
- **Docs/Templates:** não há sample executável oficial que prove a experiência de uso como produto.

## 7. Candidatos a Recipe/Composer

Prioridade conceitual, sem propor cortes de implementação:

1. `GameApplication Recipe` + `Application Composer`: settings, startup route, startup activity, UIGlobal scene e logging.
2. `Global UI Surface Recipe` + `Surface Composer`: transition curtain, loading screen, pause surface e required adapter policy.
3. `Route Recipe` + `Route Composer`: primary scene, route content profile, anchors, route camera binding e startup activity.
4. `Activity Recipe` + `Activity Composer`: activity content profile, activity-local visibility, activity camera, reset/restart policy.
5. `Resettable Object Recipe` + `Object Composer`: object entry, reset subject, built-in participants, resettable component bridge e triggers.
6. `Player Rig Recipe` + `Player Composer`: slot, actor, entry, view, control, input gate evidence e future runtime binding.
7. `Content Placement Recipe` + `Anchor Composer`: prefab/content resource to route/activity anchor with logical owner, release policy and placement intent.
8. `Save Profile` + `Save Composer`: slots, snapshot participants, backend policy, manual save/load commands and future lifecycle hooks.

## 8. Candidatos a Runtime Context/Session

1. `Application Runtime Context`: já existe internamente em `FrameworkRuntimeHost`; falta superfície pública controlada de produto.
2. `Route Runtime Context`: já existe como runtime scope/root e state; bom candidato para APIs de módulos.
3. `Activity Runtime Context`: já existe como runtime scope/root, content execution e scene ledger; bom candidato para gameplay-facing boundaries.
4. `Player Session Context`: ainda ausente; necessário antes de transformar Player/Actor/Slot em produto.
5. `Content Placement Context`: necessário para conectar RuntimeContent, ContentAnchor, physical materialization e release sem bridge manual.
6. `Save Session Context`: necessário para progressão, snapshot aggregation, autosave e restore lifecycle.
7. `Diagnostics Context`: já há facts/logging/QA; precisa virar suporte transversal, não fluxo principal.

## 9. ADRs de produto recomendados

ADRs recomendados para decisão de produto, não implementação:

1. **ADR Product Surface 001 - Framework Authoring Model:** define o que é Recipe, Profile, Template, Composer, Apply/Rebuild e materialização técnica no `com.immersive.framework`.
2. **ADR Product Surface 002 - Designer-First Inspector Policy:** define Basic/Advanced/Debug, nomes públicos, campos proibidos na camada básica e linguagem de erro.
3. **ADR Product Surface 003 - Official Samples and Templates:** decide `Samples~`, `Templates~`, prefabs e scenes oficiais mínimos.
4. **ADR Runtime Context 001 - Public Runtime Contexts:** decide quais contexts são públicos, tipados, escopados e sem lookup global.
5. **ADR Player Product 001 - Player Runtime Ownership:** decide se Player vira produto, qual owner runtime existe e o que continua consumer-owned.
6. **ADR Content Product 001 - Content Placement Product Surface:** decide a camada de produto acima de RuntimeContent/ContentAnchor/materialization.
7. **ADR Save Product 001 - Save/Progression Authoring Surface:** decide save profiles, slots, snapshot aggregation, lifecycle hooks e backend policy.
8. **ADR QA Product Boundary 001 - Validators as Support:** define que validators/smokes são diagnóstico, não fluxo principal de criação.

## 10. Ordem sugerida de formalização

Ordem sugerida de formalização conceitual, sem cortes de implementação:

1. Formalizar a política de produto: Recipe/Profile/Template/Composer/Apply/Rebuild e Inspector designer-first.
2. Atualizar o guia canônico para refletir `1.0.0-preview.14` e separar guia de usuário de arquitetura interna.
3. Formalizar `GameApplication -> Route -> Activity -> UIGlobal` como primeira superfície de produto, porque já tem runtime real.
4. Formalizar `Global UI Surface` para Pause/Loading/Transition, porque hoje o setup depende de cena/adapters manuais.
5. Formalizar Reset/Restart como produto authorável, preservando o runtime forte já existente.
6. Formalizar Camera como produto authorável, acima dos bindings atuais.
7. Formalizar Content/Anchors/RuntimeContent como produto de placement/materialization, antes de ampliar uso por gameplay.
8. Formalizar Player/Actor/Slot como produto somente depois de decidir `Player Session Context` e ownership de join/spawn/input/camera/control.
9. Formalizar Save/Progression depois de decidir `Save Session Context`, snapshot aggregation e backend authoring.
10. Rebaixar QA/validators para suporte de confiança após cada superfície de produto existir.

## O que não mudar agora

- Não alterar `com.immersive.foundation`, `com.immersive.logging` ou `com.immersive.pooling`.
- Não copiar Base/NewScripts para o framework.
- Não criar lifecycle novo só para preencher lacuna de UX.
- Não transformar `FrameworkQaCanvas` em produto.
- Não expor bridge/materialization técnica como UX final.
- Não criar runtime de Player, Save ou Content Placement sem ADR de produto e ownership explícito.

## Validação realizada

- Auditoria estática por arquivos, diretórios, `package.json`, asmdefs, runtime/editor source e docs.
- Não foi executado Unity, build, playmode, smoke, batchmode ou testes.

## Validação manual recomendada

1. Abrir Unity e confirmar que `Project Settings > Immersive Framework` ainda aponta para o guia canônico.
2. Confirmar no Inspector real quais campos aparecem como Basic versus técnico nos principais componentes.
3. Confirmar se existe algum sample/template fora do package que não foi versionado aqui.
4. Confirmar com um consumidor real quais fluxos ainda dependem de `FrameworkQaCanvas` ou validação manual para serem compreendidos.
