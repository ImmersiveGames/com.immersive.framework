using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// Reusable authored Progression Save intent for one application.
    ///
    /// The Profile chooses the backend strategy only. It does not hold live save
    /// state, execute gameplay requests or observe Route/Activity lifecycle.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ProgressionSaveProfile",
        menuName = "Immersive Framework/Progression Save/Profile",
        order = 40)]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ADR018-C Progression Save backend authoring profile.")]
    public sealed class ProgressionSaveProfile : ScriptableObject
    {
        [SerializeField]
        [Tooltip(
            "Explicit backend selection. Built-in JSON uses the Framework minimum local backend. Custom Provider delegates materialization to the assigned provider asset.")]
        private ProgressionSaveBackendSelection backend =
            ProgressionSaveBackendSelection.BuiltInJson;

        [SerializeField]
        [Tooltip(
            "Required only when Backend is Custom Provider. The provider must return a valid IProgressionSaveStore or composition fails; the Framework never falls back to JSON.")]
        private ProgressionSaveStoreProviderAsset customProvider;

        public ProgressionSaveBackendSelection Backend => backend;

        public ProgressionSaveStoreProviderAsset CustomProvider =>
            customProvider;

        public bool IsValid => TryValidate(out _);

        public bool TryValidate(out string issue)
        {
            if (!Enum.IsDefined(
                    typeof(ProgressionSaveBackendSelection),
                    backend) ||
                backend == ProgressionSaveBackendSelection.Unknown)
            {
                issue =
                    $"Progression Save Profile '{name}' has invalid Backend '{backend}'.";
                return false;
            }

            if (backend ==
                ProgressionSaveBackendSelection.CustomProvider)
            {
                if (customProvider == null)
                {
                    issue =
                        $"Progression Save Profile '{name}' selects Custom Provider but no provider asset is assigned.";
                    return false;
                }

                try
                {
                    if (!customProvider.TryValidate(
                            out string providerIssue))
                    {
                        issue =
                            $"Progression Save Profile '{name}' custom provider '{customProvider.name}' is invalid. " +
                            providerIssue.NormalizeTextOrFallback(
                                "Provider validation failed without a diagnostic.");
                        return false;
                    }
                }
                catch (Exception exception)
                {
                    issue =
                        $"Progression Save Profile '{name}' custom provider '{customProvider.name}' threw during validation. " +
                        $"{exception.GetType().Name}: {exception.Message}";
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }

        /// <summary>
        /// Materializes the explicitly authored backend. This method does not create a
        /// ProgressionSaveRuntime; application composition owns runtime lifetime.
        /// </summary>
        public bool TryCreateStore(
            ProgressionSaveStoreCreationContext context,
            out IProgressionSaveStore store,
            out string issue)
        {
            store = null;

            if (!context.IsValid)
            {
                issue =
                    "Progression Save store materialization requires a valid application context.";
                return false;
            }

            if (!TryValidate(out issue))
            {
                return false;
            }

            try
            {
                switch (backend)
                {
                    case ProgressionSaveBackendSelection.BuiltInJson:
                        store =
                            JsonProgressionSaveStore.CreateDefault(
                                context.ApplicationName);
                        break;

                    case ProgressionSaveBackendSelection.CustomProvider:
                        if (!customProvider.TryCreateStore(
                                context,
                                out store,
                                out string providerIssue))
                        {
                            issue =
                                $"Progression Save custom provider '{customProvider.name}' could not create the selected backend. " +
                                providerIssue.NormalizeTextOrFallback(
                                    "Provider returned failure without a diagnostic.") +
                                " No fallback backend was used.";
                            store = null;
                            return false;
                        }

                        break;

                    default:
                        issue =
                            $"Progression Save backend selection '{backend}' is unsupported.";
                        return false;
                }
            }
            catch (Exception exception)
            {
                issue =
                    $"Progression Save backend materialization failed for selection '{backend}'. " +
                    $"{exception.GetType().Name}: {exception.Message}. No fallback backend was used.";
                store = null;
                return false;
            }

            if (store == null)
            {
                issue =
                    $"Progression Save backend selection '{backend}' produced no store. No fallback backend was used.";
                return false;
            }

            if (!store.BackendId.IsValid)
            {
                issue =
                    $"Progression Save backend selection '{backend}' produced a store with an invalid BackendId. No fallback backend was used.";
                store = null;
                return false;
            }

            issue = string.Empty;
            return true;
        }
    }
}
