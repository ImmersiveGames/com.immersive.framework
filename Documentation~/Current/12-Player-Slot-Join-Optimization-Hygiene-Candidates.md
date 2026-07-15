# 12 — Player / Slot / Join — Candidatos a Otimização, Limpeza e Higienização

**Status:** Draft / revisão pós-integração de join  
**Date:** 2026-07-15  
**Package:** `com.immersive.framework`  
**Escopo:** Player identity, Slot allocation, local join, e camadas adjacentes  
**Tipo:** auditoria de higienização (sem implementação neste documento)  
**Fontes principais:**

```text
ADR-PROD-0010 (manual local join / PlayerInputManager)
ADR-PROD-0011 (ordered Slot allocation)
ADR-PROD-0014 (Local Player Host composition)
F45-ADR-ACTOR-001 (Actor / PlayerSlot boundary)
F49* notes + Player-Architecture-Flow.md
P3E / P3G1 / P3J2 / P3K* manifests e audits
Runtime/PlayerParticipation, PlayerSlots, PlayerEntry, PlayerBinding,
PlayerViews, PlayerControls, PlayerTopology, UnityInput
```

---

## 1. Objetivo

Após a integração do **join de player**, este documento lista **candidatos concretos** a:

- remover redundância entre camadas legadas e a lane P3 ativa;
- reduzir repetição de contratos `Result` / `Snapshot` / `Status` / `Token`;
- alinhar documentação com o código real;
- identificar pedaços mal otimizados ou de alto custo cognitivo no path join → Slot → host.

**Não é um plano de execução.** Cada item tem severidade, evidência e direção sugerida. Decisões de aceite/rejeição devem ocorrer antes de qualquer PR de limpeza.

---

## 2. Resumo executivo

O sistema de Player evoluiu em **duas eras** que ainda coexistem no package:

| Era | Origem | Papel declarado | Estado no código |
|-----|--------|-----------------|------------------|
| **F45 / F49 passiva** | ADRs F45/F49 | identidade + evidência + validação diagnóstica | ainda presente e pública |
| **P3 runtime** | ADR-PROD-0010+ / P3F–P3K | join, alocação Session, host, preparation, gameplay admission | lane canônica atual |

O join implementado em `LocalPlayerProvisioningBridge` + `PlayerParticipationRuntimeContext` está **alinhado aos ADRs de produto** (manual join, first-available Slot, pending correlation, host técnico ≠ Actor lógico). O problema principal não é o fluxo canônico do join — é a **sobreposição de modelos**, a **explosão de contratos espelhados** e a **documentação desatualizada** que ainda descreve o mundo F49 como “o que o código faz hoje”.

### Métricas de superfície (ordem de grandeza)

| Módulo | Arquivos `.cs` | Linhas (aprox.) |
|--------|----------------|-----------------|
| `PlayerParticipation` | 163 | ~26 200 |
| `PlayerBinding` | 41 | ~5 100 |
| `PlayerSlots` + `PlayerEntry` + `PlayerViews` + `PlayerControls` + `PlayerTopology` | 36 | ~3 700 |
| Contratos só em `PlayerParticipation/Contracts` | 104 | — |
| Enums `*Status` / lifecycle só em Participation | 27+ | — |

---

## 3. Mapa mental do path canônico (P3)

```text
GameApplicationAsset.LocalPlayerSlots[]   (PlayerSlotProfile ordered)
        │
        ▼
PlayerParticipationRuntimeContext         (Session roster + allocation + selection)
        │
        ▼
LocalPlayerProvisioningAuthoring.RequestJoin(...)
        │
        ▼
LocalPlayerProvisioningBridge.TryJoin
  1. validate request / backend
  2. TryReserveNextAvailableSlot
  3. PlayerInputManager.JoinPlayer (via UnityLocalPlayerProvisioningBackend)
  4. correlate JoinPlayer return + onPlayerJoined callback
  5. LocalPlayerHostAuthoring.TryStageAdmission (+ runtime PlayerSlotDeclaration)
  6. TryMarkJoined
  7. CommitStagedAdmission
        │
        ▼
Joined technical host (Session lifetime under FrameworkRuntimeHost)
        │
        ▼  (fora do join síncrono, mas acoplado no endpoint)
RegisterJoinWithActorPreparation → preparation / occupancy / gameplay chain
```

