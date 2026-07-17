# Auditoria de Fragilidades de Arquitetura

**Produto:** Immersive Framework (`com.immersive.framework`)  
**Alvo:** Unity 6.5 — framework de ciclo de vida para jogos de médio porte  
**Versão auditada:** `1.0.0-preview.15` (branch `master`)  
**Data:** 2026-07-17  
**Escopo:** Runtime + Editor (≈ 989 arquivos `.cs`)  
**Foco principal:** referências não tipadas, localização por nome de objetos e padrões correlatos que enfraquecem o contrato de lifecycle

---

## 1. Resumo executivo

O framework **já codifica intenção arquitetural correta** em vários lugares: comentários e contratos proíbem `GameObject.Find`, tags, hierarchy paths como identidade funcional e service locators. Há IDs tipados (`ActorId`, `ContentAnchorId`, `PlayerSlotId`, `FrameworkIdentityKey`, etc.) e authoring explícito via assets/referências.

Apesar disso, a auditoria encontrou **fragilidades reais** onde a prática diverge da intenção:

| Severidade | Qtd. | Tema dominante |
|---|---:|---|
| **Crítica** | 4 | Discovery runtime por `FindObjectsByType`, auto-discover, identidade por string com fallback frágil |
| **Alta** | 6 | Referências `GameObject`/`MonoBehaviour` sem tipo de contrato, action maps por nome, cena por nome |
| **Média** | 7 | Contaminação de igualdade/diagnóstico com `gameObject.name` / hierarchy path |
| **Baixa / higiene** | 5 | Defaults string, `Resources.Load`, GetComponent em prefab sem wrapper tipado |

**Conclusão:** o produto **não está cheio de anti-padrões clássicos de Unity tutorial** (`Find` por nome em massa, tags como API), mas **ainda tem “portas dos fundos”** que reintroduzem acoplamento frágil em caminhos de runtime, input e content identity. Em jogos de médio porte com cenas, prefabs e multiplayer local, essas portas são exatamente onde bugs de “funciona no editor / quebra no build” e regressões de rename aparecem.

---

## 2. Método

Buscas estáticas em `Runtime/` e `Editor/` por:

1. Resolução por nome / hierarquia / tag / `Find*`
2. Referências não tipadas (`GameObject`, `MonoBehaviour` + cast, `string` como identidade funcional)
3. Discovery implícita / fallback silencioso
4. Contaminação de contratos com dados de cena (nome de GO, path de hierarquia, scene name)

Classificação:

- **Crítica** — pode quebrar lifecycle/runtime de forma não determinística ou silenciosa
- **Alta** — quebra sob rename/refactor/duplicação de cenas; contrato frágil
- **Média** — principalmente diagnóstico/igualdade/authoring; risco indireto
- **Baixa** — higiene, defaults, utilitários aceitáveis se contidos

**Não é** auditoria de performance, segurança de rede ou cobertura de testes.

---

## 3. O que o framework já faz bem (contexto)

Importante para não “pichar” o design inteiro:

- `ICameraTargetSource` documenta: *não* usar `Camera.main`, nomes ou hierarchy paths como resolução.
- `LocalPlayerProvisioningAuthoringDiscovery` documenta: resolução por **tipo de componente** e cena carregada — não por nome/tag/path.
- Content Anchors documentam que `anchorId` é identidade; nomes e paths são “diagnostics only”.
- Muitos IDs passam por wrappers tipados (`*Id.From(...)`) em vez de `string` solta na API pública.
- Editor validators (`FrameworkAuthoringValidator`, validators de Player/Reset) compensam parte do risco em tempo de authoring.

As fragilidades abaixo são **lacunas e inconsistências**, não ausência total de disciplina.

---

## 4. Achados críticos

### C1 — Auto-discover runtime via `FindObjectsByType` (Input / Pause)

**Onde**

- `Runtime/InputMode/PauseInputModeUnityPlayerInputRuntimeBridge.cs`  
  - `autoDiscoverMissingReferences` (default **true**)  
  - resolve `PlayerInput` via `FindObjectsByType<PlayerActorDeclaration>`
- `Runtime/InputMode/PauseInputActionRuntimeBridgeTrigger.cs`  
  - `autoDiscoverBridge` (default **true**)  
  - `GetComponent` → `GetComponentInParent` → **`FindObjectsByType` e pega o primeiro** `[0]`

**Por que é frágil**

