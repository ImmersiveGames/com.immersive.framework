# Fragilidades de Arquitetura — Status e Próximos Passos

**Produto:** Immersive Framework (`com.immersive.framework`)  
**Relacionado:** [ARCHITECTURE-WEAKNESS-AUDIT.md](./ARCHITECTURE-WEAKNESS-AUDIT.md)  
**Data:** 2026-07-17  
**Escopo:** backlog priorizado **após** Leva 1 (C1, C3, C4, A2) e migração de logger do Camera Rig Apply/Rebuild

---

## 1. Objetivo deste documento

Servir como **mapa de decisão** para as próximas correções de fragilidade de arquitetura:

- o que já foi mitigado;
- o que ainda está aberto;
- prioridade, esforço, risco e valor;
- levadas sugeridas de implementação.

Não substitui a auditoria completa (`ARCHITECTURE-WEAKNESS-AUDIT.md`). Complementa com **estado atual** e **ordem de ataque**.

---

## 2. Já fechados / mitigados

| ID | Tema | Status | Notas |
|---|---|---|---|
| **C1** | Auto-discover Input/Pause via `FindObjectsByType` + pick-first | **Mitigado** | Defaults `false`; sem Find global para PlayerInput/bridge; hierarquia local apenas se flag ligada |
| **C3** | `ContentId` com fallback para nome de cena | **Mitigado** | `ContentId` = explícito; `DiagnosticContentId` só diagnóstico; readiness valida id explícito |
| **C4** | Match de cena por path **ou** nome | **Mitigado** | Path presente e mismatch → não cai em `GetSceneByName`; UIGlobal só por path |
| **A2** | `MonoBehaviour` + cast `ICameraTargetSource` | **Mitigado** | `TryValidateForApply` + Inspector HelpBox/Clear; sem `Debug.LogError` ad-hoc |
| **Logger Camera Rig** | `Debug.Log` no Apply/Rebuild utility | **Migrado** | `FrameworkLogger` + `LogFields` (`com.immersive.logging`) |

### Resíduos conscientes (não “zerados”)

| Resíduo | Onde | Por quê ainda existe |
|---|---|---|
| Assets serializados com flags antigas | Input/Pause components já no consumer | Unity não regrava default; valor YAML antigo permanece |
| `autoDiscover*` ainda pode, se **ligado**, varrer declarações de cena | `ValidateLoadedSceneDeclarations` | Opt-in legado; default off |
| Match de cena **só por nome** se path vazio | Scene lifecycle | Compat authoring incompleto legado |
| Equality com ObjectName/ResourcePath | Descriptors / ContentAnchor | Ainda aberto (M1/M2) |

---

## 3. Matriz do que ainda importa

| Prioridade de ataque | ID | Severidade original | Tema | Esforço | Risco de quebra | Valor |
|---:|---|---|---|---|---|---|
| 1 | **C2** | Crítica | Composition root por discovery de cena | M | Baixo (com fallback) | Alto |
| 2 | **M1 + M2** | Média | Equality contaminada por nome/path | S | Baixo | Médio–Alto |
| 3 | **A3** | Alta | Input por string de map/action | S–M | Baixo (com fallback) | Alto |
| 4 | **C1 residual** | Crítica residual | Flag opt-in ainda pode Find de cena | S | Baixo | Médio |
| 5 | **A1** | Alta | Prefabs `GameObject` sem tipo de contrato | M (Player) | Médio | Alto (lane P3) |
| 6 | **A6** | Alta | Host modules por `GetComponent` colocalizado | S–M | Baixo | Médio |
| 7 | **A5** | Alta | Activity identity string / `asset.name` | M | Médio–Alto | Alto (médio prazo) |
| 8 | **A4** | Alta residual baixa | Nome cosmético Cinemachine | S | Baixo | Baixo |
| 9 | **M4–M7 + higiene** | Média/Baixa | Catálogo IDs, Reset dual, Resources, QA Find, Debug.Log residual | Contínuo | Variável | Higiene / prevenção |

---

## 4. Detalhamento dos próximos pontos

### 4.1 C2 — Composition root por discovery de cena *(P0)*

**Problema**

