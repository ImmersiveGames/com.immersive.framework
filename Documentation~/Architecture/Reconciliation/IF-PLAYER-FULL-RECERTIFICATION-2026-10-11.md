# Immersive Framework — Player Full Recertification (Checkpoint 1 e auditoria estática)

Data: 2026-10-11
Baseline Framework: `1.1.0-preview.7` / `49848313131daffd1b7f8c82c709a7fad09ca459`
Status: **BLOCKED para recertificação integral; avaliação estática concluída. Nenhum teste Unity foi executado.**

## 1. Resumo executivo

A documentação e os contratos Player foram inspecionados contra o baseline atual. A evidência histórica continua válida somente para os escopos e revisões que registrou. Não há evidência nesta execução que permita dizer que o agregado Player passou no HEAD atual, nem para atualizar as maturidades.

Dois fatos impedem encerrar a recertificação agora:

1. Os documentos históricos do Framework apontam `QaPlayerFullCertificationOrchestrator` e o agregado de 27 contratos, mas essa implementação não foi localizada no HEAD Framework `4984831` nem no QAFramework `7608130`. O QAFramework atual tem cenários focados de IF-ADR-042/043/044/045 e documentação de certificação; seu checkout declara Unity `6000.5.0f1` em `ProjectSettings/ProjectVersion.txt`, resolve o Framework `.7` por `file:` sem hash imutável e não contém um executor amplo de Player. Assim, não é demonstrável que o agregado 27/27 possa ser repetido com a infraestrutura atualmente versionada.
2. As instruções vigentes de `immersive-framework-package-general` e `immersive-framework-qa-smokes` proíbem iniciar Unity, Play Mode, batchmode ou smokes. A tentativa de compilação estática dos projetos Unity gerados também ficou indisponível: `dotnet build` terminou antes da compilação com `No .NET SDKs were found`.

Nenhum defeito de runtime foi reproduzido. Não alterei runtime, testes, QAFramework, FirstGame, PEGA, ADRs aceitos nem status de API. Não há promoção recomendada nesta rodada.

## 2. Baseline e proveniência

| Repositório | Estado lido | Evidência | Resultado |
|---|---|---|---|
| Framework | `master`, `49848313131daffd1b7f8c82c709a7fad09ca459`; árvore limpa, alinhada com `origin/master` | `git status --short --branch`, `git rev-parse HEAD`; `package.json` | `1.1.0-preview.7`, Unity `6000.5.0f1` declarada |
| QAFramework | `main`, `76081302f5fa245ecc3b374a192911b210299b50`; árvore limpa, alinhada com `origin/main` | `git status --short --branch`, `git rev-parse HEAD` | `ProjectVersion.txt`: Unity `6000.5.0f1`; manifest/lock: Framework local `.7`, sem hash imutável; capturar HEAD do pacote por execução |
| FirstGame | `main`, `98a0c23ea33f11e721afe80161fe0aba8cb7acc7`; árvore limpa, alinhada com `origin/main` | `git status --short --branch`, `git rev-parse HEAD`, `ProjectSettings/ProjectVersion.txt` | Unity `6000.5.0f1`; Framework local `file:C:/Projetos/ImmersivePackages/com.immersive.framework`, sem hash no lock |

O Framework apontado atualmente pelo FirstGame é o checkout `4984831`, versão `.7`. Isso prova a resolução presente, não fixa retroativamente os SHAs usados pelos Play Modes descritos nos READMEs/logs antigos.

`CHANGELOG.md` de `.7` registra uma release focada em Persistent Content, sem promover Player. `Documentation~/Architecture/ADRs/IF-ADR-038-...md` continua com `Unity validation pending`. `IF-GOV-001` define Stable como compromisso de compatibilidade, Experimental sem essa garantia, Internal como detalhe, DevelopmentTooling como ferramenta; o atributo de maturidade não muda execução.

## 3. Contratos e status observados

A fonte pública consultada foi `Documentation~/API/Public-API.md` (seção Player) e a fonte normativa foi IF-GOV-001 com IF-ADR-003/015/016/019/020/021/023A/024/025/038/044.