1. Ordem de descoberta **não é contrato** — depende de ordem de cenas/objetos.
2. Em multiplayer local ou cenas com múltiplos actors/bridges, o “primeiro” pode ser o errado.
3. Default `true` esconde authoring incompleto: o jogo “funciona” até a cena crescer.
4. Contradiz a narrativa do framework de wiring **explícito**.

**Impacto:** pause input, troca de action map e bridge de UI podem acoplar ao Player/bridge errado sem erro claro.

**Recomendação**

- Default de auto-discover = **false** em builds de produto (ou remover do runtime).
- Exigir referência serializada tipada; discovery só em Editor/QA com warning.
- Se discovery for inevitável: exigir exatamente 1 candidato; se 0 ou >1 → **fail explícito** (já parcialmente feito em provisioning, não aqui).

---

### C2 — Discovery de composição-root por varredura global de cenas

**Onde**

- `Runtime/PlayerParticipation/Runtime/LocalPlayerProvisioningAuthoringDiscovery.cs` — `FindObjectsByType<LocalPlayerProvisioningAuthoring>`
- `Runtime/ObjectEntry/ObjectEntryDeclarationSource.cs` — `CollectLoadedSceneDeclarations` usa `FindObjectsByType` (marcado como diagnostics; atenção se vazar para authority)
- Validators de Actors/UnityInput (`ActorValidator`, `PlayerActorValidator`, `UnityInputTargetValidator`) — `FindObjectsByType`
- `Runtime/Bootstrap/ImmersiveFrameworkBootstrap.cs` consome discovery de provisioning

**Por que é frágil**

- Não há registry tipado de composition roots injetado no host; o runtime **caça** authoring no grafo de cenas.
- Documentação diz “não usa nomes” — correto — mas ainda é **service-discovery implícito por tipo**, equivalente fraco a service locator de cena.
- Duplicatas entre additive scenes / DontDestroyOnLoad geram falhas difíceis de diagnosticar em consumer projects.

**Recomendação**

- Composition root **explícito** no `FrameworkRuntimeHost` / bootstrap asset (referência serializada ou registro no boot).
- Discovery global só para validação Editor e smoke QA.
- Manter fail-on-duplicate (já existe em provisioning) em **todos** os roots críticos.

---

### C3 — Identidade de conteúdo com fallback para nome de cena

**Onde**

- `Runtime/Authoring/RouteContentSceneEntry.cs` — propriedade `ContentId`:
  1. usa `contentId` se preenchido  
  2. senão **cai para `SceneName`**  
  3. senão `"route-scene"`
- `SceneName` por sua vez deriva do path (`GetFileNameWithoutExtension`) se o campo estiver vazio
- Tooltip admite: *“legacy fallback is diagnostics/planning-only”* — mas a propriedade pública `ContentId` **ainda expõe o fallback**

**Por que é frágil**

- Rename de arquivo de cena ou de `sceneName` muda identidade de conteúdo.
- Dois entries sem `contentId` explícito em cenas com o mesmo file name colidem.
- Mistura “identidade estável” com “label derivada do asset do Unity”.

**Recomendação**

- Em paths de **execução** (F6 composition): rejeitar se `!HasExplicitContentId`.
- Manter fallback apenas em API de diagnóstico renomeada (`DiagnosticContentId` / `LegacyContentId`).
- Validator Editor: erro bloqueante se entry de cena sem content id explícito.

*(Padrão similar em `ActivityContentSceneEntry` — auditar o mesmo contrato.)*

---

### C4 — Resolução de cena por **nome** como fallback de path

**Onde**

- `Runtime/SceneLifecycle/SceneScopedComponentQuery.MatchesScene` — path **ou** `scene.name` (case-insensitive)
- `Runtime/SceneLifecycle/SceneLifecycleRuntime` — comparação por nome
- `Runtime/GlobalUi/GlobalUiSceneRuntime` — `GetSceneByPath` e, se falhar, `GetSceneByName(application.GlobalUiSceneName)`
- Activity scene composition — chaves com `SceneName` / `ScenePath`

**Por que é frágil**

- Unity permite cenas com o mesmo nome em pastas diferentes; path é o discriminador real.
- Rename de scene asset sem atualizar authoring: path quebra, nome pode “passar” e carregar a cena errada se houver homônimo.
- `OrdinalIgnoreCase` no nome aumenta chance de match ambíguo.

**Recomendação**

- Path como **única** identidade funcional de cena; nome só diagnóstico.
- Se path inválido → erro explícito, sem fallback por nome em runtime de produto.
- Authoring: sempre persistir `scenePath` canônico (guid + path via AssetDatabase no Editor).

