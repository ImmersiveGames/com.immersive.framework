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
    /// Activity-scoped transaction coordinator for Scene Local Player admission and Actor selection.
    /// It owns contextual admission/selection only; physical Host and Actor objects remain scene-owned.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3M4B2A/P3M4B2B Activity lifecycle coordination for Scene Local Player admission, selection and external Actor adoption.")]
    internal sealed class SceneLocalPlayerAdmissionActivityLifecycleRuntime
    {
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

        private readonly SceneLocalPlayerAdmissionRuntimeHostModule module;
        private readonly PlayerActorPreparationRuntimeHostModule preparationModule;
        private ActiveRecord activeRecord;
        private string diagnostic =
            "Scene Local Player Activity lifecycle has not executed.";

        internal SceneLocalPlayerAdmissionActivityLifecycleRuntime(
            SceneLocalPlayerAdmissionRuntimeHostModule module,
            PlayerActorPreparationRuntimeHostModule preparationModule = null)
        {
            this.module = module ?? throw new ArgumentNullException(nameof(module));
            this.preparationModule = preparationModule;
        }

        internal string Diagnostic => diagnostic;
        internal int ActiveEntryCount => activeRecord?.Entries.Count ?? 0;

        internal SceneLocalPlayerAdmissionActivityLifecycleResult TryEnter(
            ActivityAsset activity,
            RuntimeContentOwner owner,
            string source,
            string reason)
        {
            string resolvedSource = Normalize(source, nameof(SceneLocalPlayerAdmissionActivityLifecycleRuntime));
            string resolvedReason = Normalize(reason, "scene-local-player-activity-enter");

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

            if (activeRecord != null)
            {
                if (ReferenceEquals(activeRecord.Activity, activity) &&
                    activeRecord.Owner == owner)
                {
                    return Success(
                        SceneLocalPlayerAdmissionActivityLifecycleStatus.SucceededAlreadyEntered,
                        activity,
                        owner,
                        resolvedSource,
                        resolvedReason,
                        activeRecord.Entries.Count,
                        "Scene Local Player Activity lifecycle is already entered for the same owner.");
                }

                return Failure(
                    SceneLocalPlayerAdmissionActivityLifecycleStatus.RejectedForeignOrStaleActivity,
                    activity,
                    owner,
                    resolvedSource,
                    resolvedReason,
                    $"Activity owner '{owner.StableText}' cannot replace retained Scene Local Player owner '{activeRecord.Owner.StableText}' without exit.");
            }

            if (!module.TryResolveAutomaticActivityAuthoring(
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
                (preparationModule == null || !preparationModule.IsReady))
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
                SceneLocalPlayerAdmissionRuntimeResult admission = module.TryAdmit(
                    surface,
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
                    !module.TryGetSlotSnapshot(playerSlotId, out PlayerSlotRuntimeSnapshot slot))
                {
                    TryReleaseAdmissionOnly(surface, admission.Token, resolvedSource, resolvedReason, out _);
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

                if (slot.HasSelectedActor)
                {
                    TryReleaseAdmissionOnly(surface, admission.Token, resolvedSource, resolvedReason, out _);
                    return FailEnterAndRollback(
                        activity,
                        owner,
                        entries,
                        resolvedSource,
                        resolvedReason,
                        SceneLocalPlayerAdmissionActivityLifecycleStatus.RejectedSelectionConflict,
                        $"Scene Local Player Slot '{playerSlotId.StableText}' already has an Actor selection before Activity-owned admission. Ownership is ambiguous and no fallback was applied.");
                }

                var selectionRequest = new PlayerActorSelectionRequest(
                    playerSlotId,
                    surface.ActorProfile,
                    resolvedSource,
                    $"{resolvedReason}:select:{index}",
                    slot.SelectionRevision);
                PlayerActorSelectionResult selection =
                    module.TrySelectActorProfile(selectionRequest);
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
                        issue = $"{issue} Admission rollback failed. {releaseIssue}";
                    }

                    return FailEnterAndRollback(
                        activity,
                        owner,
                        entries,
                        resolvedSource,
                        resolvedReason,
                        SceneLocalPlayerAdmissionActivityLifecycleStatus.FailedSelection,
                        issue);
                }

                ScenePlayerActorAdoptionToken adoptionToken = default;
                bool adoptionApplied = false;
                if (requiresActorAdoption)
                {
                    var scopeContext = new RuntimeScopeContext(
                        owner,
                        resolvedSource,
                        $"{resolvedReason}:adopt:{index}");
                    ScenePlayerActorAdoptionResult adoption =
                        preparationModule.TryAdoptSceneLocalPlayerActor(
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

            activeRecord = new ActiveRecord(activity, owner, entries);
            return Success(
                SceneLocalPlayerAdmissionActivityLifecycleStatus.SucceededEntered,
                activity,
                owner,
                resolvedSource,
                resolvedReason,
                entries.Count,
                requiresActorAdoption
                    ? $"Admitted, selected and adopted required Scene Local Players before canonical Activity Player lifecycle execution. count='{entries.Count}'."
                    : $"Admitted and selected Scene Local Players before canonical Activity Player lifecycle execution. count='{entries.Count}'.");
        }

        internal SceneLocalPlayerAdmissionActivityLifecycleResult TryExit(
            ActivityAsset activity,
            RuntimeContentOwner owner,
            string source,
            string reason)
        {
            string resolvedSource = Normalize(source, nameof(SceneLocalPlayerAdmissionActivityLifecycleRuntime));
            string resolvedReason = Normalize(reason, "scene-local-player-activity-exit");

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

            if (activeRecord == null)
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

            if (!ReferenceEquals(activeRecord.Activity, activity) ||
                activeRecord.Owner != owner)
            {
                return Failure(
                    SceneLocalPlayerAdmissionActivityLifecycleStatus.RejectedForeignOrStaleActivity,
                    activity,
                    owner,
                    resolvedSource,
                    resolvedReason,
                    $"Activity owner '{owner.StableText}' does not match retained Scene Local Player owner '{activeRecord.Owner.StableText}'.");
            }

            if (!TryReleaseEntries(
                    activeRecord.Entries,
                    activeRecord.Owner,
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
                    activeRecord.Entries.Count);
            }

            int releasedCount = activeRecord.Entries.Count;
            activeRecord = null;
            return Success(
                SceneLocalPlayerAdmissionActivityLifecycleStatus.SucceededExited,
                activity,
                owner,
                resolvedSource,
                resolvedReason,
                releasedCount,
                $"Cleared Actor selection and released '{releasedCount}' Scene Local Player admissions after canonical Activity Player lifecycle exit.");
        }

        internal SceneLocalPlayerAdmissionActivityLifecycleResult TryRollbackEnter(
            ActivityAsset activity,
            RuntimeContentOwner owner,
            string source,
            string reason)
        {
            string resolvedSource = Normalize(source, nameof(SceneLocalPlayerAdmissionActivityLifecycleRuntime));
            string resolvedReason = Normalize(reason, "scene-local-player-activity-enter-rollback");
            if (activeRecord == null)
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

            if (!ReferenceEquals(activeRecord.Activity, activity) ||
                activeRecord.Owner != owner)
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
                    activeRecord.Entries,
                    activeRecord.Owner,
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
                    activeRecord.Entries.Count);
            }

            int rolledBackCount = activeRecord.Entries.Count;
            activeRecord = null;
            return Success(
                SceneLocalPlayerAdmissionActivityLifecycleStatus.SucceededRolledBack,
                activity,
                owner,
                resolvedSource,
                resolvedReason,
                rolledBackCount,
                $"Rolled back '{rolledBackCount}' Scene Local Player Activity enter entries.");
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

            activeRecord = new ActiveRecord(activity, owner, entries);
            if (TryReleaseEntries(
                    entries,
                    owner,
                    compensateReleasedEntries: false,
                    source,
                    $"{reason}:rollback",
                    out string rollbackIssue))
            {
                activeRecord = null;
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
            var released = new List<Entry>();
            var failures = new List<string>();

            for (int index = entries.Count - 1; index >= 0; index--)
            {
                Entry entry = entries[index];
                if (!entry.AdmissionActive)
                {
                    continue;
                }

                bool adoptionReleased = false;
                if (entry.AdoptionApplied)
                {
                    ScenePlayerActorAdoptionResult adoptionRelease =
                        preparationModule != null
                            ? preparationModule.TryReleaseSceneLocalPlayerActor(
                                entry.Authoring,
                                entry.AdoptionToken,
                                source,
                                $"{reason}:release-adoption:{index}")
                            : null;
                    entry.Authoring.SetActorAdoptionResult(adoptionRelease);
                    if (adoptionRelease == null || !adoptionRelease.Succeeded)
                    {
                        failures.Add(adoptionRelease != null
                            ? adoptionRelease.ToDiagnosticString()
                            : $"Scene Actor adoption release returned no result for Slot '{entry.PlayerSlotId.StableText}'.");
                        break;
                    }

                    entry.AdoptionApplied = false;
                    adoptionReleased = true;
                }

                bool selectionCleared = false;
                if (entry.SelectionApplied)
                {
                    var clearRequest = new PlayerActorSelectionRequest(
                        entry.PlayerSlotId,
                        null,
                        source,
                        $"{reason}:clear-selection:{index}",
                        entry.SelectionRevision);
                    PlayerActorSelectionResult clear =
                        module.TryClearActorSelection(clearRequest);
                    if (clear == null || !clear.Succeeded)
                    {
                        string clearIssue = clear != null
                            ? clear.ToDiagnosticString()
                            : $"Actor selection clear returned no result for Slot '{entry.PlayerSlotId.StableText}'.";
                        if (adoptionReleased &&
                            !TryRestoreAdoption(entry, owner, source, reason, out string adoptionRestoreIssue))
                        {
                            clearIssue = $"{clearIssue} Adoption compensation failed. {adoptionRestoreIssue}";
                        }

                        failures.Add(clearIssue);
                        break;
                    }

                    entry.SelectionRevision = clear.SelectionRevision;
                    entry.SelectionApplied = false;
                    selectionCleared = clear.StateChanged;
                }
                else if (module.TryGetSlotSnapshot(
                             entry.PlayerSlotId,
                             out PlayerSlotRuntimeSnapshot currentSlot) &&
                         currentSlot.HasSelectedActor)
                {
                    failures.Add(
                        $"Scene Local Player Slot '{entry.PlayerSlotId.StableText}' has a selection not owned by this Activity lifecycle; release was blocked.");
                    break;
                }

                SceneLocalPlayerAdmissionRuntimeResult release = module.TryRelease(
                    entry.Authoring,
                    entry.AdmissionToken,
                    source,
                    $"{reason}:release-admission:{index}");
                if (release == null || !release.Succeeded)
                {
                    string releaseIssue = release != null
                        ? release.ToDiagnosticString()
                        : $"Scene Local Player release returned no result for '{entry.Authoring.name}'.";
                    if (selectionCleared &&
                        !TryRestoreSelection(entry, source, reason, out string selectionRestoreIssue))
                    {
                        releaseIssue = $"{releaseIssue} Selection compensation failed. {selectionRestoreIssue}";
                    }
                    else if (adoptionReleased &&
                             !TryRestoreAdoption(entry, owner, source, reason, out string adoptionRestoreIssue))
                    {
                        releaseIssue = $"{releaseIssue} Adoption compensation failed. {adoptionRestoreIssue}";
                    }

                    failures.Add(releaseIssue);
                    break;
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
                if (!TryRestoreReleasedEntries(released, owner, source, reason, out string compensationIssue))
                {
                    failures.Add($"Released-entry compensation failed. {compensationIssue}");
                }
            }

            issue = string.Join(" | ", failures);
            return false;
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
                SceneLocalPlayerAdmissionRuntimeResult admission = module.TryAdmit(
                    entry.Authoring,
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
                if (!TryRestoreSelection(entry, source, reason, out string selectionIssue))
                {
                    failures.Add(selectionIssue);
                    continue;
                }

                if (entry.AdoptionToken.IsValid &&
                    !TryRestoreAdoption(entry, owner, source, reason, out string adoptionIssue))
                {
                    failures.Add(adoptionIssue);
                }
            }

            issue = string.Join(" | ", failures);
            return failures.Count == 0;
        }

        private bool TryRestoreSelection(
            Entry entry,
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            if (!module.TryGetSlotSnapshot(
                    entry.PlayerSlotId,
                    out PlayerSlotRuntimeSnapshot slot))
            {
                issue = $"Selection compensation could not resolve Slot '{entry.PlayerSlotId.StableText}'.";
                return false;
            }

            if (slot.HasSelectedActor)
            {
                if (slot.SelectedActorProfile == entry.ActorProfile)
                {
                    entry.SelectionRevision = slot.SelectionRevision;
                    entry.SelectionApplied = true;
                    return true;
                }

                issue = $"Selection compensation found a different ActorProfile on Slot '{entry.PlayerSlotId.StableText}'.";
                return false;
            }

            var request = new PlayerActorSelectionRequest(
                entry.PlayerSlotId,
                entry.ActorProfile,
                source,
                $"{reason}:compensate-selection",
                slot.SelectionRevision);
            PlayerActorSelectionResult selection = module.TrySelectActorProfile(request);
            if (selection == null || !selection.Succeeded)
            {
                issue = selection != null
                    ? selection.ToDiagnosticString()
                    : $"Selection compensation returned no result for Slot '{entry.PlayerSlotId.StableText}'.";
                return false;
            }

            entry.SelectionRevision = selection.SelectionRevision;
            entry.SelectionApplied = true;
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
            var failures = new List<string>();
            if (selectionApplied)
            {
                PlayerActorSelectionResult clear = module.TryClearActorSelection(
                    new PlayerActorSelectionRequest(
                        playerSlotId,
                        null,
                        source,
                        $"{reason}:current-entry-selection-rollback",
                        selectionRevision));
                if (clear == null || !clear.Succeeded)
                {
                    failures.Add(clear != null
                        ? clear.ToDiagnosticString()
                        : $"Current-entry Actor selection rollback returned no result for Slot '{playerSlotId.StableText}'.");
                }
            }

            if (!TryReleaseAdmissionOnly(
                    surface,
                    admissionToken,
                    source,
                    reason,
                    out string admissionIssue))
            {
                failures.Add(admissionIssue);
            }

            return string.Join(" | ", failures);
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

            if (preparationModule == null || !preparationModule.IsReady)
            {
                issue = "Scene Actor adoption compensation requires the ready Player Actor preparation authority.";
                return false;
            }

            var scopeContext = new RuntimeScopeContext(
                owner,
                source,
                $"{reason}:compensate-adoption");
            ScenePlayerActorAdoptionResult adoption =
                preparationModule.TryAdoptSceneLocalPlayerActor(
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
            SceneLocalPlayerAdmissionRuntimeResult release = module.TryRelease(
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
            diagnostic = message ?? string.Empty;
            return new SceneLocalPlayerAdmissionActivityLifecycleResult(
                status,
                status,
                activity,
                owner,
                affectedCount,
                0,
                source,
                reason,
                diagnostic);
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
            diagnostic = message ?? string.Empty;
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
                diagnostic);
        }

        private static string Normalize(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }
}
