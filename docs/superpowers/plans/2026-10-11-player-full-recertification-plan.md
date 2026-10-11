# Player Full Recertification & Stabilization Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Recertificar, no baseline imutável do Immersive Framework, os contratos Player vigentes e preparar uma avaliação de maturidade por superfície sem promoção automática.

**Architecture:** Preservar os ADRs aceitos e distinguir contratos de Session, provisionamento, Actor occurrence, input, readiness e interfaces compostas. Usar QAFramework como consumidor técnico e FirstGame somente como consumidor de integração já existente; não alterar FirstGame. Correções internas permitidas serão entregues no Framework e limitadas ao contrato aceito, com regressões causais.

**Tech Stack:** Unity 6.5 / 6000.5.0f1; Immersive Framework `1.1.0-preview.7`; C#; NUnit/Unity Test Framework; QAFramework; FirstGame.

**Spec:** Solicitação “Immersive Framework — Player Full Recertification & Stabilization” recebida em `C:\Users\renat\.codex\attachments\0ba39563-d791-4f10-9166-4281136b5d33\Texto colado.txt`.

## Global Constraints

- Não alterar PEGA.
- Não modificar FirstGame sem autorização; neste plano, apenas inspecionar as provas existentes.
- Não atualizar dependências, promover APIs, alterar `FrameworkApiStatus`, publicar, taguear, fazer commit ou push.
- Não introduzir novas APIs públicas sem justificativa e autorização.
- Não reabrir ADR aceito sem decisão explícita.
- Não executar Unity, Play Mode, smoke ou batchmode segundo `immersive-framework-package-general` e `immersive-framework-qa-smokes`; execução Unity permanece manual pelo usuário.
- Repositórios do Framework e QAFramework são editáveis apenas dentro do escopo autorizado; documentação de saída fica sob `Documentation~` ou `docs` do Framework, conforme apropriado.
- Resultados históricos não serão apresentados como execução no HEAD atual sem prova de proveniência.
- Contratos `Stable` seguem IF-GOV-001; seleção/Join não direcionado não promete Join por Slot exato; nenhum esquema universal de controle será criado.
- Camera será coberta somente nos limites de dependência real para Player definidos por IF-ADR-038.

## Review Focus

- Actor Runtime após IF-ADR-023A/038: ocorrência física, autoridade espacial e Subject atuais; testar stale/rejoin, rollback e cleanup.
- Manager-Provisioned lifecycle: Join antes/durante Activity, falha de materialização, Leave/Rejoin e reconciliação sem duplicação.
- Session e seleção: enum/serialização, allocation order, default/LeaveUnresolved, duplicate policy, revisão stale e chamadas fora do lifecycle.
- Input ownership: devices determinísticos → ocorrência → Slot → PlayerInput → Reader, dois Players, Leave/Rejoin e coexistência de gates.
- Readiness e projection: zero/P1/P1+P2, Required/Optional, late Join/Leave, transições Route/Activity e teardown; `GameplayReady` não presume movimento ou câmera.

---

## Baseline observado em 2026-10-10/11

| Repositório | Branch/HEAD | Working tree | Unity/package |
|---|---|---|---|
| Framework `C:\Projetos\ImmersivePackages\com.immersive.framework` | `master` / `49848313131daffd1b7f8c82c709a7fad09ca459` | clean, alinhado a `origin/master` | `package.json`: `1.1.0-preview.7`, `unity 6000.5`, `6000.5.0f1` |
| QAFramework `C:\Projetos\QAFramework` | `main` / `76081302f5fa245ecc3b374a192911b210299b50` | clean, alinhado a `origin/main` | `ProjectSettings/ProjectVersion.txt`: Unity `6000.5.0f1`; manifest/lock resolvem Framework por caminho local `.7`, sem hash imutável |
| FirstGame `C:\Projetos\planet-devourer` | `main` / `98a0c23ea33f11e721afe80161fe0aba8cb7acc7` | clean, alinhado a `origin/main` | `ProjectVersion.txt`: `6000.5.0f1`; Framework no manifest/lock é `file:C:/Projetos/ImmersivePackages/com.immersive.framework`, source `local`, sem hash |

A versão FirstGame resolve hoje o diretório Framework acima, cujo HEAD observado é `4984831`; isso não identifica retroativamente o Framework usado nos logs antigos. Não foi localizada no QAFramework a pasta `Assets/ImmersiveFrameworkQA/README.md` nem o orquestrador `QaPlayerFullCertificationOrchestrator`; os records em Framework citam o agregado de 27 contratos, mas o executor atual precisa ser localizado antes de afirmar que o fluxo agregado é reexecutável a partir do QAFramework neste checkout. QAFramework contém evidência focada versionada para IF-ADR-042/043/044/045.

## Matriz de rastreabilidade inicial

