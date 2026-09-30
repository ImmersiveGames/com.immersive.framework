# Immersive Framework Documentation

This is the official documentation entry point for consumers of `com.immersive.framework`. The package provides supported authoring surfaces and runtime contracts for application, Session, Route, Activity and game features.

## Start here

- [Getting Started](Guides/Getting-Started.md) — create the application and author the smallest navigable setup.
- [Game Flow](Guides/Game-Flow.md) — understand Routes, Activities, requests, ownership and transitions.

## Usage guides

| Domain | Canonical guide |
|---|---|
| Application setup | [Getting Started](Guides/Getting-Started.md) |
| Game Flow, Route and Activity | [Game Flow](Guides/Game-Flow.md) |
| Activity readiness | [Activity Readiness](Guides/Activity-Readiness.md) |
| Player Participation, Local Player and Actor | [Player Usage](Guides/Player-Usage.md) |
| Camera | [Camera Usage](Guides/Camera-Usage.md) |
| Input and Pause | [Pause Usage](Guides/Pause-Usage.md) |
| Persistent Content authoring | [Persistent Content Scene Template](Guides/Persistent-Content-Scene-Template.md) |
| Reset | [Reset Usage](Guides/Reset-Usage.md) |
| Audio | [Audio Usage](Guides/Audio-Usage.md) |
| Logging | [Logging Usage](Guides/Logging-Usage.md) |
| Progression Save | [Progression Save authoring](Guides/Progression-Save-Authoring.md) |
| Application frame rate | [Application Frame Rate](Guides/Application-Frame-Rate-Usage.md) |
| Scene lifecycle events | [Scene Lifecycle Events](Guides/Scene-Lifecycle-Events.md) |
| Editor authoring conventions | [Editor Authoring Standard](Guides/Editor-Authoring-Standard.md) |

Use a domain guide for setup and normal behavior. An ADR explains an underlying decision; it does not replace the usage steps.

## Public API and architecture

- [Curated Public API Reference](API/Public-API.md) — supported authoring, assets, consumer contracts and experimental surfaces.
- [Architecture map](Architecture/README.md) — decisions, governance, reconciliation records, tracking and archives.
- [ADRs](Architecture/ADRs/) — architecture decisions and their accepted or proposed status.
- [API maturity governance](Architecture/Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md) — Stable, Experimental, Internal and tooling classifications.

C# `public` visibility alone does not make a type recommended consumer API. Follow the Public API Reference and the linked guide for the intended usage path.

## Package and release information

See the [package README](../README.md) for Unity requirements, package installation and release information. This documentation describes how to use the package after installation.

The `Architecture/Archive/` directory contains historical execution evidence. It is not the active consumer navigation path.
