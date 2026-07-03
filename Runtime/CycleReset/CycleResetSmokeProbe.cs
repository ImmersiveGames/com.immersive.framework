#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Diagnostics;
using UnityEngine;

namespace Immersive.Framework.CycleReset
{
    /// <summary>
    /// Development-only smoke probe for the F11 Cycle Reset foundation.
    /// It validates the canonical runtime-host request path with synthetic participants only.
    /// It does not reset Unity objects, reload scenes, release content, restore snapshots, return pooled objects or touch gameplay state.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/QA/Cycle Reset Smoke Probe")]
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F11D/F11E QA probe for Cycle Reset runtime-host request path; no physical reset.")]
    public sealed class CycleResetSmokeProbe : MonoBehaviour
    {
        private const string QaSource = nameof(CycleResetSmokeProbe);

        private FrameworkLogger _logger;

        [Header("Smoke Steps")]
        [SerializeField] private bool runRouteCycleReset = true;
        [SerializeField] private bool runActivityCycleReset = true;

        [Header("Diagnostics")]
        [SerializeField] private bool logParticipantDetails = true;

        private void Awake()
        {
            EnsureLogger();
        }

        [ContextMenu("Run Cycle Reset Smoke")]
        public async void RunCycleResetSmoke()
        {
            EnsureLogger();
            await RunCycleResetSmokeAsync();
        }

        private async Task<bool> RunCycleResetSmokeAsync()
        {
            if (!FrameworkRuntimeHost.TryGetCurrent(out var runtimeHost))
            {
                _logger.Warning($"QA Smoke aborted. name='{CycleResetQaSmokeRunner.SmokeName}'. reason='Framework Runtime Host is missing'.");
                return false;
            }

            return await CycleResetQaSmokeRunner.RunRuntimeHostSmokeAsync(
                runtimeHost,
                _logger,
                QaSource,
                runRouteCycleReset,
                runActivityCycleReset,
                logParticipantDetails,
                emitSmokeEnvelope: true);
        }

        private void EnsureLogger()
        {
            if (_logger != null)
            {
                return;
            }

            _logger = FrameworkLogger.Create<CycleResetSmokeProbe>();
        }
    }
}
#endif
