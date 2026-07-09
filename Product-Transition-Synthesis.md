# Product Transition Synthesis - Immersive Framework 1.0

Status: sintese de direcao de produto, sem implementacao
Data: 2026-07-09
Fontes principais:

- `Product-Transition-Audit-Package.md`
- `Assets/_Project/Documentation/Product-Transition-Audit-FIRSTGAME.md`

## Pergunta principal

Qual e a menor direcao de produto necessaria para impedir que o Immersive Framework continue evoluindo como contratos + validators + smokes?

Resposta direta: a menor direcao necessaria e instituir um modelo obrigatorio de Product Surface para qualquer feature recorrente do framework:

```text
Recipe/Profile/Template
+ Composer/Authoring
+ Apply/Rebuild idempotente
+ Materializacao tecnica em _Framework/_Bindings ou equivalente
+ Runtime Context tipado quando houver comportamento em Play Mode
+ Diagnostics como suporte
+ Sample/template minimo quando a feature for de uso comum
```

Isso deve ser decidido antes de novos cortes funcionais. A primeira prova deve ser uma vertical slice de `Player Composer / Player Recipe`, porque o FIRSTGAME mostra que Player e o maior vazamento tecnico no consumidor real e tambem cruza Input, Camera anchors e Reset. Route/Activity continua sendo o centro do framework, mas ja tem runtime forte; Player e onde a falta de produto authoravel aparece com mais custo para um jogo real.

## 1. Diagnostico final

### O que o package ja faz bem tecnicamente

O package ja tem um nucleo runtime real. `GameApplication`, boot, Route, Activity, Scene Lifecycle, Pause, Loading, Transition e Reset nao sao apenas contratos passivos. Existe execucao em Play Mode, lifecycle, gates, loading surface, transition orchestration, pause state, reset registry/executor e triggers que operam no jogo.

Route/Activity e o eixo mais maduro como runtime. O package consegue carregar cenas, operar route/activity transitions, manter estado, executar content lifecycle e integrar Global UI. Reset/Restart tambem esta acima da media: subjects, participants, group reset, activity restart e runtime objects ja formam um caminho real, ainda que a autoria seja tecnica.

Tambem ha bons contratos: ids tipados, rejeicao de fallback silencioso em pontos importantes, validators ricos e diagnostics extensos. Isso e uma base tecnica forte, nao o problema.

### O que o FIRSTGAME provou em uso real

O FIRSTGAME provou que o framework roda um fluxo minimo de jogo real:

- Menu -> Start Game -> Gameplay.
- Startup Activity.
- Player minimo.
- Pause, Loading e Transition.
- Reset Player, Reset Room e Restart Activity.
- Runtime Box entrando no reset registry.
- Camera e BGM reagindo a Route/Activity.

Isso e importante porque FIRSTGAME nao e QA sintetico. Ele prova que os contratos do package podem compor um jogo pequeno.

Mas FIRSTGAME tambem provou a dor principal: o fluxo ainda e tecnico demais. O jogador existe porque uma pilha de componentes e referencias foi montada corretamente. A camera existe porque um tool local configura roots, rigs, anchors e bindings. O player binding e sustentado por menus de validate/apply/repair. O designer enxerga adapters, triggers, bridges, subject ids, slot ids, action maps e bindings internos como se fossem a UX final.

### Onde a experiencia ainda falha como produto

A falha nao e falta de runtime em todos os sistemas. A falha e falta de superficie authoring-first.

Hoje, para muitas features, o usuario precisa:

- Saber quais componentes adicionar.
- Saber quais IDs e reasons preencher.
- Saber quais referencias tipadas sao obrigatorias.
- Saber quando usar um validator.
- Saber qual menu local aplica ou repara wiring.
- Interpretar logs para saber se o jogo esta pronto.

Isso ainda e "configuravel", nao "authoravel".

O package tem assets de intencao em alguns pontos (`GameApplicationAsset`, `RouteAsset`, `ActivityAsset`, content profiles), mas eles nao fecham o fluxo. `routeContentProfile` e `activityContentProfile` ainda aparecem vazios no FIRSTGAME, e a intencao real fica materializada diretamente em cena por bindings/adapters.

### Por que validators/smokes nao podem continuar sendo a experiencia principal

Validators e smokes respondem "o wiring esta correto?". Eles nao respondem "como o usuario cria isso?".

