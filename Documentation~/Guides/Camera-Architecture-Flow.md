# Camera — Arquitetura e Fluxo (derivado do código)

> **Fonte:** análise direta de `Runtime/Camera/`. Este documento descreve o que o código **faz hoje**.
>
> **Status geral:** contratos marcados como `Experimental` via `[FrameworkApiStatus]` (F46B/F46C).

---

## Princípio central

O framework possui um **`FrameworkCameraDirector`** que é o único ponto de **precedência** de câmera Route/Activity. Ele:

- recebe rigs de câmera via bindings de lifecycle (`FrameworkRouteCameraBinding`, `FrameworkActivityCameraBinding`);
- resolve qual rig é efetivo conforme política de Activity;
- ativa/desativa GameObjects de rig (`setRigActiveState`);
- delega aplicação concreta (prioridade, targets) a um **`IFrameworkCameraRigApplier`** opcional (ex.: Cinemachine).

O director **não** integra com `PlayerView`, `PlayerEntry` ou `PlayerSlot`. `PlayerViewBehaviour` apenas guarda evidência de `Camera` como flag diagnóstica — não chama o director.

`ImmersiveFrameworkBootstrap` declara explicitamente que lifecycle de Camera **não é owned** no bootstrap.

---

## Visão geral da arquitetura

```mermaid
flowchart TB
    subgraph Lifecycle["Lifecycle bindings"]
        FRCB["FrameworkRouteCameraBinding<br/>RouteContentBehaviour"]
        FACB["FrameworkActivityCameraBinding<br/>ActivityContentBehaviour"]
    end

    subgraph Director["FrameworkCameraDirector"]
        SET["SetRouteCamera / SetActivityCamera"]
        REF["Refresh() → ResolveEffectiveRig()"]
        ACT["SetRigActive(rig, bool)"]
        APP["ApplyRig(descriptor)"]
    end

    subgraph Descriptors["Modelos passivos"]
        FCRD["FrameworkCameraRigDescriptor"]
        FCAD["FrameworkCameraAnchorDescriptor"]
        FCPS["FrameworkCameraPriorityState"]
    end

    subgraph Anchors["Anchor authoring"]
        FCAH["FrameworkCameraAnchorHost<br/>trackingTarget + lookAtTarget"]
    end

    subgraph Applier["Adapter opcional"]
        IFRA["IFrameworkCameraRigApplier"]
        FCRA["FrameworkCinemachineRigApplier<br/>#if IMMERSIVE_FRAMEWORK_CINEMACHINE"]
    end

    subgraph Rigs["GameObjects na cena"]
        DEF["defaultCameraRig"]
        ROUTE["routeCameraRig"]
        ACTRIG["activityCameraRig"]
    end

    FRCB -->|OnRouteContentEntered| SET
    FACB -->|OnActivityContentEntered| SET
    FRCB -->|OnRouteContentExited| SET
    FACB -->|OnActivityContentExited| SET

    SET --> REF
    REF --> FCRD
    FCAH --> FCAD
    FCAD --> FCRD
    FCPS --> FCRD

    REF --> ACT
    REF --> APP
    APP --> IFRA
    IFRA --> FCRA

    DEF --> Director
    ROUTE --> FRCB
    ACTRIG --> FACB
    FCAH --> FRCB
    FCAH --> FACB
```

---

## Componentes Unity

### FrameworkCameraDirector

Arquivo: `Runtime/Camera/Unity/FrameworkCameraDirector.cs`

| Campo | Default | Papel |
|-------|---------|-------|
| `defaultCameraRig` | — | Fallback quando não há route/activity |
| `defaultAnchors` | — | Anchors do fallback |
| `routePriority` | 20 | Prioridade numérica do rig Route |
| `activityPriority` | 100 | Prioridade numérica do rig Activity |
| `setRigActiveState` | true | Desativa rig anterior, ativa o novo |
| `rigApplier` | — | MonoBehaviour que implementa `IFrameworkCameraRigApplier` |
| `logTransitions` | true | Log `[FRAMEWORK_CAMERA]` |

**Estado interno rastreado:**

- `currentRouteRig` / `currentRouteAnchors`
- `currentActivityRig` / `currentActivityAnchors`
- `retainedActivityRigForCurrentRoute` — para política de retenção
- `hasActiveActivityCameraBinding` — se houve binding de activity neste ciclo
- `currentActivityPolicy`
- `currentEffectiveRig` / `currentEffectiveDescriptor`

