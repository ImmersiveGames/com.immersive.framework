# Auditoria: Trilhos Duplos e Duplicidades — Input e Camera

**Produto:** Immersive Framework (`com.immersive.framework`)  
**Alvo:** Unity 6 — framework de ciclo de vida para jogos de médio porte  
**Versão auditada:** `1.0.0-preview.15` (branch `master`)  
**Data:** 2026-07-17  
**Escopo:** Runtime de **Input** (`InputMode`, `UnityInput`, `Pause` input adapters) e **Camera** (`Camera`, `CameraAuthoring`, publicação via Player gameplay)  
**Foco principal:** trilhos paralelos, autoridades concorrentes, duplicidade de superfície e erros de arquitetura de ownership  

**Documentos relacionados:**

- `ROUTE-ACTIVITY-EXIT-ORDER-REENTRANCY-AUDIT.md` — ordem/reentrada de Route/Activity (interage com camera overrides no exit)
- `ARCHITECTURE-WEAKNESS-AUDIT.md` — discovery frágil / identidade (parcialmente overlap com bridges de Pause)
- `Documentation~/Guides/Camera-Architecture-Flow.md` — fluxo canônico de Camera
- `Documentation~/Guides/Player-Architecture-Flow.md` — Pause/InputMode (parcial)
- `Documentation~/Product/C9I-Route-Activity-Camera-Request-Bindings.md` — naming legado vs implementação

---

## 1. Resumo executivo

Camera e Input **já têm um núcleo de autoridade bem desenhado em cada domínio**:

| Domínio | Núcleo canônico (bom) |
|---------|------------------------|
| **Camera** | Um `CameraOutputSession` + `CameraOutputContext` (único seletor de winner) + publishers tipados |
| **InputMode** | Vocabulário `InputModeKind` + pipeline Preview → Plan → Apply em `PlayerInput` |
| **Pause lógico** | `PauseRuntime` + Gate blockers (sem ownership de input) |

O problema não é “falta de abstração”. É **múltiplos trilhos de mutação** que chegam ao mesmo efeito físico (action map / Cinemachine winner) **sem um único owner de estado de coordenação**.

| Severidade | Qtd. | Tema dominante |
|---|---:|---|
| **Crítica** | 3 | Duas publicações LocalPlayer de camera; 3+ mutadores de action map; InputMode sem state owner no host |
| **Alta** | 4 | Pause input dual (minimal vs bridge); Gate adapter vs InputMode; disponibilidade vs publish em overrides; cascade Pause/InputMode |
| **Média** | 4 | Naming legado C9I; owners de camera sem publisher; API Switch vs assignment; injection scan global |
| **Baixa / higiene** | 3 | Stub Removed ainda no menu; type explosion InputMode; enum owners mortos |

**Conclusão:** Camera está **mais unificada no fundo** (arbitration única) e **fragmentada na borda de publicação**. Input está **fragmentado no fundo e na borda** — vários caminhos escrevem `PlayerInput` / action maps com contratos parcialmente sobrepostos e sem `InputModeState` canônico no runtime host.

---

## 2. Método

Análise estática de:

1. Quem **admite** e quem **aplica** intent de camera
2. Quem **muda** `PlayerInput.currentActionMap` / `SwitchCurrentActionMap` / `Enable`/`Disable` de maps
3. Quem **dispara** Pause e se sincroniza InputMode
4. Superfícies de authoring (MonoBehaviour menu) vs runtime contexts (PlayerParticipation)
5. Documentação canônica vs nomes/classes reais

**Não é** auditoria de performance de Cinemachine, binding quality de Input System, ou cobertura de testes.

### Superfícies principais

| Área | Pastas / tipos |
|------|----------------|
| Camera output | `Camera/Output/*`, `CameraOutputSessionBinding` |
| Camera publish | `Camera/Publishing/*`, `Camera/Bindings/*`, `LocalPlayerCameraRequestPublisher` |
| Camera player chain | `PlayerGameplayCameraEligibility*`, `PlayerGameplayAdmissionRuntimeContext.TryPublishCamera` |
| Camera lifecycle | `CameraOutputSessionInjectionRuntime`, `SessionCameraTransitionOrchestrator` |
| InputMode core | `InputMode/InputMode*`, `InputModeUnityPlayerInput*` |
| Pause→Input | `PauseInputMode*`, `PauseInputActionRuntimeBridgeTrigger` |
| Pause legacy/minimal | `Pause/PauseInputActionTrigger`, `UnityPauseInputActionAdapter` (Removed) |
| Gate input | `UnityInput/UnityPlayerInputGateAdapter` |
| Gameplay input bind | `PlayerGameplayInputBindingRuntimeContext` |

