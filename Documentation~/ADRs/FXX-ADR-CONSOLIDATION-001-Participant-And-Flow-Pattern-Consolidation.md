# FXX-ADR-CONSOLIDATION-001 — Participant & Flow Pattern Consolidation

Status: Proposed / audit-only / documentation governance / revised
Fase: candidata — número de fase a definir pelo roadmap; não reivindica F34 nem slot oficial
Tipo: Core / Cross-cutting / Refactor Governance
Última atualização: 2026-06-30

> Este ADR registra uma decisão de direção. Ele não implementa código e não autoriza alteração por si só.
> A implementação precisa seguir o plano associado `FXX-PLAN — Participant & Flow Pattern Consolidation Plan`.

---

## 1. Contexto

O framework chegou a um ponto em que a disciplina de contrato já é visível e útil:

```text
Request / Plan / Result / Status / Issue
readonly struct
validação no construtor
diagnostics estruturados
fontes explícitas
smokes por fase
```

Essa disciplina deve ser preservada. Ela é parte do motivo pelo qual o lifecycle do framework é previsível.

O problema identificado pela auditoria não é falta de arquitetura. É repetição manual da mesma arquitetura em domínios diferentes. O padrão de participante foi recriado em `CycleReset`, `ObjectReset`, `Snapshot` e `ActivityContentExecution` com estruturas muito parecidas. Isso já gerou drift real: `CycleResetRuntime` valida o resultado retornado por participante contra a entrada/request original, enquanto `ObjectResetRuntime` não faz validação simétrica equivalente.

Além disso, `ActivityRequestTrigger` e `RouteRequestTrigger` compartilham um fluxo muito parecido de request/evento/estado de execução. Porém, como são `MonoBehaviour` com campos serializados, a consolidação desse par é mais arriscada do que a consolidação dos executores de participante.

Este ADR propõe uma consolidação conservadora: primeiro criar primitivos comuns internos em `Common/`, depois migrar apenas `CycleReset` e `ObjectReset` como pilotos. A consolidação de `Snapshot`, `ActivityContentExecution`, `LocalContribution` e triggers de fluxo fica bloqueada até o fechamento da fase piloto.

---

## 2. Dor original

Objetivo original do framework:

```text
Base reutilizável para diversos jogos, com lifecycles controlados e atividades fáceis de replicar.
```

Hoje, quando um novo domínio precisa do padrão “participante”, a tendência é copiar um conjunto de arquivos e trocar o prefixo:

```text
I<Domain>Participant
I<Domain>ParticipantSource
<Domain>ParticipantDescriptor
<Domain>ParticipantEntry
<Domain>ParticipantId
<Domain>ParticipantRequiredness
<Domain>ParticipantResult
<Domain>ParticipantResultStatus
Empty<Domain>ParticipantSource
executor/runtime específico
issues/status agregados
```

Isso cria três problemas:

```text
1. Custo de manutenção: bug fix em um executor não se propaga para os outros.
2. Custo de onboarding: o leitor precisa entender o mesmo algoritmo repetido em várias pastas.
3. Custo de evolução: cada domínio novo tende a nascer por cópia, não por composição.
```

A consolidação proposta não resolve sozinha a UX de uso no Inspector. Ela resolve primeiro o problema de manutenção interna do core.

---

## 3. Evidência de duplicação

### 3.1. Padrão Participant confirmado

A auditoria identificou o padrão de participante nos seguintes domínios:

```text
CycleReset
ObjectReset
Snapshot
ActivityFlow / ActivityContentExecution
```

Nesses domínios, aparecem combinações recorrentes de:

```text
I<Domain>Participant
I<Domain>ParticipantSource
<Domain>ParticipantDescriptor
<Domain>ParticipantEntry
<Domain>ParticipantId
<Domain>ParticipantRequiredness
<Domain>ParticipantResult
<Domain>ParticipantResultStatus
Empty<Domain>ParticipantSource
```

`CycleResetParticipantDescriptor` e `ObjectResetParticipantDescriptor` são os melhores pilotos porque têm shape muito parecido, mas alvos diferentes:

```text
CycleReset  -> CycleResetScope
ObjectReset -> ObjectResetTarget resolvido pelo domínio
```