### FrameworkRouteCameraBinding

Arquivo: `Runtime/Camera/Unity/FrameworkRouteCameraBinding.cs`

Herda `RouteContentBehaviour`:

```mermaid
sequenceDiagram
    participant Route as Route lifecycle
    participant Binding as FrameworkRouteCameraBinding
    participant Director as FrameworkCameraDirector
    participant Startup as FrameworkActivityCameraBinding

    Route->>Binding: OnRouteContentEntered(context)
    Binding->>Director: SetRouteCamera(routeRig, anchors, deferRefresh)
    alt Route tem Startup Activity
        Binding->>Startup: TryApplyStartupActivityCamera(director, startupActivity)
        alt binding válido
            Startup->>Director: SetActivityCamera(activityRig, policy, anchors)
        else sem binding válido
            Binding->>Director: Refresh() (fallback route)
        end
    end

    Route->>Binding: OnRouteContentExited(context)
    Binding->>Director: ClearRouteCamera(routeCameraRig)
```

`deferRefreshForStartupActivity = true` quando a Route tem Startup Activity — evita flash da câmera Route antes da Activity startup.

### FrameworkActivityCameraBinding

Arquivo: `Runtime/Camera/Unity/FrameworkActivityCameraBinding.cs`

Herda `ActivityContentBehaviour`:

| Evento | Ação |
|--------|------|
| `OnActivityContentEntered` | `director.SetActivityCamera(activityCameraRig, policy, anchors)` |
| `OnActivityContentExited` | `director.ClearActivityCamera(rig, deferRefresh)` |

`deferRefreshForActivityTransition = true` quando `context.NextActivity != null` — evita refresh intermediário na troca Activity→Activity.

Também expõe `TryApplyStartupActivityCamera()` para uso pelo Route binding na entrada da Route.

### FrameworkCameraAnchorHost

Arquivo: `Runtime/Camera/Unity/FrameworkCameraAnchorHost.cs`

- `trackingTarget` — Transform de follow
- `lookAtTarget` — Transform de look-at
- `ToDescriptor()` → `FrameworkCameraAnchorDescriptor`

Usado pelos bindings e repassado ao applier via `FrameworkCameraRigDescriptor`.

---

## Resolução de precedência (`Refresh`)

```mermaid
flowchart TD
    START["Refresh()"] --> RES["ResolveEffectiveRig()"]
    RES --> Q1{hasActiveActivityCameraBinding?}

    Q1 -->|não| R1["Route rig ou defaultCameraRig<br/>role=Route/DefaultFallback<br/>priority=routePriority"]

    Q1 -->|sim| Q2{currentActivityPolicy}

    Q2 -->|UseRoute| R2["Route rig ou default<br/>role=Route/DefaultFallback"]

    Q2 -->|UseOwnOrRetainActivityUntilRouteExit| Q3{currentActivityRig?}
    Q3 -->|sim| R3["activity rig<br/>role=Activity, priority=activityPriority"]
    Q3 -->|não| Q4{retainedActivityRig?}
    Q4 -->|sim| R4["retained rig<br/>role=RetainedActivity"]
    Q4 -->|não| R5["fallback route/default"]

    Q2 -->|UseOwnOrRoute default| Q5{currentActivityRig?}
    Q5 -->|sim| R3
    Q5 -->|não| R5

    R1 --> APPLY
    R2 --> APPLY
    R3 --> APPLY
    R4 --> APPLY
    R5 --> APPLY

    APPLY["Criar FrameworkCameraRigDescriptor"]
    APPLY --> CHG{rig mudou?}
    CHG -->|sim + setRigActiveState| DEACT["desativa rig anterior"]
    DEACT --> ACT["ativa novo rig"]
    CHG -->|não| SKIP["skip troca active"]
    ACT --> APR
    SKIP --> APR
    APR["ApplyRig() via IFrameworkCameraRigApplier"]
```

### Políticas de Activity (`FrameworkCameraActivityPolicy`)

| Política | Comportamento |
|----------|---------------|
| `UseOwnOrRoute` (default) | Usa activity rig se presente; senão route/default |
| `UseOwnOrRetainActivityUntilRouteExit` | Mantém última activity rig até sair da Route |
| `UseRoute` | Ignora activity rig; força route/default |

---

## Prioridades numéricas

| Rig | Prioridade default | Papel |
|-----|-------------------|-------|
| Route / Default | `routePriority` (20) | Câmera base da Route |
| Activity / Retained | `activityPriority` (100) | Câmera da Activity (maior precedência) |