Quando o fluxo principal e rodar validator/smoke:

- O usuario aprende o erro, nao a intencao.
- A feature fica dependente de conhecimento interno.
- O Inspector vira diagnostico, nao authoring.
- O package parece maduro tecnicamente, mas continua hostil para criar jogo real.
- FIRSTGAME vira prova de que alguem conseguiu configurar, nao prova de que o produto e facil de usar.

Diagnostics sao obrigatorios, mas devem ser a segunda camada. A primeira camada deve ser Product Surface.

## 2. Mudanca de visao

### Antes

```text
feature = componentes + validator + smoke
```

Uma feature era considerada madura quando existiam componentes, contratos, validators, smokes e logs suficientes para provar que o comportamento podia ser configurado.

### Depois

```text
feature = Recipe/Profile/Template
        + Composer/Authoring
        + Apply/Rebuild
        + Materializacao tecnica
        + Runtime Context quando necessario
        + Diagnostics
        + Samples/Templates
```

Uma feature so e produto quando o usuario consegue criar, configurar, aplicar, entender, depurar e usar em jogo real sem tratar componentes internos como UX principal.

### Papel de cada camada

#### Recipe/Profile/Template

Define a intencao reutilizavel. Deve responder "o que este jogo quer?" em termos de dominio: Player, Route, Activity, Global UI, Camera, Resettable Object, Input Profile.

Nao deve expor ids internos, bridges ou detalhes de materializacao como primeira camada.

#### Composer/Authoring

E a superficie concreta de criacao e configuracao em cena, prefab ou asset. Deve responder "como crio isso do zero?".

Pode ser um wizard, inspector authoring component, create menu ou editor window, mas precisa ser idempotente e orientado a dominio.

#### Materializacao tecnica

E o resultado derivado: bindings, adapters, anchors, generated objects, `_Framework/_Bindings`, references e contracts que o runtime precisa.

Materializacao tecnica pode existir e ser inspecionavel, mas nao deve ser o caminho primario de autoria. Campos tecnicos pertencem a Advanced/Debug.

#### Runtime Context / Session / Service escopado

Existe quando a feature opera em Play Mode e precisa de autoridade runtime clara.

Deve ser tipado, escopado e dono de um dominio. Nao pode virar singleton, service locator ou `Manager` global implicito. Uma eventual Session deve orquestrar contexts tipados, nao substituir ownership de Route, Activity, Player, Input, Camera, Reset ou UI.

#### Diagnostics

Inclui validators, smokes, readiness checks, facts, logs e QA canvases. Serve para confirmar, explicar e diagnosticar. Nao cria a feature e nao e UX principal.

#### Samples/Templates

Provam o fluxo de uso oficial. Um sample minimo deve mostrar como uma feature nasce pelo Product Surface oficial, nao por uma sequencia de menus de reparo ou wiring manual.

## 3. Sistemas por prioridade de produto

| Sistema | Estado atual | Problema de produto | Direcao recomendada | Prioridade |
|---|---|---|---|---|
| Player | Contratos bons, declarations e validators; no FIRSTGAME funciona com muitos componentes expostos | Maior vazamento tecnico: identity, slot, input bridge, gate, reset e camera anchors ficam no Player GameObject | `Player Recipe` + `Player Composer` + materializacao em `_Framework/_Bindings`; definir `Player Runtime Context` apenas no escopo minimo necessario | P0 |
| Route / Activity | Runtime forte e assets existentes; FIRSTGAME prova fluxo real | Criacao completa ainda exige bindings, roots, profiles vazios e triggers manuais | Formalizar `Route Recipe`, `Activity Recipe` e Composer oficial; manter runtime atual como base | P0 |
| Validators / QA tools | Muito fortes tecnicamente | Em varias areas viraram o fluxo mais concreto de uso | Rebaixar para Diagnostics; nenhum sistema novo deve ser aceito so por validator/smoke | P0 |
| Docs / Samples / Templates | Docs tecnicas existem; samples/templates oficiais ausentes | Usuario aprende logs/smokes, nao fluxo de criacao | Criar guia product-first e sample minimo por vertical slice | P0 |
| Camera | Runtime local pratico via director; FIRSTGAME depende de setup editor local | Rigs e anchors funcionam, mas bindings e director aparecem como UX | `Camera Recipe` + `Camera Composer`, usando Route/Activity e Player anchors | P1 |
| Global UI / Pause / Loading / Transition | Runtime real forte; FIRSTGAME tem UIGlobal funcional | Criacao de surfaces depende de cena/adapters manuais | `Global UI Recipe` + `Surface Composer` + template de UIGlobal | P1 |
| Reset / Objects | Reset runtime maduro; FIRSTGAME prova player/runtime box resetaveis | Subject/participant/scope/idGeneration ainda sao tecnicos | `Resettable Object Recipe` + `Reset Composer`; materializar adapters derivados | P1 |
| Input | PlayerInput funciona; pause input existe; player input binding e tecnico | Action maps, slots, bridge/gate/activation targets vazam | `Input Profile` + Input materialization via Player/Global UI Composer | P1 |
| Content / Anchors | Content roots e anchors funcionam parcialmente; RuntimeContent tem contratos | Bridges, localContentId, anchor owner e materialization sao tecnicos | `Content/Anchor Composer` depois de Route/Activity e Camera; esconder bridges | P2 |
| Save / Preferences / Progression | Contratos/stores existem no package; FIRSTGAME nao prova uso | Ausente como produto authoravel | Definir depois de `Save Runtime Context` e save/profile UX; nao priorizar agora | Later |

