# IF-FW-RESET-REFORM — Plano de Implementação v2

> Documento de norte para a refatoração forte do sistema de Reset do `com.immersive.framework`.
>
> Estado de referência: `v1.0.0-preview.12` em andamento, após validação de `preview.11` e após a tentativa inicial de Runtime Object Participation.
>
> Objetivo: substituir o reset baseado em `ObjectEntryDeclaration`, snapshot de `ObjectEntry` e listas manuais por um modelo único de `ResetSubject` + `ResetParticipant` + `ResetRegistry` + `ResetSelectionPolicy` + `ResetExecutor`, compatível com objetos de cena e objetos instanciados em runtime.

---

## 0. Revisão v2 — decisões fechadas

Esta v2 fecha lacunas da primeira versão do plano.

### Mudanças decisivas

```text
1. ObjectResetTargetResolver morre. Não há condição.
2. ObjectResetTarget morre. Reset não resolve mais via ObjectEntryRuntimeContextSnapshot.
3. ResetSubject core não terá ObjectEntryId tipado.
4. ResetSubjectRequiredness foi removido do modelo.
5. IResetParticipant.Reset(...) será síncrono.
6. Awaitable fica no ResetExecutor e nos triggers/orquestradores, não no participant individual.
7. UnityRuntimeObjectParticipationAdapter e RuntimeObjectParticipationRegistry atuais são legado do reset antigo e serão removidos no cleanup, salvo se antes forem recriados com outro escopo fora de Reset.
8. A geração de id runtime será decisão de arquitetura, não detalhe posterior: prefixo autorado + contador monotônico por sessão/contexto.
```

### Motivo da v2

A v1 estava correta na direção, mas deixava algumas portas abertas:

```text
- manter ObjectResetTargetResolver “se ainda fosse útil”;
- decidir depois o destino de RuntimeObjectParticipation;
- carregar ObjectEntryId tipado dentro do ResetSubject core;
- duplicar obrigatoriedade em Subject, Participant e Policy;
- tornar todo participant Awaitable mesmo quando o reset local é síncrono.
```

Essas portas agora estão fechadas.

---

## 1. Decisão executiva

A refatoração será uma **substituição arquitetural**, não um remendo incremental sobre o desenho antigo.

O reset atual funciona para objetos já existentes na cena, mas não é adequado para `FIRSTGAME` e para gameplay real porque:

```text
- o alvo resetável ainda é resolvido como ObjectEntry;
- os participants Unity ainda dependem de ObjectEntryDeclaration;
- objetos runtime não entram naturalmente no reset;
- múltiplas instâncias do mesmo prefab podem colidir por id;
- há dois motores de reset: single e group/restart;
- o authoring divergiu entre ObjectResetGroupTrigger e ActivityRestartTrigger.
```

A nova direção é:

```text
ResetSubject
  identidade resetável própria;
  não depende de ObjectEntryDeclaration;
  não depende de ObjectEntryRuntimeContextSnapshot;
  pode existir em cena ou em prefab runtime.

ResetParticipant
  comportamento local de reset;
  pertence a um ResetSubject;
  registra-se por handle;
  executa reset síncrono local;
  não aponta para ObjectEntryDeclaration.

ResetRegistry
  fonte runtime única de subjects e participants;
  suporta register/unregister por handle;
  suporta objetos de cena e runtime;
  remove stale handles defensivamente.

ResetSelectionPolicy
  escolhe ResetSubjects a partir do ResetRegistry.

ResetExecutor
  único motor de execução;
  usa UnityEngine.Awaitable no nível de orquestração;
  executa participants síncronos;
  é usado por reset unitário, reset set/group e Activity Restart.
```

### Decisões congeladas

```text
1. ResetSubjectId é identidade própria de reset.
2. ResetSubjectId não é ObjectEntryId.
3. ResetSubject core não referencia ObjectEntryId nem ObjectEntryDeclaration.
4. ObjectEntry continua existindo para presença/composição de conteúdo.
5. ResetSubject é a faceta resetável de um objeto.
6. Reset nunca resolve alvo via ObjectEntryRuntimeContextSnapshot.
7. ResetParticipant não aponta para ObjectEntryDeclaration.
8. ResetRegistry é a fonte única de verdade do módulo Reset.
9. ResetExecutor é o único motor de execução.
10. ObjectResetTrigger, ObjectResetGroupTrigger e ActivityRestartTrigger são apenas request surfaces.
11. Runtime prefab deve funcionar sem ObjectEntryDeclaration.
12. Duas instâncias do mesmo prefab devem poder coexistir e resetar sem colisão.
13. O trilho antigo deve ser removido ao final, sem compat layer.
14. ObjectResetTargetResolver e ObjectResetTarget são removidos.
15. RuntimeObjectParticipation atual não será usado como trilho de reset.
```

