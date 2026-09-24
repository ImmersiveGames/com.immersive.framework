# Changelog

All notable package changes are documented in this file.

## [1.0.1] - 2026-09-24

First documented GitHub Release of the stable package line. The pre-existing
`v1.0.0` Git tag is preserved as an immutable historical tag; no GitHub Release
was published for it.

### Included product boundaries

- Application bootstrap, Persistent Content, Session, Route and Activity
  lifecycle authorities.
- Route/Activity authoring, content contribution, visibility, readiness,
  loading and transition policies.
- Player Session observation and explicit command surfaces, Manager-Provisioned
  and Scene-Provided local Players, Actor selection, preparation, replacement,
  spatial entry and relocation.
- IF-ADR-032 Camera Outputs, Presentations, requests, rig materialization,
  Subjects, split-layout integration and lifecycle ownership.
- Pause/Input Mode, Reset, Progression Save, Scene Lifecycle Events and
  application frame-rate authoring.
- Optional Audio/BGM integration and Logging guidance.
- Designer-first Inspectors, validation, diagnostics and focused Unity Test
  Framework assemblies.

### Changed

- Rewrote the package README for the current public product surface.
- Added pinned Git installation and getting-started instructions.
- Documented per-surface API maturity and current validation limits.
- Linked the complete set of active usage guides.

### Validation note

This release-preparation cut changes package metadata and documentation only.
Camera aggregate Unity recertification after the IF-ADR-032 legacy removal
remains pending as recorded by the current tracker.
