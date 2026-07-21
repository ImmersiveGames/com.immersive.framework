using System;
using System.Collections.Generic;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerParticipation;
using UnityEngine.InputSystem;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// Activity-scoped registration authority for one explicitly admitted Local Player Host.
    /// It receives all evidence from its caller and never discovers a host, binding or Pause port.
    /// </summary>
    internal sealed class PauseActivityBindingRuntimeContext
    {
        private PauseActivityBindingRuntimeState _state = PauseActivityBindingRuntimeState.Inactive;
        private PauseActivityBindingScope _activeScope;
        private PauseActivityBindingScope _lastReleasedScope;
        private PauseActivityBindingIntentResolution _activeIntent;
        private LocalPlayerHostAuthoring _activeHost;
        private PausePlayerInputBinding _activeBinding;
        private IPauseProductBindingPort _activePort;
        private string _lastDiagnostic = "Pause Activity binding is inactive.";

        internal PauseActivityBindingRuntimeState State => _state;
        internal PauseActivityBindingRuntimeSnapshot Snapshot => CreateSnapshot();

        internal bool TryActivate(
            PauseActivityBindingScope activityScope,
            PauseActivityBindingIntentResolution intentResolution,
            IReadOnlyList<LocalPlayerHostAuthoring> admittedHosts,
            IPauseProductBindingPort bindingPort,
            string source,
            string reason,
            out PauseActivityBindingOperationResult result)
        {
            const string Operation = "activate";
            string resolvedSource = source.NormalizeTextOrFallback(nameof(PauseActivityBindingRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback("pause-activity-binding-activate");

            if (intentResolution.IsAbsent)
            {
                if (HasActiveEvidence)
                {
                    return Complete(
                        PauseActivityBindingOperationStatus.Rejected,
                        Operation,
                        "active-scope-conflict: Pause Activity binding is already active; an absent intent cannot replace it.",
                        false,
                        false,
                        string.Empty,
                        out result);
                }

                return Complete(
                    PauseActivityBindingOperationStatus.Absent,
                    Operation,
                    "intent-absent: Activity does not require Pause binding.",
                    false,
                    false,
                    string.Empty,
                    out result);
            }

            if (!IsValidActivityScope(activityScope))
            {
                return Complete(
                    PauseActivityBindingOperationStatus.Rejected,
                    Operation,
                    "activity-scope-invalid: Pause Activity binding requires a valid Activity binding scope.",
                    false,
                    false,
                    string.Empty,
                    out result);
            }

            if (HasActiveEvidence)
            {
                if (IsSameActivation(activityScope, intentResolution, admittedHosts, bindingPort))
                {
                    return Complete(
                        PauseActivityBindingOperationStatus.AlreadyActive,
                        Operation,
                        "binding-already-active: Pause Activity binding is already registered for this exact Activity scope.",
                        false,
                        false,
                        string.Empty,
                        out result);
                }

                return Complete(
                    PauseActivityBindingOperationStatus.Rejected,
                    Operation,
                    "active-scope-conflict: Pause Activity binding cannot replace an active scope, host, binding or port.",
                    false,
                    false,
                    string.Empty,
                    out result);
            }

            if (intentResolution.HasBlockingIssue || !intentResolution.HasIntent || !intentResolution.Intent.IsRequired)
            {
                return Complete(
                    PauseActivityBindingOperationStatus.Failed,
                    Operation,
                    $"intent-invalid: {intentResolution.Diagnostic.NormalizeTextOrFallback("Pause Activity binding intent is invalid.")}",
                    false,
                    false,
                    string.Empty,
                    out result);
            }

            if (bindingPort == null)
            {
                return Complete(
                    PauseActivityBindingOperationStatus.Rejected,
                    Operation,
                    "binding-port-missing: Pause Activity binding requires the explicit product binding port.",
                    false,
                    false,
                    string.Empty,
                    out result);
            }

            if (!TryResolveEligibleHost(admittedHosts, out LocalPlayerHostAuthoring host, out string hostDiagnostic))
            {
                return Complete(
                    PauseActivityBindingOperationStatus.Failed,
                    Operation,
                    hostDiagnostic,
                    false,
                    false,
                    string.Empty,
                    out result);
            }

            if (!TryResolveCoLocatedBinding(host, out PausePlayerInputBinding binding, out string bindingDiagnostic))
            {
                return Complete(
                    PauseActivityBindingOperationStatus.Failed,
                    Operation,
                    bindingDiagnostic,
                    false,
                    false,
                    string.Empty,
                    out result);
            }

            _state = PauseActivityBindingRuntimeState.Activating;
            if (!binding.TryInjectBindingPort(bindingPort, out string registrationDiagnostic))
            {
                _state = PauseActivityBindingRuntimeState.Inactive;
                return Complete(
                    PauseActivityBindingOperationStatus.Failed,
                    Operation,
                    $"binding-registration-failed: {registrationDiagnostic.NormalizeTextOrFallback("Pause PlayerInput Binding rejected registration.")}",
                    false,
                    false,
                    string.Empty,
                    out result);
            }

            if (!binding.HasActiveBinding)
            {
                bool rollbackSucceeded = binding.TryReleaseBinding(
                    resolvedReason,
                    out string rollbackDiagnostic);
                _state = PauseActivityBindingRuntimeState.Inactive;
                return Complete(
                    PauseActivityBindingOperationStatus.Failed,
                    Operation,
                    $"binding-registration-failed: Pause PlayerInput Binding reported registration without retained token evidence. {(rollbackSucceeded ? "rollback-succeeded" : "rollback-failed")}: {rollbackDiagnostic.NormalizeTextOrFallback("No rollback diagnostic was supplied.")}",
                    true,
                    rollbackSucceeded,
                    rollbackDiagnostic,
                    out result);
            }

            // All fallible validation completed before registration. Commit retained evidence only after it succeeds.
            _activeScope = activityScope;
            _activeIntent = intentResolution;
            _activeHost = host;
            _activeBinding = binding;
            _activePort = bindingPort;
            _state = PauseActivityBindingRuntimeState.Active;
            return Complete(
                PauseActivityBindingOperationStatus.Activated,
                Operation,
                $"binding-registered: Pause Activity binding registered for '{activityScope.StableText}' from '{resolvedSource}' reason '{resolvedReason}'.",
                false,
                false,
                string.Empty,
                out result);
        }

        internal bool TryRelease(
            PauseActivityBindingScope activityScope,
            string source,
            string reason,
            out PauseActivityBindingOperationResult result)
        {
            const string Operation = "release";
            string resolvedSource = source.NormalizeTextOrFallback(nameof(PauseActivityBindingRuntimeContext));
            string resolvedReason = reason.NormalizeTextOrFallback("pause-activity-binding-release");

            if (!IsValidActivityScope(activityScope))
            {
                return Complete(
                    PauseActivityBindingOperationStatus.Rejected,
                    Operation,
                    "activity-scope-invalid: Pause Activity binding release requires a valid Activity binding scope.",
                    false,
                    false,
                    string.Empty,
                    out result);
            }

            if (!HasActiveEvidence)
            {
                if (_lastReleasedScope.IsValid && activityScope == _lastReleasedScope)
                {
                    return Complete(
                        PauseActivityBindingOperationStatus.AlreadyReleased,
                        Operation,
                        "binding-released: Pause Activity binding was already released for this scope.",
                        false,
                        false,
                        string.Empty,
                        out result);
                }

                return Complete(
                    PauseActivityBindingOperationStatus.Rejected,
                    Operation,
                    ScopeMismatchDiagnostic(activityScope, _lastReleasedScope),
                    false,
                    false,
                    string.Empty,
                    out result);
            }

            if (activityScope != _activeScope)
            {
                return Complete(
                    PauseActivityBindingOperationStatus.Rejected,
                    Operation,
                    ScopeMismatchDiagnostic(activityScope, _activeScope),
                    false,
                    false,
                    string.Empty,
                    out result);
            }

            _state = PauseActivityBindingRuntimeState.Releasing;
            if (!_activeBinding.TryReleaseBinding(resolvedReason, out string releaseDiagnostic))
            {
                _state = PauseActivityBindingRuntimeState.Failed;
                return Complete(
                    PauseActivityBindingOperationStatus.Failed,
                    Operation,
                    $"binding-release-failed: {releaseDiagnostic.NormalizeTextOrFallback("Pause PlayerInput Binding rejected release.")}",
                    false,
                    false,
                    string.Empty,
                    out result);
            }

            _lastReleasedScope = _activeScope;
            ClearActiveEvidence();
            _state = PauseActivityBindingRuntimeState.Inactive;
            return Complete(
                PauseActivityBindingOperationStatus.Released,
                Operation,
                $"binding-released: Pause Activity binding released from '{resolvedSource}' reason '{resolvedReason}'.",
                false,
                false,
                string.Empty,
                out result);
        }

        private bool Complete(
            PauseActivityBindingOperationStatus status,
            string operation,
            string diagnostic,
            bool rollbackAttempted,
            bool rollbackSucceeded,
            string rollbackDiagnostic,
            out PauseActivityBindingOperationResult result)
        {
            _lastDiagnostic = diagnostic.NormalizeText();
            result = new PauseActivityBindingOperationResult(
                status,
                operation,
                CreateSnapshot(),
                rollbackAttempted,
                rollbackSucceeded,
                rollbackDiagnostic,
                _lastDiagnostic);
            return result.Succeeded;
        }

        private bool IsSameActivation(
            PauseActivityBindingScope activityScope,
            PauseActivityBindingIntentResolution intentResolution,
            IReadOnlyList<LocalPlayerHostAuthoring> admittedHosts,
            IPauseProductBindingPort bindingPort)
        {
            return activityScope == _activeScope &&
                intentResolution.Equals(_activeIntent) &&
                admittedHosts != null &&
                admittedHosts.Count == 1 &&
                ReferenceEquals(admittedHosts[0], _activeHost) &&
                ReferenceEquals(bindingPort, _activePort);
        }

        private static bool TryResolveEligibleHost(
            IReadOnlyList<LocalPlayerHostAuthoring> admittedHosts,
            out LocalPlayerHostAuthoring eligibleHost,
            out string diagnostic)
        {
            eligibleHost = null;
            if (admittedHosts == null || admittedHosts.Count == 0)
            {
                diagnostic = "admitted-host-evidence-missing: required Pause binding has no admitted Local Player Host.";
                return false;
            }

            for (int index = 0; index < admittedHosts.Count; index++)
            {
                LocalPlayerHostAuthoring host = admittedHosts[index];
                if (host == null)
                {
                    diagnostic = "admitted-host-evidence-missing: admitted Local Player Host evidence contains a null host.";
                    return false;
                }

                for (int compareIndex = index + 1; compareIndex < admittedHosts.Count; compareIndex++)
                {
                    if (ReferenceEquals(host, admittedHosts[compareIndex]))
                    {
                        diagnostic = "admitted-host-evidence-duplicate: the same Local Player Host was supplied more than once.";
                        return false;
                    }
                }

                if (!host.IsJoined)
                {
                    diagnostic = "host-not-joined: admitted Local Player Host must be Joined before Pause binding registration.";
                    return false;
                }

                if (!host.HasJoinedSlot || !host.JoinedPlayerSlotId.IsValid)
                {
                    diagnostic = "host-slot-invalid: admitted Local Player Host has no valid joined Player Slot identity.";
                    return false;
                }

                if (host.PlayerInput == null)
                {
                    diagnostic = "eligible-host-missing: admitted Local Player Host has no valid PlayerInput evidence.";
                    return false;
                }
            }

            if (admittedHosts.Count > 1)
            {
                diagnostic = "unsupported-multiple-eligible-hosts: multiple eligible Local Player Hosts are unsupported.";
                return false;
            }

            eligibleHost = admittedHosts[0];
            diagnostic = string.Empty;
            return true;
        }

        private static bool TryResolveCoLocatedBinding(
            LocalPlayerHostAuthoring host,
            out PausePlayerInputBinding binding,
            out string diagnostic)
        {
            binding = null;
            PausePlayerInputBinding[] bindings = host.GetComponents<PausePlayerInputBinding>();
            if (bindings.Length != 1 || bindings[0] == null)
            {
                PausePlayerInputBinding[] hierarchyBindings =
                    host.GetComponentsInChildren<PausePlayerInputBinding>(true);
                diagnostic = hierarchyBindings.Length > 0
                    ? "binding-not-colocated: Pause PlayerInput Binding must be co-located on the admitted Local Player Host GameObject."
                    : "binding-missing: admitted Local Player Host requires exactly one co-located Pause PlayerInput Binding.";
                return false;
            }

            binding = bindings[0];
            PlayerInput hostInput = host.PlayerInput;
            if (!ReferenceEquals(binding.PlayerInput, hostInput))
            {
                diagnostic = "binding-playerinput-mismatch: Pause PlayerInput Binding must reference the admitted Local Player Host PlayerInput.";
                return false;
            }

            diagnostic = string.Empty;
            return true;
        }

        private static bool IsValidActivityScope(PauseActivityBindingScope scope)
        {
            return scope.IsValid;
        }

        private static string ScopeMismatchDiagnostic(
            PauseActivityBindingScope supplied,
            PauseActivityBindingScope expected)
        {
            return expected.IsValid && supplied.Owner == expected.Owner
                ? "stale-scope-release: Activity scope entry sequence does not match the retained binding scope."
                : "foreign-scope-release: Activity scope does not own the retained binding scope.";
        }

        private bool HasActiveEvidence =>
            _activeScope.IsValid &&
            _activeHost != null &&
            _activeBinding != null &&
            _activePort != null;

        private void ClearActiveEvidence()
        {
            _activeScope = default;
            _activeIntent = default;
            _activeHost = null;
            _activeBinding = null;
            _activePort = null;
        }

        private PauseActivityBindingRuntimeSnapshot CreateSnapshot()
        {
            return new PauseActivityBindingRuntimeSnapshot(
                _state,
                _activeScope,
                _lastReleasedScope,
                HasActiveEvidence,
                _lastDiagnostic);
        }
    }
}