## 4. Primeira vertical slice recomendada

### Vertical slice escolhida

`Player Composer / Player Recipe`.

### Por que ela vem primeiro

A hipotese esta confirmada.

Route/Activity e o centro conceitual do framework, mas ja tem runtime forte e assets de base. Player e onde o consumidor real mostrou o maior custo de usabilidade: para ter um player minimo, o FIRSTGAME precisou expor declarations, slot, occupancy, input gate, bridge target, activation target, reset subject, transform participant, camera anchors e validadores de binding.

Player tambem e a melhor primeira prova de Product Surface porque cruza quatro dores reais:

- Input.
- Camera anchors.
- Reset.
- Identity/Actor/Slot.

Se o framework consegue transformar Player de pilha tecnica em fluxo authorable, o mesmo modelo se aplica a Camera, Resettable Object, Global UI e Content.

### O que ela precisa provar como produto

A vertical slice deve provar:

- Um usuario cria um player oficial do framework sem conhecer a pilha interna.
- A intencao vive em `Player Recipe` ou authoring equivalente.
- `Apply/Rebuild` cria ou repara bindings/adapters de forma idempotente.
- Componentes tecnicos ficam materializados em `_Framework/_Bindings` ou em area Advanced/Debug.
- O Inspector basico fala em Player, Slot, Input, Camera Target e Reset, nao em bridge/gate/evidence.
- Diagnostics confirmam o resultado, mas nao substituem a criacao.
- FIRSTGAME consegue usar a superficie oficial sem copiar a facade local como API final.

### O que fica fora dela

Fora da primeira slice:

- Join multiplayer.
- Spawn system generico.
- Movement framework-owned.
- Gameplay command execution.
- Save/progression.
- Camera Composer completo.
- Route/Activity Composer completo.
- Session generica.
- Copia dos IDs, paths e scripts FIRSTGAME.

O Player Composer pode aceitar um `PlayerInput`, um actor/slot target e um camera target, mas nao precisa virar dono de todo gameplay.

### Como ela vira referencia para os proximos sistemas

Ela define o padrao de produto:

```text
Recipe -> Composer -> Apply/Rebuild -> _Framework/_Bindings -> Runtime Context minimo -> Diagnostics
```

Depois, Camera, Global UI, Reset e Route/Activity devem seguir o mesmo contrato de produto. A slice de Player vira o primeiro exemplo oficial de como esconder materializacao tecnica sem perder diagnostico.

## 5. Cadeia minima de ADRs de produto

Quatro ADRs resolvem a mudanca de visao agora. Nao criar dez ADRs antes de provar a primeira superficie.

### ADR-PROD-0001 - Product Surface Model

Objetivo: definir o modelo oficial de Product Surface do framework.

Decisao que precisa registrar: toda feature recorrente deve declarar se possui Recipe/Profile/Template, Composer/Authoring, Apply/Rebuild, materializacao tecnica, Runtime Context e Diagnostics. Quando alguma camada nao existir, isso deve ser decisao explicita, nao omissao.

