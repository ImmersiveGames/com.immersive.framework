using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Activity-scoped transaction coordinator for Scene Local Player contextual representation
    /// and initial Scene-Provided Actor adoption. Logical Player membership, successful
    /// physical adoption and Actor selection are Session-scoped; this runtime owns only
    /// Activity contextual admission.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "ADR-019 Activity lifecycle coordination for Scene Local Player contextual representation and external Actor adoption.")]
    internal sealed class SceneLocalPlayerAdmissionActivityLifecycleRuntime
    {
        private enum ContextualRetirementCause
        {
            ActivityExit = 10,
            SessionPlayerLeave = 20,
            SessionTermination = 30
        }

        private sealed class Entry
        {
            internal Entry(
                SceneLocalPlayerAdmissionAuthoring authoring,
                PlayerSlotId playerSlotId,
                ActorProfile actorProfile,
                SceneLocalPlayerAdmissionToken admissionToken,
                int selectionRevision,
                bool selectionApplied,
                ScenePlayerActorAdoptionToken adoptionToken,
                bool adoptionApplied)
            {
                Authoring = authoring;
                PlayerSlotId = playerSlotId;
                ActorProfile = actorProfile;
                AdmissionToken = admissionToken;
                SelectionRevision = selectionRevision;
                SelectionApplied = selectionApplied;
                AdoptionToken = adoptionToken;
                AdoptionApplied = adoptionApplied;
                AdmissionActive = true;
            }

            internal SceneLocalPlayerAdmissionAuthoring Authoring { get; }
            internal PlayerSlotId PlayerSlotId { get; }
            internal ActorProfile ActorProfile { get; }
            internal SceneLocalPlayerAdmissionToken AdmissionToken { get; set; }
            internal int SelectionRevision { get; set; }
            internal bool SelectionApplied { get; set; }
            internal ScenePlayerActorAdoptionToken AdoptionToken { get; set; }
            internal bool AdoptionApplied { get; set; }
            internal bool AdmissionActive { get; set; }
        }

        private sealed class ActiveRecord
        {
            internal ActiveRecord(
                ActivityAsset activity,
                RuntimeContentOwner owner,
                List<Entry> entries)
            {
                Activity = activity;
                Owner = owner;
                Entries = entries ?? new List<Entry>();
            }

            internal ActivityAsset Activity { get; }
            internal RuntimeContentOwner Owner { get; }
            internal List<Entry> Entries { get; }
        }

        private readonly SceneLocalPlayerAdmissionRuntimeHostModule _module;
        private readonly PlayerActorPreparationRuntimeHostModule _preparationModule;
        private ActiveRecord _activeRecord;
        private string _diagnostic =
            "Scene Local Player Activity lifecycle has not executed.";

        internal SceneLocalPlayerAdmissionActivityLifecycleRuntime(
            SceneLocalPlayerAdmissionRuntimeHostModule module,
            PlayerActorPreparationRuntimeHostModule preparationModule = null)
        {
            this._module = module ?? throw new ArgumentNullException(nameof(module));
            this._preparationModule = preparationModule;
        }

        internal string Diagnostic => _diagnostic;
        internal int ActiveEntryCount => _activeRecord?.Entries.Count ?? 0;

        internal SceneLocalPlayerAdmissionActivityLifecycleResult TryEnter(
            ActivityAsset activity,
            RuntimeContentOwner owner,
            string source,
            string reason)
        {
            string resolvedSource = Normalize(
                source,
                nameof(SceneLocalPlayerAdmissionActivityLifecycleRuntime));
            string resolvedReason = Normalize(
                reason,
                "scene-local-player-activity-enter");

            if (activity == null || !owner.IsValid)
            {
                return Failure(
                    SceneLocalPlayerAdmissionActivityLifecycleStatus.RejectedInvalidRequest,
                    activity,
                    owner,
                    resolvedSource,
                    resolvedReason,
                    "Scene Local Player Activity enter requires an Activity and valid Activity owner.");
            }

            if (_activeRecord != null)
            {
                if (ReferenceEquals(_activeRecord.Activity, activity) &&
                    _activeRecord.Owner == owner)
                {
                    return Success(
                        SceneLocalPlayerAdmissionActivityLifecycleStatus.SucceededAlreadyEntered,
                        activity,
                        owner,
                        resolvedSource,
                        resolvedReason,
                        _activeRecord.Entries.Count,
                        "Scene Local Player Activity lifecycle is already entered for the same owner.");
                }

                return Failure(
                    SceneLocalPlayerAdmissionActivityLifecycleStatus.RejectedForeignOrStaleActivity,
                    activity,
                    owner,
                    resolvedSource,
                    resolvedReason,
                    $"Activity owner '{owner.StableText}' cannot replace retained Scene Local Player owner '{_activeRecord.Owner.StableText}' without exit.");
            }

            if (!_module.TryResolveAutomaticActivityAuthoring(
                    activity,
                    out IReadOnlyList<SceneLocalPlayerAdmissionAuthoring> authoring,
                    out string resolveIssue))
            {
                return Failure(
                    SceneLocalPlayerAdmissionActivityLifecycleStatus.FailedAuthoringResolution,
                    activity,
                    owner,
                    resolvedSource,
                    resolvedReason,
                    resolveIssue);
            }

            if (authoring.Count == 0)
            {
                return Success(
                    SceneLocalPlayerAdmissionActivityLifecycleStatus.SucceededNoAutomaticPlayers,
                    activity,
                    owner,
                    resolvedSource,
                    resolvedReason,
                    0,
                    "Activity has no automatic Scene Local Player Admission surfaces.");
            }

            if (!activity.HasDefinedPlayerParticipationRequirementLevel)
            {
                return Failure(
                    SceneLocalPlayerAdmissionActivityLifecycleStatus.FailedRequirement,
                    activity,
                    owner,
                    resolvedSource,
                    resolvedReason,
                    $"Activity '{activity.ActivityName}' requires a valid Player Participation Requirements Profile.");
            }

            PlayerParticipationRequirementLevel requirementLevel =
                activity.PlayerParticipationRequirementLevel;
            bool requiresActorAdoption =
                (int)requirementLevel >=
                (int)PlayerParticipationRequirementLevel.LogicalActorsPrepared;
            if (requiresActorAdoption &&
                (_preparationModule == null || !_preparationModule.IsReady))
            {
                return Failure(
                    SceneLocalPlayerAdmissionActivityLifecycleStatus.RejectedActorAdoptionRequired,
                    activity,
                    owner,
                    resolvedSource,
                    resolvedReason,
                    $"Activity '{activity.ActivityName}' requires '{requirementLevel}', but the host-scoped Player Actor preparation authority is unavailable.");
            }

            var entries = new List<Entry>(authoring.Count);
            for (int index = 0; index < authoring.Count; index++)
            {
                SceneLocalPlayerAdmissionAuthoring surface = authoring[index];
                SceneLocalPlayerAdmissionRuntimeResult admission = _module.TryAdmit(
                    surface,
                    owner,
                    resolvedSource,
                    $"{resolvedReason}:admit:{index}");
                if (admission == null || !admission.Succeeded || !admission.Token.IsValid)
                {
                    string issue = admission != null
                        ? admission.ToDiagnosticString()
                        : $"Scene Local Player admission returned no result for '{surface.name}'.";
                    return FailEnterAndRollback(
                        activity,
                        owner,
                        entries,
                        resolvedSource,
                        resolvedReason,
                        SceneLocalPlayerAdmissionActivityLifecycleStatus.FailedAdmission,
                        issue);
                }

                if (!surface.TryGetPlayerSlotId(
                        out PlayerSlotId playerSlotId,
                        out string slotIssue) ||
                    !_module.TryGetSlotSnapshot(
                        playerSlotId,
                        out PlayerSlotRuntimeSnapshot slot))
                {
                    TryReleaseAdmissionOnly(
                        surface,
                        admission.Token,
                        resolvedSource,
                        resolvedReason,
                        out _);
                    return FailEnterAndRollback(
                        activity,
                        owner,
                        entries,
                        resolvedSource,
                        resolvedReason,
                        SceneLocalPlayerAdmissionActivityLifecycleStatus.FailedAdmission,
                        string.IsNullOrWhiteSpace(slotIssue)
                            ? $"Joined Scene Local Player Slot '{playerSlotId.StableText}' could not be resolved."
                            : slotIssue);
                }

                // ADR-019: Actor selection is Session-scoped intent. Select is intentionally
                // idempotent for the same stable ActorProfile identity and rejects a different
                // existing selection without replacement or fallback.
                var selectionRequest = new PlayerActorSelectionRequest(
                    playerSlotId,
                    surface.ActorProfile,
                    resolvedSource,
                    $"{resolvedReason}:select:{index}",
                    slot.SelectionRevision);
                PlayerActorSelectionResult selection =
                    _module.TrySelectActorProfile(selectionRequest);
                if (selection == null || !selection.Succeeded)
                {
                    string releaseIssue = string.Empty;
                    TryReleaseAdmissionOnly(
                        surface,
                        admission.Token,
                        resolvedSource,
                        resolvedReason,
                        out releaseIssue);
                    string issue = selection != null
                        ? selection.ToDiagnosticString()
                        : $"Actor selection returned no result for Slot '{playerSlotId.StableText}'.";
                    if (!string.IsNullOrEmpty(releaseIssue))
                    {
                        issue = $"{issue} Contextual admission rollback failed. {releaseIssue}";
                    }

                    SceneLocalPlayerAdmissionActivityLifecycleStatus status =
                        slot.HasSelectedActor
                            ? SceneLocalPlayerAdmissionActivityLifecycleStatus.RejectedSelectionConflict
                            : SceneLocalPlayerAdmissionActivityLifecycleStatus.FailedSelection;
                    return FailEnterAndRollback(
                        activity,
                        owner,
                        entries,
                        resolvedSource,
                        resolvedReason,
                        status,
                        issue);
                }

                ScenePlayerActorAdoptionToken adoptionToken = default;
                bool adoptionApplied = false;
                if (requiresActorAdoption)
                {
                    bool hasSessionPhysicalAdoption =
                        _preparationModule.TryGetScenePlayerActorAdoption(
                            playerSlotId,
                            out _);
                    if (hasSessionPhysicalAdoption)
                    {
                        // The existing adoption is Session-owned physical state. This Activity
                        // contributes only its scene-local PlayerInput evidence; it must not
                        // attempt a second adoption for the same Slot.
                        surface.SceneLogicalPlayerActor.BindPlayerInputEvidence(
                            surface.LocalPlayerHost.PlayerInput);
                    }
                    else
                    {
                        var scopeContext = new RuntimeScopeContext(
                            owner,
                            resolvedSource,
                            $"{resolvedReason}:adopt:{index}");
                        ScenePlayerActorAdoptionResult adoption =
                            _preparationModule.TryAdoptSceneLocalPlayerActor(
                                scopeContext,
                                surface,
                                resolvedSource,
                                $"{resolvedReason}:adopt:{index}");
                        surface.SetActorAdoptionResult(adoption);
                        if (adoption == null || !adoption.Succeeded || !adoption.Token.IsValid)
                        {
                            string rollbackIssue = RollbackCurrentSelectionAndAdmission(
                                surface,
                                playerSlotId,
                                selection.SelectionRevision,
                                selection.StateChanged,
                                admission.Token,
                                resolvedSource,
                                resolvedReason);
                            string issue = adoption != null
                                ? adoption.ToDiagnosticString()
                                : $"Scene Actor adoption returned no result for Slot '{playerSlotId.StableText}'.";
                            if (!string.IsNullOrEmpty(rollbackIssue))
                            {
                                issue = $"{issue} Current-entry rollback failed. {rollbackIssue}";
                            }

                            return FailEnterAndRollback(
                                activity,
                                owner,
                                entries,
                                resolvedSource,
                                resolvedReason,
                                SceneLocalPlayerAdmissionActivityLifecycleStatus.FailedActorAdoption,
                                issue);
                        }

                        adoptionToken = adoption.Token;
                        adoptionApplied = true;
                    }
                }

                entries.Add(new Entry(
                    surface,
                    playerSlotId,
                    surface.ActorProfile,
                    admission.Token,
                    selection.SelectionRevision,
                    selection.StateChanged,
                    adoptionToken,
                    adoptionApplied));
            }

            _activeRecord = new ActiveRecord(activity, owner, entries);
            return Success(
                SceneLocalPlayerAdmissionActivityLifecycleStatus.SucceededEntered,
                activity,
                owner,
                resolvedSource,
                resolvedReason,
                entries.Count,
                requiresActorAdoption
                    ? $"Admitted or reprojected, resolved Session Actor selection and adopted required Scene Local Players before canonical Activity Player lifecycle execution. count='{entries.Count}'."
                    : $"Admitted or reprojected Scene Local Players and resolved Session Actor selection before canonical Activity Player lifecycle execution. count='{entries.Count}'.");
        }

        internal SceneLocalPlayerAdmissionActivityLifecycleResult TryExit(
            ActivityAsset activity,
            RuntimeContentOwner owner,
            string source,
            string reason)
        {
            string resolvedSource = Normalize(
                source,
                nameof(SceneLocalPlayerAdmissionActivityLifecycleRuntime));
            string resolvedReason = Normalize(
                reason,
                "scene-local-player-activity-exit");

            if (activity == null || !owner.IsValid)
            {
                return Failure(
                    SceneLocalPlayerAdmissionActivityLifecycleStatus.RejectedInvalidRequest,
                    activity,
                    owner,
                    resolvedSource,
                    resolvedReason,
                    "Scene Local Player Activity exit requires an Activity and valid Activity owner.");
            }

            if (_activeRecord == null)
            {
                return Success(
                    SceneLocalPlayerAdmissionActivityLifecycleStatus.SucceededAlreadyExited,
                    activity,
                    owner,
                    resolvedSource,
                    resolvedReason,
                    0,
                    "Scene Local Player Activity lifecycle is already exited.");
            }

            if (!ReferenceEquals(_activeRecord.Activity, activity) ||
                _activeRecord.Owner != owner)
            {
                return Failure(
                    SceneLocalPlayerAdmissionActivityLifecycleStatus.RejectedForeignOrStaleActivity,
                    activity,
                    owner,
                    resolvedSource,
                    resolvedReason,
                    $"Activity owner '{owner.StableText}' does not match retained Scene Local Player owner '{_activeRecord.Owner.StableText}'.");
            }

            if (!TryReleaseEntries(
                    _activeRecord.Entries,
                    _activeRecord.Owner,
                    compensateReleasedEntries: true,
                    resolvedSource,
                    resolvedReason,
                    out string issue))
            {
                return Failure(
                    SceneLocalPlayerAdmissionActivityLifecycleStatus.FailedExit,
                    activity,
                    owner,
                    resolvedSource,
                    resolvedReason,
                    issue,
                    _activeRecord.Entries.Count);
            }

            int releasedCount = _activeRecord.Entries.Count;
            _activeRecord = null;
            return Success(
                SceneLocalPlayerAdmissionActivityLifecycleStatus.SucceededExited,
                activity,
                owner,
                resolvedSource,
                resolvedReason,
                releasedCount,
                $"Released '{releasedCount}' Scene Local Player contextual representations after canonical Activity Player lifecycle exit while preserving Session membership and Actor selection intent.");
        }

        internal SceneLocalPlayerAdmissionActivityLifecycleResult TryRollbackEnter(
            ActivityAsset activity,
            RuntimeContentOwner owner,
            string source,
            string reason)
        {
            string resolvedSource = Normalize(
                source,
                nameof(SceneLocalPlayerAdmissionActivityLifecycleRuntime));
            string resolvedReason = Normalize(
                reason,
                "scene-local-player-activity-enter-rollback");
            if (_activeRecord == null)
            {
                return Success(
                    SceneLocalPlayerAdmissionActivityLifecycleStatus.SucceededAlreadyExited,
                    activity,
                    owner,
                    resolvedSource,
                    resolvedReason,
                    0,
                    "Scene Local Player enter rollback had no retained entries.");
            }

            if (!ReferenceEquals(_activeRecord.Activity, activity) ||
                _activeRecord.Owner != owner)
            {
                return Failure(
                    SceneLocalPlayerAdmissionActivityLifecycleStatus.RejectedForeignOrStaleActivity,
                    activity,
                    owner,
                    resolvedSource,
                    resolvedReason,
                    "Scene Local Player enter rollback rejected a foreign Activity owner.");
            }

            if (!TryReleaseEntries(
                    _activeRecord.Entries,
                    _activeRecord.Owner,
                    compensateReleasedEntries: false,
                    resolvedSource,
                    resolvedReason,
                    out string issue))
            {
                return Failure(
                    SceneLocalPlayerAdmissionActivityLifecycleStatus.FailedRollback,
                    activity,
                    owner,
                    resolvedSource,
                    resolvedReason,
                    issue,
                    _activeRecord.Entries.Count);
            }

            int rolledBackCount = _activeRecord.Entries.Count;
            _activeRecord = null;
            return Success(
                SceneLocalPlayerAdmissionActivityLifecycleStatus.SucceededRolledBack,
                activity,
                owner,
                resolvedSource,
                resolvedReason,
                rolledBackCount,
                $"Rolled back '{rolledBackCount}' Scene Local Player Activity contextual entries while retaining Session Player state.");
        }

        internal SceneLocalPlayerAdmissionActivityLifecycleResult
            TryRetireContextForSessionPlayerLeave(
                SessionPlayerLeaveToken leaveToken,
                string source,
                string reason)
        {
            string resolvedSource = Normalize(
                source,
                nameof(SceneLocalPlayerAdmissionActivityLifecycleRuntime));
            string resolvedReason = Normalize(
                reason,
                "scene-local-player-session-leave-context-retirement");

            if (!leaveToken.IsValid)
            {
                return Failure(
                    SceneLocalPlayerAdmissionActivityLifecycleStatus.RejectedInvalidRequest,
                    _activeRecord?.Activity,
                    _activeRecord != null ? _activeRecord.Owner : default,
                    resolvedSource,
                    resolvedReason,
                    "Scene Local Player Session Leave contextual retirement requires a valid Leave correlation token.");
            }

            if (_activeRecord == null)
            {
                return Success(
                    SceneLocalPlayerAdmissionActivityLifecycleStatus.SucceededAlreadyExited,
                    null,
                    default,
                    resolvedSource,
                    resolvedReason,
                    0,
                    "Scene Local Player contextual lifecycle has no active Activity owner for the Leaving Slot.");
            }

            var entries = new List<Entry>();
            for (int index = 0; index < _activeRecord.Entries.Count; index++)
            {
                Entry entry = _activeRecord.Entries[index];
                if (entry.AdmissionActive && entry.PlayerSlotId == leaveToken.PlayerSlotId)
                {
                    entries.Add(entry);
                }
            }

            if (entries.Count == 0)
            {
                return Success(
                    SceneLocalPlayerAdmissionActivityLifecycleStatus.SucceededAlreadyExited,
                    _activeRecord.Activity,
                    _activeRecord.Owner,
                    resolvedSource,
                    resolvedReason,
                    0,
                    "The current Scene Local Player Activity owner has no active contextual admission for the Leaving Slot.");
            }

            if (!TryReleaseEntriesForSessionPlayerLeave(
                    entries,
                    _activeRecord.Owner,
                    leaveToken,
                    resolvedSource,
                    resolvedReason,
                    out string issue))
            {
                return Failure(
                    SceneLocalPlayerAdmissionActivityLifecycleStatus.FailedExit,
                    _activeRecord.Activity,
                    _activeRecord.Owner,
                    resolvedSource,
                    resolvedReason,
                    issue,
                    entries.Count);
            }

            for (int index = _activeRecord.Entries.Count - 1; index >= 0; index--)
            {
                if (!_activeRecord.Entries[index].AdmissionActive)
                {
                    _activeRecord.Entries.RemoveAt(index);
                }
            }

            ActivityAsset retiredActivity = _activeRecord.Activity;
            RuntimeContentOwner retiredOwner = _activeRecord.Owner;
            if (_activeRecord.Entries.Count == 0)
            {
                _activeRecord = null;
            }

            return Success(
                SceneLocalPlayerAdmissionActivityLifecycleStatus.SucceededExited,
                retiredActivity,
                retiredOwner,
                resolvedSource,
                resolvedReason,
                entries.Count,
                "Scene Local Player Session Leave retired the exact current contextual admission and cleared its retained Activity owner when no entries remained.");
        }

        internal bool TryRetireAllContextForSessionTermination(
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            if (_activeRecord == null)
            {
                return true;
            }

            if (!TryReleaseEntriesForSessionTermination(
                    _activeRecord.Entries,
                    _activeRecord.Owner,
                    Normalize(source, nameof(SceneLocalPlayerAdmissionActivityLifecycleRuntime)),
                    Normalize(reason, "scene-local-player-session-termination-context-retirement"),
                    out issue))
            {
                return false;
            }

            _activeRecord = null;
            return true;
        }

        private SceneLocalPlayerAdmissionActivityLifecycleResult FailEnterAndRollback(
            ActivityAsset activity,
            RuntimeContentOwner owner,
            List<Entry> entries,
            string source,
            string reason,
            SceneLocalPlayerAdmissionActivityLifecycleStatus originalStatus,
            string issue)
        {
            if (entries.Count == 0)
            {
                return Failure(
                    originalStatus,
                    activity,
                    owner,
                    source,
                    reason,
                    issue);
            }

            _activeRecord = new ActiveRecord(activity, owner, entries);
            if (TryReleaseEntries(
                    entries,
                    owner,
                    compensateReleasedEntries: false,
                    source,
                    $"{reason}:rollback",
                    out string rollbackIssue))
            {
                _activeRecord = null;
                return Failure(
                    originalStatus,
                    activity,
                    owner,
                    source,
                    reason,
                    issue,
                    entries.Count);
            }

            return Failure(
                SceneLocalPlayerAdmissionActivityLifecycleStatus.FailedRollback,
                activity,
                owner,
                source,
                reason,
                $"{issue} Rollback failed. {rollbackIssue}",
                entries.Count,
                originalStatus);
        }

        private bool TryReleaseEntries(
            List<Entry> entries,
            RuntimeContentOwner owner,
            bool compensateReleasedEntries,
            string source,
            string reason,
            out string issue)
        {
            return TryReleaseEntriesCore(
                entries,
                owner,
                ContextualRetirementCause.ActivityExit,
                default,
                compensateReleasedEntries,
                source,
                reason,
                out issue);
        }

        private bool TryReleaseEntriesForSessionPlayerLeave(
            List<Entry> entries,
            RuntimeContentOwner owner,
            SessionPlayerLeaveToken leaveToken,
            string source,
            string reason,
            out string issue)
        {
            return TryReleaseEntriesCore(
                entries,
                owner,
                ContextualRetirementCause.SessionPlayerLeave,
                leaveToken,
                compensateReleasedEntries: false,
                source,
                reason,
                out issue);
        }

        private bool TryReleaseEntriesForSessionTermination(
            List<Entry> entries,
            RuntimeContentOwner owner,
            string source,
            string reason,
            out string issue)
        {
            return TryReleaseEntriesCore(
                entries,
                owner,
                ContextualRetirementCause.SessionTermination,
                default,
                compensateReleasedEntries: false,
                source,
                reason,
                out issue);
        }

        private bool TryReleaseEntriesCore(
            List<Entry> entries,
            RuntimeContentOwner owner,
            ContextualRetirementCause cause,
            SessionPlayerLeaveToken leaveToken,
            bool compensateReleasedEntries,
            string source,
            string reason,
            out string issue)
        {
            var released = new List<Entry>();
            var failures = new List<string>();

            for (int index = entries.Count - 1; index >= 0; index--)
            {
                Entry entry = entries[index];
                if (!entry.AdmissionActive)
                {
                    continue;
                }

                SceneLocalPlayerAdmissionRuntimeResult retirement =
                    ResolveEntryRetirement(
                        entry,
                        cause,
                        leaveToken,
                        source,
                        reason);
                if (retirement == null || !retirement.Succeeded)
                {
                    failures.Add(retirement != null
                        ? retirement.ToDiagnosticString()
                        : $"Scene contextual retirement returned no result for '{entry.Authoring.name}'.");
                    continue;
                }

                entry.AdmissionActive = false;
                released.Add(entry);
            }

            if (failures.Count == 0)
            {
                issue = string.Empty;
                return true;
            }

            if (compensateReleasedEntries && released.Count > 0)
            {
                if (!TryRestoreReleasedEntries(
                        released,
                        owner,
                        source,
                        reason,
                        out string compensationIssue))
                {
                    failures.Add($"Released-entry compensation failed. {compensationIssue}");
                }
            }

            issue = string.Join(" | ", failures);
            return false;
        }

        private SceneLocalPlayerAdmissionRuntimeResult ResolveEntryRetirement(
            Entry entry,
            ContextualRetirementCause cause,
            SessionPlayerLeaveToken leaveToken,
            string source,
            string reason)
        {
            if (entry.AdoptionApplied)
            {
                return cause switch
                {
                    ContextualRetirementCause.SessionPlayerLeave =>
                        _module.TryRetireContextualRepresentationForSessionPlayerLeave(
                            entry.Authoring,
                            entry.AdmissionToken,
                            leaveToken,
                            source,
                            $"{reason}:retire-contextual-admission"),
                    ContextualRetirementCause.SessionTermination =>
                        _module.TryRetireContextualRepresentationForSessionTermination(
                            entry.Authoring,
                            entry.AdmissionToken,
                            source,
                            $"{reason}:retire-contextual-admission"),
                    _ => _module.TryRetireContextualRepresentation(
                        entry.Authoring,
                        entry.AdmissionToken,
                        source,
                        $"{reason}:retire-contextual-admission")
                };
            }

            return cause switch
            {
                ContextualRetirementCause.SessionPlayerLeave =>
                    _module.TryReleaseForSessionPlayerLeave(
                        entry.Authoring,
                        entry.AdmissionToken,
                        leaveToken,
                        source,
                        $"{reason}:release-non-adopted-admission"),
                ContextualRetirementCause.SessionTermination =>
                    _module.TryReleaseForSessionTermination(
                        entry.Authoring,
                        entry.AdmissionToken,
                        source,
                        $"{reason}:release-non-adopted-admission"),
                _ => _module.TryRelease(
                    entry.Authoring,
                    entry.AdmissionToken,
                    source,
                    $"{reason}:release-non-adopted-admission")
            };
        }

        private bool TryRestoreReleasedEntries(
            List<Entry> released,
            RuntimeContentOwner owner,
            string source,
            string reason,
            out string issue)
        {
            var failures = new List<string>();
            for (int index = released.Count - 1; index >= 0; index--)
            {
                Entry entry = released[index];
                SceneLocalPlayerAdmissionRuntimeResult admission = _module.TryAdmit(
                    entry.Authoring,
                    owner,
                    source,
                    $"{reason}:compensate-admission:{index}");
                if (admission == null || !admission.Succeeded || !admission.Token.IsValid)
                {
                    failures.Add(admission != null
                        ? admission.ToDiagnosticString()
                        : $"Admission compensation returned no result for '{entry.Authoring.name}'.");
                    continue;
                }

                entry.AdmissionToken = admission.Token;
                entry.AdmissionActive = true;
                if (!TryConfirmSessionSelection(
                        entry,
                        source,
                        reason,
                        out string selectionIssue))
                {
                    failures.Add(selectionIssue);
                    continue;
                }

                if (entry.AdoptionToken.IsValid &&
                    !TryRestoreAdoption(
                        entry,
                        owner,
                        source,
                        reason,
                        out string adoptionIssue))
                {
                    failures.Add(adoptionIssue);
                }
            }

            issue = string.Join(" | ", failures);
            return failures.Count == 0;
        }

        private bool TryConfirmSessionSelection(
            Entry entry,
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            if (!_module.TryGetSlotSnapshot(
                    entry.PlayerSlotId,
                    out PlayerSlotRuntimeSnapshot slot))
            {
                issue = $"Selection compensation could not resolve Slot '{entry.PlayerSlotId.StableText}'.";
                return false;
            }

            if (!slot.HasSelectedActor)
            {
                issue = $"Session Actor selection disappeared while compensating Scene representation for Slot '{entry.PlayerSlotId.StableText}'. Activity compensation will not recreate Session intent implicitly.";
                return false;
            }

            var request = new PlayerActorSelectionRequest(
                entry.PlayerSlotId,
                entry.ActorProfile,
                source,
                $"{reason}:confirm-session-selection",
                slot.SelectionRevision);
            PlayerActorSelectionResult selection = _module.TrySelectActorProfile(request);
            if (selection == null || !selection.Succeeded)
            {
                issue = selection != null
                    ? selection.ToDiagnosticString()
                    : $"Session Actor selection confirmation returned no result for Slot '{entry.PlayerSlotId.StableText}'.";
                return false;
            }

            entry.SelectionRevision = selection.SelectionRevision;
            entry.SelectionApplied = false;
            return true;
        }

        private string RollbackCurrentSelectionAndAdmission(
            SceneLocalPlayerAdmissionAuthoring surface,
            PlayerSlotId playerSlotId,
            int selectionRevision,
            bool selectionApplied,
            SceneLocalPlayerAdmissionToken admissionToken,
            string source,
            string reason)
        {
            // ADR-019: once Join/selection succeeds it belongs to the Session Player lifetime.
            // Activity preparation failure retires only the contextual representation.
            _ = playerSlotId;
            _ = selectionRevision;
            _ = selectionApplied;

            return TryReleaseAdmissionOnly(
                surface,
                admissionToken,
                source,
                reason,
                out string admissionIssue)
                    ? string.Empty
                    : admissionIssue;
        }

        private bool TryRestoreAdoption(
            Entry entry,
            RuntimeContentOwner owner,
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            if (!entry.AdoptionToken.IsValid)
            {
                return true;
            }

            if (_preparationModule == null || !_preparationModule.IsReady)
            {
                issue = "Scene Actor adoption compensation requires the ready Player Actor preparation authority.";
                return false;
            }

            var scopeContext = new RuntimeScopeContext(
                owner,
                source,
                $"{reason}:compensate-adoption");
            ScenePlayerActorAdoptionResult adoption =
                _preparationModule.TryAdoptSceneLocalPlayerActor(
                    scopeContext,
                    entry.Authoring,
                    source,
                    $"{reason}:compensate-adoption");
            entry.Authoring.SetActorAdoptionResult(adoption);
            if (adoption == null || !adoption.Succeeded || !adoption.Token.IsValid)
            {
                issue = adoption != null
                    ? adoption.ToDiagnosticString()
                    : $"Scene Actor adoption compensation returned no result for Slot '{entry.PlayerSlotId.StableText}'.";
                return false;
            }

            entry.AdoptionToken = adoption.Token;
            entry.AdoptionApplied = true;
            return true;
        }

        private bool TryReleaseAdmissionOnly(
            SceneLocalPlayerAdmissionAuthoring authoring,
            SceneLocalPlayerAdmissionToken token,
            string source,
            string reason,
            out string issue)
        {
            SceneLocalPlayerAdmissionRuntimeResult release = _module.TryRelease(
                authoring,
                token,
                source,
                $"{reason}:admission-only-rollback");
            if (release != null && release.Succeeded)
            {
                issue = string.Empty;
                return true;
            }

            issue = release != null
                ? release.ToDiagnosticString()
                : $"Admission rollback returned no result for '{authoring.name}'.";
            return false;
        }

        private SceneLocalPlayerAdmissionActivityLifecycleResult Success(
            SceneLocalPlayerAdmissionActivityLifecycleStatus status,
            ActivityAsset activity,
            RuntimeContentOwner owner,
            string source,
            string reason,
            int affectedCount,
            string message)
        {
            _diagnostic = message ?? string.Empty;
            return new SceneLocalPlayerAdmissionActivityLifecycleResult(
                status,
                status,
                activity,
                owner,
                affectedCount,
                0,
                source,
                reason,
                _diagnostic);
        }

        private SceneLocalPlayerAdmissionActivityLifecycleResult Failure(
            SceneLocalPlayerAdmissionActivityLifecycleStatus status,
            ActivityAsset activity,
            RuntimeContentOwner owner,
            string source,
            string reason,
            string message,
            int affectedCount = 0,
            SceneLocalPlayerAdmissionActivityLifecycleStatus originalStatus =
                SceneLocalPlayerAdmissionActivityLifecycleStatus.None)
        {
            _diagnostic = message ?? string.Empty;
            return new SceneLocalPlayerAdmissionActivityLifecycleResult(
                status,
                originalStatus == SceneLocalPlayerAdmissionActivityLifecycleStatus.None
                    ? status
                    : originalStatus,
                activity,
                owner,
                affectedCount,
                1,
                source,
                reason,
                _diagnostic);
        }

        private static string Normalize(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }
}