- `LocalPlayerProvisioningAuthoringDiscovery` e o boot ainda resolvem authoring via `FindObjectsByType` em cenas carregadas.
- Contrato “não usa nome/tag” está correto, mas ainda é **service-discovery implícito por tipo**.

**Impacto**

- Join local e boot dependem de unicidade acidental na cena.
- Additive scenes / DontDestroyOnLoad / duplicatas geram falhas opacas.

**Direção de fix**

| Fase | Ação | Quebra? |
|---|---|---|
| 1 | Garantir fail-closed em 0 e >1 em todos os callers | Não (já desejável) |
| 2 | Campo explícito no bootstrap / GameApplication / host; discovery só fallback legado | Não se fallback permanecer |
| 3 | Remover discovery de paths de produto | Sim se consumer não wiring |

**Arquivos-chave (orientação)**

- `Runtime/PlayerParticipation/Runtime/LocalPlayerProvisioningAuthoringDiscovery.cs`
- `Runtime/Bootstrap/ImmersiveFrameworkBootstrap.cs`
- Módulos de host de Player Participation

**Critério de pronto**

- Product path resolve authoring por **referência serializada** (ou registro no boot).
- Discovery, se existir, é legado documentado + fail-closed + opcionalmente só Development.

---

### 4.2 M1 + M2 — Equality contaminada por nome / hierarchy path *(P1 barato)*

**Problema**

- `ActorDescriptor`, `PlayerActorDescriptor`, `UnityInputTargetDescriptor`: `Equals` / `GetHashCode` incluem `SceneName`, `ObjectName` (e às vezes DisplayName/Source/Reason).
- `ContentAnchorDeclaration.Equals` inclui `ResourceName` / `ResourcePath` derivados de hierarquia.

**Impacto**

- Rename cosmético de GameObject ou reparent muda igualdade mesmo com o mesmo Id tipado.
- Caches, sets e dedup podem invalidar ou duplicar entradas.

**Direção de fix**

- Equality funcional = **IDs tipados** (+ role/scope/kind se forem parte do contrato).
- Nome, scene name e hierarchy path → **somente** `ToDiagnosticString` / campos de evidência.

**Arquivos-chave (orientação)**

- `Runtime/Actors/ActorDescriptor.cs`
- `Runtime/Actors/PlayerActorDescriptor.cs`
- `Runtime/UnityInput/UnityInputTargetDescriptor.cs`
- `Runtime/ContentAnchor/ContentAnchorDeclaration.cs`
- `Runtime/ContentAnchor/RouteContentAnchor.cs` / `ActivityContentAnchor.cs` (origem do ResourcePath)

**Critério de pronto**

- Dois descriptors com mesmo Id e metadados cosméticos diferentes são iguais.
- Testes/QA de equality (se existirem) atualizados.

**Atenção**

- Buscar usos de equality/hash em collections antes de mudar (grep por sets/dicts de descriptors).

---

### 4.3 A3 — Input System por string *(P1 produto)*

**Problema**

- Action maps/actions authorados e defaultados como string: `"Player"`, `"UI"`, `"Pause"`.
- Resolução via `FindActionMap` / `FindAction`.

**Impacto**

- Rename no `.inputactions` quebra runtime sem erro de compilação.
- Core do framework assume convenção do projeto consumer.

**Direção de fix**

| Passo | Ação |
|---|---|
| 1 | Validar presença de map/action no enable/boot → Failed + log estruturado |
| 2 | `InputActionReference` no Pause trigger (crítico) |
| 3 | Estender a bridges/gates; defaults só em template de jogo |

**Arquivos-chave (orientação)**

- `Runtime/InputMode/PauseInputActionRuntimeBridgeTrigger.cs`
- `Runtime/InputMode/PauseInputModeUnityPlayerInputRuntimeBridge.cs`
- `Runtime/UnityInput/UnityPlayerInputGateAdapter.cs`
- `Runtime/Pause/PauseInputActionTrigger.cs` (se no mesmo fluxo)

**Critério de pronto**

- Ação crítica de Pause referenciável por asset tipado **ou** string com validação fail-closed.
- Mensagem diagnóstica clara quando map/action ausente.

**Packages**

