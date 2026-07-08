using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerBinding;
using Immersive.Framework.PlayerControls;
using Immersive.Framework.PlayerEntry;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.PlayerViews;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.Framework.Editor.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Editor-only surface helper for running passive Player binding authoring validation.
    /// This utility does not modify scenes, bind views, activate cameras, activate input, enable movement or spawn actors.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F50B Player binding authoring validation editor utility.")]
    public static class PlayerBindingAuthoringValidationEditorUtility
    {
        public const string DefaultSource = nameof(PlayerBindingAuthoringValidationEditorUtility);

        public static PlayerBindingAuthoringValidationReport ValidateSelectedRoot(string reason = null)
        {
            return ValidateRoot(Selection.activeGameObject, reason);
        }

        public static PlayerBindingAuthoringValidationReport ValidateRoot(GameObject validationRoot, string reason = null)
        {
            return PlayerBindingAuthoringValidator.ValidateHierarchy(
                validationRoot,
                DefaultSource,
                reason);
        }

        public static PlayerBindingAuthoringValidationReport ValidateActiveScene(string reason = null)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return PlayerBindingAuthoringValidator.ValidateHierarchy(
                    null,
                    DefaultSource,
                    reason);
            }

            GameObject[] roots = scene.GetRootGameObjects();
            var slotDeclarations = new List<PlayerSlotDeclaration>();
            var slotOccupancies = new List<PlayerSlotOccupancy>();
            var readinessBehaviours = new List<ActorReadinessBehaviour>();
            var entryBehaviours = new List<PlayerEntryBehaviour>();
            var viewBehaviours = new List<PlayerViewBehaviour>();
            var controlBehaviours = new List<PlayerControlBehaviour>();

            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null)
                {
                    continue;
                }

                slotDeclarations.AddRange(root.GetComponentsInChildren<PlayerSlotDeclaration>(true));
                slotOccupancies.AddRange(root.GetComponentsInChildren<PlayerSlotOccupancy>(true));
                readinessBehaviours.AddRange(root.GetComponentsInChildren<ActorReadinessBehaviour>(true));
                entryBehaviours.AddRange(root.GetComponentsInChildren<PlayerEntryBehaviour>(true));
                viewBehaviours.AddRange(root.GetComponentsInChildren<PlayerViewBehaviour>(true));
                controlBehaviours.AddRange(root.GetComponentsInChildren<PlayerControlBehaviour>(true));
            }

            return PlayerBindingAuthoringValidator.ValidateComponents(
                slotDeclarations,
                slotOccupancies,
                readinessBehaviours,
                entryBehaviours,
                viewBehaviours,
                controlBehaviours,
                DefaultSource,
                reason);
        }

        public static void LogReport(PlayerBindingAuthoringValidationReport report)
        {
            string message = FormatReport(report);
            if (report == null || report.Failed)
            {
                Debug.LogError(message);
                return;
            }

            Debug.Log(message);
        }

        public static string FormatReport(PlayerBindingAuthoringValidationReport report)
        {
            if (report == null)
            {
                return "[Immersive.Framework][PlayerBindingAuthoringValidation] report='<null>'";
            }

            return "[Immersive.Framework][PlayerBindingAuthoringValidation] " + report.ToDiagnosticString();
        }
    }
}