---

## 2. Problema atual

### 2.1 Causa raiz

Fluxo atual aproximado:

```text
ObjectResetTrigger / ObjectResetGroupTrigger / ActivityRestartTrigger
→ ObjectEntryDeclaration ou ObjectEntryId
→ ObjectEntryRuntimeContextSnapshot
→ ObjectResetTarget
→ ObjectResetUnityParticipantSource / RuntimeObjectParticipationRegistry
→ ObjectResetRuntime ou ObjectResetGroupExecutor
```

Problemas:

```text
- ObjectResetTarget é ObjectEntry disfarçado.
- ObjectResetTargetResolver existe para resolver reset via ObjectEntry snapshot; ele não existe no modelo novo.
- ObjectResetUnityParticipantBehaviour exige ObjectEntryDeclaration.
- ObjectResetTransformParticipant herda essa dependência.
- ObjectResetUnityParticipantSource exige lista manual central de participants.
- UnityRuntimeObjectParticipationAdapter mistura ObjectEntry runtime e reset participants.
- RuntimeObjectParticipationRegistry é registry de ObjectEntry/runtime participation, não ResetRegistry.
- ObjectResetRuntime e ObjectResetGroupExecutor duplicam motor.
- ObjectResetGroupTrigger e ActivityRestartTrigger têm authoring divergente.
- ObjectEntryId global não atende múltiplas instâncias do mesmo prefab.
```

### 2.2 Sintoma prático no FIRSTGAME

O smoke atual funciona para:

```text
PlayerPrototype na cena
ObjectEntryDeclaration na cena
ObjectResetTransformParticipant apontando para Target Declaration
ObjectResetUnityParticipantSource listando o participant
```

Mas não é um modelo saudável para:

```text
Prefab runtime instanciado
sem ObjectEntryDeclaration
com participant local de reset
com unregister no destroy
com múltiplas instâncias do mesmo prefab
```

O problema não é o botão e não é a cena. O problema é o modelo de reset.

---

## 3. Modelo alvo

### 3.1 Núcleo runtime

#### `ResetSubjectId`

Identidade tipada do objeto resetável.

Regras:

```text
- É opaca para decisão funcional.
- Aceita id autorado para objetos estáveis de cena.
- Suporta geração por instância para prefabs runtime.
- Não é ObjectEntryId.
- Pode ter o mesmo texto de um ObjectEntryId em objeto de cena por conveniência de authoring, mas isso não cria dependência de tipo.
```

Shape sugerido:

```csharp
public readonly struct ResetSubjectId : IEquatable<ResetSubjectId>
{
    public string Value { get; }
    public bool IsValid { get; }
}
```

#### Estratégia de id runtime

Para prefab runtime:

```text
ResetSubjectId = <authoredPrefix>#<monotonicCounter>
```

Exemplo:

```text
firstgame.runtime.box#1
firstgame.runtime.box#2
firstgame.runtime.box#3
```

Regras:

```text
- O prefixo é autorado no prefab.
- O contador é gerado pelo registry ou pelo adapter durante registration.
- O contador é monotônico na sessão atual do framework.
- Não reutilizar id dentro da mesma sessão, mesmo após destroy, para evitar stale ambiguity.
- O id gerado não é save-stable e não deve ser usado para Save Progression.
- Se no futuro houver Save/Load de instância, isso vira outro identificador persistente, fora do reset gamefirst.
```

#### `ResetSubjectScope`

Escopo lógico para seleção.

Modelo recomendado inicial:

```text
Route
Activity
Runtime
```

Definição:

```text
Route
  Subject pertence ao contexto da Route atual.

Activity
  Subject pertence ao contexto da Activity atual.

Runtime
  Subject foi instanciado ou registrado dinamicamente e não deve ser confundido com conteúdo autorado de cena.
```

Observação: `Runtime` não substitui owner/context. Um subject runtime ainda deve registrar o contexto atual de Route/Activity quando existir, para seleção por current route/current activity.

Não adicionar `Session` agora. Reset de sessão inteira não é objetivo de gamefirst.

#### `ResetSubjectOrigin`

Origem do subject.

```text
SceneAuthored
RuntimeRegistered
```

Uso:

```text
- logs;
- validators;
- diagnóstico;
- filtros futuros scene-only/runtime-only;
- stale runtime subject cleanup.
```

#### `ResetSubject`

Descriptor passivo do subject resetável.

Campos mínimos:

```text
ResetSubjectId SubjectId
ResetSubjectScope Scope
ResetSubjectOrigin Origin
FrameworkOwnerIdentity Owner
string DisplayName
string DiagnosticTag
UnityEngine.Object OwnerObject        // somente se o tipo ficar em camada Unity; não no core puro.
```

Importante:

