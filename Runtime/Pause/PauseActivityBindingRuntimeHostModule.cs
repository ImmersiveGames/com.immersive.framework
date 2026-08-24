using System;
using System.Collections.Generic;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// Host-scoped bridge between the official Activity transaction and the P2.1B binding authority.
    /// It only consumes explicit scene roots and Player lifecycle evidence.
    /// </summary>
    internal sealed class PauseActivityBindingRuntimeHostModule
    {
        private readonly PauseActivityBindingRuntimeContext _runtime;
        private readonly IPauseProductBindingPort _bindingPort;
        private IPauseActivityBindingPlayerEvidence _playerEvidence;
        private RuntimeContentOwner _preparedOwner;
        private int _preparedEntrySequence;
        private PauseActivityBindingIntentResolution _preparedIntent;
        private bool _hasPreparedIntent;

        internal PauseActivityBindingRuntimeHostModule(
            PauseActivityBindingRuntimeContext runtime,
            IPauseProductBindingPort bindingPort)
        {
            this._runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            this._bindingPort = bindingPort ?? throw new ArgumentNullException(nameof(bindingPort));
        }

        internal PauseActivityBindingRuntimeSnapshot Snapshot => _runtime.Snapshot;

        internal void SetPlayerEvidence(IPauseActivityBindingPlayerEvidence evidence)
        {
            _playerEvidence = evidence;
        }

        internal bool TryPrepareIntent(
            RuntimeContentOwner owner,
            int entrySequence,
            IReadOnlyList<GameObject> materializedRoots,
            string source,
            string reason,
            out string diagnostic)
        {
            diagnostic = string.Empty;
            if (!owner.IsValid || owner.Scope != RuntimeContentScope.Activity ||
                entrySequence <= 0)
            {
                diagnostic = "activity-scope-invalid: Pause Activity binding intent preparation requires a valid Activity owner and positive canonical entry sequence.";
                return false;
            }

            PauseActivityBindingIntentResolution intent =
                PauseActivityBindingAuthoringValidator.ResolveFromRoots(
                    materializedRoots,
                    source);
            _preparedOwner = owner;
            _preparedEntrySequence = entrySequence;
            _preparedIntent = intent;
            _hasPreparedIntent = true;
            if (intent.IsAbsent)
            {
                diagnostic = "intent-absent: Activity has no Pause Activity Binding declaration.";
                return true;
            }

            if (intent.HasBlockingIssue || !intent.HasIntent)
            {
                diagnostic = "intent-invalid: " + intent.Diagnostic;
                return false;
            }

            diagnostic = $"intent-prepared: owner='{owner.StableText}' entrySequence='{entrySequence}' status='{intent.Status}'.";
            return true;
        }

        internal bool TryActivate(
            ActivityAsset activity,
            RuntimeContentOwner owner,
            int entrySequence,
            string source,
            string reason,
            out string diagnostic)
        {
            diagnostic = string.Empty;
            PauseActivityBindingScope scope;
            try
            {
                scope = new PauseActivityBindingScope(owner, entrySequence);
            }
            catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException)
            {
                diagnostic = $"activity-scope-invalid: {exception.Message}";
                return false;
            }

            if (!_hasPreparedIntent ||
                _preparedOwner != owner ||
                _preparedEntrySequence != entrySequence)
            {
                diagnostic = "intent-not-prepared: Pause Activity binding activation requires the intent frozen by this Activity transition.";
                return false;
            }

            if (_preparedIntent.IsAbsent)
            {
                diagnostic = "intent-absent: Activity has no Pause Activity Binding declaration.";
                return true;
            }

            IReadOnlyList<LocalPlayerHostAuthoring> hosts = Array.Empty<LocalPlayerHostAuthoring>();
            string playerDiagnostic = string.Empty;
            if (_playerEvidence == null ||
                !_playerEvidence.TryResolveAdmittedHosts(
                    activity,
                    owner,
                    out hosts,
                    out playerDiagnostic))
            {
                diagnostic =
                    "waiting-for-player-admission: Pause Activity binding required explicit admitted Local Player Host evidence. " +
                    (string.IsNullOrWhiteSpace(playerDiagnostic)
                        ? "No official Player lifecycle evidence is available."
                        : playerDiagnostic);
                return false;
            }

            bool succeeded = _runtime.TryActivate(
                scope,
                _preparedIntent,
                hosts,
                _bindingPort,
                source,
                reason,
                out PauseActivityBindingOperationResult operation);
            diagnostic = succeeded
                ? operation.Status == PauseActivityBindingOperationStatus.AlreadyActive
                    ? "binding-already-active: " + operation.Diagnostic
                    : "binding-activated: " + operation.Diagnostic
                : $"binding-activation-failed: scope='{scope.StableText}' hosts='{hosts?.Count ?? 0}' intent='{_preparedIntent.Status}'. {operation.Diagnostic}";
            return succeeded;
        }

        internal bool TryReleaseForOwner(
            RuntimeContentOwner owner,
            string source,
            string reason,
            out string diagnostic)
        {
            PauseActivityBindingRuntimeSnapshot snapshot = _runtime.Snapshot;
            if (!snapshot.HasActiveBinding)
            {
                diagnostic = "binding-release-not-required: no Pause Activity binding is active.";
                return true;
            }

            if (snapshot.ActiveScope.Owner != owner)
            {
                diagnostic =
                    $"binding-release-failed: active Pause binding belongs to '{snapshot.ActiveScope.Owner.StableText}', not exiting owner '{owner.StableText}'.";
                return false;
            }

            bool succeeded = _runtime.TryRelease(
                snapshot.ActiveScope,
                source,
                reason,
                out PauseActivityBindingOperationResult operation);
            if (succeeded)
            {
                ClearPreparedIntentForOwner(owner);
            }
            diagnostic = succeeded
                ? "binding-released: " + operation.Diagnostic
                : "binding-release-failed: " + operation.Diagnostic;
            return succeeded;
        }

        internal bool TryRollbackTarget(
            RuntimeContentOwner owner,
            string source,
            string reason,
            out string diagnostic)
        {
            diagnostic = string.Empty;
            ClearPreparedIntentForOwner(owner);
            if (!_runtime.Snapshot.HasActiveBinding || _runtime.Snapshot.ActiveScope.Owner != owner)
            {
                return true;
            }

            bool succeeded = TryReleaseForOwner(owner, source, reason, out string releaseDiagnostic);
            diagnostic = succeeded
                ? "binding-rollback-started: " + releaseDiagnostic
                : "binding-rollback-failed: " + releaseDiagnostic;
            return succeeded;
        }

        private void ClearPreparedIntentForOwner(RuntimeContentOwner owner)
        {
            if (!_hasPreparedIntent || _preparedOwner != owner)
            {
                return;
            }

            _hasPreparedIntent = false;
            _preparedOwner = default;
            _preparedEntrySequence = 0;
            _preparedIntent = default;
        }
    }
}