---

## 3. Mapas canônicos (intenção documentada)

### 3.1 Camera — fluxo canônico (guide)

```text
Authoring: CameraRigRecipe → CameraRigComposer → CinemachineCamera
Output:    UIGlobal Camera + Brain → CameraOutputSessionBinding
           → Context + Applicator + Session
Publish:   LocalPlayer / Activity / Route / Session publishers
           → Session.Admit → Context winner → RigApplicator
```

Precedência documentada:

```text
Player 50 < Activity 100 < Route 200 < Session 300
```

Regras documentadas importantes:

- Lifecycle de Activity/Route **só deixa override disponível** (`SetOwnerActive`); **não** publica sozinho.
- `RequestOverride()` / `ReleaseOverride()` são explicit.
- Session override durante transition é orquestrado por `SessionCameraTransitionOrchestrator`.

### 3.2 Input — fluxo canônico (parcialmente documentado)

```text
PauseInputActionRuntimeBridgeTrigger
  → PauseInputModeUnityPlayerInputRuntimeBridge
  → PauseInputModeApplyService
  → PauseRuntime.RequestPause
  → PauseInputModeUnityPlayerInputApplication
  → InputModeUnityPlayerInputRequestApplication
  → InputModeUnityPlayerInputApplication / Adapter
  → PlayerInput action map
```

Player guide afirma esse path como canônico para Pause/InputMode.

---

## 4. O que já está bem

### Camera

1. **Única autoridade de winner:** `CameraOutputContext.SelectWinner` — owners não competem por priority Cinemachine solta.
2. **Session aplica + rollback:** `CameraOutputSession.Admit/Release` sincroniza context e applicator; falha de apply tenta rollback.
3. **Publishers tipados por owner:** Session / Route / Activity / LocalPlayer com validação de `OwnerKind` + `LifetimeKind`.
4. **Injection de output sem authoring cross-scene:** `CameraOutputSessionInjectionRuntime` anexa `CameraOutputSessionBinding` a consumers em cenas loaded.
5. **Guide canônico** (`Camera-Architecture-Flow.md`) alinha com a maior parte do código.

### Input

1. **Separação conceitual InputMode ≠ action map name** (`InputModeKind` documentado como posture).
2. **Pipeline em estágios** (request → application preview → action map preview → plan → apply) é diagnosticável.
3. **Pause lógico sem ownership de input** (`PauseRuntime`) — correto.
4. **Legacy F27B marcado Removed** (`UnityPauseInputActionAdapter`) com mensagem de migração.
5. **Gameplay input binding** tem restore/rollback de action map no admission path.

---

## 5. Achados — Camera

### C-C1 — Dois publicadores LocalPlayer para o mesmo Session

**Severidade:** Crítica  

**Trilhos**

| Trilho | Superfície | Quando publica |
|--------|------------|----------------|
| **A — Scene binding** | `LocalPlayerCameraRequestBinding` | `OnEnable` se `eligibleOnEnable` (default **true**) |
| **B — Gameplay admission** | `PlayerGameplayAdmissionRuntimeContext.TryPublishCamera` | Após eligibility + occupancy + input bind |

Ambos usam:

- `CameraRequestOwnerKind.LocalPlayer`
- `CameraRequestLifetimeKind.LocalPlayerEligibility`
- `LocalPlayerCameraRequestPublisher` → `CameraOutputSession.Admit`

**Por que é frágil**

1. Dois requests LocalPlayer com `requestId` / `scopeId` / `tieBreaker` diferentes **coexistem** no context; winner depende só de precedence/tie-break — não de “um Player, um request”.
2. Se IDs colidem → admission do segundo é **blocked** como duplicate (`camera.output-context.request-duplicate`).
3. Se ambos publicam com precedences iguais e tie-breakers distintos → ambiguidade ou flip não intencional.
4. Guide lista **um** consumer LocalPlayer; o path de PlayerParticipation **não** aparece no diagrama de consumers do guide (só o binding de cena).

**Impacto**