| Superfície | Maturidade declarada no `.7` | Evidência existente | Resultado desta rodada |
|---|---|---|---|
| `SceneProvidedLocalPlayerAuthoring` | Stable (authoring delimitado) | certificações históricas e guia Player Usage | status delimitado continua coerente; execução atual não repetida |
| `LocalPlayerHostAuthoring` | Stable (Host técnico) | guide/API; composição depende de superfícies experimentais | não estabiliza o workflow completo |
| `PlayerSlotProfile` | Stable apenas para Scene-Provided | guide/API | extensão Manager/Session não é automaticamente Stable |
| `PlayerSessionProfile`, provisioning mode e Manager-Provisioned | Experimental | aggregate histórico, certificação Manager 14/14 e samples FirstGame | precisa run versionado e compatibilidade/serialização avaliadas |
| Join, observer, Leave, seleção e `IPlayerSessionScopedAccess` | Experimental | aggregate histórico; IF-ADR-015B; IF-ADR-025; FirstGame | `IPlayerSessionScopedAccess` reúne operações de escopos distintos; não promover em bloco |
| Actor occurrence/runtime, `ActorProfile`, Actor materialization | Experimental | IF-ADR-023 histórico, IF-ADR-024 16/16 e testes Editor pontuais | IF-ADR-023 foi superado por IF-ADR-023A/038; falta recertificação executada sobre o boundary atual |
| Device ownership e `PlayerInput` | Experimental nas superfícies que o expõem | IF-ADR-025 documenta 16 invariantes e aggregate 17/17 histórico; devices determinísticos | contrato técnico é delimitado; falta execução atual com SHA registrado, sem exigir hardware físico para essa garantia |
| `PlayerGameplayInputReader` | Experimental | uso em samples e evidência histórica aggregate | falta prova atual focada de bind/rebind/stale/Leave/Activity no Actor vigente |
| Route spatial entry, Activity relocation e readiness | Experimental | Route 18/18, Activity 23/23 e aggregate histórico | repetir com Actor atual, Required/Optional, late Join/Leave e recovery |
| Valores e `PlayerGameplayAvailabilityBlockTrigger` de IF-ADR-044 | Stable bounded | QA IF-ADR-044 8/8, execução histórica e integração manual reportada | escopo do status permanece limitado; não promove o acesso scoped inteiro |
| `ActorCameraSubjectAuthoring` | Experimental | testes Editor pontuais (`ActorCameraSubjectAuthoringTests`) e QA de Camera separados | incluir apenas nos pontos de dependência real do Player; recertificação ampla IF-ADR-038 pendente |
| `SceneProvidedLocalPlayerCreator`, validadores e runtime interno | DevelopmentTooling/Internal | documentação/API | fora de promoção de produto |

## 4. Matriz contrato → ADR → implementação → testes → evidência → status