Qualquer limpeza deve **preservar** esta authority boundary (ADR-PROD-0010/0011/0014).

---

## 4. Candidatos — Alta prioridade

### C1. Dois modelos de Slot / Player que não conversam

**Severidade:** Alta (redundância estrutural + confusão de produto)  
**Área:** `PlayerSlots` / `PlayerEntry` / `PlayerParticipation`

| Modelo | Onde | Mutabilidade | Quem usa join? |
|--------|------|--------------|----------------|
| `PlayerSlotDeclaration` + `PlayerSlotOccupancy` + `PlayerEntry*` | cena / passivo F45–F49 | autoria + rebuild | **não** |
| `PlayerSlotProfile` + `PlayerSlotAllocationState` + `PlayerSlotRuntimeSnapshot` | assets + Session context P3 | runtime Session | **sim** |

**Evidências:**

- `PlayerSlotOccupancy` declara: *não seta occupants, não spawna, não resolve capabilities*.
- `PlayerGameplayOccupancyRuntimeContext` declara explicitamente: *não muta `PlayerSlotOccupancy` passivo*.
- Join cria `PlayerSlotDeclaration` **em runtime** no host (`LocalPlayerHostAuthoring.TryStageAdmission`) — reutiliza o componente F45 só como **evidence carrier**, não como autoridade de alocação.
- `PlayerEntryState` ainda tem vocabulário `Joined` / `Assigned` / `ViewBound` como “futuro”, enquanto o join real usa `PlayerSlotAllocationState` + estados de preparation/gameplay.

**Problema:**

Consumidores e designers podem achar que:

1. `PlayerSlotDeclaration` na cena = Slot da Session; ou  
2. `PlayerEntry.Joined` = join admitido; ou  
3. `PlayerSlotOccupancy` = occupancy efetiva de gameplay.

Nenhuma dessas equivalências é verdade no path P3.

**Direção sugerida:**

1. Documentar matriz canônica: *Profile (config) → Allocation (Session) → Host declaration (evidence) → Occupancy efetiva (gameplay)*.
2. Marcar stack F49 (`PlayerEntry` chain, validators de topologia passiva) como **Legacy / Diagnostic-only** ou planejar depreciação.
3. Evitar novos usos de `PlayerEntryState` no path de join; se necessário um “entry view” de produto, derivar de snapshots P3.

**Não fazer sem decisão de produto:** deletar `PlayerSlots`/`PlayerEntry` de uma vez — ainda servem Gate/adapters e QA legada.

---

### C2. Documentação desatualizada sobre join e player

**Severidade:** Alta (higiene / onboarding / regressão cognitiva)  
**Área:** `Documentation~/Guides`, `Documentation~/Current`

**Evidências:**

`Player-Architecture-Flow.md` ainda afirma:

```text
Nenhum componente ou serviço no código atual:
- faz join de jogadores (PlayerInputManager.JoinPlayer);
...
```

Isso é **falso** desde P3G/P3J (`LocalPlayerProvisioningBridge`, bootstrap, authoring endpoint).

`Documentation~/Current/04-Player-Passive-Binding-Foundation.md` e o guide ainda descrevem o player como “evidência passiva + validação”, sem a lane Session de join.

**Direção sugerida:**

1. Atualizar `Player-Architecture-Flow.md` com a lane P3 (join → host → preparation → admission) e marcar a stack F49 como legada/paralela.
2. Adicionar um “Current Map” em `00-Current-State.md` para Player/Slot/Join (hoje o current state fala mais de câmera).
3. Indexar ADRs de produto P3 no guide principal para evitar leitura só de F49.

---

### C3. Dualidade `SessionPlayerInputManagerDeclaration` vs `LocalPlayerProvisioningAuthoring`

**Severidade:** Alta (duas “portas” para o mesmo `PlayerInputManager`)  
**Área:** `UnityInput` / `PlayerParticipation`

| Superfície | Papel | Status API |
|------------|-------|------------|
| `SessionPlayerInputManagerDeclaration` | evidência session-scoped; não join | Experimental F31B |
| `LocalPlayerProvisioningAuthoring` | endpoint de join + open/close capacity | Experimental P3G/P3J |

Ambos apontam para `PlayerInputManager`. Pause/InputMode bridges ainda usam o declaration F31B; o path de join usa o authoring P3.

**Problema:**