Por que e necessario agora: impede que novos cortes terminem apenas em componentes + validator + smoke.

Fora de escopo: implementacao de qualquer composer especifico.

Sistemas afetados: todos.

### ADR-PROD-0002 - Diagnostics Are Not Product UX

Objetivo: separar UX principal de diagnostics.

Decisao que precisa registrar: validators, smokes, readiness checks, QA canvas e repair proofs sao suporte tecnico. Eles nao podem ser criterio suficiente para declarar uma feature authoravel.

Por que e necessario agora: Package e FIRSTGAME mostram validators como caminho principal em Player, Camera e readiness.

Fora de escopo: remover validators existentes.

Sistemas afetados: Validators/QA, Player, Route/Activity, Camera, Reset, Content, Docs.

### ADR-PROD-0003 - Domain Runtime Context Policy

Objetivo: definir quando criar runtime authority e como evitar Session generica.

Decisao que precisa registrar: runtime authority deve ser por dominio, tipada, escopada e sem singleton/service locator. Uma Session, se existir, apenas coordena contexts explicitos; nao vira dono generico.

Por que e necessario agora: Player, Input, Camera, Content e Save precisam de autoridade, mas criar um `PlayerManager` ou `Session` generica repetiria o problema antigo.

Fora de escopo: desenhar API final de cada context.

Sistemas afetados: Player, Route, Activity, Input, Camera, Reset, Global UI, Content, Save.

### ADR-PROD-0004 - First Reference Product Surface

Objetivo: escolher a primeira vertical slice oficial.

Decisao que precisa registrar: `Player Recipe / Player Composer` e a primeira referencia de Product Surface, com FIRSTGAME como consumidor real e QA tecnico depois do contrato oficial.

Por que e necessario agora: sem uma primeira slice, a mudanca de visao fica abstrata.

Fora de escopo: Route/Activity Composer completo, Camera Composer completo, Save, multiplayer join/spawn.

Sistemas afetados: Player, Input, Camera anchors, Reset, FIRSTGAME, QA.

## 6. Direcoes descartadas

Descartar como direcao de produto:

- Validators como fluxo principal.
- Smoke como prova de maturidade de produto.
- Facade tecnica como UX final.
- Copiar FIRSTGAME para o package.
- Copiar paths, IDs e nomes `firstgame.*` / `FG_*`.
- `PlayerManager` global.
- Session generica sem ownership por dominio.
- Singleton/service locator para resolver dependencies obrigatorias.
- Name/path-based binding como fonte de identidade.
- Bridges/materialization adapters como primeira camada de Inspector.
- Esconder componentes tecnicos sem Advanced/Debug inspecionavel.
- `Manager`, `Coordinator` ou `Processor` como container para ownership indefinido.
- Expandir Save/Progression antes de definir produto e runtime authority.
- Ampliar QA antes de existir contrato oficial de produto para a feature.
- Tratar `_Framework/_Bindings` como autoridade principal. Ele e materializacao tecnica, nao modelo de dominio.

## 7. Runtime Contexts

Posicao: runtime authority deve existir quando ha comportamento real em Play Mode, mas sempre por dominio. Nao criar Session generica como solucao magica.

| Context | Classificacao | Direcao |
|---|---|---|
| Route Runtime Context | Ja existe | Formalizar como boundary de produto/modulos. Nao reimplementar. |
| Activity Runtime Context | Ja existe | Formalizar current activity, restart/clear/reenter e content lifecycle como APIs de dominio. |
| Reset Runtime Context | Ja existe/parcial | ResetRegistry/Executor ja sao runtime real; precisa superficie de produto para subject/participant setup. |
| Global UI Runtime Context | Ja existe/parcial | Pause/Loading/Transition existem; falta Product Surface para criar surfaces. |
| Camera Runtime Context | Parcial | `FrameworkCameraDirector` decide rigs, mas falta context oficial por Route/Activity/Player anchors. |
| Player Runtime Context | Parcial/ausente | Existem declarations e bindings; falta autoridade clara de player slot/current actor/input/camera target. Prioridade P0 no escopo minimo. |
| Input Runtime Context | Parcial/ausente | Existem adapters/gates; falta ownership claro de action map por player/UI/pause. Deve vir acoplado ao Player/Global UI Composer. |
| Content Runtime Context | Parcial | RuntimeContent e anchors existem, mas placement/materialization ainda vaza como bridge. Prioridade P2. |
| Save Runtime Context | Ausente como produto | Contratos existem; nao priorizar agora. |