Isso permite testar se uma abstração comum cobre um alvo simples e um alvo resolvido por domínio, sem tocar em domínios mais complexos.

### 3.2. Correção sobre LocalContribution

A versão anterior do ADR tratava `LocalContribution` como se ele fosse mais um domínio completo do padrão Participant. Essa afirmação fica corrigida.

No pacote atual, `LocalContribution` compartilha padrões de resultado, issue, set, handle, requiredness e validation, mas não deve ser tratado como equivalente direto a:

```text
ICycleResetParticipant
IObjectResetParticipant
ISnapshotParticipant
IActivityContentExecutionParticipant
```

Portanto:

```text
LocalContribution NÃO é piloto desta fase.
LocalContribution NÃO deve ser migrado por este ADR.
LocalContribution pode ser reavaliado depois, se um contrato real de participante local existir ou se a consolidação de Result/Issue/Validation provar valor suficiente.
```

### 3.3. Requiredness não é semanticamente idêntico em todos os domínios

A versão anterior também dizia que o enum de requiredness era idêntico nos 5 domínios. Isso precisa ser limitado.

Confirmado como equivalente em intenção e valores:

```text
CycleResetParticipantRequiredness: Unknown=0, Required=10, Optional=20
ObjectResetParticipantRequiredness: Unknown=0, Required=10, Optional=20
SnapshotParticipantRequiredness: Unknown=0, Required=10, Optional=20
```

Não equivalente em valores:

```text
ActivityContentExecutionRequiredness: Unknown=0, Optional=10, Required=20
```

Decisão normativa:

```text
Nenhum enum público de domínio será substituído por ParticipantRequiredness comum nesta fase.
O Common pode ter uma representação interna de requiredness para execução genérica.
Cada domínio deve mapear seu enum público para o primitivo comum internamente.
```

Isso preserva API pública, serialização e semântica documental de cada domínio.

### 3.4. Divergência real entre CycleReset e ObjectReset

`CycleResetRuntime.ExecutePlan` valida se o resultado retornado pelo participante combina com a entrada esperada. A validação cobre, no mínimo:

```text
request
participantId
scope/target
requiredness
```

`ObjectResetRuntime.Execute` não aplica validação simétrica equivalente antes de aceitar o resultado do participante.

Essa diferença não parece ser intencional. Ela é o tipo de drift que aparece quando o mesmo algoritmo é mantido em cópias separadas.

### 3.5. Padrão Flow espelhado Activity ↔ Route

Também existe duplicação entre:

```text
ActivityRequestTrigger
RouteRequestTrigger
```

Ambos compartilham:

```text
request in-flight
publicação de evento submitted/completed
mapeamento de outcome
state textual para Inspector/diagnostics
uso de EventBus local ao trigger
```

Diferença relevante:

```text
ActivityRequestTrigger também tem ClearActivity().
```

Porém, por serem `MonoBehaviour` com campos `[SerializeField]`, a consolidação via classe base genérica pode afetar Inspector e serialização Unity. Essa frente deve ser tratada como candidata separada, não como parte obrigatória do primeiro piloto.

### 3.6. Common subutilizado

`Runtime/Common/` ainda está muito pequeno diante dos padrões recorrentes do framework. Hoje ele concentra basicamente normalização de string. Isso é incompatível com o volume de padrões repetidos em:

```text
Participants
Results
Issues
Requiredness
Validation helpers
Execution loops
```

A proposta é aumentar `Common/` de forma aditiva e controlada, sem transformar `Common` em service locator, registry global ou domínio genérico demais.

---

## 4. Decisão proposta

Aceitar uma fase de consolidação interna com escopo reduzido:

```text
1. Criar primitivos comuns internos/aditivos em Runtime/Common/.
2. Criar um executor genérico de participantes, testado primeiro com participantes sintéticos.
3. Migrar CycleReset internamente para compor os genéricos.
4. Migrar ObjectReset internamente para compor os genéricos.
5. Fechar a fase com evidência de smoke e decisão explícita sobre continuar ou parar.
```

A fase não busca alterar o comportamento observado do framework. O objetivo é reduzir duplicação interna, impedir drift e preservar a API pública existente.