- Em projetos que usam chain P3 gameplay **e** deixam `LocalPlayerCameraRequestBinding` no prefab com `eligibleOnEnable`, há **double publish** silencioso ou bloqueio confuso.
- Teardown: admission release vs `OnDisable` do binding podem liberar requests diferentes e deixar o outro “órfão”.

**Recomendação**

- **Uma** superfície canônica de publish LocalPlayer:
  - **Produto atual (P3):** admission runtime publica; `LocalPlayerCameraRequestBinding` vira authoring/evidence **ou** opt-in legacy com `eligibleOnEnable` default **false**.
  - Ou o inverso documentado para protótipos sem chain.
- Validator Editor: erro se ambos os paths estão ativos no mesmo Player/slot.
- Guide: incluir `PlayerGameplayAdmission → LocalPlayerCameraRequestPublisher` no diagrama.

---

### C-C2 — Override Route/Activity: “available” ≠ “published” (fácil de errar)

**Severidade:** Alta  

**Comportamento**

- `OnRouteContentEntered` / `OnActivityContentEntered` → `SetOwnerActive` apenas.
- Publish real → chamada **manual** `RequestOverride()`.
- Session transition → `SessionCameraTransitionOrchestrator` chama `RequestOverride`/`ReleaseOverride` **automaticamente**.

**Por que é frágil**

1. Session tem orquestrador; Route/Activity **não** têm orquestrador de produto equivalente.
2. Consumers esperam “entrou na Activity, camera muda” — mas só fica *available*.
3. Não há API de host `RequestActivityCameraOverride` central; cada binding é imperativo de cena/código de jogo.
4. Doc C9I usa nomes **`RouteCameraRequestBinding` / `ActivityCameraRequestBinding`** e herança de `*ContentBehaviour`; código real é `*CameraOverrideBinding` + interfaces de lifecycle — **drift de naming** aumenta erro de wiring.

**Impacto**

- Override “morto” em produção: lifecycle ok, diagnostics “Available”, winner continua Player.
- Ou publish manual em momento errado (antes de injection de session / antes de enter).

**Recomendação**

- Produto: auto-publish opcional no enter (`publishOnOwnerEnter`) **ou** um único orchestrator Activity/Route simétrico ao Session.
- Atualizar C9I e menu docs para o nome real `*OverrideBinding`.
- Diagnostics: status distinto `AvailableUnpublished` vs `Published`.

---

### C-C3 — Eligibility camera vs publish: duas autoridades em série sem API unificada

**Severidade:** Média  

**Trilhos**

| Etapa | Tipo | Publica request? |
|-------|------|------------------|
| `PlayerGameplayCameraEligibilityRuntimeContext` | Decide eligibility / skip optional | **Não** (documentado) |
| `PlayerGameplayAdmissionRuntimeContext.TryPublishCamera` | Cria request + publish | **Sim** |

**Por que importa**

- Correto como pipeline, mas o nome “Eligibility” vs binding scene “Eligible” colide semanticamente com `LocalPlayerCameraRequestBinding.SetLocalPlayerEligible`.
- Três verbos de “eligible” em camadas diferentes (scene, eligibility context, admission).

**Recomendação**

- Vocabulário único: `Prepared` → `CameraEligible` → `CameraPublished` → `AdmittedGameplayReady`.
- Evitar reutilizar “Eligible” no MonoBehaviour de cena se a cadeia P3 for canônica.

---

### C-C4 — Injection por varredura de MonoBehaviours em toda cena

**Severidade:** Média  

**Onde:** `CameraOutputSessionInjectionRuntime.AttachScene`  
`GetComponentsInChildren<MonoBehaviour>(true)` e teste `is ICameraOutputSessionConsumer`.

**Por que é frágil**

- Custo e ordem dependem do grafo de cena.
- Qualquer consumer disabled/enabled após load precisa de re-attach (só `sceneLoaded` é ouvido).
- `ISessionCameraOverrideConsumer` é paralelo a `ICameraOutputSessionConsumer` — dois contratos de injection.

**Recomendação**

- Registry tipado de consumers no boot da Route, ou marker component query em vez de todos os MonoBehaviours.
- Re-scan em enable de consumer se injection tardia for requisito.

---

### C-C5 — OwnerKinds / LifetimeKinds sem trilho de publish

**Severidade:** Baixa  

