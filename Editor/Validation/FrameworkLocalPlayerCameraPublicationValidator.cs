using Immersive.Framework.Camera;
using Immersive.Framework.PlayerParticipation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Immersive.Framework.Editor.Editor.Validation
{
    /// <summary>
    /// Prevents two Local Player camera request publishers from being authored for
    /// the same admitted Player. Gameplay admission is the canonical publisher.
    /// </summary>
    internal static class FrameworkLocalPlayerCameraPublicationValidator
    {
        internal static void ValidateOpenScenes(
            FrameworkAuthoringValidationReport report)
        {
            if (report == null)
            {
                return;
            }

            LocalPlayerCameraRequestBinding[] bindings =
                Object.FindObjectsByType<LocalPlayerCameraRequestBinding>(
                    FindObjectsInactive.Include);
            SceneLocalPlayerAdmissionAuthoring[] admissions =
                Object.FindObjectsByType<SceneLocalPlayerAdmissionAuthoring>(
                    FindObjectsInactive.Include);

            if (bindings == null || admissions == null)
            {
                return;
            }

            for (int bindingIndex = 0;
                 bindingIndex < bindings.Length;
                 bindingIndex++)
            {
                LocalPlayerCameraRequestBinding binding = bindings[bindingIndex];
                if (!IsLoadedSceneComponent(binding) ||
                    !binding.IsSceneAutoPublisherOptIn ||
                    binding.LocalPlayerHost == null ||
                    binding.PlayerActor == null)
                {
                    continue;
                }

                for (int admissionIndex = 0;
                     admissionIndex < admissions.Length;
                     admissionIndex++)
                {
                    SceneLocalPlayerAdmissionAuthoring admission =
                        admissions[admissionIndex];
                    if (!IsLoadedSceneComponent(admission) ||
                        !object.ReferenceEquals(
                            binding.LocalPlayerHost,
                            admission.LocalPlayerHost) ||
                        !object.ReferenceEquals(
                            binding.PlayerActor,
                            admission.SceneLogicalPlayerActor))
                    {
                        continue;
                    }

                    report.AddError(
                        "Local Player Camera Request Binding enables the Scene Auto-Publisher for a Player that is also admitted through Scene Local Player Admission. " +
                        "PlayerGameplayAdmissionRuntimeContext is the canonical publisher. Disable the binding's Scene Auto-Publisher opt-in and retain it only as authoring evidence, or remove the admission publisher for this Player.",
                        binding);
                }
            }
        }

        private static bool IsLoadedSceneComponent(Component component)
        {
            return component != null &&
                component.gameObject != null &&
                component.gameObject.scene.IsValid() &&
                component.gameObject.scene.isLoaded;
        }
    }
}
