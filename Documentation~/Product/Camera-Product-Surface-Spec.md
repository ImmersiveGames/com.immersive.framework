# Camera Product Surface Spec

Status: Product specification, no implementation
Package: `com.immersive.framework`
Surface: `Camera Recipe / Camera Composer`

## 1. Objetivo

Definir a proxima Product Surface do Immersive Framework 1.0 depois do fechamento do eixo `Player Recipe / Player Composer`.

A superficie alvo e:

```text
Camera Recipe / Camera Composer
```

Este documento define o shape esperado. Ele nao implementa `CameraRecipe`, `CameraComposer`, runtime context, validators, smokes ou tooling.

## 2. Problema que resolve

O eixo Player agora consegue materializar intencao de player, camera targets e look-at targets. Mesmo assim, Camera ainda tende a depender de setup local, rigs, anchors, bindings e configuracoes manuais.

O problema de produto nao e apenas fazer uma camera funcionar. O problema e permitir que um usuario configure uma camera de Route/Activity/Player sem entender todos os contratos tecnicos internos.

Hoje a dor esperada e:

- selecionar ou criar camera rig correta;
- conectar tracking target e look-at target;
- decidir se a camera pertence a Route, Activity ou Player;
- organizar anchors e bindings;
- diagnosticar conflitos de prioridade;
- entender se a camera foi configurada por produto ou por helper local.

Isso ainda tende a ser configuravel, mas nao authorable.

## 3. Publico-alvo

Publico principal:

- designers tecnicos;
- gameplay programmers;
- integradores Unity;
- usuarios que configuram cameras de gameplay em cenas reais.

Publico secundario:

- mantenedores do package;
- autores de QA;
- usuarios avancados inspecionando bindings e evidencias tecnicas.

## 4. Modelo de autoridade

`CameraComposer` deve ser a superficie principal de authoring de uma camera ou camera rig.

`CameraRecipe` deve representar intencao reutilizavel.

`_Framework/_Bindings` e materializacao tecnica, nao autoridade principal.

Camera runtime authority, quando existir, deve ser tipada e escopada. Ela nao deve virar `CameraManager` global, singleton, service locator ou registry universal.

O Composer nao deve executar gameplay e nao deve assumir ownership do Player. Ele pode consumir targets de Player, Route ou Activity, mas nao deve criar ou resolver identidade por nome/path como fonte funcional.

## 5. Camera Recipe

`CameraRecipe` futuro deve representar intencao reutilizavel de camera.

Responsabilidades candidatas:

- camera mode ou rig mode;
- target policy;
- look-at policy;
- scope padrao: Route, Activity ou Player;
- priority policy;
- transition policy;
- fallback policy explicita;
- diagnostics/validation strictness.

O primeiro MVP pode permitir `CameraComposer` sem Recipe obrigatoria. Mesmo assim, templates oficiais devem caminhar para Recipe para evitar configuracao manual repetida.

`CameraRecipe` nao deve conter paths ou IDs FIRSTGAME.

## 6. Camera Composer

`CameraComposer` futuro deve ser o ponto principal de autoria de camera no prefab/cena.

Responsabilidades:

- centralizar configuracao de camera ou rig;
- receber targets tipados ou referencias explicitas;
- aplicar/reconstruir bindings tecnicos;
- evitar lookup por nome;
- expor Inspector designer-first;
- oferecer Advanced/Debug para evidencias tecnicas;
- consumir anchors criados pelo PlayerComposer quando aplicavel;
- nao executar gameplay;
- nao virar `CameraManager`.

## 7. Hierarquia Unity desejada

Alvo conceitual:

```text
CameraRig
  CameraComposer
  Camera / Cinemachine camera or compatible rig

  _Framework
    _Bindings
      technical camera components
```

Quando o rig estiver dentro de uma Route/Activity:

```text
RouteOrActivityRoot
  CameraRig
    CameraComposer
    _Framework
      _Bindings
```

Quando a camera consumir Player targets:

```text
Player
  Anchors
    CameraTarget
    LookAtTarget
```

O Camera Composer deve consumir esses anchors, nao recriar Player authority.

## 8. Apply / Rebuild

`Apply` ou `Rebuild` deve materializar ou reparar o estado tecnico derivado do CameraComposer/Recipe.

Responsabilidades esperadas:

- criar `_Framework/_Bindings` quando necessario;
- conectar tracking target;
- conectar look-at target;
- criar ou reparar camera binding evidence;
- configurar scope/prioridade quando o contrato existir;
- preservar camera rig do consumidor;
- falhar com diagnostics claros quando targets obrigatorios estiverem ausentes;
- reportar created/repaired/alreadyValid/skippedByPolicy/blocked.

Regras:

- idempotente;
- sem duplicar componentes;
- sem fallback silencioso;
- sem lookup por nome como identidade funcional;
- sem criar Player, Route ou Activity automaticamente;
- sem transformar validator/smoke em pre-requisito de uso.