- risco de dois managers ou dois “donos” na cena;
- validação e discovery separados (`LocalPlayerProvisioningAuthoringDiscovery` vs `UnityInputTargetValidator`);
- designer não tem um único “Session Player Input surface”.

**Direção sugerida:**

1. Unificar authoring: um componente Session que **declara** o manager e **expõe** o endpoint de join.
2. Ou fazer `LocalPlayerProvisioningAuthoring` *require/own* a declaration e invalidar a existência de managers sem declaration.
3. Atualizar bridges de Pause/InputMode para resolver o manager via a mesma superfície canônica.

---

### C4. Validação de prefab/host/manager triplicada

**Severidade:** Média–Alta (repetição + drift)  
**Área:** join runtime + editor

As mesmas regras (“manual join”, “prefab com `PlayerInput` + `LocalPlayerHostAuthoring`”, “host resolve o mesmo PlayerInput”, “mount vazio”) aparecem em:

1. `LocalPlayerProvisioningBridge.TryValidateBackend`
2. `LocalPlayerProvisioningRuntimeHostModule.TryValidateRuntimeConfiguration`
3. `Editor/.../LocalPlayerProvisioningValidator` + `LocalPlayerHostAuthoringValidator`
4. `LocalPlayerHostAuthoring.TryValidateConfiguration` (fonte de verdade parcial do shape do host)

**Problema:**

Qualquer mudança de regra (ex.: permitir declaration pré-existente, mudar policy de mount) exige editar 3–4 caminhos e os smoke runners.

**Direção sugerida:**

Extrair um **validador puro compartilhável** (sem Unity Editor GUI):

```text
LocalPlayerProvisioningConfigurationRules
  ValidateManager(PlayerInputManager)
  ValidateHostPrefab(GameObject)
  ValidateHostInstance(LocalPlayerHostAuthoring, stage)
```

Runtime e Editor só consomem o mesmo resultado tipado de issues.

---

### C5. `CreateResult` / rejection path verboso e frágil no bridge

**Severidade:** Média–Alta (manutenção / micro-otimização de clareza)  
**Área:** `LocalPlayerProvisioningBridge.TryJoin`

O método `CreateResult` recebe **14 parâmetros**, e cada early-reject repete dezenas de `null` / `default` / `-1`. Há também:

```csharp
if (ReferenceEquals(provisionedPlayerInput, null)) { ... }
// depois
if (provisionedPlayerInput == null) { ... } // segundo check pós RecordDirectResult
```

e em `RejectDistinctPlayers`:

```csharp
if (!ReferenceEquals(first, null) && first != null)
```

Mistura de `ReferenceEquals` (proteção a Unity fake-null) com `== null` em sequência confunde o leitor e pode mascarar intent.

**Direção sugerida:**

1. Introduzir factory methods por família de falha:

```text
LocalPlayerJoinResult.RejectedInvalidRequest(...)
LocalPlayerJoinResult.RejectedCapacity(...)
LocalPlayerJoinResult.FailedRollback(...)
```

2. Centralizar policy de Unity fake-null em um helper (`UnityObjectEvidence.IsAlive(PlayerInput)`).
3. Manter a semântica ADR: pending existe **antes** de `JoinPlayer`; rollback libera reservation e rejeita hosts.

---

## 5. Candidatos — Prioridade média

### C6. Explosão de contratos espelhados (Result / Snapshot / Status / Token / Summary)

**Severidade:** Média (custo cognitivo + LOC)  
**Área:** `PlayerParticipation/Contracts` (~104 tipos)

Padrão repetido quase idêntico em várias stages:

```text
*Status enum
*State enum (quando há machine)
*Token
*Summary
*Snapshot
*Result (+ Succeeded/Failed/Rejected/ToDiagnosticString)
*RuntimeContext
*RuntimeHostModule (+ partials ActivityAdmission / Promotion)
```

Stages observáveis:

```text
Participation allocation
Local join
Actor selection
Actor materialization
Actor preparation
Gameplay occupancy
Gameplay input binding
Gameplay camera eligibility
Gameplay admission
Candidate stage
Chain handoff
Activity admission stage
Activity lifecycle admission
Handoff group
```

**Problema:**

- cada stage “parece um micro-framework”;
- helpers `Succeeded` / `ToDiagnosticString` / `RuntimeUnavailable` copiados;
- difícil saber o que é **Session-persistent** vs **Activity-scoped**.

**Direção sugerida (incremental, sem big-bang):**

