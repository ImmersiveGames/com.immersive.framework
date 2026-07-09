# Product Surface Index

Status: Product direction
Last updated: 2026-07-09
Package: `com.immersive.framework`

## 1. Proposito

Este indice define a nova direcao de produto do Immersive Framework 1.0:

```text
produto authoravel + contratos tecnicos + runtime real + diagnostico
```

O framework nao deve evoluir como uma colecao de componentes soltos, validators e smokes. Contratos tecnicos e diagnostics continuam obrigatorios, mas a experiencia principal deve ser criar, configurar, aplicar, entender e usar features em um jogo real.

## 2. Modelo Product Surface

Toda feature recorrente do framework deve ser avaliada pelo modelo abaixo:

```text
Recipe / Profile / Template
+ Composer / Authoring Component
+ Apply / Rebuild
+ Materializacao tecnica
+ Runtime Context quando necessario
+ Diagnostics
```

Nem toda feature precisa implementar todas as camadas no primeiro corte, mas a ausencia de uma camada deve ser uma decisao explicita de produto.

## 3. Papel de cada camada

### Recipe / Profile / Template

Representa intencao reutilizavel.

Exemplos de intencao:

- que tipo de Player o jogo usa;
- qual Route inicia uma aplicacao;
- como uma Activity configura camera, audio e reset;
- como uma Global UI oferece pause, loading e transition.

Recipes e Profiles devem falar a linguagem do usuario. Eles nao devem expor bindings, adapters, bridges ou IDs internos como primeira camada.

### Composer / Authoring Component

Representa a instancia concreta em prefab ou cena.

Um Composer deve:

- centralizar a configuracao de uma feature;
- expor Inspector designer-first;
- receber ou criar referencias tipadas;
- aplicar/reconstruir materializacao tecnica;
- evitar lookup por nome;
- deixar evidencias tecnicas em Advanced/Debug.

### Apply / Rebuild

Materializa ou repara contratos tecnicos derivados da intencao.

`Apply` ou `Rebuild` deve ser idempotente: rodar de novo nao deve duplicar bindings, criar objetos redundantes ou esconder drift. Quando nao for possivel aplicar automaticamente, o fluxo deve falhar com diagnostico claro.

### Materializacao tecnica

E o conjunto de objetos e componentes que o runtime precisa, mas que nao deve ser a UX principal.

Pode incluir:

- bindings;
- adapters;
- declarations;
- generated references;
- containers `_Framework/_Bindings`;
- evidence components;
- debug-only support objects.

`_Framework/_Bindings` e materializacao tecnica. Ele nao e a autoridade principal de produto.

### Runtime Context

Autoridade runtime tipada quando houver comportamento real em Play Mode.

Um Runtime Context deve ser:

- tipado;
- escopado;
- sem singleton obrigatorio;
- sem service locator;
- sem lookup global implicito;
- sem fallback silencioso.

Uma Session futura pode coordenar contexts, mas nao deve virar owner generico de todos os dominios.

### Diagnostics

Inclui validators, smokes, logs, reports, facts e QA.

Diagnostics confirmam e explicam o resultado. Eles nao criam a feature e nao substituem a experiencia principal de uso.

## 4. Primeira vertical slice: Player

A primeira vertical slice de produto e:

```text
Player Recipe / Player Composer
```

Motivo:

- FIRSTGAME mostrou que Player e o maior vazamento tecnico no consumidor real.
- Player cruza Input, Camera anchors, Reset, Actor identity e Player Slot.
- O package ja possui contratos tecnicos importantes, mas ainda nao possui superficie authoring-first.

Direcao:

- `PlayerComposer` e a superficie principal de authoring da instancia.
- `PlayerRecipe` e a intencao reutilizavel recomendada.
- `_Framework/_Bindings` materializa componentes tecnicos derivados.
- Diagnostics confirmam Apply/Rebuild, mas nao sao o fluxo principal.

## 5. Proximas superficies candidatas

Depois da vertical slice de Player, as candidatas naturais sao:

1. `Route Recipe / Route Composer`
2. `Activity Recipe / Activity Composer`
3. `Camera Recipe / Camera Composer`
4. `Global UI Recipe / Surface Composer`
5. `Resettable Object Recipe / Reset Composer`
6. `Input Profile / Input Composer`
7. `Content Anchor Recipe / Content Composer`
8. `Save Profile / Save Composer`

Prioridade deve seguir dor real de uso, maturidade runtime e capacidade de validar em FIRSTGAME sem copiar solucoes FIRSTGAME para o package.

## 6. Regra sobre diagnostics

Validators e smokes sao suporte tecnico.

Eles nao sao a experiencia principal de uso.

Uma feature nao deve ser considerada produto apenas porque:

- compila;
- passa smoke;
- passa validator;
- emite logs corretos;
- pode ser montada manualmente por alguem que conhece os internals.

O criterio de produto e diferente: o usuario deve conseguir criar, configurar, aplicar/reconstruir, entender e usar a feature.

## 7. Relacao com FIRSTGAME e QAFramework

### FIRSTGAME

FIRSTGAME e consumidor real e prova de usabilidade.

Ele deve revelar:

- onde o framework e dificil de usar;
- onde o Inspector vaza componentes tecnicos;
- onde faltam Recipes, Composers, Apply/Rebuild e templates;
- onde validators estao substituindo UX.

FIRSTGAME nao e fonte oficial permanente do framework. O package nao deve copiar paths, IDs, nomes, scripts ou facades FIRSTGAME.

### QAFramework

QAFramework e prova tecnica.

Ele deve validar contratos oficiais, casos negativos, regressao e smokes depois que a superficie oficial existir.

QAFramework nao deve definir a UX principal de uma feature.