| Contrato atual | ADR | Implementação pública representativa | Testes/evidência encontrados | Status de recertificação atual |
|---|---|---|---|---|
| Configuração/serialização Session, Supported Slots e allocation order | 003/016 | `Runtime/PlayerParticipation/Authoring/PlayerSessionProfile.cs`, profiles e validators | reports serialization integrity e aggregate históricos; sem runner localizado neste checkout | **NOT TESTED no HEAD**; cobrir enum, IDs, ordem, default e valores inválidos |
| Join/Leave/selection/revisions/stale calls | 015/015B/019/020/025 | Join/Leave/selection command surfaces e `IPlayerSessionScopedAccess` | IF-ADR-015B e aggregate histórico; FirstGame ManagerProvisioned | **NOT TESTED no HEAD**; não criar exact-Slot Join (API atual é untargeted) |
| Scene-Provided adoption e resource cleanup | 003/015/019/020 | `SceneProvidedLocalPlayerAuthoring`, resolver e lifecycle | aggregate 27/27 histórico e manual FirstGame anterior | **NOT TESTED no HEAD**; verificar falhas sem adoption parcial, Leave e termination |
| Manager-Provisioned Host/Actor materialization | 003/015/016/019/020 | `LocalPlayerProvisioningAuthoring`, Host, PlayerInput e materialization | IF-ADR-023 14/14/29-Aug histórico; samples ManagerProvisioned | **NOT TESTED no HEAD**; cobrir Join antes/durante Activity, rollback, duplicate/leak, Leave/Rejoin |
| Actor occurrence identity, physical/spatial authority, Subject | 023A/024/038 | `PlayerActorRuntimeHost`, `PlayerActorDeclaration`, Actor occurrence/materialization, explicit Subject | IF-ADR-023 14/14 anterior; IF-ADR-024 16/16 histórica; testes pontuais opcionais/Subject encontrados em `Tests/Editor/Camera/Assignments` | **BLOCKED** até executar regressão conforme IF-ADR-038 para stale references, new occurrence, replacement, rollback e cleanup |
| Prepared Actor replacement | 024/038 | replacement request/result/authority | certificação 16/16 histórica | **NOT TESTED no HEAD**; validar que replacement lógico não vira hot-swap físico |
| Input ownership e Reader | 025/038 | Input pairing, `PlayerInput` projection, `PlayerGameplayInputReader` | IF-ADR-025 16 invariantes + aggregate 17/17 histórico; FirstGame LocalMultiplayer | **NOT TESTED no HEAD**; P1/P2 com devices sintéticos, Reader isolado, gates, stale e leave/rejoin |
| Participation/readiness e transições espaciais | 003/012/019/021/021A/038 | Activity projection, Route entry, relocation, admission | Route 18/18, Activity 23/23, aggregate histórico | **NOT TESTED no HEAD**; zero/P1/P1+P2, Required/Optional, late Join/Leave, recovery e teardown |
| Gameplay availability | 044 | token/result/status e `PlayerGameplayAvailabilityBlockTrigger` | QA IF-ADR-044 8/8 histórica; FirstGame manual histórico | evidência existente é bounded e não atualizada nesta execução; preservar status limitado |
| Camera ligada ao Actor | 038 | `ActorCameraSubjectAuthoring`, Assignment/membership | teste Editor Subject e QA camera 042/043 separados | Camera QA não substitui a recertificação Player de occurrence; IF-ADR-038 continua pendente |

`Tests/Editor/PlayerParticipation` contém `ActivityPlayerActorLeaveLedgerTests` (3 testes); `Tests/Editor/Camera/Assignments` inclui `ActorOccurrenceOptionalVisualContentTests` e `ActorCameraSubjectAuthoringTests`. A contagem é inspeção estática dos atributos/código; nenhum desses testes foi executado. O pacote não contém no HEAD inspecionado o antigo agregado de 27 fases citado pelos ADRs. Os resultados históricos 25/25 e 27/27 permanecem datados e não são promovidos a execução `.7`.

## 5. Cobertura necessária e sequência QA

O QAFramework atual enumera evidência e assets para IF-ADR-042/043/044/045. A arquitetura QA-001 separa Package/NUnit (contratos isoláveis), QAFramework (lifecycle real, boundary/adversarial/negative) e FirstGame (happy path integrado). Para retomar a recertificação integral sem criar runtime paralelo, são necessários cenários canônicos separados por causalidade:

1. **Session/Provisioning:** Session create/initial config, joining open/closed, Manager Join untargeted, slot order, default e LeaveUnresolved, seleção + selection revision stale, Scene-Provided/Manager-Provisioned convergence, Join antes/durante Activity, Leave/Rejoin/termination, falha e cleanup.
2. **Actor atual IF-ADR-038:** Host vs Actor occurrence, identity domains/revisions, Actor Declaration/Profile, staged materialization/adoption, explicit spatial authority e optional visuals, replacement permitido, rollback/failure, teardown/rejoin e referências antigas rejeitadas. Subject apenas quando o cenário comprovar que é requisito.
3. **Input ownership/Reader:** dois devices sintéticos, Device → ocorrência → Slot → `PlayerInput` → Reader, leitura isolada P1/P2, bindings/rebind por Activity, Availability/Pause/Transition gate owners independentes, stale token, Leave/Rejoin e remoção de device dentro do contrato.
4. **Participation/readiness:** zero/P1/P1+P2 e Required/Optional; `WaitingForJoin`, `WaitingForActorSelection`, `LogicalActorsPrepared`, `GameplayReady`; late Join/Leave durante Activity, relocalização Route/Activity, recovery e cleanup.

