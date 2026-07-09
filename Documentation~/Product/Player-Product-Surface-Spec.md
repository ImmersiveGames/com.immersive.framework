# Player Product Surface Spec

Status: Product specification, no implementation
Last updated: 2026-07-09
Package: `com.immersive.framework`

## 1. Objetivo

Definir a primeira vertical slice de produto do Immersive Framework 1.0:

```text
Player Recipe / Player Composer
```

Este documento define o shape esperado da superficie de produto. Ele nao implementa `PlayerRecipe`, `PlayerComposer`, `PlayerRuntimeContext`, validators, smokes ou tooling.

## 2. Problema que resolve

O Player atual possui bons contratos tecnicos, mas a experiencia de uso ainda e tecnica demais.

Em um consumidor real, criar um Player exige conhecer e configurar diretamente:

- actor identity;
- player slot;
- PlayerInput;
- input gate;
- bridge/activation targets;
- reset subject;
- reset participants;
- camera anchors;
- binding targets;
- validators de coerencia.

Isso prova que a feature e configuravel, mas nao que ela e authoravel.

O Player Product Surface deve transformar essa pilha tecnica em uma experiencia de criacao e configuracao orientada a dominio.

## 3. Publico-alvo

Publico principal:

- designers tecnicos;
- gameplay programmers;
- integradores Unity que montam prefabs e cenas;
- times que usam FIRSTGAME ou samples como referencia de uso real.

Publico secundario:

- autores de QA;
- mantenedores do package;
- usuarios avancados que precisam inspecionar materializacao tecnica.

## 4. Modelo de autoridade

`PlayerComposer` e a superficie principal de authoring da instancia.

`PlayerRecipe` e a intencao reutilizavel recomendada.

`_Framework/_Bindings` e materializacao tecnica, nao autoridade principal.

Diagnostics confirmam o resultado, mas nao criam a feature.

O Composer nao deve virar `PlayerManager`. Ele nao executa gameplay, nao centraliza todos os players do jogo e nao substitui ownership de Route, Activity, Input, Camera ou Reset.

## 5. Player Recipe

`PlayerRecipe` futuro deve representar intencao reutilizavel de Player.

Responsabilidades esperadas:

- actor identity padrao;
- player slot padrao;
- gameplay action map esperado;
- reset policy;
- camera target policy;
- required bindings;
- authoring validation policy.

O primeiro MVP pode permitir `PlayerComposer` sem Recipe obrigatoria. Mesmo assim, templates oficiais devem caminhar para uso de Recipe, para evitar configuracao manual repetida.

`PlayerRecipe` nao deve conter paths ou IDs de FIRSTGAME, nem nomes `FG_*` / `firstgame.*`.

## 6. Player Composer

`PlayerComposer` futuro deve ser o ponto principal de autoria da instancia de Player.

Responsabilidades:

- centralizar configuracao do player;
- aplicar/reconstruir bindings tecnicos;
- manter referencias tipadas;
- evitar lookup por nome;
- expor Inspector designer-first;
- oferecer Advanced/Debug para evidencias tecnicas;
- nao executar gameplay;
- nao virar `PlayerManager`;
- nao criar spawn/multiplayer/save/movement framework-owned neste corte.

O Composer pode apontar para gameplay components owned pelo consumidor, mas nao deve assumir ownership do comportamento de jogo.

## 7. Hierarquia Unity desejada

Alvo conceitual:

```text
Player
  PlayerComposer
  PlayerInput
  Gameplay components

  Anchors
    CameraTarget
    LookAtTarget

  _Framework
    _Bindings
      technical framework components
```

Essa hierarquia e direcao de produto, nao promessa de migracao imediata.

A implementacao futura deve validar dependencias locais antes de mover componentes tecnicos. Alguns componentes podem precisar permanecer no root ou em objetos especificos por restricao Unity, serializacao, lifecycle ou requisitos de runtime.

## 8. Apply / Rebuild

`Apply` ou `Rebuild` deve materializar ou reparar o estado tecnico derivado do Composer/Recipe.

Responsabilidades esperadas:

- criar bindings ausentes;
- reparar referencias tipadas;
- atualizar actor/slot declarations derivadas;
- conectar PlayerInput e action map esperado;
- conectar camera target e look-at target;
- configurar reset subject e reset policy;
- preservar componentes gameplay do consumidor;
- falhar com diagnostics claros quando uma dependencia obrigatoria estiver ausente.

Regras:

- idempotente;
- sem duplicar componentes;
- sem fallback silencioso;
- sem lookup por nome como identidade funcional;
- sem criar gameplay behavior;
- sem criar smoke/validator como pre-requisito de uso.

## 9. Materializacao tecnica

Materializacao tecnica pode incluir, conforme decisao futura:

- actor declaration;
- player slot declaration;
- player slot occupancy;
- player input gate adapter;
- player input bridge target;
- player input activation target;
- player control binding target;
- reset subject adapter;
- reset participants derivados;
- camera anchor host ou bindings equivalentes;
- diagnostics/evidence components.

