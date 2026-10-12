# IF-ADR-046 — Decisão de promoção Stable

Status: **APPROVED — Player Join Stable**
Data: **2026-10-12**
Autoridade decisória: **usuário na qualidade de autoridade responsável do projeto; aceite explícito na instrução da tarefa de 2026-10-12**
Escopo: **somente o contrato público Manager-Provisioned Join de `PlayerSessionJoinTrigger`**

## Decisão

**APPROVED — Player Join Stable.** A autoridade responsável aceitou a [Amendment 003](IF-ADR-046-AMENDMENT-003-PARTIAL-PROVENANCE-ACCEPTANCE.md) e o risco residual de proveniência parcial. IF-GOV-001 §2 define Stable como compromisso de compatibilidade; §7 exige tratamento de governança para mudanças no significado de maturidade e ADR/migração para mudanças incompatíveis. Esta Amendment altera somente os gates de evidência da IF-ADR-046 para este pacote, sem redefinir Stable, enfraquecer a compatibilidade ou alterar semântica Runtime. A IF-GOV-001 não impede essa exceção específica.

O ZIP original foi examinado integralmente e arquivado com os membros intactos em `QAFramework/Documentation~/Certification/PlayerJoin/Evidence/PlayerStable-2026-10-12/`. O hash do FirstGame log (339106 bytes, 886 linhas), da screenshot, dos terminais e do ZIP foi conferido com `SHA256SUMS.txt`. O log registra P1/P2 e um rejoin de cada, quatro leaves concluídos com `partialRelease=False`, readiness concluída e nenhum marcador de severity error/warning ou exception. A screenshot mostra 292 aprovados/0 falhas e 101 testes PlayerParticipation. `QA-terminals.txt` é transcrição das linhas de resultado fornecidas, não log bruto. Nenhum desses artefatos registra SHA de Framework/QA ou timestamp da execução; essa proveniência permanece parcial e foi expressamente aceita. Não foi repetido nenhum teste.

## Closure promovida

- `PlayerSessionJoinTrigger`, including its public `Scope`, `LastOutcome`, `RequestJoin`, `Invoke`, and `InvokeFromDevice` members.
- The complete `LocalPlayerProvisioningConsumerScope` enum, with its existing names and numeric values.
- `LocalPlayerJoinRequest` and all current public members, including exact-reference semantics for `PairWithDevice`.
- `LocalPlayerJoinOutcome` and its current public properties; its constructor remains internal.
- The complete `LocalPlayerJoinOutcomeCode` and `LocalPlayerJoinAdmissionState` enums, with current names and numeric values.
- Compatibility dependencies on Unity `MonoBehaviour` and Input System `InputDevice` as already declared by the package.

Esta closure limita-se à admissão técnica de Session e ao commit de Host no modo Manager-Provisioned. Não promete preparação/materialização de Actor, Gameplay Readiness, admissão de gameplay ou sucesso observável da destruição de objetos Unity.

## Contratos que permanecem Experimental

Esta decisão não promove `IPlayerSessionScopedAccess`, `PlayerSessionObserver`, perfis de Session/Slot/Actor, configuração/authoring Manager-Provisioned, `LocalPlayerJoinStatus`, evidências de Host ou `PlayerInput`, APIs de assignment/ownership, comandos Leave/Joining, comportamento multiplayer de Input/Pause nem o Framework inteiro. Contratos já Stable mantêm somente os respectivos escopos documentados.

## Evidências aceitas e ressalvas preservadas

O usuário reportou Framework EditMode 292/292, PlayerParticipation 101/101, IF-ADR-042/043/044 totalizando 22/22, QA-PLAYER-SESSION-JOIN Slice 1 8/8 com `BaselineRestored`, QA-PLAYER-LEAVE-REJOIN-OCCURRENCE PASS e Join/Leave/Rejoin no FirstGame Local Multiplayer. Permanecem relatos funcionais com proveniência parcial, não execuções vinculadas a SHA.

I4 permanece aceito por evidência composta, com ressalvas de revisão estática P1/P2 e I4.2 LEASE historicamente `BLOCKED 3/3`. A compensação física permanece **aceita por evidência composta** no Amendment 002; não é um PASS integrado de Play Mode com callback seguido por throw. A falha no commit da Session permanece classificada estaticamente como **inalcançável** pela boundary pública válida sob as premissas Unity síncronas examinadas. A migração para uma única boundary pública de Join do Amendment 001 permanece registrada.

## Estado de maturidade aprovado

Stable: `PlayerSessionJoinTrigger`, `LocalPlayerProvisioningConsumerScope`, `LocalPlayerJoinRequest`, `LocalPlayerJoinOutcome`, `LocalPlayerJoinOutcomeCode` e `LocalPlayerJoinAdmissionState`. Permanecem em suas maturidades anteriores `IPlayerSessionScopedAccess`, `LocalPlayerJoinStatus`, superfícies Session/Slot/Actor, APIs assignment/ownership e o Framework inteiro. A versão continua `1.1.0-preview.8`; nenhum comportamento Runtime, `package.json`, estado de release ou tag foi alterado.

## Estado Git verificado durante a avaliação

- Framework: branch `master`, HEAD `c66be5709e669dfc71b5e9ea9320156496058490`, `ahead 2` de `origin/master`; no início desta decisão havia documentação preparatória não commitada, mas os atributos Runtime ainda estavam Experimental.
- QAFramework: branch `main`, HEAD `693e0d1048303f5fde26831b117ad4c82fd2cf7d`, alteração local preexistente em `Assets/_Project/Settings/ImmersiveFramework/Resources/ImmersiveFrameworkSettings.asset` preservada; evidências adicionadas somente em pasta nova.
- FirstGame: branch `main`, HEAD `c0d14a057197624ff2f070a6e601d7d225f972db`, árvore limpa, `ahead 1` de `origin/main`.

Nenhum HEAD acima é declarado como SHA usado nas execuções Unity relatadas. FirstGame/PEGA não foram modificados.