Para cada cenário, o brief deve registrar baseline, owner, chamada pública, evidência tipada/revisionada, resultado terminal, caminho de falha, primeira causa, unwind e cleanup. A primeira falha deve apontar a fronteira causal; evidência deve capturar o SHA de Framework e QA, versão Unity, manifest/lock e resultado integral. Não exigir hardware físico para provar ownership determinístico; não introduzir control scheme universal ou Join para Slot específico.

Os quatro samples FirstGame solicitados (ManagerProvisioned, CharacterSelection, CharacterSelectionMultiplayerSplitScreen, LocalMultiplayer) servem como provas manuais integradas existentes. Reexecutá-los após a QA controlada e registrar o hash do Framework resolvido durante cada run; não modificar o consumidor.

## 6. Correções e validações realizadas nesta execução

| Ação | Resultado |
|---|---|
| Inspeção de HEAD, versão, working tree e Unity declarada/resolvida | concluída estaticamente para Framework, QAFramework e FirstGame |
| Inspeção IF-GOV-001, IF-ADR-038, Player Usage, Public API, aggregate/records históricos | concluída por leitura estática |
| Busca do antigo Player aggregate/orchestrator nos repositórios alvos | não localizado no source atual; ocorrência apenas em records/documentação histórica |
| `dotnet build` dos dois csproj gerados em `Immersive.Framework.PlayerParticipation.Tests.csproj` e `.Editor.Tests.csproj` | **BLOCKED antes da compilação**: sem .NET SDK; mensagem `No .NET SDKs were found` |
| NUnit, Unity import/compile, Play Mode, QAFramework smokes, FirstGame Play Mode | **NOT RUN**; proibidos pela instrução da skill de pacote/QA aplicável |
| Correção de runtime/QA | nenhuma; nenhum defeito concreto foi reproduzido, e não se deve inventar patch sem regressão executável |
| Promoção `FrameworkApiStatus` / release | nenhuma; Checkpoint 4 depende de autorização explícita posterior |

## 7. Maturidade e bloqueadores

**Nenhum bloqueador arquitetural de Session/Manager-Provisioned foi demonstrado nesta revisão estática.** O Join permanece não direcionado e a seleção lógica permanece separada da substituição física.

Bloqueadores demonstrados para aceite de recertificação integral:

- Executor/casos de aggregate citados historicamente não estão presentes nos dois checkouts atuais; reconstituir cobertura no QAFramework atual ou localizar e versionar a fonte correta antes de executar.
- IF-ADR-038 declara validação Unity pendente; testes editor de Subject e optional visual não cobrem todo o lifecycle físico/espacial do Actor occurrence.
- Nenhum run QA neste HEAD foi autorizado/executado; nenhum SHA de execução foi registrado.
- O compilador dotnet local não está instalado/disponível; não há validação compile estática de assemblies.
- FirstGame usa dependência local sem hash no lock. O SHA `.7` atual é verificável no momento da inspeção, mas precisa ser capturado no run.

Avaliação preliminar: manter maturidades declaradas. Os contratos Stable já delimitados permanecem dentro de seus limites; as outras superfícies não devem ser promovidas com base em resultados históricos sem repetição/proveniência. `IPlayerSessionScopedAccess` não é candidato a Stable como interface integral por reunir comandos e garantias com coberturas distintas.

## 8. Estado dos checkpoints

- **Checkpoint 1 — Plano executável:** concluído como documento; matriz inicial e sequência de cobertura constam em `docs/superpowers/plans/2026-10-11-player-full-recertification-plan.md`.
- **Checkpoint 2 — Correções e QA:** bloqueado; não houve falha reproduzida, o runner agregado histórico não foi encontrado, o .NET SDK não está disponível e Unity/QA não pode ser executado pelo agente conforme skill aplicável.
- **Checkpoint 3 — Avaliação de estabilização:** avaliação estática preliminar registrada; decisão final depende dos runs versionados e revisão de serialização/compatibilidade por superfície.
- **Checkpoint 4 — Promoção:** não iniciado; exige autorização explícita posterior.

## 9. Ações necessárias para retomar