- Logging: falhas via `FrameworkLogger` / `LogFields`, não `Debug.Log*`.

---

### 4.4 C1 residual — discovery opt-in ainda pode varrer cena *(P1 higiene)*

**Problema**

- Com `autoDiscoverMissingReferences == true`, target sets / player actors / provisioning ainda podem usar validators baseados em `FindObjectsByType`.

**Direção de fix**

- Remover scene-wide Find desse path **ou**
- Restringir a Editor/QA **ou**
- `[Obsolete]` + warning em runtime product builds.

**Critério de pronto**

- Nenhum path de produto faz Find de cena para wiring de Pause/Input, mesmo com flag ligada (ou flag removida).

---

### 4.5 A1 — Referências `GameObject` onde há contrato tipado *(P2)*

**Problema**

- Prefabs/surfaces como `GameObject`; contrato real exigido depois via `GetComponent`.

**Prioridade pragmática**

1. **Player lane** (`PlayerPrefab` → host tipado `LocalPlayerHostAuthoring` / evidência de `PlayerInput`).
2. ActorProfile host prefab (validator Editor forte antes de mudar tipo do campo).
3. Surfaces (pause/loading/fade) — só se o contrato for mais que “qualquer root”.

**Critério de pronto (mínimo)**

- Assign de Player prefab sem `LocalPlayerHostAuthoring` é erro de authoring no Editor e rejeição no runtime de join.

---

### 4.6 A6 — Host modules acoplados por colocation *(P2)*

**Problema**

- `runtimeHost.GetComponent<OtherModule>()` assume modules no mesmo GameObject.

**Direção de fix**

- `[RequireComponent(typeof(...))]` no host canônico do package.
- Prefab de RuntimeHost com composição fixa validada.
- Opcional: campos serializados internos se a composição for fixa.

**Critério de pronto**

- Prefab/host incompleto falha no Editor; runtime não “acha sumido” silenciosamente.

---

### 4.7 A5 — Identity de Activity por string / asset.name *(P2–P3, migration)*

**Problema**

- `ActivityName` com fallback para `ScriptableObject.name`.
- Owner identity de ObjectEntry/Activity usa essa string.

**Direção de fix (segura)**

1. Introduzir `activityId` estável (opcional no começo).
2. Dual-read: id se preenchido, senão name legado + warning de validator.
3. Só depois: proibir fallback em chaves de runtime.

**Não fazer**

- Troca dura de identity sem inventário no consumer (FIRSTGAME).

**Critério de pronto (fase dual-read)**

- Novos assets authoram `activityId`.
- Validator aponta legados.
- Runtime aceita ambos sem colisão silenciosa.

---

### 4.8 A4 — Nome de objeto Cinemachine *(P3 opcional)*

**Situação**

- Nome usado na **criação** do GO; resolução prefere ref tipada / children.

**Ação mínima**

- Tooltip: label cosmético, não lookup.
- Garantir gravação da ref tipada após create no materializer.

**Prioridade:** baixa entre os Altos.

---

### 4.9 Backlog médio / higiene *(contínuo)*

| ID | Tema | Ação sugerida |
|---|---|---|
| **M3** | Display name ← `gameObject.name` | Garantir que nunca alimenta matching |
| **M4** | IDs string no Inspector sem catálogo | Shared identity assets / uniqueness validators |
| **M5** | `ResetSubjectReference` dual (adapter ou string) | Preferir adapter; string só com catálogo |
| **M6** | `Resources.Load` settings | Path canônico versionado + validação no boot |
| **M7** | Find em QA/Diagnostics | Manter isolado; não reutilizar em lifecycle |
| **Higiene logging** | `Debug.Log*` em Camera bindings, BGM, settings utility | Migrar para `FrameworkLogger` + `LogFields` |
| **Guardrail** | Analyzer / teste de arquitetura | Banir novos `FindObjectsByType` fora de Diagnostics/Editor/Validation |

---

## 5. Levas de implementação sugeridas

### Leva 2 — Boot + identity estável *(recomendado agora)*

| Item | Por quê nesta leva |
|---|---|
| **C2** | Única crítica residual forte no lifecycle Player |
| **M1 + M2** | Barato, alto ROI, evita bugs de equality |