```text
- Não incluir ObjectEntryId tipado no core ResetSubject.
- Não incluir ObjectEntryDeclaration.
- Não incluir ResetSubjectRequiredness.
```

`DiagnosticTag` é string genérica para rastreabilidade, por exemplo:

```text
ObjectEntry:firstgame.player
Prefab:FG_RuntimeBox
Scene:FG_Gameplay
```

Mas o core Reset não deve depender de tipos de `ObjectEntry`.

#### `ResetParticipantId`

Identidade tipada do participant local.

Pode reaproveitar a ideia de `ObjectResetParticipantId`, mas no namespace/modelo novo.

#### `ResetParticipantRequiredness`

Obrigatoriedade fica no participant, não no subject.

```text
Required
Optional
```

Uso:

```text
- participant required falha bloqueia o reset do subject;
- participant optional pode virar warning/skip conforme resultado;
- subject sem participants é tratado pela policy AllowNoParticipants.
```

#### `ResetParticipantDescriptor`

Campos mínimos:

```text
ResetParticipantId ParticipantId
ResetSubjectId SubjectId
ResetParticipantRequiredness Requiredness
int Order
string DisplayName
string Source
string Reason
```

#### `IResetParticipant`

Contrato novo, síncrono.

Shape conceitual:

```csharp
public interface IResetParticipant
{
    bool TryCreateResetParticipantDescriptor(
        ResetSubject subject,
        out ResetParticipantDescriptor descriptor,
        out ResetIssue issue);

    ResetParticipantResult Reset(ResetContext context);
}
```

Decisão:

```text
- Participant individual não retorna Awaitable.
- Transform reset e GameObject active reset são operações síncronas.
- Awaitable fica no ResetExecutor para orquestração, yielding e integração com triggers.
- Se no futuro surgir participant realmente async, criar outro contrato ou evoluir com caso concreto.
```

#### `ResetRegistrationHandle`

Handle opaco para unregister determinístico.

Regras:

```text
- Não expõe índice interno.
- Identifica subject ou participant registrado.
- Unregister é idempotente.
- Unregister de handle inválido gera warning estruturado, não crash.
```

#### `ResetRegistry`

Fonte única runtime do módulo Reset.

Responsabilidades:

```text
- registrar ResetSubject;
- registrar ResetParticipant sob um ResetSubject;
- gerar id runtime por prefixo + contador monotônico;
- rejeitar duplicidade indevida;
- manter relação subject → participants;
- consultar por id;
- consultar por scope/current route/current activity;
- limpar stale owner defensivamente;
- unregister por handle;
- produzir snapshots de reset sem depender de ObjectEntryRuntimeContextSnapshot.
```

Regras de duplicidade:

```text
- Subject scene-authored com mesmo id no mesmo contexto deve ser rejeitado.
- Subject runtime com prefixo igual gera id por instância; não deve colidir.
- Dois runtime subjects não devem compartilhar o mesmo id gerado.
- Id textual manual duplicado deve ser rejeitado com log claro.
```

---

## 4. Relação com ObjectEntry

### Decisão

```text
ObjectEntry = presença/composição de conteúdo.
ResetSubject = capacidade resetável.
```

Um objeto pode ter:

```text
- ObjectEntryDeclaration sem ResetSubject;
- ResetSubject sem ObjectEntryDeclaration;
- ambos;
- nenhum.
```

Reset não depende de `ObjectEntryDeclaration` e não consulta `ObjectEntryRuntimeContextSnapshot`.

### Ponte opcional de authoring

A ponte com ObjectEntry deve existir apenas em camada Unity/Editor, como conveniência.

Exemplo permitido:

```text
UnityResetSubjectAdapter
  botão: Derive Subject Id From ObjectEntryDeclaration
```

ou comportamento editor-only:

```text
Se houver ObjectEntryDeclaration no mesmo GameObject,
o Inspector pode sugerir ResetSubjectId igual ao ObjectEntryId textual.
```

Mas o core não deve ter:

```text
ObjectEntryId? LinkedObjectEntryId
```

Use no máximo:

```text
string DiagnosticTag
```

para log/rastreabilidade.

---

## 5. Relação com Runtime Object Participation

### Decisão fechada

As classes atuais da tentativa F44:

```text
UnityRuntimeObjectParticipationAdapter
RuntimeObjectParticipationRegistry
RuntimeObjectParticipationRecord
RuntimeObjectParticipationHandle
CompositeObjectResetParticipantSource, se existir apenas para o reset antigo
```

são consideradas **legado do caminho errado de reset** e devem ser removidas no cleanup `preview.12G`, se ainda existirem com a responsabilidade atual.

### Motivo

Elas misturam:

```text
- ObjectEntry runtime;
- reset participants;
- tentativa de participação materializada;
- source de reset.
```

