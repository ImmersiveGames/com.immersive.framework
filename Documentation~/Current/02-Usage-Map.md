# 02 — Usage Map

Use this map when deciding which framework surface to use in game code or scene authoring.

Consumer role reference: [`03-Consumer-Project-Roles.md`](03-Consumer-Project-Roles.md).

## Common tasks

| Task | Use | Do not use |
|---|---|---|
| Boot the app through framework | `GameApplicationAsset` + Startup Route | Scene-local bootstrap scripts. |
| Switch route | `RouteRequestTrigger` or runtime host route request surface | Direct scene loading from gameplay button. |
| Start/re-enter activity | Activity lifecycle through route/startup activity or request surface | Manual scene reload as restart. |
| Show fade around lifecycle | Route/Activity Transition settings + UIGlobal transition adapter | Custom fade script disconnected from lifecycle. |
| Show loading state | UIGlobal loading surface adapter | Loading UI that does not observe framework operation. |
| Toggle pause | `PauseInputActionTrigger` / pause request surface | Directly setting `Time.timeScale` from random UI. |
| Block PlayerInput during pause/transition | `UnityPlayerInputGateAdapter` | Gameplay script polling Pause directly. |
| Make scene object resettable | `UnityResetSubjectAdapter` + participants/components | `ObjectEntryDeclaration` reset path. |
| Reset transform/active state | `UnityTransformResetParticipant`, `UnityGameObjectActiveResetParticipant` | Custom participant unless custom semantics are needed. |
| Reset gameplay state | Gameplay component implements `IUnityResettable` | One extra participant component per gameplay variable. |
| Reset one object | `ObjectResetTrigger` with `ResetSubjectReference` | Direct `ResetRegistry` access. |
| Reset a room/activity scope | `ObjectResetGroupTrigger` + `ResetSelectionConfig` | Manual list iteration in game code. |
| Restart current activity | `ActivityRestartTrigger` | Reset group trigger + separate activity request on the same button. |
| Runtime prefab reset | Prefab has `UnityResetSubjectAdapter` with runtime id generation | Legacy runtime object participation path. |

## Consumer project mapping

| Need | Project/root | Use |
|---|---|---|
| Validate framework technical behavior | QA Project / `Assets/ImmersiveFrameworkQA/` | Synthetic scenes, QA buttons, probes and negative cases. |
| Prove a real starting game is usable | FIRSTGAME / `Assets/_Project/` | Minimal player, menu, gameplay, reset, pause, transition and runtime object examples. |
| Record official framework decisions | Package / `Documentation~/` | Current docs, ADRs, guides, planning and history. |

## Reset examples

### Gameplay component reset

```csharp
using Immersive.Framework.Reset;
using Immersive.Framework.Reset.Unity;
using UnityEngine;

public sealed class PlayerHealthState : MonoBehaviour, IUnityResettable, IUnityResettableMetadata
{
    [SerializeField] private int maxHealth = 100;
    private int _health;

    public string ResetParticipantId => "player.health";
    public ResetParticipantRequiredness ResetRequiredness => ResetParticipantRequiredness.Required;
    public int ResetOrder => 100;
    public string ResetDisplayName => "Player Health";
    public string ResetSource => nameof(PlayerHealthState);
    public string ResetReason => "player.health.reset";

    private void Awake()
    {
        _health = maxHealth;
    }

    public ResetParticipantResult Reset(ResetContext context)
    {
        _health = maxHealth;
        return ResetParticipantResult.CreateSucceeded(
            context.Participant,
            nameof(PlayerHealthState),
            context.Reason,
            "Player health reset.");
    }
}
```

### Scene shape

```text
PlayerPrototype
  UnityResetSubjectAdapter
    Subject Id = firstgame.player
    Scope = Activity
    Include Unity Resettable Components = true
  UnityTransformResetParticipant
  PlayerHealthState : IUnityResettable
```

### Trigger shape

```text
Button_ResetRoom
  ObjectResetGroupTrigger
    Selection Mode = CurrentRouteAndActivitySubjects
    Allow No Subjects = false
    Allow No Participants = false
    Stop On Failure = true
```

### Programmatic trigger call

Prefer calling configured triggers. This keeps selection policy in authoring and keeps registry ownership inside the framework.

```csharp
using Immersive.Framework.ObjectReset;
using UnityEngine;

public sealed class ResetRoomButtonProxy : MonoBehaviour
{
    [SerializeField] private ObjectResetGroupTrigger resetGroupTrigger;

    public void ResetRoom()
    {
        if (resetGroupTrigger == null)
        {
            Debug.LogError("Reset group trigger is missing.");
            return;
        }

        resetGroupTrigger.RequestObjectResetGroup();
    }
}
```

## Boundaries

- Game code should not access `ResetRegistry` directly.
- Game code should not create framework lifecycle ownership manually.
- A resettable gameplay component should implement `IUnityResettable` when it owns meaningful state.
- Use `UnityResetParticipantBehaviour` when a reusable participant component is better than embedding reset into gameplay code.
