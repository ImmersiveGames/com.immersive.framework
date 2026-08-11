using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// Canonical application-level materialization path for authored Progression Save
    /// configuration.
    ///
    /// The default framework bootstrap uses this same path. Custom bootstraps and QA
    /// may also resolve it explicitly without relying on global lookup.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "ADR018-C canonical authored Progression Save application composition.")]
    public static class ProgressionSaveApplicationComposition
    {
        public static ProgressionSaveApplicationCompositionResult Resolve(
            GameApplicationAsset gameApplication)
        {
            if (gameApplication == null)
            {
                throw new ArgumentNullException(
                    nameof(gameApplication));
            }

            if (!gameApplication.ProgressionSaveEnabled)
            {
                return ProgressionSaveApplicationCompositionResult.Disabled(
                    "Progression Save is disabled by the Game Application.");
            }

            ProgressionSaveProfile profile =
                gameApplication.DefaultProgressionSaveProfile;

            if (profile == null)
            {
                return ProgressionSaveApplicationCompositionResult.Rejected(
                    null,
                    "Progression Save is enabled but Default Progression Save Profile is missing.");
            }

            var context =
                new ProgressionSaveStoreCreationContext(
                    gameApplication.ApplicationName);

            if (!profile.TryCreateStore(
                    context,
                    out IProgressionSaveStore store,
                    out string issue))
            {
                return ProgressionSaveApplicationCompositionResult.Rejected(
                    profile,
                    $"Progression Save Profile '{profile.name}' could not materialize its selected backend. {issue}");
            }

            ProgressionSaveRuntime runtime;

            try
            {
                runtime =
                    new ProgressionSaveRuntime(
                        store);
            }
            catch (Exception exception)
            {
                return ProgressionSaveApplicationCompositionResult.Rejected(
                    profile,
                    $"Progression Save Runtime could not be created for Profile '{profile.name}'. " +
                    $"{exception.GetType().Name}: {exception.Message}");
            }

            return ProgressionSaveApplicationCompositionResult.Ready(
                profile,
                runtime,
                $"Progression Save ready. profile='{profile.name}' backendSelection='{profile.Backend}' backendId='{runtime.BackendId.StableText}'.");
        }
    }
}