Isso reintroduz o problema que a refatoração quer remover.

### Regra futura

O conceito de runtime participation/materialization pode voltar depois, mas separado:

```text
RuntimeObjectParticipation
  presença/materialização/participação geral do framework.

ResetSubject
  resetabilidade.
```

A regra é:

```text
Reset não depende de RuntimeObjectParticipation.
RuntimeObjectParticipation não registra reset participants.
Ambos podem coexistir no mesmo prefab, mas não são o mesmo sistema.
```

Para esta refatoração, `UnityResetSubjectAdapter` resolve o problema de reset runtime.

---

## 6. Authoring alvo

### 6.1 Objeto de cena resetável

Exemplo:

```text
PlayerPrototype
  UnityResetSubjectAdapter
  UnityTransformResetParticipant
```

`UnityResetSubjectAdapter`:

```text
Subject Id = firstgame.player
Scope = Route ou Activity, conforme uso real
Origin = SceneAuthored
Display Name = Player
Id Generation = AuthoredStableId
Diagnostic Tag = ObjectEntry:firstgame.player, opcional/string
```

`UnityTransformResetParticipant`:

```text
Participant Id = transform
Requiredness = Required
Order = 0
Capture Baseline / Baseline fields
```

Campos que desaparecem:

```text
Target Declaration
ObjectEntryDeclaration reference
manual source list
```

### 6.2 Objeto runtime prefab

Exemplo:

```text
FG_RuntimeBox.prefab
  UnityResetSubjectAdapter
  UnityTransformResetParticipant
```

`UnityResetSubjectAdapter`:

```text
Subject Id Prefix = firstgame.runtime.box
Scope = Activity ou Runtime, conforme smoke
Origin = RuntimeRegistered
Id Generation = RuntimeInstanceId
Display Name = Runtime Box
```

Id gerado em runtime:

```text
firstgame.runtime.box#1
firstgame.runtime.box#2
```

`UnityTransformResetParticipant`:

```text
Participant Id = transform
Requiredness = Required
Order = 0
```

O prefab runtime não deve ter:

```text
ObjectEntryDeclaration
ObjectResetUnityParticipantSource
UnityRuntimeObjectParticipationAdapter antigo
lista manual de participants
```

### 6.3 Botão Reset Player

```text
Button_ResetPlayer
  ObjectResetTrigger
```

Authoring alvo:

```text
Target Mode = ExplicitSubject
Target Subject = referência para UnityResetSubjectAdapter do Player
Reason = firstgame.reset.player
Allow No Subjects = false
Allow No Participants = false
```

Alternativa textual permitida:

```text
Target Subject Id = firstgame.player
```

Referência direta ao adapter é preferível em cena.

### 6.4 Botão Reset Room

```text
Button_ResetRoom
  ObjectResetGroupTrigger
```

Authoring alvo:

```text
Selection Mode = CurrentRouteAndActivitySubjects
Reason = firstgame.reset.room
Allow No Subjects = false
Allow No Participants = false
Stop On Failure = true, salvo smoke específico contrário
```

`ObjectResetGroupTrigger` deve expor as mesmas policies que `ActivityRestartTrigger`, não apenas `ExplicitTargets`.

### 6.5 Botão Restart Activity

```text
Button_RestartActivity
  ActivityRestartTrigger
```

Authoring alvo:

```text
Target Activity = vazio
Use Current Activity When Target Missing = true
Require Target Activity Is Current = true
Reason = firstgame.restart.activity
Reset Selection Mode = CurrentRouteAndActivitySubjects
Allow No Subjects = false
Allow No Participants = false
Stop On Failure = true
```

O restart continua sendo:

```text
ResetExecutor
→ RestartActivityAsync
→ Clear + Reenter com uma única transição visual
```

---

## 7. ResetSelectionPolicy

### 7.1 Modelo mínimo

Manter enum simples, sem criar linguagem de filtros complexa.

Valores recomendados:

```text
ExplicitSubjects
CurrentActivitySubjects
CurrentRouteSubjects
CurrentRouteAndActivitySubjects
AllCurrentSubjects
RuntimeOnlySubjects
SceneOnlySubjects
```

Observação:

```text
RuntimeOnlySubjects e SceneOnlySubjects são úteis para smoke e diagnóstico.
Se ficarem pesados para o primeiro corte, podem entrar em 12D ou 12F, mas não devem depender de ObjectEntry.
```

### 7.2 Policy inline e asset

Para evitar complexidade inicial:

```text
- triggers podem ter policy inline;
- asset compartilhável pode ser criado depois ou junto do rewrite se já for simples;
- não é obrigatório criar ScriptableObject no 12A.
```

Shape conceitual:

```text
ResetSelectionMode Mode
List<ResetSubjectReference> ExplicitSubjects
bool AllowNoSubjects
bool AllowNoParticipants
bool StopOnFailure
```