---

## 5. Achados de severidade alta

### A1 — Referências `GameObject` onde o contrato exige componentes tipados

**Onde (amostra representativa)**

| Superfície | Campo | Contrato esperado (implícito) |
|---|---|---|
| `ActorProfile` | `logicalActorHostPrefab` | Host lógico com composição de Actor |
| `LocalPlayerProvisioning*` / backend | `PlayerPrefab` | `PlayerInput` + `LocalPlayerHostAuthoring` |
| `UnityContentAnchorMaterializationBridge` | `prefab` | Prefab materializável do anchor |
| `PauseVisualSurfaceAuthoring` | `visualPrefab` | Superfície de pause |
| `UnityLoadingSurfaceAdapter` | `surfaceRoot`, `progressRoot` | roots de UI |
| `UnityFadeCurtainEffectAdapter` | `surfaceRoot` | root de fade |
| `UnityPauseResidentSurfaceAdapter` | `surfaceRoot` | root residente |
| `UnityGameObjectActiveResetParticipant` | `target` | GO a resetar |

**Por que é frágil**

- Qualquer prefab/GO pode ser arrastado; validação é **posterior** (`GetComponent` em runtime/editor rules).
- Erros aparecem tarde (boot, join, materialize), não no momento do assign (exceto validators que existam).
- Não há tipo de “prefab reference” ou interface de host no campo serializado.

**Recomendação**

- Preferir `[SerializeField] LocalPlayerHostAuthoring` / componente de contrato no root do prefab, ou wrapper `PrefabReference<T>` com validação no Editor.
- Manter `GameObject` só quando o contrato **é** literalmente “qualquer root” (ex.: active state reset) — e documentar.

---

### A2 — `MonoBehaviour` + cast para interface (referência semi-tipada)

**Onde**

- `Runtime/CameraAuthoring/CameraRigComposer.cs`  
  - `[SerializeField] private MonoBehaviour targetSource;`  
  - `targetSource as ICameraTargetSource`

**Por que é frágil**

- Unity não serializa interfaces nativamente; o padrão `MonoBehaviour` + cast é comum, mas:
  - permite assign de **qualquer** MonoBehaviour;
  - falha só em `TryValidateForApply` / resolve time;
  - quebra o “typed binding” prometido no product surface de câmera.

**Recomendação**

- Manter validação forte no Editor (`CameraRigComposerEditor`) com erro bloqueante.
- Considerar lista de tipos permitidos / `SerializeReference` com implementações conhecidas.
- Não aceitar kind ≠ ExplicitTransform sem provider **já validado**.

---

### A3 — Input System acoplado a **nomes string** de Action Map / Action

**Onde**

- `UnityPlayerInputGateAdapter` — `gameplayActionMapName = "Player"`
- `PauseInputModeUnityPlayerInputRuntimeBridge` — `gameplayActionMapName = "Player"`, `uiActionMapName = "UI"`
- `PauseInputActionRuntimeBridgeTrigger` — `actionMapName = "UI"`, `actionName = "Pause"`  
  - resolve com `FindActionMap` / `FindAction` por string

**Por que é frágil**

- Rename no `.inputactions` quebra runtime sem erro de compilação.
- Defaults mágicos (`"Player"`, `"UI"`, `"Pause"`) assumem convenção do consumer.
- Mistura política de InputMode do framework com naming do projeto de jogo.

**Recomendação**

- Authoring com referência a `InputActionReference` (asset tipado do Input System) para ações críticas.
- Maps: validar no boot contra o asset real; falhar se map/action ausente.
- Defaults só em templates de projeto, não hardcoded no core sem override explícito do `GameApplication`.

---

### A4 — Nome de objeto Cinemachine como parâmetro de materialização

**Onde**

- `CameraRigComposer` / `CameraRigRecipe` — `cinemachineCameraObjectName = "Cinemachine Camera"`
- `Editor/Camera/Cinemachine/CinemachineRigMaterializer.cs` — usa o nome ao **criar** GO

**Mitigação já presente**

- Resolução prefere referência explícita `CinemachineCamera` e depois `GetComponentInChildren` no rig — **não** busca global por nome. Bom.

**Risco residual**

- Nome vira contrato de authoring/rebuild (duplicatas locais, rebuilds, comparações humanas).
- Se no futuro alguém reutilizar o nome para lookup, reabre a porta.

**Recomendação**