Valores passados ao applier via `FrameworkCameraPriorityState` dentro do descriptor.

---

## Adapter Cinemachine (opcional)

Arquivo: `Runtime/Camera/Cinemachine/FrameworkCinemachineRigApplier.cs`

Compilado apenas com `#if IMMERSIVE_FRAMEWORK_CINEMACHINE` (asmdef separado: `Immersive.Framework.Camera.Cinemachine`).

Implementa `IFrameworkCameraRigApplier`:

1. `Supports(descriptor)` — rig tem `CinemachineCamera` em children
2. `Apply(descriptor)`:
   - `cinemachineCamera.Priority = descriptor.Priority.Priority`
   - Se anchors presentes: `TrackingTarget` e `LookAtTarget`

Sem applier configurado, o director apenas troca `GameObject.SetActive` nos rigs (se `setRigActiveState`).

---

## Contrato do applier

Arquivo: `Runtime/Camera/IFrameworkCameraRigApplier.cs`

```csharp
bool Supports(FrameworkCameraRigDescriptor descriptor);
void Apply(FrameworkCameraRigDescriptor descriptor);
```

O director valida que `rigApplier` implementa a interface; caso contrário loga erro e pula aplicação.

---

## Modelos e enums

### FrameworkCameraRigDescriptor

Arquivo: `Runtime/Camera/FrameworkCameraRigDescriptor.cs`

Struct readonly com: `Rig`, `Role`, `Scope`, `Anchors`, `Priority`, `Source`, `Reason`.

`IsValid` exige rig não-nulo, role/scope conhecidos e priority válida.

### FrameworkCameraRigRole

| Valor | Significado |
|-------|-------------|
| `DefaultFallback` | Rig default do director |
| `Route` | Selecionado pela Route |
| `Activity` | Selecionado pela Activity ativa |
| `RetainedActivity` | Activity retida até sair da Route |

### FrameworkCameraScope

| Valor | Significado |
|-------|-------------|
| `DefaultFallback` | Escopo fallback |
| `Route` | Autoria Route |
| `Activity` | Autoria Activity |

### FrameworkCameraPriorityState

Arquivo: `Runtime/Camera/FrameworkCameraPriorityState.cs`

Empacota `role`, `priority` int, `source`, `reason` para diagnóstico.

### FrameworkCameraAnchorDescriptor

Arquivo: `Runtime/Camera/FrameworkCameraAnchorDescriptor.cs`

Struct com `TrackingTarget`, `LookAtTarget`, `HasAnyTarget`, `Empty` factory.

---

## Inventário de arquivos

### Core (`Runtime/Camera/`)

| Arquivo | Papel |
|---------|-------|
| `IFrameworkCameraRigApplier.cs` | Contrato do adapter de pacote de câmera |
| `FrameworkCameraRigDescriptor.cs` | Descriptor do rig selecionado |
| `FrameworkCameraAnchorDescriptor.cs` | Descriptor de targets follow/look-at |
| `FrameworkCameraPriorityState.cs` | Estado de prioridade |
| `FrameworkCameraRigRole.cs` | Papel do rig efetivo |
| `FrameworkCameraScope.cs` | Escopo de autoria |
| `FrameworkCameraActivityPolicy.cs` | Política Route vs Activity |

### Unity bindings (`Runtime/Camera/Unity/`)

| Arquivo | Papel |
|---------|-------|
| `FrameworkCameraDirector.cs` | Director de precedência |
| `FrameworkRouteCameraBinding.cs` | Binding Route → director |
| `FrameworkActivityCameraBinding.cs` | Binding Activity → director |
| `FrameworkCameraAnchorHost.cs` | Provider de anchors na cena |

### Cinemachine (`Runtime/Camera/Cinemachine/`)

| Arquivo | Papel |
|---------|-------|
| `FrameworkCinemachineRigApplier.cs` | Adapter Cinemachine 3 |
| `Immersive.Framework.Camera.Cinemachine.asmdef` | Assembly condicional |

---

## Relação com Player (sem integração direta)

```mermaid
flowchart LR
    subgraph PlayerPassive["Player (passivo)"]
        PVB["PlayerViewBehaviour"]
        CAM["UnityEngine.Camera<br/>evidência apenas"]
    end

    subgraph CameraFramework["Camera (framework)"]
        DIR["FrameworkCameraDirector"]
        RIG["cameraRig GameObject"]
    end

    PVB -->|"HasCameraEvidence"| CAM
    PVB -.->|sem chamada| DIR
    DIR --> RIG
    RIG -.->|pode conter| CAM

    style PVB fill:#eee
    style DIR fill:#ddf
```