1. No checkout/runner Unity autorizado, localizar ou definir o entrypoint atual de QA Player. Fixar o SHA do Framework resolvido (não só `.7`) e SHA QA.
2. Executar os assemblies NUnit aplicáveis e os quatro cenários de runtime listados, registrando casos, contagens, logs e cleanup.
3. Se houver fail, capturar primeira divergência, reproduzir, classificar o owner e abrir correção interna no ADR aceito com teste regressivo.
4. Reexecutar somente FirstGame happy paths sem alterar seus arquivos e capturar SHA/version/manifest por cenário.
5. Finalizar a matriz superfície por superfície, atualizar a documentação do Framework sem reescrever o histórico e apresentar uma proposta de release sem aplicar promoção.
6. Após revisão do relatório, solicitar autorização separada para o Checkpoint 4.

## 10. Integridade da entrega

O plano e este relatório são os únicos arquivos documentais pretendidos nesta rodada. Nenhum log bruto foi copiado. Nenhum arquivo de PEGA, FirstGame, QAFramework ou runtime do Framework foi alterado. Nenhum commit, push, tag ou publicação foi realizado.


## 11. QAFramework Player Slices — follow-up (2026-10-11)

This addendum records evidence and implementation supplied for the new QA-001 vertical slices. It does not close full Player recertification and does not change API maturity.

| Slice | Scope | Evidence recorded | Current recertification status |
|---|---|---|---|
| Slice 1 — Session / Join | Manager-Provisioned P1, typed invalid Join rejection, default Actor preparation/materialization, Leave and baseline restoration | User-reported `PASS 8/8`, including `BaselineRestored`; original run SHA/log were not present in the QAFramework checkout and no immutable Framework SHA was tied to that run | Historical/user-supplied result; not independently re-executed or SHA-attributed here |
| Slice 2 — Leave / Rejoin | Same Slot reused, Actor occurrence A invalidated, B distinct, stale A Leave rejected, final Leave and restoration | User-reported `PASS`, `RejectedForeignOrStaleOccurrence` and `BaselineRestored`; no original run SHA/log was available to bind to this addendum | Historical/user-supplied result; not independently re-executed or SHA-attributed here |
| Slice 3 — Local Multiplayer / Input Ownership | Two synthetic devices, P1/P2 Join, public device/PlayerInput/Host/Actor occurrence correlation, P1 Leave while P2 remains, P1 rejoin, cleanup | Scenario and independent Editor authoring setup added in QAFramework; no Configure, Unity import, Inspector validation or Play Mode was run by the agent | Implemented statically; runtime `NOT RUN` pending manual Unity validation |

The QAFramework manifest and lock resolve `com.immersive.framework` from `file:C:/Projetos/ImmersivePackages/com.immersive.framework`; the installed package reports `1.1.0-preview.7`. The QA project declares Unity `6000.5.0f1`. Slice 3 uses experimental Player Session, Manager-Provisioned, occurrence and input-ownership surfaces. Its Actor Selection Duplicate Policy is explicitly `AllowDuplicates` because this test fixture assigns the same default Actor Profile to both Slots; that is fixture-specific and is not a general policy recommendation.

The Slice 3 ownership proof uses the public `PlayerSessionJoinCommandTrigger.InvokeFromDevice(InputDevice)`, typed `LocalPlayerJoinResult`, scoped Session snapshots and `LocalPlayerInputOwnershipSummary`, plus the exact `PlayerInput.devices` collection and Host/Actor Mount returned by Join. It does not infer ownership from GameObject names and does not use Framework internals. Setup postflight is explicitly labeled serialized postflight; the GameApplication, Route, Activity, Profile and Camera inspectors still require manual validation.

The requested file `2026-10-11-player-qa-suite-implementation-plan.md` was not found in the QAFramework checkout or the resolved package Documentation tree during this task. The accepted QA-001 ADR, current installed `.7` contracts and the user's detailed Slice 3 brief were used; the missing plan is a traceability gap, not a runtime blocker.

Do not mark the integral Player certification complete from these slices. Slice 3 remains pending Configure run twice, generated-asset/Build Profile review, Inspector validators, Play Mode terminal `PASS`, P2 preservation during P1 Leave/Rejoin, and confirmed `BaselineRestored`. The Slice 1/2 runs remain user-reported without retrospectively assigned SHAs.
