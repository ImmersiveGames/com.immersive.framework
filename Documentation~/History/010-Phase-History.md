# 010 — Phase History

| Range | Historical result |
|---|---|
| F0–F28 | Bootstrap, Route/Activity, lifecycle, transition/loading, pause, gate, save boundaries and early input boundaries. |
| F34–F44 | Pause/input integration and reset/restart evolution; old reset participation was superseded by Reset Reform. |
| F49 | Passive Player contracts, topology, readiness and diagnostics closed. |
| F50–F53 | Authoring validation and explicit adapter proofs; retained as technical history rather than the active product flow. |
| Player product cuts | `PlayerRecipe` and `PlayerComposer` established the current designer-first Player surface. |
| Camera product cuts | `CameraRecipe` and `CameraComposer` established the main gameplay-camera surface; legacy camera architecture was removed. |
| C8B | Route/Activity moved to explicit Cinemachine output apply-on-enter. Release/restoration remains pending. |
| R0 | Current docs reconciled; P2 Player Control Product selected as the only active lane. |

Detailed decisions remain in `../ADRs/`; product-cut manifests remain in `../Product/`.