- Tratar nome como label cosmético; identidade = referência serializada do componente.
- Após create, sempre gravar a referência tipada de volta no composer (já é o caminho certo se Apply/Rebuild preencher `cinemachineCamera`).

---

### A5 — Activity / Route identity baseada em string (e fallback para `ScriptableObject.name`)

**Onde**

- `ActivityAsset.ActivityName` — campo string; se vazio, usa **`name` do asset Unity**
- `ObjectEntryDeclaration.TryCreateAuthoredOwnerIdentity` — owner Activity via `activityOwner.ActivityName`
- Route owner via `PrimaryScenePath` (melhor) com fallback `RouteName` em alguns diagnostics/IDs

**Por que é frágil**

- `ScriptableObject.name` muda com rename no Project window.
- Dois assets com o mesmo `activityName` colidem semanticamente.
- Identity de domínio (`FrameworkIdentityKey.From(Activity, activityName)`) herda a fragilidade da string.

**Recomendação**

- Identity canônica estável separada do display name (ex.: `activityId` imutável + `ActivityName` só UI).
- Proibir fallback silencioso para `asset.name` em chaves de runtime (permitir só em label de Editor).
- Validator: uniqueness de identity no `GameApplication` / catálogo de activities.

---

### A6 — Host modules acoplados por `GetComponent` no mesmo GO

**Onde (amostra)**

- `PlayerParticipationRuntimeHostModule`, `PlayerGameplayRuntimeHostModule`, `PlayerActorPreparationRuntimeHostModule`, `LocalPlayerProvisioningRuntimeHostModule`, etc.  
  - `runtimeHost.GetComponent<OtherModule>()`

**Por que é frágil**

- Composition do host depende da **presença colocalizada** de módulos no mesmo GameObject.
- Não é lookup por nome, mas ainda é **wiring implícito por colocation**.
- Fácil quebrar ao extrair módulo para filho ou reorganizar prefab do host.

**Recomendação**

- Registro explícito no bootstrap / constructor injection interna do host.
- Ou `[RequireComponent(typeof(...))]` + validação de prefab canônico do RuntimeHost.
- Preferir o host conhecer módulos por campos serializados internos se a composição for fixa.

---

## 6. Achados de severidade média

### M1 — `gameObject.name` / `scene.name` entram em descritores e igualdade

**Onde**

- `ActorDescriptor`, `PlayerActorDescriptor`, `UnityInputTargetDescriptor`  
  - campos `SceneName`, `ObjectName`  
  - **participam de `Equals` / `GetHashCode`**

**Risco**

- Rename de GameObject na hierarquia muda igualdade do descriptor mesmo com o mesmo `ActorId` / `TargetId`.
- Se algum cache/set usar o descriptor inteiro (e não só o Id), comportamento muda com rename cosmético.

**Recomendação**

- Igualdade funcional = **só IDs tipados** (+ role se for parte do contrato).
- `ObjectName` / `SceneName` apenas em `ToDiagnosticString()`, fora de `Equals`.

---

### M2 — Hierarchy path derivado de `transform.name` em Content Anchors

**Onde**

- `RouteContentAnchor.ResourcePath` / `ActivityContentAnchor.ResourcePath`  
  - `ScenePath + "::" + parent.name + "/" + ...`
- `ContentAnchorDeclaration.Equals` inclui `ResourceName` e `ResourcePath`

**Documentação diz:** nomes/paths não são identidade funcional (`anchorId` é).  
**Código faz:** equality completa do declaration **inclui** path de hierarquia.

**Risco**

- Reparent / rename na hierarquia → declaration “diferente” para o mesmo `AnchorId`.
- Qualquer diff/set/dedup baseado em `Equals` do declaration inteiro fica instável.

**Recomendação**

- Equality de declaration = Owner + Scope + Kind + AnchorId (+ Requiredness se policy).
- Resource path só diagnóstico / materialization evidence, sem equality.

---

### M3 — Display name e IDs com fallback para `gameObject.name`

**Onde**

- `ObjectEntryDeclaration.ResolveDisplayName` → `gameObject.name` → `objectEntryId`
- `UnityResetParticipantBehaviour` / `UnityResetSubjectAdapter` — nomes de GO em diagnostics e fallbacks
- Vários `DisplayName` usam `name` do MonoBehaviour

**Risco**

- Baixo se **só** diagnóstico; médio se display name for usado em UI de jogo ou matching.

**Recomendação**

- Garantir em code review: fallback de nome nunca alimenta matching/lookup.
- Display names de produto devem ser campos authoring explícitos.

---

### M4 — IDs de authoring como `string` serializada (sem asset de identidade)