Como evitar singleton, service locator ou manager global implicito:

- Cada context deve receber dependencies explicitas do host/core ou do lifecycle owner.
- Acesso deve ser tipado por dominio, nao lookup por string.
- Identidade deve usar ids tipados e referencias authoring validadas.
- Missing required config deve falhar rapido.
- Nenhum context deve procurar globalmente "o melhor componente" como fallback silencioso.
- Session, se criada, deve ser envelope de lifetime e coordenacao, nao registry universal.

## 8. Criterio novo de aceite

### Criterios tecnicos

- Compila.
- Passa QA tecnico aplicavel.
- Sem fallback silencioso.
- Logs diagnosticaveis.
- Contratos preservados.
- Runtime authority tem owner claro.
- Config obrigatoria falha rapido.
- Identidade nao e derivada de nome/path.

### Criterios de produto

- Usuario cria a feature por Create menu, wizard, template ou Composer.
- Usuario configura intencao em Recipe/Profile/Template ou authoring surface clara.
- Inspector basico e compreensivel para designer.
- Campos tecnicos ficam em Advanced/Debug.
- Apply/Rebuild existe quando ha materializacao tecnica derivada.
- Materializacao tecnica e idempotente e inspecionavel.
- Diagnostics confirmam o resultado, mas nao sao o fluxo principal.
- Sample/template existe quando a feature for comum.
- FIRSTGAME prova uso real quando aplicavel.
- QA tecnico vem depois que a superficie oficial existe.

Uma feature nao deve ser considerada produto apenas porque compila, passa smoke e emite logs corretos.

## 9. Ordem de formalizacao

Ordem curta:

1. Criar os quatro ADRs minimos: Product Surface Model, Diagnostics Are Not Product UX, Domain Runtime Context Policy, First Reference Product Surface.
2. Definir a primeira vertical slice: `Player Recipe / Player Composer`.
3. Criar docs/samples minimos da slice antes de expandir sistemas laterais.
4. Validar a slice no FIRSTGAME como consumidor real, removendo dependencia de facade local como UX principal.
5. So entao criar QA tecnico oficial para a superficie nova.

Nao criar plano tecnico detalhado ainda. A proxima etapa e decisao de produto compacta, seguida de uma vertical slice pequena e completa.

## 10. Resumo executivo final

### Decisao de direcao em 5 bullets

- O Immersive Framework 1.0 deve parar de aceitar "componentes + validators + smokes" como definicao de feature pronta.
- Toda feature recorrente precisa de Product Surface: Recipe/Profile/Template, Composer/Authoring, Apply/Rebuild, materializacao tecnica, runtime authority quando necessario e Diagnostics.
- Validators e smokes continuam obrigatorios, mas como diagnostics, nao como UX principal.
- FIRSTGAME deve continuar sendo prova de usabilidade real, nao fonte oficial a ser copiada.
- A primeira prova da nova visao deve atacar o maior vazamento tecnico visto no consumidor: Player.

### Primeira vertical slice recomendada

`Player Recipe / Player Composer`, com materializacao de input binding, player identity, reset subject e camera target bindings fora da UX principal.

### ADRs minimos recomendados

- `ADR-PROD-0001 - Product Surface Model`
- `ADR-PROD-0002 - Diagnostics Are Not Product UX`
- `ADR-PROD-0003 - Domain Runtime Context Policy`
- `ADR-PROD-0004 - First Reference Product Surface`

### O que nao fazer

Nao copiar FIRSTGAME, nao transformar facade tecnica em API final, nao criar `PlayerManager`, nao criar Session generica, nao esconder bridges sem Advanced/Debug, nao expandir QA antes de existir superficie de produto.

### Proximo artefato a criar

Criar `ADR-PROD-0001 - Product Surface Model` como ADR compacto e decisivo. Ele deve estabelecer o contrato de produto que todos os proximos cortes precisam obedecer.

## Validacao

- Sintese baseada em leitura estatica dos dois relatorios de auditoria.
- Nenhum arquivo runtime/editor foi alterado.
- Nenhum codigo foi implementado.
- Unity, Play Mode, build, smoke e batchmode nao foram executados.

