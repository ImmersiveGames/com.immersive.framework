using Immersive.Framework.Common;
using Immersive.Framework.Diagnostics;
using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Scene-authored owner of one scoped CameraOutputSession.
    /// No global registration or lookup is performed.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Camera/Camera Output Session Binding")]
    public sealed class CameraOutputSessionBinding : MonoBehaviour
    {
        [SerializeField] private string outputId = "camera.output.main";
        [SerializeField] private UnityEngine.Camera unityCamera;
        [SerializeField] private CinemachineBrain cinemachineBrain;
        [SerializeField] private bool initializeOnAwake = true;
        [SerializeField] private bool logDiagnostics = true;

        [Header("Debug")]
        [SerializeField] private string lastStatus = "NotInitialized";
        [SerializeField] private string lastDiagnostic;

        private CameraOutputContext context;
        private CameraOutputRigApplicator applicator;
        private CameraOutputSession session;
        private FrameworkLogger logger;

        public string OutputIdText => outputId.NormalizeText();
        public UnityEngine.Camera UnityCamera => unityCamera;
        public CinemachineBrain CinemachineBrain => cinemachineBrain;
        public bool IsInitialized => session != null;
        public CameraOutputContext Context => context;
        public CameraOutputRigApplicator Applicator => applicator;
        public CameraOutputSession Session => session;
        public string LastStatus => lastStatus ?? string.Empty;
        public string LastDiagnostic => lastDiagnostic ?? string.Empty;

        private void Awake()
        {
            if (initializeOnAwake)
            {
                TryInitialize(out _);
            }
        }

        public bool TryInitialize(out string diagnostic)
        {
            if (session != null)
            {
                diagnostic = "Camera output session is already initialized.";
                SetDiagnostic("Preserved", diagnostic, false);
                return true;
            }

            string normalizedOutputId = outputId.NormalizeText();

            if (string.IsNullOrWhiteSpace(normalizedOutputId))
            {
                diagnostic = "Camera Output Session Binding requires an explicit output id.";
                SetDiagnostic("Blocked", diagnostic, true);
                return false;
            }

            if (unityCamera == null)
            {
                diagnostic = "Camera Output Session Binding requires an explicit Unity Camera.";
                SetDiagnostic("Blocked", diagnostic, true);
                return false;
            }

            if (cinemachineBrain == null)
            {
                diagnostic = "Camera Output Session Binding requires an explicit CinemachineBrain.";
                SetDiagnostic("Blocked", diagnostic, true);
                return false;
            }

            if (cinemachineBrain.gameObject != unityCamera.gameObject)
            {
                diagnostic = "Unity Camera and CinemachineBrain must exist on the same GameObject.";
                SetDiagnostic("Blocked", diagnostic, true);
                return false;
            }

            var resolvedOutputId = new CameraOutputId(normalizedOutputId);

            try
            {
                context = new CameraOutputContext(resolvedOutputId);
                applicator = new CameraOutputRigApplicator(
                    new CameraOutputBinding(
                        resolvedOutputId,
                        unityCamera,
                        cinemachineBrain));
                session = new CameraOutputSession(context, applicator);
            }
            catch (System.Exception exception)
            {
                context = null;
                applicator = null;
                session = null;
                diagnostic =
                    $"Camera Output Session Binding initialization failed. exception='{exception.GetType().Name}' message='{exception.Message}'.";
                SetDiagnostic("Blocked", diagnostic, true);
                return false;
            }

            diagnostic =
                $"Camera output session initialized. output='{resolvedOutputId}' camera='{unityCamera.name}' brain='{cinemachineBrain.name}'.";
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

            resolvedSession = session;
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
                $"[FRAMEWORK_CAMERA] Camera Output Session Binding status='{lastStatus}' diagnostic='{lastDiagnostic}'.";

            EnsureLogger();
            if (error)
            {
                logger.Error(message);
            }
            else if (logDiagnostics)
            {
                logger.Debug(message);
            }
        }

        private void EnsureLogger()
        {
            logger ??= FrameworkLogger.Create<CameraOutputSessionBinding>();
        }
    }
}
