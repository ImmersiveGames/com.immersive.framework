using System;
using System.Collections.Generic;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Unity.Cinemachine;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Immersive.Framework.Camera
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Session-owned lifetime for Session Camera occurrences and their membership state.")]
    internal sealed class SessionCameraOccurrence
    {
        private readonly Dictionary<PlayerOccurrenceId, SessionCameraMemberState> _members = new();
        private readonly CameraOutputFallbackCoverageOwnerId _subjectCoverageOwnerId;
        private bool _ownsSubjectFallbackCoverage;

        internal SessionCameraOccurrence(
            SessionCameraAssignment assignment,
            CameraDefinition definition,
            CameraOccurrenceIdentity identity,
            GameObject root,
            CameraRigComposer composer,
            CameraOutputAuthoring output)
        {
            Assignment = assignment;
            Definition = definition;
            Identity = identity;
            Root = root;
            Composer = composer;
            Output = output;
            _subjectCoverageOwnerId = new CameraOutputFallbackCoverageOwnerId(
                $"session-camera-subjects:{assignment.Id}:{identity.OutputId}");
        }

        internal SessionCameraAssignment Assignment { get; }
        internal CameraDefinition Definition { get; }
        internal CameraOccurrenceIdentity Identity { get; }
        internal GameObject Root { get; }
        internal CameraRigComposer Composer { get; }
        internal CameraOutputAuthoring Output { get; }
        internal string SubjectDiagnostic { get; private set; } = string.Empty;
        internal IReadOnlyList<SessionCameraMemberState> Members =>
            new List<SessionCameraMemberState>(_members.Values).AsReadOnly();

        internal bool ReconcileMember(
            PlayerOccurrenceId playerOccurrenceId,
            PlayerSlotId playerSlotId,
            CameraSubject subject)
        {
            bool isConfiguredMemberSlot = false;
            for (int index = 0; index < Assignment.MemberSlots.Count; index++)
            {
                if (Assignment.MemberSlots[index] == playerSlotId)
                {
                    isConfiguredMemberSlot = true;
                    break;
                }
            }

            if (!playerOccurrenceId.IsValid || !playerSlotId.IsValid ||
                !isConfiguredMemberSlot)
            {
                return false;
            }

            _members[playerOccurrenceId] = new SessionCameraMemberState(
                playerOccurrenceId,
                playerSlotId,
                subject.IsValid ? subject : default);
            ApplyResolvedSubjects();
            return true;
        }

        internal void RemoveMember(PlayerOccurrenceId playerOccurrenceId)
        {
            if (_members.Remove(playerOccurrenceId))
            {
                ApplyResolvedSubjects();
            }
        }

        internal void ReleaseSubjectFallbackCoverage()
        {
            if (!_ownsSubjectFallbackCoverage || Output?.Session == null)
            {
                return;
            }

            CameraOutputApplyResult result =
                Output.Session.ReleaseFallbackCoverage(_subjectCoverageOwnerId);
            if (result.Succeeded)
            {
                _ownsSubjectFallbackCoverage = false;
                SubjectDiagnostic = string.Empty;
            }
            else
            {
                SubjectDiagnostic = result.DiagnosticSummary;
            }
        }

        private void ApplyResolvedSubjects()
        {
            if (Assignment.TargetPolicy != CameraTargetPolicy.MemberActorTargets ||
                Composer == null || Composer.CinemachineCamera == null)
            {
                return;
            }

            CameraSubject resolvedSubject = default;
            int subjectCount = 0;
            foreach (SessionCameraMemberState member in _members.Values)
            {
                if (!member.HasSubject)
                {
                    continue;
                }
                resolvedSubject = member.Subject;
                subjectCount++;
            }

            // Multi-subject group projection is a later cut. Never choose an arbitrary member.
            if (subjectCount != 1)
            {
                Composer.CinemachineCamera.Follow = null;
                Composer.CinemachineCamera.LookAt = null;
                bool targetRequired =
                    Composer.EffectiveFollowRequirement == CameraTargetRequirement.Required ||
                    Composer.EffectiveLookAtRequirement == CameraTargetRequirement.Required;
                if (targetRequired)
                {
                    CoverForMissingRequiredSubject();
                }
                else
                {
                    ReleaseSubjectFallbackCoverage();
                }
                return;
            }

            Composer.CinemachineCamera.Follow =
                Composer.EffectiveFollowRequirement == CameraTargetRequirement.NotUsed
                    ? null
                    : resolvedSubject.Observation;
            Composer.CinemachineCamera.LookAt =
                Composer.EffectiveLookAtRequirement == CameraTargetRequirement.NotUsed
                    ? null
                    : resolvedSubject.Observation;
            ReleaseSubjectFallbackCoverage();
        }

        private void CoverForMissingRequiredSubject()
        {
            if (_ownsSubjectFallbackCoverage)
            {
                return;
            }
            if (Output?.Session == null)
            {
                SubjectDiagnostic = "Required member Actor Subject is unavailable and the Session Output cannot apply Fallback coverage.";
                return;
            }

            CameraOutputApplyResult result =
                Output.Session.CoverWithFallback(_subjectCoverageOwnerId);
            _ownsSubjectFallbackCoverage = result.Succeeded;
            SubjectDiagnostic = result.Succeeded ? string.Empty : result.DiagnosticSummary;
        }
    }

    internal readonly struct SessionCameraMemberState
    {
        internal SessionCameraMemberState(
            PlayerOccurrenceId playerOccurrenceId,
            PlayerSlotId playerSlotId,
            CameraSubject subject)
        {
            PlayerOccurrenceId = playerOccurrenceId;
            PlayerSlotId = playerSlotId;
            Subject = subject;
        }

        internal PlayerOccurrenceId PlayerOccurrenceId { get; }
        internal PlayerSlotId PlayerSlotId { get; }
        internal CameraSubject Subject { get; }
        internal bool HasSubject => Subject.IsValid;
    }

    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Session-scoped Session Camera Assignment materialization, membership and teardown.")]
    internal sealed class SessionCameraAssignmentRuntime : IDisposable
    {
        private readonly SessionCameraOccurrence[] _occurrences;
        private bool _disposed;

        private SessionCameraAssignmentRuntime(SessionCameraOccurrence[] occurrences)
        {
            _occurrences = occurrences ?? Array.Empty<SessionCameraOccurrence>();
        }

        internal IReadOnlyList<SessionCameraOccurrence> Occurrences => Array.AsReadOnly(_occurrences);
        internal bool RequiresPlayerMembership
        {
            get
            {
                for (int index = 0; index < _occurrences.Length; index++)
                {
                    if (_occurrences[index].Assignment.MembershipPolicy ==
                            CameraMembershipPolicy.ExplicitPlayerSlots &&
                        _occurrences[index].Assignment.MemberSlots.Count > 0)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        internal static bool TryCreate(
            IReadOnlyList<SessionCameraAssignmentAuthoring> authoredAssignments,
            CameraOutputSessionTopology outputs,
            Transform sessionParent,
            out SessionCameraAssignmentRuntime runtime,
            out string issue)
        {
            runtime = null;
            if (outputs == null || sessionParent == null)
            {
                issue = "Session Camera Assignment startup requires the exact Output topology and Session lifetime parent.";
                return false;
            }

            authoredAssignments ??= Array.Empty<SessionCameraAssignmentAuthoring>();
            if (authoredAssignments.Count == 0)
            {
                runtime = new SessionCameraAssignmentRuntime(Array.Empty<SessionCameraOccurrence>());
                issue = string.Empty;
                return true;
            }

            var assignments = new List<SessionCameraAssignment>(authoredAssignments.Count);
            var definitionsById = new Dictionary<CameraDefinitionId, CameraDefinition>();
            var assignmentIds = new HashSet<SessionCameraAssignmentId>();
            var usedOutputIds = new HashSet<CameraOutputId>();
            int occurrenceCount = 0;

            for (int index = 0; index < authoredAssignments.Count; index++)
            {
                SessionCameraAssignmentAuthoring authored = authoredAssignments[index];
                if (authored == null)
                {
                    issue = $"Session Camera Assignments[{index}] is missing.";
                    return false;
                }
                if (!authored.TryBuild(out SessionCameraAssignment assignment, out issue))
                {
                    issue = $"Session Camera Assignments[{index}] is invalid. {issue}";
                    return false;
                }

                if ((assignment.OccurrenceMode != CameraOccurrenceMode.SessionScoped &&
                     assignment.OccurrenceMode != CameraOccurrenceMode.SharedGroup) ||
                    (assignment.MembershipPolicy != CameraMembershipPolicy.None &&
                     assignment.MembershipPolicy != CameraMembershipPolicy.ExplicitPlayerSlots) ||
                    (assignment.TargetPolicy != CameraTargetPolicy.NoSubject &&
                     assignment.TargetPolicy != CameraTargetPolicy.MemberActorTargets))
                {
                    issue = $"Session Camera Assignment '{assignment.Id}' is outside this cut's Session/Shared membership and target contract.";
                    return false;
                }

                if (!assignmentIds.Add(assignment.Id))
                {
                    issue = $"Session Camera Assignment identity '{assignment.Id}' is duplicated.";
                    return false;
                }

                CameraDefinition definition = authored.Definition;
                if (!definition.TryValidateSessionCamera(assignment.TargetPolicy, out issue))
                {
                    issue = $"Camera Definition '{definition.name}' is invalid for a Session camera. {issue}";
                    return false;
                }

                CameraDefinitionId definitionId = definition.DefinitionId;
                if (definitionsById.TryGetValue(definitionId, out CameraDefinition previousDefinition) &&
                    !ReferenceEquals(previousDefinition, definition))
                {
                    issue = $"Camera Definition identity collision '{definitionId}' references different assets.";
                    return false;
                }
                definitionsById[definitionId] = definition;

                IReadOnlyList<CameraOutputDefinition> mappings = authored.OutputDefinitions;
                for (int mappingIndex = 0; mappingIndex < mappings.Count; mappingIndex++)
                {
                    CameraOutputDefinition outputDefinition = mappings[mappingIndex];
                    if (!outputs.TryGetOutput(outputDefinition.OutputId, out CameraOutputAuthoring output, out issue) ||
                        !ReferenceEquals(output.OutputDefinition, outputDefinition))
                    {
                        issue = $"Session Camera Assignment '{assignment.Id}' maps an Output that is not the exact configured Output definition. {issue}";
                        return false;
                    }

                    if (output.Session == null || output.Session.OutputState.HasActiveAssignment)
                    {
                        issue = $"Camera Output '{outputDefinition.OutputId}' already has an active normal Assignment.";
                        return false;
                    }

                    if (!usedOutputIds.Add(outputDefinition.OutputId))
                    {
                        issue = $"Output '{outputDefinition.OutputId}' is mapped by more than one active Session Camera Assignment.";
                        return false;
                    }

                    occurrenceCount++;
                }

                assignments.Add(assignment);
            }

            var candidates = new List<SessionCameraOccurrence>(occurrenceCount);
            GameObject stagingRoot = null;
            try
            {
                stagingRoot = new GameObject("[Session Camera Assignment] Staging");
                stagingRoot.SetActive(false);
                stagingRoot.transform.SetParent(sessionParent, false);

                for (int index = 0; index < authoredAssignments.Count; index++)
                {
                    SessionCameraAssignmentAuthoring authored = authoredAssignments[index];
                    SessionCameraAssignment assignment = assignments[index];
                    CameraDefinition definition = authored.Definition;
                    IReadOnlyList<CameraOutputDefinition> mappings = authored.OutputDefinitions;
                    for (int mappingIndex = 0; mappingIndex < mappings.Count; mappingIndex++)
                    {
                        CameraOutputDefinition outputDefinition = mappings[mappingIndex];
                        outputs.TryGetOutput(outputDefinition.OutputId, out CameraOutputAuthoring output, out _);

                        GameObject instance = Object.Instantiate(
                            definition.RigPrefab,
                            stagingRoot.transform,
                            false);
                        if (instance == null)
                        {
                            issue = $"Camera Definition '{definition.name}' Rig Prefab instantiation returned null.";
                            DestroyOccurrences(candidates);
                            return false;
                        }
                        instance.SetActive(false);
                        instance.name = $"Camera Occurrence [{assignment.Id}/{outputDefinition.OutputId}]";

                        CameraRigComposer[] composers = instance.GetComponentsInChildren<CameraRigComposer>(true);
                        if (composers.Length != 1 || composers[0] == null)
                        {
                            issue = $"Materialized Camera Occurrence '{instance.name}' must contain exactly one CameraRigComposer.";
                            DestroyObject(instance);
                            DestroyOccurrences(candidates);
                            return false;
                        }

                        CameraRigComposer composer = composers[0];
                        if (!composer.TryValidateForApply(out issue) || composer.CinemachineCamera == null)
                        {
                            issue = $"Materialized Camera Occurrence '{instance.name}' is invalid. {issue}";
                            DestroyObject(instance);
                            DestroyOccurrences(candidates);
                            return false;
                        }

                        OutputChannels outputChannel = output.CinemachineBrain.ChannelMask;
                        int channelMask = (int)outputChannel;
                        if (channelMask <= 0 || (channelMask & (channelMask - 1)) != 0)
                        {
                            issue = $"Camera Output '{outputDefinition.OutputId}' must have one isolated Cinemachine channel before its Session occurrence is materialized.";
                            DestroyObject(instance);
                            DestroyOccurrences(candidates);
                            return false;
                        }
                        composer.CinemachineCamera.OutputChannel = outputChannel;
                        composer.CinemachineCamera.enabled = false;

                        CameraOccurrenceIdentity identity = CameraOccurrenceIdentity.ForSessionOrShared(
                            assignment.Id,
                            outputDefinition.OutputId);
                        candidates.Add(new SessionCameraOccurrence(
                            assignment,
                            definition,
                            identity,
                            instance,
                            composer,
                            output));
                    }
                }

                for (int index = 0; index < candidates.Count; index++)
                {
                    SessionCameraOccurrence occurrence = candidates[index];
                    occurrence.Root.transform.SetParent(sessionParent, false);
                    occurrence.Root.SetActive(true);

                    if (!occurrence.Output.Session.TrySetActiveAssignment(occurrence.Assignment, out issue))
                    {
                        issue = $"Session Camera Assignment '{occurrence.Assignment.Id}' could not become active on Output '{occurrence.Identity.OutputId}'. {issue}";
                        Rollback(candidates);
                        DestroyOccurrences(candidates);
                        return false;
                    }

                    CameraOccurrenceOutputResult applyResult = occurrence.Output.Session.PresentNormalOccurrence(
                        occurrence.Identity,
                        occurrence.Composer);
                    if (!applyResult.Succeeded)
                    {
                        issue = $"Session Camera Occurrence '{occurrence.Identity}' could not be applied. {applyResult.Diagnostic}";
                        Rollback(candidates);
                        DestroyOccurrences(candidates);
                        return false;
                    }
                }

                DestroyObject(stagingRoot);
                stagingRoot = null;
                runtime = new SessionCameraAssignmentRuntime(candidates.ToArray());
                issue = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                Rollback(candidates);
                DestroyOccurrences(candidates);
                issue = $"Session Camera Assignment startup failed. exception='{exception.GetType().Name}' message='{exception.Message}'.";
                return false;
            }
            finally
            {
                if (stagingRoot != null) DestroyObject(stagingRoot);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            for (int index = _occurrences.Length - 1; index >= 0; index--)
            {
                SessionCameraOccurrence occurrence = _occurrences[index];
                if (occurrence.Output != null && occurrence.Output.Session != null)
                {
                    occurrence.ReleaseSubjectFallbackCoverage();
                    occurrence.Output.Session.ResetSessionAssignmentToFallback(out _);
                }
                DestroyObject(occurrence.Root);
            }
        }

        private static void Rollback(IReadOnlyList<SessionCameraOccurrence> candidates)
        {
            for (int index = candidates.Count - 1; index >= 0; index--)
            {
                SessionCameraOccurrence occurrence = candidates[index];
                CameraOutputState state = occurrence.Output.Session.OutputState;
                if (state.HasActiveAssignment &&
                    state.ActiveAssignmentId == occurrence.Assignment.Id)
                {
                    occurrence.Output.Session.ResetSessionAssignmentToFallback(out _);
                }
            }
        }

        private static void DestroyOccurrences(IReadOnlyList<SessionCameraOccurrence> occurrences)
        {
            for (int index = occurrences.Count - 1; index >= 0; index--)
            {
                DestroyObject(occurrences[index].Root);
            }
        }

        private static void DestroyObject(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Object.Destroy(value);
            else Object.DestroyImmediate(value);
        }
    }

    /// <summary>Reconciles current Player membership and Actor Subjects without owning occurrence lifetime.</summary>
    internal sealed class SessionCameraMembershipRuntime : IDisposable
    {
        private readonly SessionCameraAssignmentRuntime _assignments;
        private readonly PlayerParticipationRuntimeContext _players;
        private readonly IPlayerPreparedActorOccurrenceSource _actors;
        private bool _disposed;

        internal static bool TryCreate(
            SessionCameraAssignmentRuntime assignments,
            PlayerParticipationRuntimeContext players,
            IPlayerPreparedActorOccurrenceSource actors,
            out SessionCameraMembershipRuntime runtime,
            out string diagnostic)
        {
            runtime = null;
            if (assignments == null || players == null || actors == null)
            {
                diagnostic = "Session Camera membership requires Assignment runtime, Player Session and prepared Actor evidence.";
                return false;
            }

            PlayerParticipationSnapshot snapshot = players.CreateSnapshot();
            if (snapshot == null || !snapshot.IsInitialized)
            {
                diagnostic = "Session Camera membership requires an initialized Player Session snapshot.";
                return false;
            }

            for (int occurrenceIndex = 0;
                 occurrenceIndex < assignments.Occurrences.Count;
                 occurrenceIndex++)
            {
                SessionCameraAssignment assignment =
                    assignments.Occurrences[occurrenceIndex].Assignment;
                for (int memberIndex = 0;
                     memberIndex < assignment.MemberSlots.Count;
                     memberIndex++)
                {
                    bool found = false;
                    for (int slotIndex = 0;
                         slotIndex < snapshot.Slots.Count;
                         slotIndex++)
                    {
                        if (snapshot.Slots[slotIndex].PlayerSlotId ==
                            assignment.MemberSlots[memberIndex])
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        diagnostic = $"Session Camera Assignment '{assignment.Id}' member Slot '{assignment.MemberSlots[memberIndex]}' is not configured by this Player Session.";
                        return false;
                    }
                }
            }

            runtime = new SessionCameraMembershipRuntime(assignments, players, actors);
            diagnostic = runtime.Diagnostic;
            return true;
        }

        internal SessionCameraMembershipRuntime(
            SessionCameraAssignmentRuntime assignments,
            PlayerParticipationRuntimeContext players,
            IPlayerPreparedActorOccurrenceSource actors)
        {
            _assignments = assignments ?? throw new ArgumentNullException(nameof(assignments));
            _players = players ?? throw new ArgumentNullException(nameof(players));
            _actors = actors ?? throw new ArgumentNullException(nameof(actors));
            _players.Changed += OnPlayerChanged;
            _actors.CurrentActorInvalidated += OnActorInvalidated;
            ReconcileAll();
        }

        internal bool LastReconciliationSucceeded { get; private set; } = true;
        internal string Diagnostic { get; private set; } = string.Empty;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            _players.Changed -= OnPlayerChanged;
            _actors.CurrentActorInvalidated -= OnActorInvalidated;
        }

        private void OnPlayerChanged(PlayerSessionChange change)
        {
            if (_disposed || change == null ||
                (change.Kind != PlayerSessionChangeKind.SlotAllocationChanged &&
                 change.Kind != PlayerSessionChangeKind.ActorSelectionChanged))
            {
                return;
            }

            try
            {
                ReconcileSlot(change.PlayerSlotId, change.PreviousSlot);
            }
            catch (Exception exception)
            {
                RecordFailure(change.PlayerSlotId, exception);
            }
        }

        private void OnActorInvalidated(PlayerSlotId playerSlotId)
        {
            if (!_disposed)
            {
                try
                {
                    ReconcileSlot(playerSlotId, default);
                }
                catch (Exception exception)
                {
                    RecordFailure(playerSlotId, exception);
                }
            }
        }

        private void ReconcileAll()
        {
            PlayerParticipationSnapshot snapshot = _players.CreateSnapshot();
            for (int index = 0; index < snapshot.Slots.Count; index++)
            {
                PlayerSlotRuntimeSnapshot slot = snapshot.Slots[index];
                ReconcileSlot(slot.PlayerSlotId, default);
            }
        }

        private void ReconcileSlot(
            PlayerSlotId playerSlotId,
            PlayerSlotRuntimeSnapshot previousSlot)
        {
            if (!_players.TryGetSlotSnapshot(playerSlotId, out PlayerSlotRuntimeSnapshot currentSlot))
            {
                return;
            }

            PlayerOccurrenceId previousPlayerId = previousSlot.PlayerOccurrenceId;
            if (previousPlayerId.IsValid &&
                (!currentSlot.IsJoined || currentSlot.PlayerOccurrenceId != previousPlayerId))
            {
                RemoveMember(previousPlayerId);
            }

            if (!currentSlot.IsJoined || !currentSlot.PlayerOccurrenceId.IsValid)
            {
                LastReconciliationSucceeded = true;
                Diagnostic = string.Empty;
                return;
            }

            bool succeeded = true;
            string diagnostic = string.Empty;
            for (int index = 0; index < _assignments.Occurrences.Count; index++)
            {
                SessionCameraOccurrence occurrence = _assignments.Occurrences[index];
                if (occurrence.Assignment.MembershipPolicy != CameraMembershipPolicy.ExplicitPlayerSlots ||
                    !IsConfiguredMember(occurrence.Assignment, playerSlotId))
                {
                    continue;
                }

                CameraSubject subject = default;
                if (occurrence.Assignment.TargetPolicy == CameraTargetPolicy.MemberActorTargets &&
                    _actors.TryGetCurrentActorOccurrence(playerSlotId, out PlayerPreparedActorOccurrence actor))
                {
                    if (!TryResolveSubject(actor, out subject, out string issue))
                    {
                        succeeded = false;
                        diagnostic = issue;
                    }
                }

                if (!occurrence.ReconcileMember(
                        currentSlot.PlayerOccurrenceId,
                        playerSlotId,
                        subject))
                {
                    succeeded = false;
                    diagnostic = $"Session Camera Assignment '{occurrence.Assignment.Id}' rejected current Player membership '{currentSlot.PlayerOccurrenceId}'.";
                }
                else if (!string.IsNullOrEmpty(occurrence.SubjectDiagnostic))
                {
                    succeeded = false;
                    diagnostic = occurrence.SubjectDiagnostic;
                }
            }

            LastReconciliationSucceeded = succeeded;
            Diagnostic = diagnostic;
        }

        private void RemoveMember(PlayerOccurrenceId playerOccurrenceId)
        {
            for (int index = 0; index < _assignments.Occurrences.Count; index++)
            {
                _assignments.Occurrences[index].RemoveMember(playerOccurrenceId);
            }
        }

        private void RecordFailure(PlayerSlotId playerSlotId, Exception exception)
        {
            LastReconciliationSucceeded = false;
            Diagnostic = $"Session Camera membership reconciliation failed for '{playerSlotId.StableText}'. {exception.GetType().Name}: {exception.Message}";
        }

        private static bool IsConfiguredMember(
            SessionCameraAssignment assignment,
            PlayerSlotId playerSlotId)
        {
            for (int index = 0; index < assignment.MemberSlots.Count; index++)
            {
                if (assignment.MemberSlots[index] == playerSlotId)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool TryResolveSubject(
            PlayerPreparedActorOccurrence actor,
            out CameraSubject subject,
            out string issue)
        {
            subject = default;
            issue = string.Empty;
            if (!actor.IsValid)
            {
                issue = "Member Actor Subject resolution requires exact current prepared Actor evidence.";
                return false;
            }

            Transform observation = actor.ActorDeclaration.transform;
            float framingRadius = 0f;
            ActorCameraSubjectAuthoring[] authorings =
                actor.Presentation.GetComponentsInChildren<ActorCameraSubjectAuthoring>(true);
            if (authorings.Length > 1)
            {
                issue = $"Current Player Actor Presentation contains multiple Camera Subject declarations ('{authorings.Length}').";
                return false;
            }
            if (authorings.Length == 1 &&
                !authorings[0].TryResolveSubject(
                    actor.Presentation.transform,
                    out observation,
                    out framingRadius,
                    out issue))
            {
                issue = "Current Player Actor Camera Subject is invalid. " + issue;
                return false;
            }

            subject = new CameraSubject(
                new CameraSubjectId(
                    $"camera.subject.player-actor:{actor.PreparationToken.StableText}"),
                observation,
                $"Current Session Player Actor for {actor.PlayerSlotId.StableText}",
                framingRadius);
            if (!subject.IsValid)
            {
                issue = "Current Player Actor did not resolve a valid Camera Subject.";
                return false;
            }
            return true;
        }
    }
}