1. Extrair base genérica interna para outcomes:

```text
OperationOutcomeKind { Succeeded, Rejected, Failed, Ignored }
FrameworkOperationResultBase (message, operation, source, reason, revision pair)
```

2. Manter enums de domínio específicos (não fundir semanticamente `LocalPlayerJoinStatus` com `PlayerGameplayAdmissionStatus`).
3. Agrupar pastas por lifetime:

```text
Contracts/Session/
Contracts/Join/
Contracts/Gameplay/
Contracts/Activity/
```

4. Documentar **pipeline de stages** com um diagrama único no guide (hoje o conhecimento está espalhado em 15 manifests P3K*).

---

### C7. Duplicação `TryCreate` / `TryCreateWithActorSelectionPolicy`

**Severidade:** Média (DRY)  
**Área:** `PlayerParticipationRuntimeContext`

Os dois factories compartilham ~80% do corpo (slot records, capacity bounds, init result). A diferença real é a policy de Actor selection obrigatória.

**Direção sugerida:**

Um único `TryCreate(..., PlayerActorSelectionPolicyProfile policyOrNull, bool requirePolicy, ...)`.

---

### C8. Occupancy em três camadas sem projeção unificada

**Severidade:** Média  
**Área:** Slot occupancy semantics

| Camada | Tipo | Significado |
|--------|------|-------------|
| Autoria passiva | `PlayerSlotOccupancy` | relação scene-authored Slot→Actor (diagnóstico) |
| Session allocation | `PlayerSlotAllocationState` | Available/Reserved/Joined/Leaving |
| Gameplay efetiva | `PlayerGameplayOccupancyRuntimeContext` | Slot→prepared Actor ocupado para gameplay |

**Problema:** três verdades sem um “read model” único para UI/produto (“quem está no Slot 2?”).

**Direção sugerida:**

Introduzir um **projection snapshot** de produto:

```text
PlayerSlotPresenceSnapshot
  Profile + AllocationState
  Host joined? + UnityPlayerIndex
  SelectedActorProfile?
  EffectiveOccupancy?
  Preparation/Gameplay readiness summaries
```

Derivado, não mutável; alimentado por contexts existentes.

---

### C9. `PlayerBinding` (~5k LOC) legado em relação a P3K gameplay chain

**Severidade:** Média  
**Área:** `PlayerBinding` / P3K

P3A removeu da materialização do `PlayerComposer`:

```text
PlayerControlBindingTargetBehaviour
UnityPlayerInputBridgeTargetBehaviour
UnityPlayerInputActivationTargetBehaviour
PlayerSlotOccupancy (materializada)
passive Entry/View/Control evidence
```

Porém os tipos e adapters em `Runtime/PlayerBinding` **continuam no package** (~41 arquivos). A lane P3K introduz `PlayerGameplayInputBinding*`, camera eligibility e admission **sem referenciar** esses bindings.

**Problema:**

Dois stacks de “bind control/input/view”:

- F49/F51/F52 passive binding + adapters;
- P3K runtime gameplay chain.

**Direção sugerida:**

1. Inventory de consumidores reais (FIRSTGAME / QA / scenes).
2. Se zero consumidores de produto: marcar `[Obsolete]` / `FrameworkApiStatus.Legacy` e planejar remoção.
3. Se ainda usados em smoke: isolar em pasta `Legacy/` e excluir do caminho de onboarding.

---

### C10. `PlayerSlotDeclaration` recriado dinamicamente no join

**Severidade:** Média (híbrido estranho, mas intencional em P3J2)  
**Área:** `LocalPlayerHostAuthoring`

Join faz `AddComponent<PlayerSlotDeclaration>()` e `ConfigureForDiagnostics(...)`.

Isso **reusa** o tipo F45, mas:

- o host prefab **proíbe** declaration pré-autora;
- a declaration vira state runtime disfarçada de MonoBehaviour de autoria;
- Gate adapters que leem `PlayerSlotDeclaration` na cena podem ver hosts joined e slots de cena com a **mesma API** e semântica diferente (authored vs staged).

**Direção sugerida (opções):**

**A.** Manter, mas renomear mentalmente e documentar: *runtime-stamped evidence only*.  
**B.** Substituir por `LocalPlayerJoinedSlotEvidence` (componente runtime explícito, não o declaration de autoria).  
**C.** Fazer Gate/adapters consumirem `PlayerSlotId` + evidence tipada, não o MonoBehaviour declaration.