Esses componentes devem ficar preferencialmente sob:

```text
Player/_Framework/_Bindings
```

ou em outra estrutura tecnica equivalente aprovada.

`_Framework/_Bindings` deve ser inspecionavel, mas nao deve ser a primeira camada de autoria.

## 10. Inspector Designer / Advanced / Debug

### Designer

Campos de intencao:

- Player display name;
- actor identity;
- player slot;
- PlayerInput reference;
- gameplay action map;
- camera target policy;
- reset policy;
- recipe reference opcional;
- Apply/Rebuild action.

### Advanced

Campos tecnicos controlados:

- binding policy;
- generated object policy;
- reset scope;
- input activation policy;
- camera anchor policy;
- validation strictness.

### Debug

Evidencias e diagnostics:

- resolved actor id;
- resolved player slot id;
- resolved PlayerInput;
- action map status;
- generated binding list;
- reset subject status;
- camera target status;
- last Apply/Rebuild result;
- diagnostics issues.

## 11. Runtime Context minimo

`PlayerRuntimeContext` e uma necessidade futura quando houver comportamento real em Play Mode.

Ele nao deve ser criado neste corte documental.

Quando existir, devera ser:

- tipado;
- escopado;
- sem singleton;
- sem service locator;
- sem lookup global implicito;
- sem fallback silencioso.

Responsabilidades candidatas:

- current actor identity;
- current player slot;
- current PlayerInput binding;
- current input activation state;
- current camera target references;
- current reset subject registration.

Fora do runtime context minimo:

- movement;
- gameplay commands;
- spawn;
- multiplayer join;
- save/progression.

## 12. Diagnostics

Diagnostics devem confirmar o resultado de authoring, nao criar a feature.

Validators existentes ou futuros devem confirmar:

- Apply/Rebuild foi executado corretamente;
- bindings tecnicos estao materializados;
- identities sao tipadas;
- `PlayerInput` e action map estao consistentes;
- camera target/reset/input bindings estao coerentes;
- nao ha lookup por nome;
- nao ha fallback silencioso.

Smokes podem exercitar comportamento depois que a superficie oficial existir.

Diagnostics nao devem ser o fluxo principal de criacao.

## 13. Fora de escopo

Fora deste corte e da primeira especificacao:

- codigo C#;
- `PlayerComposer` real;
- `PlayerRecipe` real;
- `PlayerRuntimeContext` real;
- validators novos;
- smokes novos;
- alteracao de runtime;
- alteracao de editor tooling;
- alteracao de asmdefs;
- alteracao de FIRSTGAME;
- alteracao de QAFramework;
- scripts FIRSTGAME;
- paths, IDs ou nomes `FG_*` / `firstgame.*`;
- spawn;
- multiplayer join;
- save/progression;
- movement framework-owned;
- gameplay command execution;
- `PlayerManager` global.

## 14. Criterios de aceite tecnico

Uma implementacao futura da superficie deve:

- compilar;
- preservar contratos existentes;
- nao alterar packages tecnicos congelados sem decisao explicita;
- nao usar fallback silencioso;
- nao usar nome/path como identidade funcional;
- manter referencias tipadas;
- nao introduzir singleton ou service locator;
- gerar diagnostics claros;
- manter materializacao idempotente;
- nao quebrar consumidores existentes sem migracao planejada.

## 15. Criterios de aceite de produto

Uma implementacao futura da superficie deve permitir que o usuario:

- crie um Player do zero por uma surface clara;
- configure intencao sem conhecer bridges/adapters internos;
- entenda o Inspector basico;
- rode Apply/Rebuild quando necessario;
- veja materializacao tecnica em Advanced/Debug;
- conecte PlayerInput e camera targets sem lookup por nome;
- use reset policy sem configurar subject/participant manualmente quando a policy for derivavel;
- valide o resultado por diagnostics sem depender deles para criar a feature;
- prove o fluxo em FIRSTGAME como consumidor real.

## 16. Relacao com FIRSTGAME

FIRSTGAME revelou o problema que esta spec resolve.

O package oficial nao deve copiar:

- scripts FIRSTGAME;
- facades FIRSTGAME;
- paths `Assets/_Project/...`;
- IDs `firstgame.*`;
- nomes `FG_*`;
- estrutura exata de cena.

FIRSTGAME deve ser usado para validar usabilidade futura:

- o Player consegue ser criado/configurado com a surface oficial;
- a pilha tecnica deixa de ser UX principal;
- validators deixam de ser fluxo principal;
- Apply/Rebuild oficial substitui repair local de facade.

## 17. Relacao com QAFramework

QAFramework deve validar a superficie oficial depois que ela existir.

Ele deve cobrir:

- casos positivos;
- casos negativos;
- drift de materializacao;
- regressao de Apply/Rebuild;
- diagnostics de identities, PlayerInput, camera target e reset binding.

QAFramework nao deve definir a UX principal e nao deve ser necessario para criar um Player em um projeto consumidor.