---

## 5. Escopo incluído

Quando esta fase for aceita e cortada, o escopo permitido é:

```text
Common/Results/OperationResult<TStatus>
Common/Validation helpers mínimos para enum/status/texto
Common/Participants/ParticipantDescriptor<TId, TTarget>
Common/Participants/ParticipantEntry<TParticipant, TDescriptor>
Common/Participants/ParticipantExecutor<...>
Representação comum interna de requiredness, com mapeamento por domínio
Smokes sintéticos de Common, sem depender de domínio real
Migração interna de CycleReset
Migração interna de ObjectReset
Closeout documental com evidência de paridade
```

Observações normativas:

```text
- TId continua sendo o ID fortemente tipado do domínio.
- TTarget continua sendo o alvo específico do domínio.
- O domínio continua dono do vocabulário público: Status, Issue, Request, Result, Requiredness público.
- Common executa algoritmo compartilhado; não decide semântica de domínio.
```

---

## 6. Escopo excluído

Esta fase não autoriza:

```text
Migração de Snapshot.
Migração de ActivityContentExecution.
Migração de LocalContribution.
Substituição de enum público de requiredness por enum comum.
Mudança de valores de enum público.
Mudança de assinatura pública usada por consumers/adapters.
Mudança de status codes, ordem de execução ou mensagens esperadas de smoke.
Remoção de arquivos antigos antes da paridade dos smokes.
Unificação de Activity/Route SceneComposition.
Unificação de Activity/Route ContentLifecycle.
Transformar FlowRequestTrigger em base MonoBehaviour genérica nesta primeira leva.
Qualquer novo singleton, service locator ou reflection.
Qualquer trabalho de gameplay, player, actor, camera, audio, pooling ou adapter modules.
Qualquer afirmação de que esta fase resolve UX de authoring.
```

Se um corte tentar tocar nesses itens, ele deve ser rejeitado como antecipação de escopo.

---

## 7. Estratégia para Requiredness

Requiredness deve ser tratado como semântica de domínio, não como enum universal público.

Permitido:

```text
internal ParticipantRequirednessLike / CommonParticipantRequiredness
mappers explícitos por domínio
helpers comuns para validar Unknown/Required/Optional
executor recebendo requiredness já normalizado
```

Proibido:

```text
trocar CycleResetParticipantRequiredness por Common.ParticipantRequiredness publicamente
trocar ObjectResetParticipantRequiredness por Common.ParticipantRequiredness publicamente
trocar SnapshotParticipantRequiredness por Common.ParticipantRequiredness publicamente
assumir que ActivityContentExecutionRequiredness tem os mesmos valores dos outros
serializar enum comum em componentes/assets existentes sem ADR próprio
```

Essa regra existe para evitar quebra silenciosa de serialização e para preservar a linguagem de cada domínio.

---

## 8. Por que migrar só CycleReset e ObjectReset

`CycleReset` e `ObjectReset` são bons pilotos porque:

```text
CycleReset testa alvo simples: CycleResetScope.
ObjectReset testa alvo de domínio resolvido: ObjectResetTarget.
Ambos já têm QA Smoke Runners próprios.
Ambos expõem drift real de validação.
Ambos permitem refator interno sem tocar em Snapshot/Save ou ActivityContentExecution.
```

Se o genérico não conseguir cobrir esses dois sem gambiarra, ele não deve ser empurrado para `Snapshot` nem para `ActivityContentExecution`.

Se cobrir os dois com paridade, o closeout decide se vale migrar mais domínios.

---

## 9. Compatibilidade e migração

A migração deve seguir esta ordem:

```text
1. Criar Common aditivo.
2. Validar Common por smoke sintético.
3. Migrar CycleReset compondo Common internamente.
4. Rodar CycleResetQaSmokeRunner e comparar saída esperada.
5. Migrar ObjectReset compondo Common internamente.
6. Rodar ObjectResetQaSmokeRunner e comparar saída esperada.
7. Registrar o único desvio permitido: rejeição explícita de resultado ObjectReset com participantId/target/request inválido.
8. Fechar decisão sobre próximos domínios.
```