### 7.3 Subject sem participants

Não existe `ResetSubjectRequiredness`.

O caso é controlado por:

```text
AllowNoParticipants
```

Resultado esperado:

```text
- AllowNoParticipants = false → subject sem participants falha o reset.
- AllowNoParticipants = true → subject sem participants gera skip/info/warning não bloqueante.
```

---

## 8. ResetExecutor

### 8.1 Responsabilidade

`ResetExecutor` substitui:

```text
ObjectResetRuntime
ObjectResetGroupExecutor
```

Responsabilidades:

```text
- receber uma policy/selection resolvida;
- consultar ResetRegistry;
- ordenar subjects e participants;
- executar participants;
- respeitar Required/Optional;
- respeitar AllowNoSubjects/AllowNoParticipants;
- respeitar StopOnFailure;
- agregar resultados;
- produzir logs/result objects compatíveis com o padrão atual.
```

### 8.2 Async boundary

O executor usa `UnityEngine.Awaitable`.

Shape conceitual:

```csharp
public Awaitable<ResetExecutionResult> ExecuteAsync(ResetExecutionRequest request)
```

Mas o participant individual é síncrono:

```csharp
ResetParticipantResult Reset(ResetContext context)
```

O executor pode:

```text
- yieldar entre subjects, se necessário;
- permanecer determinístico;
- não usar Task.Delay;
- não empurrar Awaitable para todo participant local.
```

---

## 9. Fluxos alvo

### 9.1 Objeto de cena

```text
Scene object enabled
→ UnityResetSubjectAdapter registers ResetSubject
→ UnityTransformResetParticipant is discovered locally or registers under subject
→ ResetRegistry stores subject + participant
→ ResetSelectionPolicy can find it
```

### 9.2 Runtime spawn

```text
Prefab instantiated
→ UnityResetSubjectAdapter generates ResetSubjectId from prefix + monotonic counter
→ registers ResetSubject with current owner context
→ local participants register under subject
→ ResetRegistry stores handles
→ reset works
→ object disabled/destroyed
→ handles unregister
→ stale cleanup has nothing to reset
```

### 9.3 Reset single

```text
ObjectResetTrigger
→ builds ExplicitSubjects selection
→ ResetExecutor.ExecuteAsync
→ participant Reset(context)
→ aggregate result
→ structured log
```

### 9.4 Reset group

```text
ObjectResetGroupTrigger
→ ResetSelectionPolicy
→ ResetExecutor.ExecuteAsync
→ aggregate result
→ structured group log
```

### 9.5 Activity restart

```text
ActivityRestartTrigger
→ ResetSelectionPolicy
→ ResetExecutor.ExecuteAsync
→ FrameworkRuntimeHost.RestartActivityAsync
→ GameFlowRuntime.RestartActivityAsync
→ one visual transition
→ structured restart log
```

---

## 10. Logs e diagnostics

Manter padrão atual:

```text
source
reason
status
subjectId
scope
origin
participants
participantSucceeded
participantSkipped
participantFailed
blockingIssues
nonBlockingIssues
```

Logs esperados:

### Subject registered

```text
Reset Subject registered.
status='Registered'
subjectId='firstgame.runtime.box#1'
scope='Activity'
origin='RuntimeRegistered'
participants='1'
```

### Subject unregistered

```text
Reset Subject unregistered.
status='Unregistered'
subjectId='firstgame.runtime.box#1'
reason='on-disable'
```

### Duplicate subject rejected

```text
Reset Subject registration rejected.
status='RejectedDuplicateSubjectId'
subjectId='firstgame.player'
scope='Route'
```

### Reset completed

```text
Object Reset Request completed.
status='Succeeded'
subjectId='firstgame.player'
participants='1'
participantSucceeded='1'
blockingIssues='0'
```

### Reset group completed

```text
Object Reset Group Request completed.
status='Succeeded'
selectionMode='CurrentRouteAndActivitySubjects'
subjects='2'
subjectSucceeded='2'
participants='2'
participantSucceeded='2'
blockingIssues='0'
```

### Activity restart completed

```text
Activity Restart Request completed.
status='Succeeded'
resetStatus='Succeeded'
resetSubjects='2'
resetSubjectSucceeded='2'
clearStatus='Succeeded'
reenterStatus='Succeeded'
```

---

## 11. Validators

### 11.1 Editor authoring validation

Validar:

```text
UnityResetSubjectAdapter sem id/prefix válido
UnityResetSubjectAdapter runtime sem id generation válido
UnityResetParticipantBehaviour sem UnityResetSubjectAdapter no mesmo GameObject ou parent configurado
Reset participant com duplicate participant id no mesmo subject
ObjectResetTrigger sem target/policy
ObjectResetGroupTrigger com ExplicitSubjects vazio
ActivityRestartTrigger com ExplicitSubjects vazio
ActivityRestartTrigger com seleção que pode retornar zero subjects e AllowNoSubjects=false
```

