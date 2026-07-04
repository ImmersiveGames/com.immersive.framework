# 060 — Consumer Project Separation Closure

## Objetivo

Fechar documentalmente a reorganizacao de papeis entre o framework package, o Framework QA Project e FIRSTGAME, sem alterar runtime, sem mover assets e sem editar arquivos Unity serializados.

## Projetos auditados

```text
C:\Projetos\ImmersivePackages\com.immersive.framework
C:\Projetos\My project
C:\Projetos\planet-devourer
```

## Estado final do Framework Package

O package `com.immersive.framework` permanece como dono de:

- contratos, runtime, editor tooling, validators e diagnostics;
- documentacao canonica em `Documentation~/`;
- Current State, Roadmap, Consumer Project Roles e historico numerado.

Arquivos atuais atualizados neste fechamento:

- `Documentation~/Current/00-Current-State.md`;
- `Documentation~/Current/01-Roadmap.md`;
- `Documentation~/Current/03-Consumer-Project-Roles.md`.

## Estado final do QA Project

Estado textual validado:

- `Assets/ImmersiveFrameworkQA/` existe;
- `Assets/_Project` nao existe;
- `Assets/_Documentation` nao existe;
- documentacao local esta em `Assets/ImmersiveFrameworkQA/Documentation/`;
- README local descreve operacao QA e aponta a documentacao canonica para o package.

O QA Project nao deve recriar `Assets/_Project` nem `Assets/_Documentation`.

## Estado final do FIRSTGAME

Estado textual validado:

- `Assets/_Project/` existe e e o root canonico de FIRSTGAME;
- `Assets/ImmersiveFrameworkQA` nao existe;
- README local descreve uso real de jogo minimo;
- documentacao local sob `Assets/_Project/Documentation/` e operacional/historica de consumidor, nao canonica do framework.

Pendencias textuais observadas, nao alteradas neste corte:

- nomes temporarios residuais `Probe`/`Test` ainda aparecem em cena/script ligados a serializacao Unity e permanecem deferidos para migracao manual via Unity Editor.

Correcao segura aplicada neste corte:

- o setup Editor local de FIRSTGAME deixou de criar/validar `Assets/_Documentation`.

Pendencia fisica nao removida por regra do corte:

- `Assets/_Documentation` ainda existe em FIRSTGAME como root metadata-only com `.meta` de subpastas vazias.

## Cortes fechados

```text
POST-RESET-B1 — PASS
POST-RESET-B2 — PASS
POST-RESET-B3 — PASS
POST-RESET-B4 — PASS
POST-RESET-B5 — PASS
POST-RESET-B5A — PASS
POST-RESET-B6A — PASS
POST-RESET-B6B0 — PASS
POST-RESET-B6B — PASS
POST-RESET-B6F — PASS
```

## Validação recebida

Validacao textual executada:

- existencia/ausencia dos roots esperados no QA Project e FIRSTGAME;
- busca por `Assets/_Project`, `Assets/_Documentation`, `_Project` e `_Documentation` no QA Project;
- busca por `ImmersiveFrameworkQA` e `Assets/_Documentation` no FIRSTGAME;
- busca por `Consumer Project Separation` e `ImmersiveFrameworkSettings` no package;
- `git status --short` nos roots alterados.

Nao executado:

- Unity;
- build;
- playmode;
- smoke;
- batchmode.

## Regras congeladas

- O package mantem a documentacao canonica em `Documentation~/`.
- O QA Project usa `Assets/ImmersiveFrameworkQA/` e documentacao operacional curta.
- O QA Project nao deve recriar `Assets/_Project` nem `Assets/_Documentation`.
- FIRSTGAME usa `Assets/_Project/` como root real de consumidor.
- FIRSTGAME nao deve conter roots QA, smokes sinteticos ou documentacao canonica do framework.
- `ImmersiveFrameworkSettings.asset` pode viver em qualquer `Resources` valido, desde que exista exatamente um settings valido com esse nome.
- Moves/renames de cenas, prefabs, `.asset` e scripts anexados devem ser feitos por Unity Editor migration.

## Próximas lanes candidatas

```text
FIRSTGAME Usage Model Hardening
Transition/Loading Surface Hardening
```

Escolher explicitamente uma lane antes de iniciar implementacao.
