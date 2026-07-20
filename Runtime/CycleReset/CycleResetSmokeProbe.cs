#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Diagnostics;
using UnityEngine;

namespace Immersive.Framework.CycleReset
{
    /// <summary>
    /// Compatibility shell for the retired package-local Cycle Reset smoke probe.
    /// Canonical Cycle Reset QA runs from QAFramework.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/QA/Cycle Reset Smoke Probe")]
    [FrameworkApiStatus(
        FrameworkApiStatus.DevelopmentTooling,
        "H2.2.13 retired package-local Cycle Reset smoke probe. Canonical technical QA lives in QAFramework.")]
    public sealed class CycleResetSmokeProbe : MonoBehaviour
    {
        private const string MigrationDiagnostic =
            "Package-local Cycle Reset smoke execution is retired. Run the canonical Cycle Reset smokes from QAFramework. No runtime request was executed.";

        [Header("Legacy Smoke Intent")]
        [SerializeField] private bool runRouteCycleReset = true;
        [SerializeField] private bool runActivityCycleReset = true;

        [Header("Legacy Diagnostics")]
        [SerializeField] private bool logParticipantDetails = true;

        private FrameworkLogger logger;

        public string Diagnostic =>
            MigrationDiagnostic;

        private void Awake()
        {
            EnsureLogger();
        }

        [ContextMenu("Run Cycle Reset Smoke")]
        public void RunCycleResetSmoke()
        {
            EnsureLogger();
            logger.Warning(
                $"QA Smoke unavailable. probe='{nameof(CycleResetSmokeProbe)}' " +
                $"routeRequested='{runRouteCycleReset}' " +
                $"activityRequested='{runActivityCycleReset}' " +
                $"participantDetailsRequested='{logParticipantDetails}' " +
                $"reason='{MigrationDiagnostic}'.");
        }

        private void EnsureLogger()
        {
            logger ??=
                FrameworkLogger.Create<
                    CycleResetSmokeProbe>();
        }
    }
}
#endif