`PlayerView` documenta câmera como evidência passiva. `PlayerViewTopologyValidator` valida topologia, não ativa câmera. A seleção efetiva de câmera é responsabilidade do `FrameworkCameraDirector` + bindings de Route/Activity.

---

## Cena típica

```
Session / Bootstrap
└── FrameworkCameraDirector
      defaultCameraRig = MainCameraFallback
      rigApplier = CinemachineRigApplier
      routePriority = 20
      activityPriority = 100

Route Content (scene da Route)
├── FrameworkRouteCameraBinding
│     routeCameraRig = RouteCameraRig
│     routeAnchors = RouteAnchorHost
│     director → ref ao director da sessão
│     startupActivityCameraBinding → opcional
└── RouteCameraRig (GameObject)
      └── CinemachineCamera(s)

Activity Content (scene da Activity)
├── FrameworkActivityCameraBinding
│     assignedActivity = MyActivityAsset
│     activityCameraRig = ActivityCameraRig
│     policy = UseOwnOrRoute
│     anchors = ActivityAnchorHost
│     director → ref ao director da sessão
└── ActivityCameraRig (GameObject)

FrameworkCameraAnchorHost (em rig ou separado)
  trackingTarget = PlayerTransform
  lookAtTarget = PlayerTransform
```

---

## Fluxo completo Route → Activity → Activity

```mermaid
sequenceDiagram
    participant R as Route enter
    participant RD as FrameworkCameraDirector
    participant A1 as Activity 1 enter
    participant A2 as Activity 2 enter
    participant RX as Route exit

    R->>RD: SetRouteCamera(routeRig)
    RD->>RD: Refresh → route rig ativo

    A1->>RD: SetActivityCamera(activity1Rig, UseOwnOrRoute)
    RD->>RD: Refresh → activity1 rig ativo

    A1->>RD: ClearActivityCamera(deferRefresh=true)
    Note over RD: defer se NextActivity existe

    A2->>RD: SetActivityCamera(activity2Rig)
    RD->>RD: Refresh → activity2 rig ativo

    A2->>RD: ClearActivityCamera
    RD->>RD: Refresh → fallback route rig

    RX->>RD: ClearRouteCamera
    RD->>RD: Refresh → defaultCameraRig
```

Com `UseOwnOrRetainActivityUntilRouteExit`, após sair da Activity 2 o director pode manter o último activity rig até `ClearRouteCamera`.

---

## O que o código não faz

| Responsabilidade | Status |
|------------------|--------|
| Binding automático PlayerView → director | Não existe |
| Seleção de câmera por PlayerSlot | Não implementado |
| Split-screen / multi-player camera | Não implementado |
| Camera shake, FOV, blend Cinemachine | Applier só seta Priority + targets |
| Validação de topologia de câmera (validator) | Não existe equivalente ao PlayerTopology |
| Integração com Gate/Pause | Director não observa Gate |
| Criação/destruição de rigs em runtime | Apenas referências a GameObjects existentes |

---

## Mapa de namespaces

```
Immersive.Framework.Camera              → descriptors, enums, contrato applier
Immersive.Framework.Camera.Cinemachine  → adapter Cinemachine (condicional)
```

Bindings ficam em `Immersive.Framework.Camera` mas herdam de:

- `RouteContentBehaviour` (`Immersive.Framework.Authoring` / Route lifecycle)
- `ActivityContentBehaviour` (`Immersive.Framework.ActivityFlow`)

---

## Referência rápida

| Necessidade | Usar |
|-------------|------|
| Câmera default da sessão | `FrameworkCameraDirector.defaultCameraRig` |
| Câmera ao entrar na Route | `FrameworkRouteCameraBinding` |
| Câmera ao entrar na Activity | `FrameworkActivityCameraBinding` |
| Manter câmera da activity entre activities | `UseOwnOrRetainActivityUntilRouteExit` |
| Forçar câmera da Route na Activity | `UseRoute` |
| Follow/look-at targets | `FrameworkCameraAnchorHost` |
| Prioridade Cinemachine | `FrameworkCinemachineRigApplier` no `rigApplier` |
| Evidência de câmera no player (passivo) | `PlayerViewBehaviour.viewCamera` (sem efeito no director) |

---

**Última revisão:** derivada do código em `master`, julho 2026.