### 11.2 Runtime defensive validation

Validar/rejeitar com logs:

```text
duplicate ResetSubjectId no mesmo contexto
participant órfão sem subject
unregister handle inválido
subject stale owner destroyed
participant stale owner destroyed
subject sem participants quando AllowNoParticipants=false
selection vazia quando AllowNoSubjects=false
```

---

## 12. Plano de cortes

A implementação deve ser feita em cortes pequenos, mas o plano final remove o trilho antigo. Não manter dois motores ao final.

### Checkpoints de revisão recomendados

Para velocidade sem perder segurança:

```text
Checkpoint 1: preview.12A + preview.12B
Checkpoint 2: preview.12C + preview.12D
Checkpoint 3: preview.12E + preview.12F
Checkpoint 4: preview.12G cleanup final
```

Ainda assim, cada subcorte deve ser commitável isoladamente se necessário.

---

### preview.12A — Reset Subject Registry

Objetivo:

```text
Criar o núcleo novo isolado do Reset sem integrar triggers antigos.
```

Escopo framework:

```text
Runtime/Reset/
  ResetSubjectId
  ResetSubjectScope
  ResetSubjectOrigin
  ResetSubject
  ResetParticipantId
  ResetParticipantRequiredness
  ResetParticipantDescriptor
  ResetRegistrationHandle
  ResetRegistry
  ResetIssue
  ResetResult primitives
```

Não fazer:

```text
- não tocar ObjectResetTrigger;
- não tocar ActivityRestartTrigger;
- não remover classes antigas ainda;
- não consultar ObjectEntryRuntimeContextSnapshot;
- não adicionar ObjectEntryId ao ResetSubject core.
```

Smoke/teste esperado:

```text
- registrar subject scene-authored;
- registrar subject runtime com prefix + contador;
- registrar participant;
- consultar por id;
- consultar por scope;
- unregister remove;
- duplicate id rejeita;
- stale owner cleanup não quebra.
```

Critério de PASS:

```text
ResetRegistry funciona isolado e não depende de ObjectEntry.
```

---

### preview.12B — Unity Reset Subject + Participants

Objetivo:

```text
Criar authoring Unity novo para subject e participants locais, sem ObjectEntryDeclaration.
```

Escopo framework:

```text
Runtime/Reset/Unity/
  UnityResetSubjectAdapter
  UnityResetParticipantBehaviour
  UnityTransformResetParticipant
  UnityGameObjectActiveResetParticipant
```

Decisões:

```text
- UnityResetSubjectAdapter registra subject em OnEnable.
- Unregister em OnDisable/OnDestroy é determinístico e idempotente.
- Adapter descobre participants locais via GetComponents no mesmo GameObject ou children configurados.
- Participant não possui Target Declaration.
- Participant usa subject fornecido pelo adapter.
```

Não fazer:

```text
- não criar PlayerActor;
- não criar spawner oficial;
- não criar Save identity;
- não depender de UnityRuntimeObjectParticipationAdapter antigo.
```

Smoke esperado:

```text
- objeto de cena registra subject + participant;
- prefab runtime registra subject + participant;
- duas instâncias do mesmo prefab geram ids diferentes;
- destroy/unregister remove uma instância;
- registry não guarda stale subject ativo.
```

Critério de PASS:

```text
Prefab runtime reseta no nível de registry sem ObjectEntryDeclaration.
```

---

### preview.12C — Reset Executor Rewrite

Objetivo:

```text
Criar ResetExecutor único e mover execução para o novo registry.
```

Escopo framework:

```text
Runtime/Reset/
  ResetExecutor
  ResetExecutionRequest
  ResetExecutionResult
  ResetSubjectResult
  ResetParticipantResult
  ResetContext
```

Substitui progressivamente:

```text
ObjectResetRuntime
ObjectResetGroupExecutor
```

Não fazer:

```text
- não manter lógica duplicada de execução;
- não resolver via ObjectResetTargetResolver;
- não resolver via ObjectEntryRuntimeContextSnapshot;
- não fazer participant Awaitable.
```

Smoke esperado:

```text
- ExecuteAsync resetando 1 subject;
- ExecuteAsync resetando N subjects;
- StopOnFailure respeitado;
- AllowNoSubjects respeitado;
- AllowNoParticipants respeitado;
- Required/Optional participant respeitado.
```

Critério de PASS:

```text
ResetExecutor é o único motor novo e produz resultados agregados suficientes para logs atuais.
```

---

### preview.12D — Trigger Rewrite

Objetivo:

```text
Reescrever ObjectResetTrigger e ObjectResetGroupTrigger como request surfaces sobre ResetSelectionPolicy + ResetExecutor.
```

Escopo framework:

```text
Runtime/Reset/
  ObjectResetTrigger
  ObjectResetGroupTrigger
  ResetSelectionPolicy ou ResetSelectionConfig
  ResetSelectionMode
  ResetSubjectReference
```

Mudanças:

```text
ObjectResetTrigger
  target = UnityResetSubjectAdapter reference ou ResetSubjectId textual.

ObjectResetGroupTrigger
  expõe todos os selection modes, não apenas explicit targets.
```

Remover dos triggers:

```text
ObjectEntryDeclaration targetDeclaration
ObjectEntryId como alvo primário
ObjectResetGroupEntry baseado em ObjectEntryDeclaration
```

Não fazer:

```text
- trigger chamando outro trigger;
- ObjectResetGroupTrigger usando motor diferente do ObjectResetTrigger;
- fallback silencioso para ObjectEntry antigo.
```

Smoke esperado:

```text
Button_ResetPlayer
  ObjectResetTrigger → explicit subject player

Button_ResetRoom
  ObjectResetGroupTrigger → CurrentRouteAndActivitySubjects
```

Critério de PASS:

```text
Reset single e group funcionam com subjects de cena e runtime usando o mesmo executor.
```

---

### preview.12E — ActivityRestart Integration

Objetivo:

```text
Migrar ActivityRestartTrigger para ResetSelectionPolicy + ResetExecutor, preservando a transição única.
```

Escopo framework:

```text
Runtime/ActivityRestart/ActivityRestartTrigger.cs
Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs, se necessário
Runtime/GameFlow/GameFlowRuntime.cs, somente se necessário
```

Não fazer:

```text
- não reabrir lifecycle de Activity;
- não recriar clear/reenter público separado;
- não duplicar executor;
- não usar ObjectResetGroupTrigger.
```

Smoke esperado:

```text
Restart Activity
→ ResetExecutor reseta selected subjects
→ RestartActivityAsync executa clear + reenter interno
→ uma única transição visual
```

Critério de PASS:

```text
Activity Restart Request completed.
status='Succeeded'
resetStatus='Succeeded'
clearStatus='Succeeded'
reenterStatus='Succeeded'
```

Sem logs públicos separados `:clear` e `:reenter`.

---

### preview.12F — Runtime Prefab Smoke

Objetivo:

```text
Provar o problema principal: runtime prefabs participam do reset corretamente.
```

Escopo FIRSTGAME:

```text
Assets/_Project/Prefabs/Runtime/FG_RuntimeBox.prefab
Assets/_Project/Scripts/FirstGame/FirstGameRuntimeObjectSpawner.cs, se ainda for necessário
FG_Gameplay.unity
```

Configuração alvo:

```text
FG_RuntimeBox.prefab
  UnityResetSubjectAdapter
  UnityTransformResetParticipant
```

Smoke:

```text
1. Entrar em Play.
2. Start Game.
3. Spawner instancia duas Runtime Boxes.
4. Cada box registra ResetSubjectId único.
5. Mover/alterar as boxes.
6. Reset Room reseta ambas.
7. Destroy uma box.
8. Reset Room não tenta resetar stale subject.
9. Restart Activity reseta os subjects restantes e mantém uma transição visual.
```

Critério de PASS:

```text
- duas instâncias do mesmo prefab coexistem;
- ids não colidem;
- reset group reseta ambas;
- unregister remove instância destruída;
- nenhum stale participant é executado;
- ActivityRestart continua PASS.
```

---

### preview.12G — Cleanup Old Reset Path

Objetivo:

```text
Remover definitivamente o trilho antigo e atualizar docs/validators/guias.
```

Remover/substituir:

```text
ObjectResetTarget
ObjectResetTargetResolver
ObjectResetUnityParticipantBehaviour antigo
ObjectResetUnityParticipantSource
ObjectResetRuntime antigo
ObjectResetGroupExecutor antigo
ObjectResetGroupAsset/ObjectResetGroupEntry antigos, se substituídos por ResetSelectionPolicy
UnityRuntimeObjectParticipationAdapter atual
RuntimeObjectParticipationRegistry atual
RuntimeObjectParticipationRecord atual
RuntimeObjectParticipationHandle atual
CompositeObjectResetParticipantSource, se só existir para o modelo antigo
Editor antigo baseado em ObjectEntryDeclaration
Validators antigos baseados em ObjectEntryDeclaration
```

Preservar com novo shape:

```text
ObjectResetTrigger
ObjectResetGroupTrigger
ActivityRestartTrigger
Transform reset participant concept
GameObject active reset participant concept
structured logs
Authoring Validation entrypoint
```

Atualizar:

