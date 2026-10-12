# IF-ADR-046 — Decisão de promoção Stable

Status: **PENDING — promoção não aprovada; sem alteração de maturidade da API**
Data: **2026-10-12**
Autoridade decisória: **IF-GOV-001**
Escopo: **somente o contrato público Manager-Provisioned Join de `PlayerSessionJoinTrigger`**

## Decision

**PENDING.** A closure pública delimitada não é promovida. IF-ADR-046 §7.4 exige replay do Slice 1 em SHA imutável do Framework e retenção da saída bruta, timestamps e proveniência da execução. IF-ADR-046 §7.7 e IF-PLAN-ADR-046 v3 Stage E exigem importação/compilação do FirstGame e evidência de Join/Leave/Rejoin no mesmo candidato imutável identificado. Os resultados reportados demonstram resultados funcionais, mas não identificam os bytes do Framework usados durante cada execução. HEADs Git consultados agora não são proveniência de execução. IF-GOV-001 não prevê dispensa desses gates específicos.

O log bruto do FirstGame não está disponível entre os anexos desta tarefa ou os arquivos dos repositórios consultados. O resumo da execução do usuário fica registrado como relato; nenhum log, timestamp ou SHA de execução é inventado. Anexar o log bruto original para arquivamento. Se os registros contemporâneos não trouxerem o vínculo exigido com o candidato imutável, a menor ação obrigatória é capturar Slice 1 e FirstGame em um candidato Framework imutável e limpo, associando a esse candidato os resultados aplicáveis do Framework e da regressão QA pós-retirada. Não repetir I4 nem a evidência composta de compensação física já aceita.

## Closure delimitada para decisão futura

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

## Registro da decisão

`PlayerSessionJoinTrigger` permanece Experimental, assim como os cinco contratos associados na closure delimitada. `IPlayerSessionScopedAccess`, `LocalPlayerJoinStatus`, superfícies Session/Slot/Actor, APIs assignment/ownership e o Framework permanecem fora desta decisão, com sua maturidade atual. A versão do pacote continua `1.1.0-preview.8`; este registro não altera `package.json`, estado de release ou tags.

## Estado Git verificado nesta decisão

- Framework: branch `master`, HEAD `7882d11f801b67aaeac6d289bbc9bd02e462e785`, árvore limpa antes das alterações documentais desta decisão; branch `ahead 1` de `origin/master`.
- QAFramework: branch `main`, HEAD `693e0d1048303f5fde26831b117ad4c82fd2cf7d`, com alteração local preexistente em `Assets/_Project/Settings/ImmersiveFramework/Resources/ImmersiveFrameworkSettings.asset`; preservada.
- FirstGame: branch `main`, HEAD `c0d14a057197624ff2f070a6e601d7d225f972db`, árvore limpa, `ahead 1` de `origin/main`.

O commit deste registro documental criará um novo HEAD do Framework; nenhum HEAD acima é declarado como SHA usado nas execuções Unity relatadas.
