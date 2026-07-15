# F49L — Player Binding Diagnostic Reporter

## Objetivo

Adicionar uma camada passiva de diagnóstico legível sobre `PlayerBindingReadinessSummary`.

O corte responde, em texto estruturado:

- se o binding de view está pronto;
- se o binding de control está pronto;
- se o binding completo está pronto;
- por que algo está bloqueado ou apenas não pronto;
- quais issues de readiness foram propagadas.

## Escopo

Criado no package oficial:

- `PlayerBindingDiagnosticSeverity`
- `PlayerBindingDiagnosticMessageKind`
- `PlayerBindingDiagnosticMessage`
- `PlayerBindingDiagnosticReport`
- `PlayerBindingDiagnosticReporter`

## Fora de escopo

Este corte não executa:

- view binding real;
- control binding real;
- camera activation;
- input activation;
- movement enable/disable;
- actor spawning;
- runtime lifecycle;
- integração FIRSTGAME.

## Regras

- O reporter recebe `PlayerBindingReadinessSummary`.
- Summary nulo gera erro explícito.
- Issues blocking viram mensagens de erro.
- Issues não blocking viram warnings.
- Ausência de participantes é não bloqueante, mas impede readiness.
- O reporter sempre declara seu boundary passivo.

## Smoke esperado

No QAFramework, rodar:

```text
Player Binding Diagnostics QA
```

Resultado esperado:

```text
[F49L_PLAYER_BINDING_DIAGNOSTICS_QA] status='Succeeded'
```

## Ganho arquitetural

O framework passa a ter uma leitura humana estável antes de qualquer corte de binding real.
Isso reduz ambiguidade entre:

- topologia inválida;
- readiness incompleta;
- ausência não bloqueante de participantes;
- binding realmente pronto.
> Status: Superseded / Removed em 2026-07-15 por `P3-ADR-Canonical-Player-Lane.md`. Mantido apenas como histórico.