| Contrato | Autoridade | Implementação/superfície | Evidência existente | Lacuna para recertificação atual | Destino a avaliar |
|---|---|---|---|---|---|
| Session inicialização, slots e serialização | IF-ADR-003/016 | `PlayerSessionProfile`, configuração efetiva e resolver | Full Player histórico 27/27; QA serialização | Executar no SHA imutável escolhido e rever assets/enums | Bounded Stable ou Experimental por garantia |
| Joining, allocation, default/LeaveUnresolved, seleção e revision | IF-ADR-015/015B/016 | Join/selection command + observer; API de Join é untargeted | 015B e aggregate históricos; FirstGame samples | Repetir Join, stale, duplicate policy, fora de lifecycle; sem exact-Slot Join | Bounded por comando |
| Scene-Provided | IF-ADR-003/015/019/020 | `SceneProvidedLocalPlayerAuthoring`, Host/Actor composition | Autorias Stable delimitadas e aggregate histórico | Proveniência no baseline e falhas sem adoção parcial/terminação | Componente vs workflow misto |
| Manager-Provisioned | IF-ADR-015/016/019/020 | `LocalPlayerProvisioningAuthoring`, Join, Host/Actor runtime | Certificação 14/14 e samples FirstGame | Reexecutar join antes/durante Activity, fail/rollback, Leave/Rejoin e transições | Bounded Manager workflow |
| Actor occurrence/materialização | IF-ADR-023A/024/038 | `PlayerActorRuntimeHost`, declaration, occurrence, materialization | IF-ADR-023 14/14 e IF-ADR-024 16/16 anteriores ao boundary atual | Recertificar identidade, spatial authority, ownership, stale occurrence, rejoin, rollback e cleanup no 038 | Contratos separados; Profile possivelmente Experimental |
| Input ownership / Reader / Gate | IF-ADR-025, IF-ADR-044 | pairing, PlayerInput projection, `PlayerGameplayInputReader`, gate adapter | ADR-025 16 invariantes + Full Player 17/17; ADR-044 8/8 | Repetir versão fixa; reader/rebind/stale e convivência Pause/Transition/Availability | Ownership vs Reader e acesso scoped |
| Leave/termination/lifetime | IF-ADR-019/020 | Session leave and resource release | Aggregate histórico e vários samples | Reexecutar cleanup na revisão identificada e garantir sessão/host unwind | Comando/resultados delimitados |
| Route entry / Activity relocation / readiness | IF-ADR-003/012/021/021A | Player participation projection e spatial lifecycle | Route 18/18; Activity 23/23; aggregate 27/27 históricos | Executar REQUIRED/OPTIONAL, zero/P1/P1+P2, late join/leave/rejoin e recuperação no Actor atual | Authoring, binding e projection separados |
| Prepared Actor replacement | IF-ADR-024 | comando preparado e runtime occurrence swap | Certificação 16/16 histórica | Repetir no baseline atual e cobrir occurrence/Subject/stale conforme IF-ADR-038 | Manter operação específica |
| Actor Camera Subject | IF-ADR-038 | `ActorCameraSubjectAuthoring` | Tests Editor locais; camera QA de outros cortes | Só recertificar dependência Player real; não importar certificação Camera inteira | Experimental até cobertura conjunta |
| `IPlayerSessionScopedAccess` | IF-ADR-015/044 | interface composta pública | Subconjuntos em 015B/024/044 | Matriz de chamadas e maturidade por método; não elevar interface pelo token Stable 044 | Manter Experimental salvo prova completa |
| Câmera/session assignments | IF-ADR-038/042/043 | Camera Session e assignment | IF-ADR-042 7/7; 043 7/7; QA-NEW-004 9/9 | Fora da certificação Player, exceto requisitos explícitos de ocorrência/Subject | Não ampliar escopo |

## Execução por checkpoint

### Checkpoint 1 — Plano e baseline

- [x] Confirmar HEAD, versão package e working tree do Framework, QAFramework e FirstGame.
- [x] Ler IF-GOV-001, IF-ADR-038, Player Usage, aggregate histórico e evidências selecionadas.
- [ ] Terminar inventário integral da API Player e identificar assemblies/test suites/executor canônico de recertificação.
- [ ] Publicar matriz final contrato → ADR → símbolo → teste → evidência → status pretendido e lista fechada de regressões necessárias.

**Resultado:** baseline estático confirmado; a matriz acima é inicial. O executor agregado citado no registro histórico não foi localizado no checkout QAFramework; precisa ser identificado antes da execução.

### Checkpoint 2 — Regressões/correções internas