`CameraRequestOwnerKind` inclui `Cutscene`, `ModalPresentation`, `Spectator`, `Debug` sem publishers/bindings equivalentes no Runtime.

**Recomendação**

- Marcar como reserved no XML/docs, ou remover até o cut existir (evita consumers inventarem OwnerKind sem stack).

---

## 6. Achados — Input

### I-C1 — Três (ou mais) autoridades que mutam action maps no mesmo `PlayerInput`

**Severidade:** Crítica  

| # | Autoridade | API de mutação | Gatilho |
|---|------------|----------------|---------|
| 1 | `InputModeUnityPlayerInputAdapter` / `Application` | `SwitchCurrentActionMap`, Activate/Deactivate | Pause→InputMode pipeline (e Apply genérico) |
| 2 | `PlayerGameplayInputBindingRuntimeContext` | `currentActionMap = map` (+ Enable) | Gameplay admission / release |
| 3 | `UnityPlayerInputGateAdapter` | `actionMap.Disable()` ou deactivate `PlayerInput` | **Update** + Gate snapshot |
| 4* | `PauseInputActionTrigger` (opt-in) | `SwitchCurrentActionMap` | Pause minimal path se `switchPlayerInputActionMap` |
| 5* | (Removed stub) `UnityPauseInputActionAdapter` | inerte | legado |

**Por que é frágil**

1. **Sem coordenador:** nenhuma camada sabe o “desired mode” global; cada uma aplica um pedaço.
2. **API diferente** para o mesmo efeito (`SwitchCurrentActionMap` vs assignment) — restore/rollback não é simétrico.
3. Gate adapter no **Update** pode re-disable o map que InputMode ou admission acabou de ativar (ou o inverso no frame seguinte).
4. Admission trata Gate como “blocked by input gate” no snapshot de ready, mas o Gate adapter é **componente de cena opt-in** separado do InputMode.

**Impacto**

- Pause resume com map errado; gameplay “morto” com status Ready; UI Pause sem input; thrash frame-a-frame.
- Multiplayer local: cada PlayerInput pode ter subset diferente de adapters no prefab.

**Recomendação**

- **Um** writer físico de action map: idealmente só o estágio final do InputMode (ou um `PlayerInputApplicationRuntime` session-scoped).
- Admission **solicita** posture Gameplay via InputMode; não seta map direto.
- Gate adapter **emite** blocker / pede InputLocked — não Disable no Update.
- Proibir `PauseInputActionTrigger.switchPlayerInputActionMap` quando o bridge canônico estiver presente (validator).

---

### I-C2 — Não existe owner de `InputModeState` no FrameworkRuntimeHost

**Severidade:** Crítica  

**Evidência**

- `InputModeState` é struct passada **por valor** nos pipelines.
- `PauseInputModeApplyService.CreateInputModeStateForPauseState` **reconstrói** state a partir de `PauseState` a cada apply (`Running→Gameplay`, `Paused→PauseOverlay`).
- Não há campo `InputModeState` / runtime module no host que sobreviva entre requests.
- Modes `FrontendMenu` e `InputLocked` existem no enum mas **não** têm owner de transição no host.

**Por que é frágil**

1. InputMode **não é estado de sessão**; é snapshot derivado de Pause no path Pause.
2. Qualquer outro caller de `InputModeUnityPlayerInputRequestApplication.Apply` precisa inventar o `currentState`.
3. Transições Gameplay ↔ FrontendMenu / InputLocked não têm trilha de produto no host.
4. Revision do state (`0` vs `1`) é fabricada no mapper de Pause, não monotônica de sessão.

**Impacto**

- Impossível raciocinar “qual InputMode está ativo?” consultando o host.
- Race conceitual: Pause diz Running mas map ainda é UI porque apply falhou — state lógico e físico divergem e o próximo preflight **assume** mode a partir de Pause só.

**Recomendação**

- `InputModeRuntime` (ou módulo no host) dono de `InputModeState` + última apply evidence.
- Pause **solicita** transição de InputMode; não fabrica state ad-hoc.
- Falha de apply → state permanece no anterior **ou** marca `Desynced` explícito.

---

### I-C3 — Dois triggers de Pause por InputAction (minimal vs canônico)

**Severidade:** Alta  