Padrão generalizado:

- `objectEntryId`, `anchorId`, `subjectId`, `participantId`, `actorProfileId`, `targetId`, `requestId`, `outputId`, `scopeId`, `tieBreakerId`, etc.

**O que está certo:** wrappers tipados em runtime (`*Id.From`).  
**O que é frágil:** a **fonte** é string solta no Inspector, sem catálogo central, sem rename refactor, sem referências cruzadas tipadas entre assets.

**Recomendação (médio prazo)**

- Para IDs compartilhados entre cenas/sistemas: ScriptableObject de identidade ou Shared String GUID.
- Validators de unicidade por domínio (Route scope, Activity scope, Session).
- Evitar default IDs de QA copiados para cenas de produção (`qa.input.target...`).

---

### M5 — `ResetSubjectReference` dual: adapter tipado **ou** string `subjectId`

**Onde:** `Runtime/Reset/ResetSubjectReference.cs`

**Risco:** path só-string permite referenciar subject sem link de cena; typos e subjects “fantasma”.

**Recomendação:** preferir adapter; string id só para subjects não-Unity / cross-scene com validação de catálogo.

---

### M6 — `Resources.Load` de settings

**Onde**

- `ImmersiveFrameworkBootstrap`, `FrameworkLogger`, Editor settings utility  
  - `Resources.Load<ImmersiveFrameworkSettingsAsset>(...)`

**Risco**

- Dependência de path/nome sob `Resources/` (convenção frágil de Unity).
- Aceitável para settings de package se o path for constante e versionado; documentar como contrato de deploy.

**Recomendação:** path canônico versionado + validação no boot; considerar `Preloaded Assets` / ScriptableObject singleton de package.

---

### M7 — Varreduras amplas em QA / diagnostics

**Onde:** `FrameworkQaCanvas` — múltiplos `FindObjectsByType` para inventário

**Risco:** aceitável em QA; **não** reutilizar esses helpers no hot path de lifecycle de produto.

**Recomendação:** manter isolado em `Diagnostics/`; code owners bloqueiam uso em Runtime de lifecycle.

---

## 7. Achados de severidade baixa / higiene

| ID | Descrição | Nota |
|---|---|---|
| B1 | Defaults string de reason/source em quase todos os triggers | OK para telemetria; padronizar prefixos de domínio |
| B2 | `GetComponent` no mesmo GO em adapters (`CanvasGroup`, `PlayerInput`) | Aceitável com `[RequireComponent]` |
| B3 | `object` só em `Equals(object)` e diagnostics | Não é untyped domain model |
| B4 | Singleton estático só em NoOp (`NoOpTransitionOrchestrator.Instance`) | OK se limitado a null-object |
| B5 | Prefab validation via `GetComponent` em join rules | Funciona; melhorar com tipo de prefab (ver A1) |

---

## 8. Mapa rápido: “localização por nome” vs “localização por tipo”

| Padrão | Presente? | Uso funcional? | Veredito |
|---|---|---|---|
| `GameObject.Find("...")` | **Não** encontrado em Runtime | — | Bom |
| Tags (`CompareTag` / FindWithTag) | **Não** como API de lifecycle | — | Bom |
| Hierarchy `transform.Find` | **Não** como lookup de produto | — | Bom |
| Nome de GO como **identidade** | Parcial (equality de descriptors, ResourcePath) | Sim, em equality/diagnostics misturados | **Corrigir** |
| Nome de **cena** como match | Sim | Fallback de path | **Corrigir** |
| `FindObjectsByType` | Sim | Runtime + Editor + QA | **Restringir** |
| String action map/action | Sim | Runtime input bridges | **Endurecer** |
| Referência `GameObject` genérica | Sim | Prefabs / surfaces | **Tipar onde houver contrato** |
| `MonoBehaviour` + interface cast | Sim (câmera) | Target source | **Validar/bloquear no Editor** |

---

## 9. Matriz de priorização sugerida

| Prioridade | Item | Esforço relativo | Benefício |
|---|---|---|---|
| P0 | C1 — desligar/endurecer auto-discover Input/Pause | Baixo | Alto (determinismo multiplayer local) |
| P0 | C3 — content id sem fallback de scene name na execução | Baixo–médio | Alto (identidade estável) |
| P0 | C4 — scene match só por path | Médio | Alto (additive scenes) |
| P1 | C2 — composition roots explícitos no host | Médio–alto | Alto (boot previsível) |
| P1 | A3 — InputActionReference / validação de maps | Médio | Alto (menos breakage de input) |
| P1 | M1/M2 — equality sem ObjectName/ResourcePath | Baixo | Médio (caches/sets estáveis) |
| P2 | A1/A5 — tipar prefabs e activity ids | Alto | Alto a médio prazo |
| P2 | A2/A6 — camera source e host modules | Médio | Médio |
| P3 | M4/M6/B* — catálogo de IDs, Resources, higiene | Contínuo | Higiene de produto |