Opção B/C é mais limpa a médio prazo; A é mais barata agora.

---

### C11. Status mapping entre camadas (reservation → join)

**Severidade:** Média (higiene de contratos)  
**Área:** `LocalPlayerProvisioningBridge.MapReservationStatus`

```text
PlayerParticipationOperationStatus → LocalPlayerJoinStatus
```

É um switch manual. Se novos statuses de participation forem adicionados, o join cai no default `FailedAdmission` e pode mentir semanticamente.

**Direção sugerida:**

- testes de exaustividade do enum mapping;
- ou shared subset de rejection codes para capacity/joining/no-slot.

---

### C12. `LocalPlayerJoinResult` vs nested operation results

**Severidade:** Baixa–Média  
**Área:** contratos de join

O result carrega:

```text
ReservationResult
CommitResult
RollbackResult
Slot snapshot
PlayerInput
LocalPlayerHost
CallbackConfirmation
OriginalStatus
```

Útil para QA, mas pesado para product code. ADR-PROD-0010 pediu evidência rica; ok. Porém callers de produto raramente precisam dos três `PlayerParticipationOperationResult`.

**Direção sugerida:**

- manter o result rico **internal/diagnostic**;
- expor um DTO de produto mais fino (`JoinedPlayerEvidence`) no authoring público, se a API Experimental for estabilizada.

---

## 6. Candidatos — Prioridade baixa / micro

### C13. `ReferenceEquals` + Unity null misturados

Já citado em C5. Padronizar helper de “Unity object still alive” em join/reject paths.

### C14. `pendingJoin` single-flight vs futuro multi-join

ADR/P3G1 deliberadamente serializa joins. Ok. Documentar explicitamente no authoring API como **limitação intencional**, não bug, até haver requisito de join paralelo.

### C15. Callback confirmation assíncrona pós-sucesso

Bridge pode retornar `SucceededJoined` com `CallbackConfirmation.Pending` e confirmar depois em `awaitingCallbackConfirmations`. Correto face ao lifecycle Unity, mas opaco.

**Direção:** expor no diagnostic snapshot do module `AwaitingCallbackConfirmationCount` (já existe internamente) e um evento/log de confirmação tardia.

### C16. `TryAttachHostToSessionLifetime` retorna `issue` de sucesso

```csharp
issue = "local-player-host-session-lifetime-transfer";
return true;
```

Em sucesso, `issue` não é erro — confunde leitores do padrão `out string issue`.

**Direção:** `out string diagnostic` separado, ou limpar `issue` em sucesso.

### C17. Leave path incompleto vs estados `Leaving`

`PlayerSlotAllocationState.Leaving` existe; o join path cobre reserve/join/rollback. Leave/device-loss product path ainda não fecha o ciclo (deferido nos ADRs). Não é bug de join, mas estado morto no enum até P3 leave.

**Direção:** ou implementar leave mínimo, ou documentar `Leaving` como reserved vocabulary.

### C18. Guide e Current docs sem diagrama de authority

Além de C2: um único diagrama “who owns what” (Host vs Context vs Manager vs Profile) reduziria rework em reviews.

---

## 7. O que **não** parece candidato a “otimização agressiva”

Estes pontos estão **intencionalmente** separados e devem ser preservados:

| Separação | Motivo (ADR) |
|-----------|----------------|
| `playerIndex` ≠ `PlayerSlotId` | ADR-PROD-0010 / F45 |
| `PlayerSlotProfile` imutável ≠ allocation state | ADR-PROD-0009 / 0011 |
| `PlayerInputManager` provisiona host; framework admite | ADR-PROD-0010 |
| Host técnico ≠ Logical Actor | ADR-PROD-0014 / P3J2 |
| Join ≠ Activity gameplay readiness | ADR-PROD-0010 / 0012 |
| Sem service locator / singleton de Player | P3E / ADR-PROD-0010 |

Qualquer “simplificação” que una essas autoridades reabre DG-P3-02/03.

---

## 8. Matriz de priorização sugerida