**Resultado esperado:** composition root previsível; descriptors/anchors com equality por Id.

---

### Leva 3 — Input fail-closed

| Item | Por quê |
|---|---|
| **A3** (validação + `InputActionReference` no Pause) | Robustez diária de gameplay/pause |
| **C1 residual** | Fecha a última porta de Find no path Input/Pause |

**Resultado esperado:** Pause/input não dependem de convenção de string sem validação.

---

### Leva 4 — Player lane rígida

| Item | Por quê |
|---|---|
| **A1** (Player prefab tipado) | Join/materialize fail early |
| **A6** (`RequireComponent` / prefab host) | Wiring de modules determinístico |

**Resultado esperado:** lane P3 com contrato de prefab/host explícito.

---

### Leva 5 — Domínio + higiene

| Item | Por quê |
|---|---|
| **A5** dual-read `activityId` | Identity de Activity estável |
| Logging residual / M4–M6 / guardrail | Consistência de packages e prevenção de regressão |

---

## 6. O que **não** priorizar agora

| Evitar | Motivo |
|---|---|
| Remover **todo** `FindObjectsByType` de Editor validators | Uso legítimo em validation/QA |
| Tipar **todos** os `GameObject` de surface de uma vez | Alto churn, baixo ROI imediato |
| `activityId` obrigatório sem dual-read | Quebra consumer |
| Equality “completa” de descriptors como API pública | Já é o bug; não expandir |
| Reintroduzir auto-discover global “por conveniência” | Desfaz C1 |

---

## 7. Princípios de implementação (guardrails)

1. **Wiring explícito no composition root** — discovery é validação/legado, não runtime feliz.  
2. **Identidade ≠ label ≠ path de hierarquia** — IDs tipados no contrato; nomes só em diagnóstico.  
3. **Fail closed em paths de produto** — sem fallback silencioso por nome/Find/primeiro da lista.  
4. **Referências de contrato tipadas** — quando o sistema exige um componente, o campo deve expressar isso.  
5. **Input por asset reference + validação** — strings só com fail explícito.  
6. **Logging via package** — `FrameworkLogger` / `LogFields` (`com.immersive.logging`); não `Debug.Log*` novo.  
7. **Foundation** — `Preconditions` / `NormalizeText` onde couber; sem inventar validators de Unity em foundation.  
8. **QA pode varrer cenas; lifecycle de produto não.**

---

## 8. Checklist de análise (por item antes de codar)

Para cada ID da fila:

- [ ] Confirmar callers e se equality/Find/string é **funcional** ou só diagnóstico  
- [ ] Listar assets consumer que dependem do comportamento legado  
- [ ] Definir dual-read / fallback documentado vs remoção  
- [ ] Escolher superfície de falha (Editor validator, runtime Rejected, ambos)  
- [ ] Logging com `FrameworkLogger` se houver mensagem de produto  
- [ ] Critério de pronto + smoke mínimo (Editor validate + 1 playmode path)

---

## 9. Decisão sugerida imediata

| Ordem | Ação |
|---:|---|
| 1 | Implementar **Leva 2: C2 + M1/M2** |
| 2 | Em seguida **Leva 3: A3 + C1 residual** |
| 3 | Planejar **Leva 4** com FIRSTGAME (serialização Player) |
| 4 | **Leva 5** com migration de Activity identity |

---

## 10. Referências

| Documento | Uso |
|---|---|
| [ARCHITECTURE-WEAKNESS-AUDIT.md](./ARCHITECTURE-WEAKNESS-AUDIT.md) | Achados originais, evidências, severidades |
| [Documentation~/Current/00-Current-State.md](./Documentation~/Current/00-Current-State.md) | Estado de produto |
| `com.immersive.logging` | Logging estruturado |
| `com.immersive.foundation` | Preconditions / NormalizeText (sem lifecycle) |

---

## 11. Histórico de atualizações

| Data | Mudança |
|---|---|
| 2026-07-17 | Documento criado pós-Leva 1 e migração logger Camera Rig Apply/Rebuild |

---

*Documento de planejamento. Não implica que todos os itens abertos sejam bugs ativos em todos os consumers; prioriza redução de superfície frágil no core do framework.*