---

## 10. Princípios arquiteturais recomendados (guardrails)

Para alinhar o framework ao discurso de “ciclo de vida explícito para jogos médios”:

1. **Wiring explícito no composition root**  
   Host/bootstrap conhece adapters e modules por referência tipada. Discovery de cena é validação, não runtime feliz.

2. **Identidade ≠ label ≠ path de hierarquia**  
   - Identity: wrapper tipado + valor estável authoring  
   - Label: display name  
   - Path/name de Unity: **somente** log/QA

3. **Nenhum fallback silencioso em path de produto**  
   Fallback de nome/cena/GO esconde misconfiguration. Preferir `Rejected` / `Blocked` com diagnostic.

4. **Referências de contrato tipadas**  
   Se o sistema exige `LocalPlayerHostAuthoring` / `ICameraTargetSource` / `CinemachineCamera`, o campo serializado deve expressar isso o máximo que Unity permitir.

5. **Input por asset reference, não por convenção de string**  
   Convenções `"Player"`/`"UI"` pertencem a templates de jogo, não ao core sem validação.

6. **QA pode varrer cenas; Runtime de lifecycle não**  
   Gate de code review / analyzer opcional para `FindObjectsByType` fora de `Diagnostics/` e `Editor/Validation/`.

---

## 11. Evidências (arquivos-chave)

```
Runtime/InputMode/PauseInputModeUnityPlayerInputRuntimeBridge.cs
Runtime/InputMode/PauseInputActionRuntimeBridgeTrigger.cs
Runtime/PlayerParticipation/Runtime/LocalPlayerProvisioningAuthoringDiscovery.cs
Runtime/Authoring/RouteContentSceneEntry.cs
Runtime/SceneLifecycle/SceneScopedComponentQuery.cs
Runtime/GlobalUi/GlobalUiSceneRuntime.cs
Runtime/CameraAuthoring/CameraRigComposer.cs
Runtime/Actors/ActorProfile.cs
Runtime/Actors/PlayerActorDescriptor.cs
Runtime/UnityInput/UnityInputTargetDescriptor.cs
Runtime/ContentAnchor/RouteContentAnchor.cs
Runtime/ContentAnchor/ActivityContentAnchor.cs
Runtime/ContentAnchor/ContentAnchorDeclaration.cs
Runtime/Authoring/ActivityAsset.cs
Runtime/ObjectEntry/ObjectEntryDeclaration.cs
Runtime/Reset/ResetSubjectReference.cs
Runtime/Bootstrap/ImmersiveFrameworkBootstrap.cs
Editor/Camera/Cinemachine/CinemachineRigMaterializer.cs
```

---

## 12. Conclusão

O Immersive Framework **já está à frente** de muitos frameworks Unity ad hoc: proíbe `GameObject.Find`/tags como API, introduz IDs tipados e empurra authoring explícito. As fragilidades relevantes **não** são “código amador de Find by name em todo lugar”, e sim:

1. **Discovery implícita ainda ligada no runtime** (especialmente Input/Pause e composition roots),  
2. **Fallbacks de identidade** (content id ← scene name; scene match ← name; activity ← asset name),  
3. **Referências Unity genéricas** onde existe contrato tipado,  
4. **Contaminação de equality** com nomes de hierarquia/GO.

Corrigir o grupo **P0** já eleva bastante a previsibilidade do ciclo de vida em projetos consumer de médio porte, sem reescrever o modelo de domínio.

---

## 13. Próximos passos sugeridos (opcional)

1. Issues/ADRs por item P0 com contrato “fail closed”.  
2. Analyzer ou teste de arquitetura: proibir novos `FindObjectsByType` fora de Diagnostics/Editor.  
3. Segunda auditoria focada em **PlayerParticipation** e **Camera request arbitration** (superfícies grandes e experimentais).  
4. Checklist de consumer project (FIRSTGAME): wiring explícito, zero auto-discover, content ids explícitos.

---

*Documento gerado por auditoria estática de código. Não substitui playmode regression nem validação em projetos consumer.*
