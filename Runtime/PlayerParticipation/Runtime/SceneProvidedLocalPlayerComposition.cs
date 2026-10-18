using Immersive.Framework.Actors;
using Immersive.Framework.PlayerSlots;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    internal readonly struct SceneProvidedLocalPlayerComposition
    {
        internal SceneProvidedLocalPlayerComposition(
            LocalPlayerHostAuthoring localPlayerHost,
            PlayerActorRuntimeHost playerActorRuntimeHost,
            PlayerActorDeclaration playerActorDeclaration,
            Transform visualContentMount,
            GameObject visualContent)
        {
            LocalPlayerHost = localPlayerHost;
            PlayerActorRuntimeHost = playerActorRuntimeHost;
            PlayerActorDeclaration = playerActorDeclaration;
            VisualContentMount = visualContentMount;
            VisualContent = visualContent;
        }

        internal LocalPlayerHostAuthoring LocalPlayerHost { get; }
        internal PlayerActorRuntimeHost PlayerActorRuntimeHost { get; }
        internal PlayerActorDeclaration PlayerActorDeclaration { get; }
        internal Transform VisualContentMount { get; }
        internal GameObject VisualContent { get; }
        internal bool IsValid =>
            LocalPlayerHost != null &&
            PlayerActorRuntimeHost != null &&
            PlayerActorDeclaration != null;
    }

    internal static class SceneProvidedLocalPlayerCompositionResolver
    {
        internal static bool TryResolve(
            SceneProvidedLocalPlayerAuthoring authoring,
            out SceneProvidedLocalPlayerComposition composition,
            out string issue)
        {
            composition = default;
            issue = string.Empty;

            if (authoring == null)
            {
                issue = "Scene-Provided Local Player composition resolution requires authoring.";
                return false;
            }

            LocalPlayerHostAuthoring host = authoring.LocalPlayerHost;
            if (host == null ||
                !ReferenceEquals(authoring.GetComponentInParent<LocalPlayerHostAuthoring>(true), host))
            {
                issue = "Scene-Provided Local Player requires the nearest ancestral Local Player Host that owns its hierarchy.";
                return false;
            }

            if (authoring.PlayerSlotProfile == null ||
                !authoring.TryGetPlayerSlotId(out PlayerSlotId _, out issue))
            {
                issue = string.IsNullOrWhiteSpace(issue)
                    ? "Scene-Provided Local Player requires an explicit Player Slot Profile."
                    : issue;
                return false;
            }

            ActorProfile profile = authoring.ActorProfile;
            if (profile == null ||
                !profile.TryGetActorProfileId(out _, out issue) ||
                profile.ActorKind != ActorKind.Player ||
                profile.ActorRole != ActorRole.Protagonist)
            {
                issue = string.IsNullOrWhiteSpace(issue)
                    ? "Scene-Provided Local Player requires a Player Protagonist Actor Profile."
                    : issue;
                return false;
            }

            if (profile.HasLegacyPresentationPrefab ||
                !ActorCompositionProfileContract.TryValidate(profile, out issue))
            {
                issue = string.IsNullOrWhiteSpace(issue)
                    ? $"Scene-Provided Actor Profile '{profile.name}' requires the explicit visual-content migration before composition."
                    : issue;
                return false;
            }

            if (!System.Enum.IsDefined(typeof(SceneLocalPlayerAdmissionTiming), authoring.AdmissionTiming))
            {
                issue = "Scene-Provided Local Player admission timing is invalid.";
                return false;
            }

            Transform actorMount = host.ActorMount;
            if (actorMount == null || actorMount.childCount != 1)
            {
                issue = "Scene-Provided Local Player Actor Mount requires exactly one direct Player Actor Runtime Host child.";
                return false;
            }

            Transform runtimeHostRoot = actorMount.GetChild(0);
            PlayerActorRuntimeHost runtimeHost =
                runtimeHostRoot.GetComponent<PlayerActorRuntimeHost>();
            if (runtimeHost == null ||
                runtimeHost.GetComponents<PlayerActorRuntimeHost>().Length != 1 ||
                actorMount.GetComponentsInChildren<PlayerActorRuntimeHost>(true).Length != 1)
            {
                issue = "Scene-Provided Local Player Actor Mount requires exactly one direct PlayerActorRuntimeHost and no nested alternatives.";
                return false;
            }

            if (!host.TryValidateAdmissionConfiguration(runtimeHost, true, out issue) ||
                !runtimeHost.TryValidateConfiguration(out issue))
            {
                return false;
            }

            Transform visualContentMount = runtimeHost.VisualContentMount;
            if (visualContentMount != null &&
                (visualContentMount.parent != runtimeHost.transform ||
                 visualContentMount.childCount > 1))
            {
                issue = "Optional Scene-Provided visual-content mount must be a direct Actor child with at most one direct content object.";
                return false;
            }

            GameObject visualContent = visualContentMount != null && visualContentMount.childCount == 1
                ? visualContentMount.GetChild(0).gameObject
                : null;
            bool hasVisualPart = profile.TryGetVisualContentPrefab(out _);
            if (profile.CompositionParts.Count > 1 ||
                (profile.CompositionParts.Count == 1 && !hasVisualPart))
            {
                issue = "Scene-Provided Actor currently supports the exact authored Base 'visual' part only; additional keyed scene-part bindings are not authored yet.";
                return false;
            }

            if (hasVisualPart && visualContentMount == null)
            {
                issue = "Scene-Provided Actor Profile declares Base 'visual' content, but the exact Actor Runtime Host has no Visual Content Mount.";
                return false;
            }

            if (hasVisualPart && visualContent == null)
            {
                issue = "Scene-Provided Actor Profile configures visual content, but the exact authored visual-content instance is missing.";
                return false;
            }

            if (!hasVisualPart && visualContent != null)
            {
                issue = "Scene-Provided Actor has an authored object under Visual Content Mount without a matching Actor Profile Base 'visual' part.";
                return false;
            }

            if (visualContent != null &&
                (visualContent.GetComponentInChildren<PlayerInput>(true) != null ||
                visualContent.GetComponentInChildren<ActorDeclaration>(true) != null ||
                visualContent.GetComponentInChildren<PlayerActorRuntimeHost>(true) != null))
            {
                issue = "Scene-Provided optional visual content must not contain PlayerInput, Actor declarations or Player Actor Runtime Host infrastructure.";
                return false;
            }

            composition = new SceneProvidedLocalPlayerComposition(
                host,
                runtimeHost,
                runtimeHost.PlayerActorDeclaration,
                visualContentMount,
                visualContent);
            return true;
        }
    }
}