| ID | Tema | Impacto | Esforço | Risco | Ordem sugerida |
|----|------|---------|---------|-------|----------------|
| C2 | Docs desatualizadas | Alto | Baixo | Baixo | 1 |
| C4 | Validação unificada | Médio | Médio | Baixo | 2 |
| C5 | Bridge CreateResult/factories | Médio | Baixo | Baixo | 3 |
| C7 | DRY TryCreate | Baixo | Baixo | Baixo | 4 |
| C16 | issue em sucesso | Baixo | Baixo | Baixo | 4 |
| C3 | Unificar surface do Manager | Alto | Médio | Médio | 5 |
| C1 | Política legado F49 vs P3 | Alto | Alto | Alto | 6 (decisão primeiro) |
| C9 | Legacy PlayerBinding | Médio | Médio | Médio | 6 |
| C8 | Presence projection | Médio | Médio | Baixo | 7 |
| C6 | Contract folder/base outcome | Médio | Alto | Médio | 8 |
| C10 | SlotDeclaration runtime type | Médio | Médio | Médio | 8 |
| C11–C15, C17–C18 | higiene pontual | Baixo–Médio | Baixo | Baixo | contínuo |

---

## 9. Checklist de decisão (antes de PRs)

Para cada candidato aceito:

- [ ] Preserva boundary ADR-PROD-0010/0011/0014?
- [ ] Há consumidor real (FIRSTGAME / QA / scenes) afetado?
- [ ] Status API (`Experimental` / `Internal` / futuro `Stable`) atualizado?
- [ ] Smoke/QA runners listados e atualizados no mesmo PR?
- [ ] Docs (Guide + Current + ADR History se breaking) no mesmo PR?
- [ ] Evita fundir Session allocation com Activity admission?

---

## 10. Proposta de waves (somente se o time quiser executar)

### Wave H1 — Higiene sem mudança de comportamento

```text
C2 docs
C5 factories + Unity null helper
C7 DRY TryCreate
C16 issue cleanup
C11 mapping tests
C14 document single-flight
C15 diagnostic await confirmation
```

### Wave H2 — Superfície de authoring

```text
C4 shared validation rules
C3 unify PlayerInputManager authoring surface
C8 presence projection (read-only)
```

### Wave H3 — Legado e contratos

```text
C1 / C9 decision: Legacy mark vs delete schedule
C6 contract grouping + optional base outcome types
C10 runtime evidence type vs PlayerSlotDeclaration
C17 leave vocabulary plan
```

---

## 11. Conclusão

O **join** em si está estruturalmente coerente com os ADRs de produto: um bridge, um pending, first-available Slot, host técnico sob `PlayerInputManager`, admission Session separada de Actor/gameplay.

Os maiores ganhos de limpeza **não** estão em reescrever o join, e sim em:

1. **declarar o canônico P3** e rebaixar/isolar a stack passiva F49;  
2. **uma superfície** para o `PlayerInputManager` de Session;  
3. **uma validação** de host/prefab/manager;  
4. **frear a proliferação** de micro-contratos espelhados com bases e pastas por lifetime;  
5. **atualizar a documentação** que ainda nega a existência do join.

Este arquivo é o backlog de candidatos. Aceite seletivo por wave evita um “cleanup PR” monolítico e destrutivo.

---

## 12. Referências rápidas de código

```text
Runtime/PlayerParticipation/Runtime/LocalPlayerProvisioningBridge.cs
Runtime/PlayerParticipation/Runtime/PendingLocalPlayerJoin.cs
Runtime/PlayerParticipation/Runtime/UnityLocalPlayerProvisioningBackend.cs
Runtime/PlayerParticipation/Runtime/LocalPlayerProvisioningRuntimeHostModule.cs
Runtime/PlayerParticipation/Runtime/PlayerParticipationRuntimeContext.cs
Runtime/PlayerParticipation/Authoring/LocalPlayerHostAuthoring.cs
Runtime/PlayerParticipation/Authoring/LocalPlayerProvisioningAuthoring.cs
Runtime/PlayerParticipation/Authoring/PlayerSlotProfile.cs
Runtime/PlayerSlots/*
Runtime/PlayerEntry/*
Runtime/PlayerBinding/*
Runtime/UnityInput/SessionPlayerInputManagerDeclaration.cs
Documentation~/ADRs/Product/ADR-PROD-0010-*.md
Documentation~/ADRs/Product/ADR-PROD-0011-*.md
Documentation~/Product/P3E-*.md
Documentation~/Product/P3G1-*.md
Documentation~/Product/P3J2-*.md
Documentation~/Guides/Player-Architecture-Flow.md
```