## 9. Materializacao tecnica

Materializacao tecnica pode incluir, conforme decisao futura:

- camera anchor host;
- camera target binding;
- look-at binding;
- route/activity camera binding evidence;
- transition binding evidence;
- diagnostics/evidence components.

Esses componentes devem ficar preferencialmente sob:

```text
CameraRig/_Framework/_Bindings
```

ou em estrutura tecnica equivalente aprovada.

## 10. Inspector Designer / Advanced / Debug

### Designer

Campos de intencao:

- recipe opcional;
- camera display name;
- camera scope;
- camera rig/camera reference;
- tracking target;
- look-at target;
- priority/activation policy;
- transition policy;
- Apply/Rebuild action.

### Advanced

Campos tecnicos controlados:

- bindings root;
- create bindings root if missing;
- target requiredness;
- look-at requiredness;
- route/activity binding policy;
- validation strictness;
- generated object policy.

### Debug

Evidencias e diagnostics:

- resolved camera reference;
- resolved tracking target;
- resolved look-at target;
- generated binding list;
- last Apply/Rebuild result;
- priority/scope evidence;
- diagnostics issues.

## 11. Runtime Context minimo

`CameraRuntimeContext` e futuro e nao deve ser criado neste corte documental.

Quando existir, deve ser:

- tipado;
- escopado;
- sem singleton;
- sem service locator;
- sem lookup global implicito;
- sem fallback silencioso.

Responsabilidades candidatas:

- current active camera binding;
- current tracking target;
- current look-at target;
- current camera scope;
- transition state.

Fora do runtime context minimo:

- gameplay movement;
- player ownership;
- global camera manager;
- spawn;
- save/progression.

## 12. Diagnostics

Diagnostics devem confirmar o resultado de authoring, nao criar a feature.

Validators futuros devem confirmar:

- Apply/Rebuild foi executado corretamente;
- bindings tecnicos estao materializados;
- targets obrigatorios existem;
- referencias sao explicitas;
- nao ha lookup por nome;
- nao ha fallback silencioso;
- Player targets, quando usados, vieram de referencias/anchors claros.

Smokes podem exercitar comportamento depois que a superficie oficial existir.

## 13. Fora de escopo

Fora deste corte e da primeira especificacao:

- codigo C#;
- `CameraComposer` real;
- `CameraRecipe` real;
- `CameraRuntimeContext` real;
- validators novos;
- smokes novos;
- alteracao de runtime;
- alteracao de editor tooling;
- alteracao de asmdefs;
- alteracao de FIRSTGAME;
- alteracao de QAFramework;
- copiar setup FIRSTGAME;
- paths, IDs ou nomes FIRSTGAME;
- CameraManager global;
- spawn/movement/save.

## 14. Criterios de aceite tecnico futuros

Uma implementacao futura deve:

- compilar;
- preservar contratos existentes;
- nao usar fallback silencioso;
- nao usar nome/path como identidade funcional;
- manter referencias explicitas;
- nao introduzir singleton ou service locator;
- gerar diagnostics claros;
- manter materializacao idempotente;
- nao quebrar PlayerComposer ou seus anchors.

## 15. Criterios de aceite de produto futuros

Uma implementacao futura deve permitir que o usuario:

- crie/configure uma camera por superficie clara;
- conecte tracking/look-at target sem mexer em bindings internos;
- entenda o Inspector basico;
- rode Apply/Rebuild quando necessario;
- veja materializacao tecnica em Advanced/Debug;
- use PlayerComposer anchors como fonte de target quando aplicavel;
- valide o resultado por diagnostics sem depender deles para criar a feature;
- prove o fluxo em FIRSTGAME como consumidor real.

## 16. Relacao com Player Product Surface

Camera vem depois de Player porque o PlayerComposer ja cria ou referencia `CameraTarget` e `LookAtTarget`.

CameraComposer deve consumir esses anchors quando aplicavel. Ele nao deve duplicar Player identity, Player slot, input ou reset ownership.

## 17. Relacao com FIRSTGAME

FIRSTGAME deve validar usabilidade futura:

- uma camera real consome targets do PlayerPrototype;
- setup local de camera deixa de ser fluxo principal;
- bindings tecnicos ficam fora da superficie principal;
- diagnostics confirmam o resultado.

Nao migrar para o package:

- scripts FIRSTGAME;
- paths `Assets/_Project/...`;
- IDs/nomeacoes locais;
- estrutura exata de cena.

## 18. Relacao com QAFramework

QAFramework deve validar a superficie oficial depois que ela existir.

Ele deve cobrir:

- Apply/Rebuild positivo;
- idempotencia;
- target ausente bloqueia quando requerido;
- look-at ausente bloqueia quando requerido;
- drift repair;
- ausencia de lookup por nome.

QAFramework nao deve definir a UX principal.
