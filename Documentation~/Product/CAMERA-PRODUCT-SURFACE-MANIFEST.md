> Superseded by ADR-PROD-0006. This document is historical and is not current implementation guidance.

# Camera Product Surface Manifest

Status: Documentation cut
Package: `com.immersive.framework`

## Corte

Definicao documental inicial da proxima Product Surface depois do fechamento do eixo Player.

Superficie preparada:

```text
Camera Recipe / Camera Composer
```

## Objetivo

Criar especificacao e plano MVP de Camera Product Surface, sem alterar codigo, runtime, editor tooling, asmdefs, FIRSTGAME ou QAFramework.

## Arquivos criados

```text
Documentation~/Product/Camera-Product-Surface-Spec.md
Documentation~/Product/Camera-Composer-MVP-Plan.md
Documentation~/Product/CAMERA-PRODUCT-SURFACE-MANIFEST.md
```

## Arquivos alterados

```text
none
```

## Arquivos removidos

```text
none
```

## Fora de escopo

```text
codigo C#
runtime
editor tooling
asmdefs
CameraComposer implementation
CameraRecipe implementation
CameraRuntimeContext implementation
FIRSTGAME
QAFramework
smokes
validators
copiar setup FIRSTGAME
CameraManager global
spawn/movement/save
```

## Decisoes registradas

- Camera e a proxima Product Surface candidata depois de Player.
- CameraComposer deve consumir anchors/targets do PlayerComposer quando aplicavel.
- CameraComposer nao deve virar CameraManager.
- CameraRecipe e recomendado, mas nao precisa bloquear o primeiro MVP.
- `_Framework/_Bindings` e materializacao tecnica, nao autoridade principal.
- Diagnostics confirmam Apply/Rebuild, mas nao sao fluxo principal.

## Decisoes pendentes

- Se CameraRecipe entra junto do Composer ou depois.
- Qual componente real do package sera usado como anchor/binding host.
- Como configurar camera targets sem reflection ou acesso fragil a serialized property names.
- Se Route/Activity binding entra no MVP ou fica para uma slice posterior.
- Se transitions entram no MVP ou ficam para outra surface.

## Criterios de aceite

Este corte e PASS se:

- os 3 documentos forem criados;
- nenhum codigo C# for alterado;
- nenhum asmdef for alterado;
- nenhum arquivo FIRSTGAME/QAFramework for alterado;
- os documentos definirem Camera Recipe / Camera Composer como direcao, nao implementacao;
- os documentos deixarem claro que CameraComposer nao e CameraManager;
- os documentos deixarem claro que Player anchors sao consumidos, nao recriados como autoridade;
- os documentos nao tratarem validators/smokes como UX principal.

## Commit message sugerida

```text
Docs: define Camera product surface
```