- [ ] Identificar testes determinísticos existentes no Framework e smokes de lifecycle canônicos do QAFramework; mapear cada teste aos contratos e às precondições de setup.
- [ ] Implementar regressões ausentes para deltas concretos de IF-ADR-023A/038 e interações atuais de Reader/Actor/Activity. Cada regressão deve capturar identidade/revision baseline, observar autoridade pública, esperar resultado terminal, preservar primeira causa e provar cleanup.
- [ ] Completar cobertura Session/Manager-Provisioned/Scene-Provided somente onde a matriz encontre caso contratual não exercitado; não duplicar NUnit determinístico em smoke.
- [ ] Para cada falha: reproduzir, atribuir owner (teste vs Framework vs composição), corrigir dentro do ADR aceito e adicionar regressão. Proposta de ADR/migração antes de semântica pública incompatível.
- [ ] Fazer auditoria prospectiva após cada primeiro `next=` e conferir unwind/cleanup de toda a sequência.
- [ ] Rodar apenas verificações estáticas permitidas. Execução Unity/Play Mode/QA e FirstGame fica manual pelo usuário; nenhum PASS será declarado sem logs.

**Saída documental:** relatório de recertificação com SHA de Framework/QA, versão e manifest/lock efetivos, escopo/casos, PASS/FAIL/SKIP reais, logs pertinentes, restauração de baseline e limitações. FirstGame permanece read-only e logs históricos não serão reclassificados.

### Checkpoint 3 — Maturidade/estabilização

- [ ] Revisar, para cada símbolo, contrato/serialização/ADR/implementação/regressão executada/consumidor/compatibilidade.
- [ ] Classificar individualmente: pronta para Stable; pronta após documento/migração; Experimental com bloqueador provado; Internal/DevelopmentTooling fora de promoção.
- [ ] Analisar `IPlayerSessionScopedAccess` método a método e escolher entre manter Experimental ou recomendar recorte público; sem refatorar por conveniência de status.
- [ ] Registrar os bloqueadores remanescentes com teste/decisão necessários e riscos PEGA/FirstGame.
- [ ] Preparar proposta de release (status metadata, docs/ADRs/changelog/versão/migrações), sem aplicar promoção.

### Checkpoint 4 — Promoção (fora desta execução)

- [ ] Aguardar autorização posterior explícita.
- [ ] Somente então atualizar maturidade, documentação, changelog, versão, tag/release e consumidores conforme decisão aprovada.

## Arquivos de saída planejados

- `docs/superpowers/plans/2026-10-11-player-full-recertification-plan.md` — este plano executável.
- `Documentation~/Architecture/Reconciliation/IF-PLAYER-FULL-RECERTIFICATION-<date>.md` — relatório final/versionado de evidência, correções e maturidade após execução real.
- Arquivos Runtime/Editor/Tests no Framework apenas se defeito/lacuna concreta for demonstrado e corrigível sob ADR aceito; gerar regressões correspondentes.
- Conteúdo de FirstGame permanece sem alteração.

## Riscos e limites

- Unity e smokes não podem ser iniciados pelos agentes conforme as skills atuais; a recertificação integrada e os testes Unity precisam de execução manual autorizada no contexto do usuário.
- O QAFramework atual declara Unity `6000.5.0f1`, resolve Framework `.7` via `file:` sem hash imutável e contém focos versionados 042/043/044/045; o agregado Player histórico não está no source atual e sua arquitetura deve ser reconstruída no padrão QA-001.
- Os smokes focused de câmera testam contratos próprios e não substituem Player Actor occurrence após IF-ADR-038.
- FirstGame usa referência local sem hash em lock; as provas antigas são evidência comportamental dos seus logs, mas não proveniência reprodutível de SHA.
- Não há base para promover `IPlayerSessionScopedAccess` em bloco ou estabilizar `ActorProfile`, Session Profile, Manager-Provisioned, Reader, readiness e Session Camera por associação.

## Checklist manual de validação

- Abrir Unity `6000.5.0f1` com QAFramework/runner e package resolvido no SHA candidato documentado; capturar manifest e lock.
- Executar NUnit EditMode das assemblies Player/Actor/Input/Camera dependentes necessárias, anotando contagens e resultados por filtro.
- Executar Full Player certification; exigir linha terminal completa e cleanup/restoration aprovado.
- Executar regressões focalizadas para Actor occurrence IF-ADR-038, Session/Manager-Provisioned e Reader/Input gate; usar dois dispositivos sintéticos determinísticos para ownership e separar prova de hardware quando houver contrato específico.
- Executar FirstGame ManagerProvisioned, CharacterSelection, CharacterSelectionMultiplayerSplitScreen e LocalMultiplayer nos cenários existentes; capturar SHA do package local efetivamente importado em cada execução. Não editar o consumidor durante esta execução.
- Conferir logs e diffs documentais/implementação, hashes de origem e estado restaurado; nenhum critério é PASS sem evidência dessa execução.