| Componente | Menu | Efeito |
|------------|------|--------|
| `PauseInputActionRuntimeBridgeTrigger` | Input Mode / … Bridge Trigger | Pause **+** InputMode apply (canônico no guide) |
| `PauseInputActionTrigger` | Pause / Pause Input Action Trigger | Pause direto no host; **opcional** switch de maps (bypassa InputMode) |
| `UnityPauseInputActionAdapter` | Removed/… | Inerte (bom), mas ainda no assembly/menu Removed |

**Por que é frágil**

1. Dois AddComponentMenu ativos podem coexistir na mesma cena e **duplicar** Toggle no mesmo frame (mitigado parcialmente por frame guards locais, não cross-component).
2. Path minimal ensina o anti-padrão “Pause sem InputMode”.
3. Nomes de action map default divergem: bridge tende a `UI`/`Pause`; minimal usa `Global`/`Pause`.

**Recomendação**

- Marcar `PauseInputActionTrigger` como `Obsolete` / `Removed` **ou** forçar `switchPlayerInputActionMap = false` e documentar “trigger only, no map”.
- Validator: no máximo um submitter de Pause por PlayerInput/actions asset.
- Defaults de action map/action name unificados no package settings.

---

### I-C4 — Cascata Pause/InputMode com tipos espelhados (complexidade estrutural)

**Severidade:** Alta (manutenção / risco de divergência)  

Camadas quase 1:1:

```text
InputModeRequestEvaluator
InputModeUnityApplicationPreviewEvaluator
InputModeUnityActionMapPreviewEvaluator
InputModeUnityApplicationPlanEvaluator
InputModeUnityPlayerInputApplication
InputModeUnityPlayerInputAdapter
InputModeUnityPlayerInputRequestApplication   ← orquestra tudo

PauseInputModeRequestMapper
PauseInputModeUnityPlayerInputApplication     ← thin wrap
PauseInputModeApplyService                    ← orquestra Pause + wrap
PauseInputModeUnityPlayerInputRuntimeBridge   ← MonoBehaviour
PauseInputActionRuntimeBridgeTrigger          ← InputAction
```

Cada estágio tem Status/Issue/Result próprios (dezenas de tipos).

**Por que importa**

- Correto para cuts incrementais F30–F38, mas **dois orchestrators** (RequestApplication genérico + Pause ApplyService) duplicam preflight.
- Preflight de Pause constrói `anticipatedPauseResult` sintético **antes** do Pause real; apply de InputMode usa o `currentInputModeState` **pré-Pause** (state “antes”), o que é sutilmente correto para transition, mas frágil se Pause for Ignored e o map já tiver sido alterado por outro trilho.

**Recomendação**

- Manter um único orchestrator “apply input posture”; Pause só produz intent.
- Colapsar Status enums espelhados onde possível após estabilizar o produto.

---

### I-C5 — Gate adapter é polling de cena, não parte do InputMode

**Severidade:** Alta  

`UnityPlayerInputGateAdapter`:

- Lê Gate do host em **Update**
- Disable action map ou deactivate PlayerInput
- Restore local do que **ele** mudou

**Conflitos**

- InputMode pode `SwitchCurrentActionMap` para UI no Pause enquanto Gate também bloqueia InputAcceptance — ordem e restore dependem de quem rodou por último.
- Gameplay admission marca Ready blocked by gate, mas **não** controla o adapter.
- Transition gate e Pause gate podem bloquear o mesmo map por razões diferentes sem stack de blockers no adapter.

**Recomendação**

- Gate → InputMode `InputLocked` (ou mask de acceptance) aplicado pelo **mesmo** writer.
- Remover polling Update em favor de subscription a mudanças de Gate no host.

---

### I-C6 — UnityInputTarget* é trilho de evidence, não de apply (bom, mas fácil de confundir)

**Severidade:** Média  

`UnityInputTargetDeclaration` / `UnityInputTargetSet` / validator descrevem **papéis** de targets; não aplicam input.

Risk: consumers tratam TargetSet como “manager de input”. Docs de API já tentam impedir isso; reforçar no guide de Input.

---

### I-B1 — Stub Removed ainda em Runtime e menu

**Severidade:** Baixa  

`UnityPauseInputActionAdapter` permanece compilado com AddComponentMenu Removed. Aceitável como migração; risco de confusão em search do Project.

---

### I-B2 — Type explosion / naming parallel

**Severidade:** Baixa  

