using Immersive.Framework.ApiStatus;
using Immersive.Framework.CameraAuthoring;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Camera/Session Camera Assignment Command Trigger")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-039 game-facing Session Camera Assignment command boundary.")]
    public sealed class SessionCameraAssignmentCommandTrigger : MonoBehaviour
    {
        [SerializeField] private SessionCameraAssignmentCommandKind command = SessionCameraAssignmentCommandKind.Activate;
        [SerializeField] private SessionCameraAssignmentAsset assignment;
        [SerializeField, Tooltip("Required by Replace. Exact currently active Assignment asset expected to be replaced.")]
        private SessionCameraAssignmentAsset previousAssignment;

        private ISessionCameraAssignmentCommandPort _runtime;
        private string _bindingDiagnostic = "Session Camera Assignment command runtime is not bound.";

        public SessionCameraAssignmentCommandKind Command => command;
        public SessionCameraAssignmentAsset Assignment => assignment;
        public SessionCameraAssignmentAsset PreviousAssignment => previousAssignment;
        public bool HasRuntimeBinding => _runtime != null;
        public string RuntimeBindingDiagnostic => _bindingDiagnostic;
        public bool LastCommandSucceeded { get; private set; }
        public string LastDiagnostic { get; private set; } = "No Session Camera Assignment command has executed.";

        internal bool TryBind(ISessionCameraAssignmentCommandPort runtime, out string issue)
        {
            if (runtime == null)
            {
                issue = "Session Camera Assignment command binding requires a non-null runtime port.";
                _bindingDiagnostic = issue;
                return false;
            }

            if (_runtime == null)
            {
                _runtime = runtime;
                issue = string.Empty;
                _bindingDiagnostic = $"Bound '{runtime.GetType().FullName}'.";
                return true;
            }

            if (ReferenceEquals(_runtime, runtime))
            {
                issue = string.Empty;
                _bindingDiagnostic = $"Bound '{runtime.GetType().FullName}' (idempotent).";
                return true;
            }

            issue = "Session Camera Assignment command binding rejected a different runtime port for the current lifetime.";
            _bindingDiagnostic = issue;
            return false;
        }

        public void Execute()
        {
            LastCommandSucceeded = false;
            if (_runtime == null)
            {
                LastDiagnostic = "Session Camera Assignment command failed because its runtime port is not bound.";
                return;
            }

            switch (command)
            {
                case SessionCameraAssignmentCommandKind.Activate:
                    LastCommandSucceeded = _runtime.TryActivate(assignment, out string activateIssue);
                    LastDiagnostic = LastCommandSucceeded ? "Session Camera Assignment activated." : activateIssue;
                    return;
                case SessionCameraAssignmentCommandKind.Replace:
                    if (previousAssignment == null)
                    {
                        LastDiagnostic = "Session Camera Assignment Replace requires an explicit previous Assignment asset.";
                        return;
                    }
                    LastCommandSucceeded = _runtime.TryReplace(previousAssignment, assignment, out string replaceIssue);
                    LastDiagnostic = LastCommandSucceeded ? "Session Camera Assignment replaced." : replaceIssue;
                    return;
                case SessionCameraAssignmentCommandKind.Clear:
                    if (assignment == null)
                    {
                        LastDiagnostic = "Session Camera Assignment Clear requires an explicit Assignment asset.";
                        return;
                    }
                    LastCommandSucceeded = _runtime.TryClear(assignment, out string clearIssue);
                    LastDiagnostic = LastCommandSucceeded ? "Session Camera Assignment cleared." : clearIssue;
                    return;
                default:
                    LastDiagnostic = "Session Camera Assignment command kind is undefined.";
                    return;
            }
        }
    }
}
