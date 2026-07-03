# F15-ADR-RESET-004 — Unity Reset Adapters mínimos

Status: Closed / Applied through F15F  
Fase: F15 — Unity Reset Adapters mínimos  
Tipo: Unity Adapter / Object Reset / Authoring  
Ultima atualizacao: 2026-06-26  
Corte: F15F — Unity Reset Adapters Closure

---

## 1. Contexto

F14 fechou a fundacao lógica de Object Reset:

```text
ObjectEntryId + Scope + OwnerIdentity
-> ObjectEntryRuntimeContextSnapshot
-> ObjectResetTargetResolver
-> ObjectResetPlan
-> ObjectResetRuntime
-> FrameworkRuntimeHost.RequestObjectResetAsync
-> ObjectResetTrigger
-> ObjectResetTriggerUnityEventBridge opcional
```

F14 não executa reset físico Unity. Ela apenas resolve alvo lógico, monta plano deterministico, executa participantes sintéticos/contratuais e agrega resultado.

F15 abriu e fechou os primeiros adapters Unity reais para tornar Object Reset observável em objetos comuns de cena, sem introduzir gameplay, Player, Actor, pooling ou save/checkpoint.

---

## 2. Problema

Depois da F14, um trigger valido pode terminar como `SucceededNoParticipants` porque ainda não há participantes Unity reais. Isso e correto para a fundacao, mas insuficiente para uso pratico.

A F15 precisa responder:

```text
Como um componente Unity real vira IObjectResetParticipant?
Como o Runtime Host recebe a participant source?
Quando ausência de adapter e erro?
Como aplicar baseline físico sem fallback silencioso?
Como evitar GameObject.name/path como identidade funcional?
```

---

## 3. Decisões aceitas

### 3.1 Boundary de assembly para F15

F15 **não fara split de asmdef**.

O package atual já possui um Runtime asmdef com `UnityEngine` habilitado e já contem componentes Unity de authoring/UX, como triggers e bridges. Para F15, os adapters Unity mínimos ficam no próprio `com.immersive.framework`.

Decisão:

```text
Não criar novo asmdef neste corte.
Não splitar core puro vs Unity adapter dentro da F15.
Manter a separação por namespace/pasta e contrato.
Registrar split futuro como refactor arquitetural separado, não como pre-requisito de F15.
```

Regra pratica:

```text
ObjectResetTargetResolver, ObjectResetPlan e ObjectResetRuntime continuam sem dependência conceitual de Unity.
Adapters Unity podem depender de UnityEngine.
Adapters não podem mover lifecycle, ownership, identity ou policy para Unity components.
```

### 3.2 Registro de participants/adapters

`FrameworkRuntimeHost` continua sendo o owner do `IObjectResetParticipantSource` ativo.

O setter interno existente pode continuar interno. F15 não deve exigir que gameplay/user code injete source manualmente.

Decisão:

```text
F15 implementara uma participant source Unity pertencente ao framework.
Essa source sera registrada pelo próprio framework, dentro do package.
Ela fornecera participants ao ObjectResetRuntime.
Ela não executara reset por conta própria.
Ela não sera service locator.
Ela não cria lifecycle paralelo.
```

A source Unity deve coletar apenas participantes explícitos.

Permitido:

```text
Componentes que implementam IObjectResetParticipant.
Componentes de adapter declarados em GameObjects de cena.
Filtro por ObjectResetTarget canônico.
Include inactive quando necessário para objetos de cena explícitamente declarados.
```

Proibido:

```text
Registrar por GameObject.name.
Registrar por hierarchy path.
Registrar por tag/layer como identidade funcional.
Criar singleton global de participants.
Criar manager de reset separado do Runtime Host.
```

### 3.3 Identidade de target dos adapters

Todo adapter Unity real deve declarar para qual target lógico ele participa.

Caminho preferido:

```text
ObjectEntryDeclaration
+ Adapter Unity no mesmo GameObject ou em filho controlado
+ adapter referencia a declaration ou resolve ObjectEntryId/scope/owner por ela
```

Caminho manual permitido apenas como authoring explícito:

```text
ObjectEntryId manual quando não houver Target Declaration atribuida.
```

Não permitido:

```text
GameObject.name
Transform hierarchy path
Scene path
InstanceID
Sibling index
```

### 3.4 `SucceededNoParticipants` em F15

`SucceededNoParticipants` continua valido para F14 foundation e para requests que explícitamente permitem ausência de participantes.

Para F15, ausência de adapter não pode mascarar erro quando o fluxo espera reset físico.

Decisão:

```text
Request/policy de reset físico deve poder exigir pelo menos um participant.
Adapter required ausente deve gerar issue blocking.
Adapter optional ausente pode gerar skip/warning não bloqueante.
Trigger/authoring deve deixar claro se no participants e permitido.
```

A F15 não deve transformar `SucceededNoParticipants` em fallback silencioso para reset físico não executado.

### 3.5 Required vs Optional

Adapters Unity usam a mesma semântica de participant requiredness da F14:

```text
Required -> falha bloqueia o Object Reset.
Optional -> falha vira warning/skip conforme o motivo.
```

Baseline ausente:

```text
Required adapter + baseline ausente -> FailedBlocking.
Optional adapter + baseline ausente -> SkippedOptional ou warning não bloqueante.
```

### 3.6 Baseline

F15 prioriza baseline mínimo de `Transform`.

Fontes aceitas:

```text
Authored baseline.
Captured baseline em boundary deterministico, se implementado em corte próprio.
```

Proibido:

```text
Capturar baseline silenciosamente no momento do reset.
Usar posicao atual como fallback se baseline obrigatório estiver ausente.
Recarregar cena para recuperar baseline.
Usar prefab/pool/save como baseline implicito na F15.
```

### 3.7 Ordem de adapters físicos

A ordenacao canônica continua sendo:

```text
participant.Order
sourceIndex
participantId
```

Bandas sugeridas para F15:

```text
Transform reset: 100
Rigidbody reset/velocity cleanup: 200
Animator reset: 300
```

A decisão operacional e: reset físico deve ser deterministico e diagnosticavel. Se uma ordem diferente for necessaria em corte específico, o corte deve justificar no smoke/diagnostics.

### 3.8 GameObject inactive / component disabled

Objetos inativos podem precisar de reset físico antes de serem reativados.

Decisão:

```text
A source Unity pode coletar adapters em GameObjects inativos quando eles sao explícitamente authored.
Component disabled não deve ser tratado como participant ativo por default.
Se um reset required depende de adapter desabilitado/ausente, isso deve virar issue explícito.
```

Nenhuma dessas regras autoriza identity por hierarchy/name.

### 3.9 Diagnostics mínimos

Logs/smokes de adapters devem declarar:

```text
objectEntry
scope
owner
participantId
adapterType
requiredness
baselineSource
order
applied fields
status
issues
blockingIssues
nonBlockingIssues
```

Diagnostics não devem despejár detalhes excessivos de Transform por frame nem gerar ruido em smokes felizes.

---

## 4. Escopo fechado da F15

Incluido e aplicado:

```text
Unity Object Reset participant source.
Authoring mínimo para adapters Unity.
Transform reset participant com baseline explícito.
Policy/diagnostics para ausência de required adapter/baseline.
Smokes de reset físico mínimo.
Documentacao curta de uso.
```

Diferido para fases futuras:

```text
Rigidbody velocity cleanup.
Animator reset mínimo.
GameObject active state reset was deferred out of F15 and closed separately in F16.
```

Fora da F15:

```text
Player stats.
Player movement design.
Health.
Powerups.
Combat.
Actor behavior.
NPC behavior.
Projectile.
Pooling return/despawn.
Save/checkpoint restore.
Scene reload como reset.
Addressables/prefab rematerialization.
Camera/audio/gameplay consumers.
```

---

## 5. Sequencia aplicada de cortes

```text
F15A — Unity Reset Adapters ADR.
F15B — Unity Object Reset Participant Source.
F15C — Transform Reset Participant / Authored Baseline.
F15D — Missing Required Adapter/Baseline Guardrails.
F15E — Transform Reset Authoring UX + Guide.
F15F — Unity Reset Adapters Closure Smoke + docs/QA cleanup.
```

---

## 6. UX esperada

Fluxo fechado recomendado para GameDesigner:

```text
1. Criar/selecionar um GameObject de cena.
2. Adicionar Object Entry Declaration.
3. Adicionar Object Reset Trigger onde fizer sentido.
4. Adicionar Transform Reset Participant no alvo ou em objeto relacionado.
5. Configurar baseline authored ou captura deterministica quando disponivel.
6. Chamar ObjectResetTrigger.RequestObjectReset() por UI/Button ou ContextMenu.
```

Inspector deve explicar:

```text
Este componente participa do Object Reset canônico.
Ele não executa reset sozinho.
Ele não recarrega cena.
Ele não controla Route/Activity lifecycle.
Ele não substitui pooling, save ou gameplay reset.
```

---

## 7. Resultado fechado

```text
ObjectResetUnityParticipantSource registra participants Unity explícitos no Runtime Host.
ObjectResetTransformParticipant restaura baseline local authored de Transform.
Required adapter ausente e required baseline ausente geram blocking issue.
Optional baseline ausente gera warning não bloqueante.
F15 closure smoke valida source, Transform reset e guardrails sem depender de GameObject.name/path.
```

## 8. Consequencias

### Positivas

- Object Reset deixa de ser apenas orquestração lógica.
- Reset de Transform fica validável por smoke real.
- Player/Actor futuro pode consumir adapters técnicos sem redefinir core.

### Custos

- A superfície Unity do framework aumenta.
- A politica de no-participants precisa ser explícita.
- Source/adapter devem ser bem diagnosticados para não virar fallback silencioso.

---

## 9. Guardrails finais

- Não usar F15 para implementar Player/Actor reset.
- Não usar F15 para pooling.
- Não usar reset físico como lifecycle paralelo.
- Não mover identity/owner/scope para components Unity.
- Não usar `GameObject.name`, path ou InstanceID como identidade funcional.
- Não fazer fallback silencioso quando adapter required estiver ausente.
- Não capturar baseline no momento do reset.
- Não criar singleton/global reset manager.