Dezenas de `*Status` / `*Issue` / `*Result` quase idênticos entre InputMode e PauseInputMode. Custo cognitivo alto; não é bug runtime por si.

---

## 7. Cruzamento Camera × Input (onde os trilhos se tocam)

```text
Gameplay chain:
  Occupancy → InputBinding (muta map) → CameraEligibility → Admission
                                              ↓
                                    Publish LocalPlayer camera
                                              ↓
                                    CameraOutputSession winner

Paralelo de cena:
  LocalPlayerCameraRequestBinding (OnEnable publish)
  Route/Activity Override (available; publish manual)
  Session Override (transition orchestrator)

Paralelo de input:
  Pause bridge → InputMode apply (muta map)
  Gate adapter Update (disable map)
  PauseInputActionTrigger optional switch (muta map)
```

**Pontos de conflito arquitetural**

1. **Admission depende de input bind**, e input bind **não** passa por InputMode — camera publish assume um mundo de maps que Pause/Gate podem invalidar no frame seguinte.
2. **Camera Session 300** durante transition não coordena InputLocked; input e camera de transição são trilhos visuais/input separados (pode ser intencional, mas não está unificado).
3. **Exit de Activity** (ver auditoria de ordem) libera camera override no content exit; se dual publish LocalPlayer existir, o winner pode oscilar de forma surpresa.

---

## 8. Diagrama de trilhos (duplicidade)

### Camera — publicação

```text
                    ┌─────────────────────────────┐
                    │   CameraOutputSession       │
                    │   (única arbitration)       │
                    └──────────────▲──────────────┘
           Admit/Release          │
     ┌─────────────┬──────────────┼──────────────┬──────────────┐
     │             │              │              │              │
 LocalPlayer   LocalPlayer    Activity       Route         Session
 Publisher A   Publisher B    Override       Override      Override
 (scene bind)  (admission)    (manual*)      (manual*)     (orchestrator)
 eligibleOnEnable  chain P3   RequestOverride RequestOverride Transition
```

\* manual = available no lifecycle, publish explícito.

### Input — mutação física de `PlayerInput`

```text
                 ┌──────────────────────────┐
                 │   Unity PlayerInput      │
                 │   currentActionMap /     │
                 │   enabled maps           │
                 └────────────▲─────────────┘
        ┌─────────────────────┼─────────────────────┐
        │                     │                     │
 InputMode Adapter    GameplayInputBinding   GateAdapter (Update)
 SwitchCurrentMap     currentActionMap=      Disable / Deactivate
        ▲                     ▲
 Pause bridge pipeline   Admission chain
        ▲
 PauseInputActionTrigger (opt switch)  ← trilho paralelo de submit Pause
```

---

## 9. Tabela consolidada

| ID | Domínio | Severidade | Tema | Status |
|----|---------|------------|------|--------|
| **C-C1** | Camera | Crítica | Double publish LocalPlayer (scene + admission) | Aberto |
| **I-C1** | Input | Crítica | 3+ writers de action map no mesmo PlayerInput | Aberto |
| **I-C2** | Input | Crítica | Sem `InputModeState` owner no host | Aberto |
| **C-C2** | Camera | Alta | Available vs Published em Route/Activity; Session assimétrico | Aberto |
| **I-C3** | Input | Alta | Dois triggers Pause (minimal vs bridge) | Aberto |
| **I-C4** | Input | Alta | Cascata Pause/InputMode duplicada / type mirror | Aberto |
| **I-C5** | Input | Alta | Gate adapter polling vs InputMode | Aberto |
| **C-C3** | Camera | Média | Vocabulário Eligible triplo | Aberto |
| **C-C4** | Camera | Média | Injection scan de todos MonoBehaviours | Aberto |
| **I-C6** | Input | Média | UnityInputTarget confundível com manager | Aberto |
| **C-C5** | Camera | Baixa | OwnerKinds sem stack | Aberto |
| **I-B1** | Input | Baixa | Stub Removed no assembly | Aberto |
| **I-B2** | Input | Baixa | Type explosion Status/Issue | Aberto |

---

## 10. Plano de mitigação sugerido (cortes)

