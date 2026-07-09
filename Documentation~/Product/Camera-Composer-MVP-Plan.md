# Camera Composer MVP Plan

Status: Technical MVP plan, no implementation
Package: `com.immersive.framework`
Surface: `Camera Recipe / Camera Composer`

## 1. Objetivo

Definir o plano tecnico minimo para implementar futuramente `CameraComposer` como segunda Product Surface do Immersive Framework 1.0.

O MVP deve transformar camera de:

```text
setup local + bindings tecnicos + helpers especificos
```

para:

```text
CameraComposer + Apply/Rebuild + materializacao tecnica + diagnostics
```

Este corte nao implementa codigo.

## 2. Escopo do MVP

O MVP deve cobrir uma camera ou camera rig scene-authored/prefab-authored com:

- camera reference explicita;
- tracking target explicito;
- look-at target opcional ou requerido por policy;
- scope de camera simples;
- `_Framework/_Bindings` para materializacao tecnica;
- Apply/Rebuild idempotente;
- diagnostics de Apply/Rebuild;
- consumo de anchors gerados pelo PlayerComposer quando aplicavel.

## 3. Fora de escopo

Fora deste MVP:

- codigo C# neste corte documental;
- `CameraComposer` real neste corte;
- `CameraRecipe` real neste corte;
- `CameraRuntimeContext` real;
- CameraManager global;
- Cinemachine-specific feature completa;
- transicoes visuais completas;
- spawn/movement/save;
- FIRSTGAME setup copy;
- QA smoke.

## 4. Componentes tecnicos candidatos

A implementacao futura deve auditar os componentes reais do package antes de codar.

Candidatos conhecidos/conceituais:

| Componente | Funcao | Necessario no MVP? | Local sugerido | Observacao |
|---|---|---|---|---|
| `CameraComposer` | Authoring/apply/diagnostics da instancia | Sim | Root do CameraRig | Novo componente futuro |
| `CameraRecipe` | Defaults reutilizaveis | Opcional no MVP | Asset | Pode vir apos Composer, como no Player |
| `FrameworkCameraAnchorHost` | Host/evidence de tracking/look-at targets | Provavel | `_Framework/_Bindings` ou rig root | Confirmar API real antes de implementar |
| camera rig/camera reference | Camera real do consumidor | Sim | Root/child do rig | Preservar objeto do consumidor |
| target binding evidence | Evidencia tecnica de target | Provavel | `_Framework/_Bindings` | Nome final depende do package real |
| route/activity camera binding | Escopo por Route/Activity | Opcional/P1 | `_Framework/_Bindings` | Nao bloquear MVP se contrato ainda estiver tecnico |
| transition binding evidence | Ligacao com transitions | Opcional/P1 | `_Framework/_Bindings` | Fora do primeiro MVP |

## 5. Hierarchy alvo

Alvo conceitual:

```text
CameraRig
  CameraComposer
  Camera or compatible camera rig

  _Framework
    _Bindings
      technical camera components
```

Quando consumir Player targets:

```text
Player
  Anchors
    CameraTarget
    LookAtTarget
```

CameraComposer deve referenciar esses targets explicitamente.

## 6. Campos do CameraComposer MVP

### Designer

Campos minimos:

- `recipe` optional;
- `cameraDisplayName`;
- `cameraReference`;
- `trackingTarget`;
- `lookAtTarget`;
- `cameraScope`;
- `trackingTargetRequired`;
- `lookAtTargetRequired`;
- `validationMode`.

### Advanced

Campos tecnicos controlados:

- `frameworkBindingsRoot`;
- `createBindingsRootIfMissing`;
- `materializeAnchorHost`;
- `materializeRouteActivityBinding`;
- `materializeTransitionBinding`;
- `logApplyRebuildDiagnostics`.

### Debug

Campos/evidencias futuras:

- resolved camera;
- resolved tracking target;
- resolved look-at target;
- bindings root status;
- materialized component list;
- last Apply/Rebuild status;
- last blocking issue.

## 7. Apply/Rebuild MVP

Comportamento esperado:

1. Validar campos Designer obrigatorios.
2. Criar `_Framework/_Bindings` quando ausente e policy permitir.
3. Criar ou reparar camera anchor/binding evidence, conforme contrato real.
4. Conectar tracking target explicitamente.
5. Conectar look-at target explicitamente quando policy exigir.
6. Preservar camera rig/camera do consumidor.
7. Nao duplicar componentes.
8. Nao usar nome/path como identidade funcional.
9. Falhar com diagnostics claros quando target obrigatorio estiver ausente.
10. Reportar created/repaired/alreadyValid/skippedByPolicy/blocked.

Apply/Rebuild nao deve:

- criar Player;
- criar Route/Activity;
- criar runtime manager global;
- alterar movement;
- executar gameplay;
- depender de validators/smokes para criar a feature.

## 8. Validacoes bloqueantes

Apply/Rebuild deve bloquear quando:

- camera reference obrigatoria estiver ausente;
- tracking target estiver ausente e policy exigir;
- look-at target estiver ausente e policy exigir;
- bindings root estiver ausente e creation policy estiver desativada;
- houver multiplos bindings conflitantes gerados pelo Composer;
- componente existente estiver em local nao suportado e repair seguro nao for possivel.

## 9. Idempotencia

Apply/Rebuild deve:

- reutilizar componentes gerados anteriormente;
- encontrar materializacao por referencias/ownership do Composer;
- nao duplicar `_Framework` ou `_Bindings`;
- reparar campos divergentes derivados do Composer;
- preservar rig/camera do consumidor;
- produzir o mesmo resultado em execucoes repetidas.

## 10. Seguranca

O MVP deve:

- falhar rapido em config obrigatoria ausente;
- nao usar fallback silencioso;
- nao usar service locator;
- nao criar singleton;
- nao usar `GameObject.name` ou path como identidade funcional;
- nao apagar componentes do consumidor;
- nao mover componentes existentes sem policy explicita;
- nao alterar FIRSTGAME ou QAFramework automaticamente.

## 11. Relacao com CameraRecipe

`CameraRecipe` e recomendado, mas pode ser adiado para depois do Composer, como aconteceu no Player.

No MVP:

- `recipe` pode ser vazio;
- campos locais do Composer devem ser suficientes para Apply/Rebuild;
- se `recipe` existir, o Composer pode usar Recipe como default de intencao.

## 12. Relacao com CameraRuntimeContext

`CameraRuntimeContext` e futuro e nao faz parte deste MVP.

O MVP pode preparar materializacao e diagnostics, mas nao deve criar runtime authority nova sem necessidade.

## 13. Relacao com FIRSTGAME

FIRSTGAME deve provar a futura implementacao como consumidor real.

Prova esperada depois da implementacao:

- camera real consome `PlayerPrototype/Anchors/CameraTarget` e `LookAtTarget`;
- setup local antigo deixa de ser fluxo principal;
- Apply/Rebuild oficial substitui helper local quando houver materializacao;
- camera continua funcionando em jogo real;
- bindings tecnicos ficam fora da superficie principal.

## 14. Relacao com QAFramework

QAFramework deve validar depois que a implementacao oficial existir.

Casos futuros:

- Apply cria bindings;
- Rebuild e idempotente;
- Apply falha sem camera requerida;
- Apply falha sem tracking target requerido;
- Apply falha sem look-at requerido quando policy exigir;
- Apply nao usa name lookup;
- Apply nao duplica componentes;
- drift repair funciona.

## 15. Riscos e decisoes pendentes

Riscos:

- Camera atual pode depender de APIs privadas ou serialized fields sem setters publicos.
- Cinemachine ou camera rig do consumidor pode nao ser uniforme.
- Scope Route/Activity pode estar maduro tecnicamente, mas nao authorable.
- Integrar transition/pause/loading cedo pode inflar o MVP.

Decisoes pendentes:

- Implementar `CameraRecipe` junto do Composer ou depois?
- MVP usa camera reference generica ou adapter especifico?
- Como configurar `FrameworkCameraAnchorHost` sem reflection/string property names?
- Route/Activity binding entra no MVP ou fica P1?
- Transitions entram no MVP ou ficam para Global UI/Transition surface?

## 16. Commit message sugerida

```text
Docs: define Camera product surface
```
