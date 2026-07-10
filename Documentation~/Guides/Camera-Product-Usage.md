# Camera — Guia de Uso do Produto

Status: **canônico após C8B6**.  
Package: `com.immersive.framework`

## Escolha rápida

| Necessidade | Superfície correta |
|---|---|
| Câmera principal que acompanha o jogador | `CameraComposer` + `PlayerComposer` |
| Câmera com targets explícitos sem PlayerComposer | `CameraComposer` + `ExplicitTransform` |
| Output técnico associado ao lifecycle de Route | `FrameworkRouteCameraBinding` + `FrameworkCinemachineCameraOutputSource` |
| Override técnico associado ao lifecycle de Activity | `FrameworkActivityCameraBinding` + `FrameworkCinemachineCameraOutputSource` |

Regra de produto:

```text
Comece com CameraComposer.
Use Route/Activity bindings apenas quando o lifecycle realmente precisar trocar ou aplicar outro output.
```

Não existe fallback para `Camera.main`, busca por nome, director de rigs ou ativação automática de GameObjects.

---

## Fluxo principal: CameraComposer

`CameraComposer` é a superfície designer-first para materializar uma câmera Cinemachine de gameplay.

### Estrutura esperada

```text
Player
  PlayerComposer
    CameraTarget
    LookAtTarget

GameplayCamera
  CameraComposer
    Target Source Kind = PlayerComposer
    Player Composer = Player
    Follow Requirement = Required
    Look At Requirement = Optional
    Priority = 10 ou valor do jogo
```

### Criar

1. Crie um GameObject para o rig de câmera.
2. Adicione `Immersive Framework/Camera/Camera Composer`.
3. Em **Target Source Kind**, use `PlayerComposer`.
4. Arraste o `PlayerComposer` explícito.
5. Defina requiredness e priority.
6. Use **Validate**.
7. Use **Apply / Rebuild**.

`Apply / Rebuild` pode criar ou reutilizar filhos locais para:

```text
Unity Camera
Cinemachine Camera
Cinemachine Brain
```

O processo é idempotente: uma segunda execução válida não deve duplicar objetos.

### Configuração mínima recomendada

| Campo | Valor recomendado |
|---|---|
| Mode | `SinglePlayerFollowCamera` |
| Ownership Scope | `SinglePlayer` |
| Target Source Kind | `PlayerComposer` |
| Follow Requirement | `Required` |
| Look At Requirement | `Optional` |
| Create Unity Camera If Missing | `true` |
| Create Cinemachine Camera If Missing | `true` |

### Recipe opcional

`CameraRecipe` guarda intenção reutilizável. Use **Apply Recipe Defaults** para copiar defaults para o Composer. Depois da cópia, os campos locais continuam editáveis.

Recipe não substitui referências concretas da cena, como o `PlayerComposer`.

---

## Targets explícitos

Use `ExplicitTransform` quando a câmera não acompanha um PlayerComposer.

```text
Target Source Kind = ExplicitTransform
Explicit Follow Target = referência explícita
Explicit Look At Target = referência explícita ou vazio quando optional/not used
```

Se Follow estiver `Required` e não houver referência, a validação bloqueia. O mesmo vale para Look At quando `Required`.

Não use nome de GameObject como identidade funcional.

---

## Route/Activity: integração técnica

Route/Activity bindings não são a superfície principal de criação da câmera. Eles consomem um output Cinemachine explícito quando o lifecycle precisa aplicá-lo.

### Route

```text
Route content object
  FrameworkRouteCameraBinding
  FrameworkCinemachineCameraOutputSource
```

O source deve declarar explicitamente:

```text
Cinemachine Camera
Cinemachine Brain
Follow Target
Look At Target
Priority
Required
Output Id
Display Name
```

Na entrada da Route:

```text
Binding
  -> TryCreateOutput
  -> FrameworkCinemachineOutputApplier.Apply
  -> diagnostic applied / blocked / skipped
```

Não há fallback.

### Activity

```text
Activity content object
  FrameworkActivityCameraBinding
  FrameworkCinemachineCameraOutputSource
```

Políticas atuais:

| Política | Resultado |
|---|---|
| `UseOwn` | Aplica o output explícito da Activity. |
| `UseRoute` | Não aplica override de Activity. |

Required inválido gera `Blocked`. Optional inválido gera `Skipped`.

### Limite atual de lifecycle

Os bindings aplicam output na entrada. O contrato automático de release/rebaixamento de priority na saída ainda não existe. Não simule esse contrato com `SetActive`, `Camera.enabled` ou fallback próprio; trate qualquer necessidade de release como um novo corte runtime explícito.

---

## Validate, Apply/Rebuild e Debug

### Validate

Valida intenção e referências antes da materialização. Exemplos de bloqueio:

```text
PlayerComposer ausente
Follow obrigatório ausente
Look At obrigatório ausente
Mode não suportado
Ownership não suportado
Target Source Kind não suportado
```

### Apply / Rebuild

Materializa ou repara a composição local. O resultado apresenta contadores:

```text
created
repaired
alreadyValid
skipped
blocked
```

Uma composição estável deve apresentar `blocked='0'`; uma segunda execução normalmente aumenta `alreadyValid`, sem duplicar objetos.

### Advanced / Debug

Use os campos de debug do `CameraComposer` para verificar:

```text
Last Apply/Rebuild Status
Last Blocking Issue
Last Target Resolution Summary
Last Materialization Summary
Last Resolved Follow Target
Last Resolved Look At Target
```

Para Route/Activity, use os diagnostics estruturados do output:

```text
output-applied
camera-output-missing
cinemachine-camera-missing
cinemachine-brain-missing
multiple-cinemachine-brains
cinemachine-brain-scope-mismatch
follow-target-missing
look-at-target-missing
optional-output-skipped
```

---

## O que não usar

Não use:

```text
Camera.main como resolução funcional
FindObjectOfType / busca por nome para obter autoridade
FrameworkCameraDirector
FrameworkCameraAnchorHost
FrameworkCinemachineRigApplier
GameObject.SetActive para selecionar câmera canônica
Camera.enabled para selecionar câmera canônica
manager ou singleton global de câmera
```

Esses componentes legacy foram removidos. Não devem ser recriados no projeto consumidor.

---

## QA e prova em jogo real

### QA técnico

```text
Immersive Framework/QA/Camera/C8B4C Canonical Route Activity Cinemachine Smoke
Immersive Framework/QA/Camera/C8B2 Cinemachine Output Applier Smoke
Immersive Framework/QA/Camera/C7 Camera Product Surface Regression Smoke
```

### FIRSTGAME

O consumidor real atual usa apenas:

```text
PlayerComposer
  -> CameraComposer
  -> Cinemachine materialization
```

Proof:

```text
FIRSTGAME > Immersive Framework > Camera Composer Proof > Configure Gameplay CameraComposer Proof
```

Evidência esperada:

```text
status='Succeeded'
createdSecond='0'
blockedSecond='0'
resolvedByName='False'
```

---

## Checklist de aceite

```text
[ ] PlayerComposer explícito ou transforms explícitos
[ ] Validate sem bloqueios
[ ] Apply/Rebuild sem bloqueios
[ ] segunda execução idempotente
[ ] Follow/LookAt resolvidos conforme requiredness
[ ] CinemachineBrain explícito
[ ] sem Camera.main
[ ] sem lookup por nome
[ ] sem legacy camera components
[ ] sem Missing Script
```