| Corte | Objetivo | IDs |
|-------|----------|-----|
| **IC1** | Declarar **um** writer de action map; deprecar mutação direta no admission e no Gate adapter | I-C1, I-C5 |
| **IC2** | Introduzir `InputModeRuntime` no host (state + last apply evidence) | I-C2, I-C4 |
| **IC3** | Unificar submit de Pause por InputAction; retire/obsoletize path minimal com switch de map | I-C3 |
| **CC1** | Unificar publish LocalPlayer: admission canônico; binding de cena default off + validator | C-C1, C-C3 |
| **CC2** | Produto claro para Route/Activity: auto-publish on enter **ou** orchestrator; alinhar docs C9I | C-C2 |
| **CC3** | Injection por registry/marker; documentar attach lifecycle | C-C4 |
| **X1** | Guide único “Input + Camera during GameplayReady / Pause / Transition” | cruzamento §7 |

### Critérios de aceite sugeridos

- **IC1:** Em smoke, só um call site package-owned muta `currentActionMap` / `SwitchCurrentActionMap` por PlayerInput por transição.
- **IC2:** `host.TryGetInputModeSnapshot` reflete o último apply bem-sucedido; após falha, snapshot não mente “igual Pause”.
- **IC3:** Cena com ambos triggers de Pause falha validator; bridge-only path é o default de template.
- **CC1:** Com chain P3 ativa, zero `LocalPlayerCameraRequestBinding` com `eligibleOnEnable` no prefab canônico; Session tem no máximo um request LocalPlayer por slot.
- **CC2:** Activity enter com flag de produto resulta em winner Activity **sem** chamada manual externa, **ou** doc/tooling proíbem expectativa de auto-switch.
- **X1:** Sequência documentada Pause+transition+admission sem thrash de map e sem double camera request.

---

## 11. Conclusão

**Camera** tem o desenho certo no **miolo** (Session/Context/Applicator) e sofre de **bordas de publicação duplicadas** (scene LocalPlayer vs admission; available vs published).

**Input** tem o desenho certo no **vocabulário** (InputMode posture) e sofre de **vários writers físicos** e de **estado lógico não residente** no host. O path Pause→InputMode canônico existe, mas convive com admission, Gate adapter e trigger minimal.

A prioridade de remediação deve ser:

1. **Um writer de PlayerInput** (IC1) + **state owner de InputMode** (IC2)  
2. **Um publish LocalPlayer** (CC1)  
3. **Contrato Route/Activity override** (CC2) e **um submitter de Pause** (IC3)  

Sem isso, bugs de “input morto depois do pause” e “camera do player some / não troca” continuarão aparecendo como defeitos de cena, quando são **defeitos de trilho duplo**.

---

## 12. Apêndice — referências de código

| Achado | Local |
|--------|--------|
| C-C1 scene publish | `Runtime/Camera/Bindings/LocalPlayerCameraRequestBinding.cs` (`eligibleOnEnable`, `OnEnable`) |
| C-C1 admission publish | `Runtime/PlayerParticipation/Runtime/PlayerGameplayAdmissionRuntimeContext.cs` (`TryPublishCamera`) |
| C-C2 available only | `RouteCameraOverrideBinding` / `ActivityCameraOverrideBinding` (`SetOwnerActive`) |
| C-C2 session auto | `SessionCameraTransitionOrchestrator` |
| C-C4 injection | `CameraOutputSessionInjectionRuntime.AttachScene` |
| Winner único | `CameraOutputContext` |
| I-C1 InputMode write | `InputModeUnityPlayerInputAdapter` (`SwitchCurrentActionMap`) |
| I-C1 admission write | `PlayerGameplayInputBindingRuntimeContext` (`currentActionMap =`) |
| I-C1 gate write | `UnityPlayerInputGateAdapter` (`Update`, `Disable`) |
| I-C1 minimal switch | `PauseInputActionTrigger` (`switchPlayerInputActionMap`) |
| I-C2 state fabricado | `PauseInputModeApplyService.CreateInputModeStateForPauseState` |
| I-C3 dual trigger | `PauseInputActionRuntimeBridgeTrigger` vs `PauseInputActionTrigger` |
| Legacy removed | `UnityPauseInputActionAdapter` |
| Doc canônica camera | `Documentation~/Guides/Camera-Architecture-Flow.md` |
| Doc naming drift | `Documentation~/Product/C9I-Route-Activity-Camera-Request-Bindings.md` |

---

*Fim da auditoria. Próximo passo natural: fechar CC1+IC1 em validators de authoring (baixo risco) e ADR de “single writer” para InputMode/PlayerInput antes de refatorar admission.*