```text
Documentation~/ADRs/ADR-INDEX.md
Documentation~/README.md
Documentation~/Guides/Usage/README.md
Reset/Restart guides
Authoring validators
FIRSTGAME usage notes
```

Critério de PASS:

```text
- build limpo;
- sem referências residuais ao reset via ObjectEntryDeclaration;
- sem ObjectResetTargetResolver;
- sem ObjectResetTarget;
- FIRSTGAME smoke completo PASS;
- docs não descrevem o trilho antigo como caminho canônico.
```

---

## 13. Destino das classes atuais

### Remover definitivamente

```text
ObjectResetTarget
ObjectResetTargetResolver
ObjectResetUnityParticipantBehaviour, na forma antiga
ObjectResetUnityParticipantSource
ObjectResetRuntime, substituído por ResetExecutor
ObjectResetGroupExecutor, substituído por ResetExecutor
UnityRuntimeObjectParticipationAdapter atual
RuntimeObjectParticipationRegistry atual
RuntimeObjectParticipationRecord atual
RuntimeObjectParticipationHandle atual
CompositeObjectResetParticipantSource, se só agregar fontes antigas
```

### Manter nome, trocar implementação

```text
ObjectResetTrigger
ObjectResetGroupTrigger
ActivityRestartTrigger
```

### Manter conceito, trocar base/nome se necessário

```text
ObjectResetTransformParticipant → UnityTransformResetParticipant ou manter nome com nova base
ObjectResetGameObjectActiveParticipant → UnityGameObjectActiveResetParticipant ou manter nome com nova base
```

Recomendação de nome:

```text
Manter ObjectResetTransformParticipant se quisermos reduzir churn de cena,
mas remover TargetDeclaration e trocar a base.
```

Se renomear, fazer agora, porque não há produção.

---

## 14. Regras de implementação

```text
1. Não criar PlayerActor.
2. Não criar Actor lifecycle.
3. Não criar Save Progression.
4. Não criar Inventory.
5. Não criar NPC system.
6. Não criar CycleReset completo.
7. Não criar Spawner oficial completo.
8. Não criar Pooling/Addressables.
9. Não usar Time.timeScale para transition.
10. Não usar Task.Delay.
11. Não criar service locator/singleton novo.
12. Não manter compat layer do reset antigo.
13. Não manter dois motores de reset ao final.
14. Não deixar participant preso a ObjectEntryDeclaration.
15. Não colocar ObjectEntryId tipado no core ResetSubject.
16. Não fazer IResetParticipant async por padrão.
```

---

## 15. Prompt de handoff para novo chat de implementação

Use este prompt no novo chat:

```text
Vamos implementar a refatoração IF-FW-RESET-REFORM seguindo o documento `IF-FW-RESET-REFORM-Plano-Implementacao-v2.md`.

Regras principais:
- Implementar corte pequeno, começando por preview.12A.
- Não implementar a refatoração inteira de uma vez.
- ResetSubjectId é identidade própria e não é ObjectEntryId.
- ResetSubject core não deve referenciar ObjectEntryId nem ObjectEntryDeclaration.
- ObjectResetTarget e ObjectResetTargetResolver serão removidos no novo modelo.
- ResetSubjectRequiredness não existe.
- Obrigatoriedade fica em ResetParticipantRequiredness e nas policies AllowNoSubjects/AllowNoParticipants.
- IResetParticipant.Reset é síncrono.
- Awaitable fica no ResetExecutor/triggers, não no participant individual.
- Runtime id generation usa prefixo autorado + contador monotônico por sessão/contexto.
- UnityRuntimeObjectParticipationAdapter/RuntimeObjectParticipationRegistry atuais são legado do caminho antigo e serão removidos no cleanup, não usados como base do novo reset.
- Não criar PlayerActor, Save, Inventory, NPC, CycleReset ou spawner oficial.

Primeiro corte:
preview.12A — Reset Subject Registry.

Objetivo do 12A:
Criar o núcleo isolado em Runtime/Reset sem tocar ainda nos triggers antigos.

Entregar:
- arquivos novos do núcleo;
- explicação curta;
- smoke/teste esperado;
- sem editar Route/Activity/Pause/Transition.
```

---

## 16. Recomendação final

A arquitetura final deve combinar:

```text
Base forte:
  ResetSubject independente de ObjectEntry.

Execução simples:
  participants síncronos;
  executor Awaitable.

Implementação segura:
  cortes pequenos;
  cleanup obrigatório;
  sem compat layer.
```

A primeira implementação deve começar por:

```text
preview.12A — Reset Subject Registry
```

Mas a primeira validação realmente importante será:

```text
preview.12B + preview.12F
```

porque é onde o framework prova que runtime prefab pode ser resetado sem `ObjectEntryDeclaration` e sem colisão entre múltiplas instâncias.
