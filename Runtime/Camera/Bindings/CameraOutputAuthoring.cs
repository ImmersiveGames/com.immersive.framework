using System;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Common;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.ApiStatus;
using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Scene-authored owner of one scoped CameraOutputSession.
    /// No global registration or lookup is performed.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Camera/Camera Output Authoring")]
    [FrameworkApiStatus(FrameworkApiStatus.Stable, "Stable single-output Camera product surface. Multi-output/split-screen is out of scope.")]
    public sealed class CameraOutputAuthoring : MonoBehaviour
    {
        [SerializeField] private string outputId;
        [SerializeField] private UnityEngine.Camera unityCamera;
        [SerializeField] private CinemachineBrain cinemachineBrain;
        [SerializeField] private CameraRigComposer defaultCameraRig;
        [SerializeField] private bool initializeOnAwake = true;
        [SerializeField] private bool logDiagnostics = true;

        [Header("Debug")]
        [SerializeField] private string lastStatus = "NotInitialized";
        [SerializeField] private string lastDiagnostic;

        private CameraOutputContext _context;
        private CameraOutputRigApplicator _applicator;
        private CameraOutputSession _session;
        private FrameworkLogger _logger;

        public string OutputIdText => outputId.NormalizeText();
        public UnityEngine.Camera UnityCamera => unityCamera;
        public CinemachineBrain CinemachineBrain => cinemachineBrain;
        public CameraRigComposer DefaultCameraRig => defaultCameraRig;
        public bool IsInitialized => _session != null;
        public CameraOutputContext Context => _context;
        public CameraOutputRigApplicator Applicator => _applicator;
        public CameraOutputSession Session => _session;
        public string LastStatus => lastStatus ?? string.Empty;
        public string LastDiagnostic => lastDiagnostic ?? string.Empty;

        private void Reset()
        {
            if (string.IsNullOrWhiteSpace(outputId))
            {
                outputId = Guid.NewGuid().ToString("N");
            }
        }

        private void Awake()
        {
            if (initializeOnAwake)
            {
                TryInitialize(out _);
            }
        }

        private void OnDestroy()
        {
            if (_session != null)
            {
                _session.Teardown();
            }

            _session = null;
            _applicator = null;
            _context = null;
        }

        public bool TryInitialize(out string diagnostic)
        {
            if (_session != null)
            {
                diagnostic = "Camera output session is already initialized.";
                SetDiagnostic("Preserved", diagnostic, false);
                return true;
            }

            string normalizedOutputId = outputId.NormalizeText();

            if (string.IsNullOrWhiteSpace(normalizedOutputId))
            {
                diagnostic = "Camera Output Authoring requires an explicit output id.";
                SetDiagnostic("Blocked", diagnostic, true);
                return false;
            }

            if (unityCamera == null)
            {
                diagnostic = "Camera Output Authoring requires an explicit Unity Camera.";
                SetDiagnostic("Blocked", diagnostic, true);
                return false;
            }

            if (cinemachineBrain == null)
            {
                diagnostic = "Camera Output Authoring requires an explicit CinemachineBrain.";
                SetDiagnostic("Blocked", diagnostic, true);
                return false;
            }

            if (cinemachineBrain.gameObject != unityCamera.gameObject)
            {
                diagnostic = "Unity Camera and CinemachineBrain must exist on the same GameObject.";
                SetDiagnostic("Blocked", diagnostic, true);
                return false;
            }

            if (defaultCameraRig == null)
            {
                diagnostic = "Camera Output Authoring requires an explicit Default Camera Rig.";
                SetDiagnostic("Blocked", diagnostic, true);
                return false;
            }

            var resolvedOutputId = new CameraOutputId(normalizedOutputId);
            var resolvedDefaultRig = CameraRigReference.FromComposer(defaultCameraRig);

            try
            {
                var resolvedContext = new CameraOutputContext(resolvedOutputId);
                var resolvedApplicator = new CameraOutputRigApplicator(
                    new CameraOutputBinding(
                        resolvedOutputId,
                        unityCamera,
                        cinemachineBrain));
                var resolvedSession = new CameraOutputSession(
                    resolvedContext,
                    resolvedApplicator,
                    resolvedDefaultRig);

                CameraOutputSessionResult synchronizeResult =
                    resolvedSession.Synchronize();
                if (!synchronizeResult.Succeeded)
                {
                    resolvedSession.Teardown();
                    diagnostic =
                        $"Camera Output Authoring could not apply the explicit Default Camera Rig. {synchronizeResult.DiagnosticSummary}";
                    SetDiagnostic("Blocked", diagnostic, true);
                    return false;
                }

                _context = resolvedContext;
                _applicator = resolvedApplicator;
                _session = resolvedSession;
            }
            catch (Exception exception)
            {
                _context = null;
                _applicator = null;
                _session = null;
                diagnostic =
                    $"Camera Output Authoring initialization failed. exception='{exception.GetType().Name}' message='{exception.Message}'.";
                SetDiagnostic("Blocked", diagnostic, true);
                return false;
            }

            diagnostic =
                $"Camera output session initialized. output='{resolvedOutputId}' camera='{unityCamera.name}' brain='{cinemachineBrain.name}' defaultRig='{defaultCameraRig.name}'.";
            SetDiagnostic("Initialized", diagnostic, false);
            return true;
        }

        public bool TryGetSession(
            out CameraOutputSession resolvedSession,
            out string diagnostic)
        {
            if (!TryInitialize(out diagnostic))
            {
                resolvedSession = null;
                return false;
            }

            resolvedSession = _session;
            return true;
        }

        private void SetDiagnostic(
            string status,
            string diagnostic,
            bool error)
        {
            lastStatus = status.NormalizeTextOrFallback("Unknown");
            lastDiagnostic = diagnostic.NormalizeText();

            string message =
                $"[FRAMEWORK_CAMERA] Camera Output Authoring status='{lastStatus}' diagnostic='{lastDiagnostic}'.";

            EnsureLogger();
            if (error)
            {
                _logger.Error(message);
            }
            else if (logDiagnostics)
            {
                _logger.Debug(message);
            }
        }

        private void EnsureLogger()
        {
            _logger ??= FrameworkLogger.Create<CameraOutputAuthoring>();
        }
    }
}