Nenhum consumer fora do package deve precisar mudar código. Recompilar por alteração interna do assembly é aceitável; mudar uso público não é.

---

## 10. Diagnostics e validação esperados

Smokes mínimos:

```text
CommonOperationResultSmoke
CommonParticipantExecutorSmoke
CycleResetQaSmokeRunner
ObjectResetQaSmokeRunner
```

O smoke sintético do executor deve cobrir:

```text
sucesso
participante optional com falha não bloqueante
participante required com falha bloqueante
participante que lança exception
participante que retorna resultado inválido para a entry atual
participante duplicado no plan, se o plan builder comum assumir essa responsabilidade
```

Critério de aceite:

```text
Saída textual dos smokes de CycleReset e ObjectReset deve permanecer igual, exceto o bugfix explicitamente autorizado em ObjectReset para resultado inválido.
```

---

## 11. Consequências positivas

```text
- Reduz duplicação entre executores.
- Diminui chance de drift entre domínios.
- Permite corrigir validação uma vez e propagar por composição.
- Dá função real para Runtime/Common/.
- Mantém os nomes públicos por domínio para documentação e Inspector.
- Preserva typed identity por domínio.
```

---

## 12. Custos e riscos

```text
- Introduz genéricos mais abstratos, menos legíveis por simples grep.
- Pode esconder semântica se o Common tentar decidir coisa de domínio.
- Pode quebrar paridade se factories/result/status forem simplificados demais.
- Pode induzir migração prematura de Snapshot ou ActivityContentExecution.
- Pode dar falsa sensação de melhora de UX sem alterar authoring surface.
```

Mitigação:

```text
- Common deve ser pequeno, executor-first, e não domínio-first.
- Domínios continuam expondo seus tipos públicos.
- Smokes existentes são autoridade de paridade.
- CONS-F deve decidir continuidade com dados, não por impulso de limpeza.
```

---

## 13. Relação com Authoring UX

Esta fase não resolve praticidade para game designer.

Ela melhora a manutenção interna do framework. A praticidade de uso deve ser tratada em uma frente separada, candidata:

```text
FXX-AUTHORING-UX — Framework Practical Use / Game Designer Surface
```

Essa frente futura pode cobrir:

```text
wizards de Route/Activity
presets canônicos
templates de cena
validators com mensagem de ação
guia rápido por tarefa
smoke buttons agrupados por objetivo
componentes Inspector-facing com nomes menos técnicos
```

Não misturar as duas frentes:

```text
Participant consolidation = manutenção interna do core.
Authoring UX = praticidade de montagem e operação no Unity Editor.
```

---

## 14. Guardrails

```text
Não alterar API pública de CycleReset/ObjectReset.
Não alterar enum público de requiredness/status.
Não migrar Snapshot/ActivityContentExecution/LocalContribution nesta fase.
Não introduzir fallback silencioso.
Não introduzir reflection.
Não introduzir singleton/service locator.
Não mover lifecycle ownership para Common.
Não transformar Common em catálogo de comportamento.
Não alterar mensagens de smoke para fazer o teste passar.
Não mexer em MonoBehaviour serializado sem fase própria.
```

---

## 15. Relação com fases futuras

Esta fase pode desbloquear, depois do closeout:

```text
Snapshot Participant Migration
ActivityContentExecution Participant Migration
LocalContribution Result/Issue/Validation review
FlowRequestTrigger shared non-MonoBehaviour core
Activity/Route SceneComposition consolidation
Activity/Route ContentLifecycle consolidation
Authoring UX phase
```

Esta fase mantém bloqueado:

```text
F34/gameplay
Adapter Modules
Player/Actor/Input/Camera/Audio/Pooling
RuntimeSpawned implementation
Content Anchor runtime binding
Save backend concreto
```

---

## 16. Próximo passo

Aceitar o ADR apenas se o plano revisado também for aceito.

Ordem recomendada:

```text
1. Aceitar ADR revisado.
2. Executar CONS-A.
3. Executar CONS-B.
4. Migrar CycleReset em CONS-C.
5. Migrar ObjectReset em CONS-D.
6. Fechar CONS-F.
7. Decidir se Snapshot/ActivityContentExecution entram depois.
```
