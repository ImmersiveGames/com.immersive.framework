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
        internal bool IsReadyForOutput
        {
            get
            {
                if (Assignment.TargetPolicy != CameraTargetPolicy.MemberActorTargets)
                {
                    return true;
                }

                bool targetRequired =
                    Composer?.EffectiveFollowRequirement == CameraTargetRequirement.Required ||
                    Composer?.EffectiveLookAtRequirement == CameraTargetRequirement.Required;
                return !targetRequired || ResolvedSubjects.Count == 1;
            }
        }
        internal IReadOnlyList<SessionCameraMemberState> Members =>
            new List<SessionCameraMemberState>(_members.Values).AsReadOnly();
        internal IReadOnlyList<SessionCameraMemberState> ResolvedSubjects
        {
            get
            {
                var subjects = new List<SessionCameraMemberState>();
                foreach (SessionCameraMemberState member in _members.Values)
                {
                    if (member.HasSubject)
                    {
                        subjects.Add(member);
                    }
                }
                return subjects.AsReadOnly();
            }
        }

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

            IReadOnlyList<SessionCameraMemberState> resolvedSubjects = ResolvedSubjects;

            // Group framing is a later cut. Keep all evidence and never choose an arbitrary member.
            if (resolvedSubjects.Count != 1)
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

            CameraSubject resolvedSubject = resolvedSubjects[0].Subject;
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
        private sealed class IndividualAssignmentRuntime
        {
            internal IndividualAssignmentRuntime(
                SessionCameraAssignment assignment,
                CameraDefinition definition,
                Dictionary<PlayerSlotId, CameraOutputAuthoring> outputsBySlot)
            {
                Assignment = assignment;
                Definition = definition;
                OutputsBySlot = outputsBySlot;
            }

            internal SessionCameraAssignment Assignment { get; }
            internal CameraDefinition Definition { get; }
            internal Dictionary<PlayerSlotId, CameraOutputAuthoring> OutputsBySlot { get; }
        }

        private readonly List<SessionCameraOccurrence> _occurrences;
        private readonly List<CameraOutputAuthoring> _activeOutputs;
        private readonly List<IndividualAssignmentRuntime> _individualAssignments;
        private readonly Transform _sessionParent;
        private bool _disposed;

        private SessionCameraAssignmentRuntime(
            IReadOnlyList<SessionCameraOccurrence> occurrences,
            IReadOnlyList<CameraOutputAuthoring> activeOutputs,
            IReadOnlyList<IndividualAssignmentRuntime> individualAssignments,
            Transform sessionParent)
        {
            _occurrences = occurrences != null
                ? new List<SessionCameraOccurrence>(occurrences)
                : new List<SessionCameraOccurrence>();
            _activeOutputs = activeOutputs != null
                ? new List<CameraOutputAuthoring>(activeOutputs)
                : new List<CameraOutputAuthoring>();
            _individualAssignments = individualAssignments != null
                ? new List<IndividualAssignmentRuntime>(individualAssignments)
                : new List<IndividualAssignmentRuntime>();
            _sessionParent = sessionParent;
        }

        internal IReadOnlyList<SessionCameraOccurrence> Occurrences => _occurrences.AsReadOnly();
        internal IReadOnlyList<SessionCameraAssignment> MembershipAssignments
        {
            get
            {
                var assignments = new List<SessionCameraAssignment>();
                var seen = new HashSet<SessionCameraAssignmentId>();
                for (int index = 0; index < _occurrences.Count; index++)
                {
                    SessionCameraAssignment assignment = _occurrences[index].Assignment;
                    if (assignment.MembershipPolicy == CameraMembershipPolicy.ExplicitPlayerSlots &&
                        seen.Add(assignment.Id))
                    {
                        assignments.Add(assignment);
                    }
                }
                for (int index = 0; index < _individualAssignments.Count; index++)
                {
                    SessionCameraAssignment assignment = _individualAssignments[index].Assignment;
                    if (seen.Add(assignment.Id))
                    {
                        assignments.Add(assignment);
                    }
                }
                return assignments.AsReadOnly();
            }
        }
        internal bool RequiresPlayerMembership
        {
            get
            {
                if (_individualAssignments.Count > 0)
                {
                    return true;
                }

                for (int index = 0; index < _occurrences.Count; index++)
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

        internal bool ReconcilePlayerOccurrence(
            PlayerOccurrenceId playerOccurrenceId,
            PlayerSlotId playerSlotId,
            CameraSubject subject,
            out string issue)
        {
            issue = string.Empty;
            if (_disposed || !playerOccurrenceId.IsValid || !playerSlotId.IsValid)
            {
                issue = "Individual Camera membership requires an active Assignment runtime and exact current Player occurrence and Slot identities.";
                return false;
            }

            var candidates = new List<SessionCameraOccurrence>();
            var presentedCandidates = new List<SessionCameraOccurrence>();
            GameObject stagingRoot = null;
            try
            {
                for (int index = 0; index < _individualAssignments.Count; index++)
                {
                    IndividualAssignmentRuntime individual = _individualAssignments[index];
                    if (!individual.OutputsBySlot.TryGetValue(
                            playerSlotId,
                            out CameraOutputAuthoring output))
                    {
                        continue;
                    }

                    CameraOccurrenceIdentity identity = CameraOccurrenceIdentity.ForIndividual(
                        individual.Assignment.Id,
                        playerOccurrenceId,
                        output.OutputDefinition.OutputId);
                    if (FindOccurrence(identity) != null)
                    {
                        continue;
                    }

                    if (stagingRoot == null)
                    {
                        stagingRoot = new GameObject("[Individual Camera Occurrence] Staging");
                        stagingRoot.SetActive(false);
                        stagingRoot.transform.SetParent(_sessionParent, false);
                    }

                    if (!TryMaterializeIndividualOccurrence(
                            individual,
                            output,
                            identity,
                            stagingRoot.transform,
                            out SessionCameraOccurrence candidate,
                            out issue))
                    {
                        DestroyOccurrences(candidates);
                        return false;
                    }
                    candidates.Add(candidate);
                }

                for (int index = 0; index < candidates.Count; index++)
                {
                    SessionCameraOccurrence candidate = candidates[index];
                    if (!candidate.ReconcileMember(
                            playerOccurrenceId,
                            playerSlotId,
                            subject))
                    {
                        issue = $"Individual Camera Occurrence '{candidate.Identity}' rejected its exact Player membership.";
                        ReleaseCandidateCoverage(candidates);
                        DestroyOccurrences(candidates);
                        return false;
                    }
                }

                for (int index = 0; index < candidates.Count; index++)
                {
                    SessionCameraOccurrence candidate = candidates[index];
                    candidate.Root.transform.SetParent(_sessionParent, false);
                    candidate.Root.SetActive(true);
                    if (!candidate.IsReadyForOutput)
                    {
                        continue;
                    }

                    CameraOccurrenceOutputResult presentation =
                        candidate.Output.Session.PresentNormalOccurrence(
                            candidate.Identity,
                            candidate.Composer);
                    if (!presentation.Succeeded)
                    {
                        issue = $"Individual Camera Occurrence '{candidate.Identity}' could not be applied. {presentation.Diagnostic}";
                        for (int presentedIndex = presentedCandidates.Count - 1;
                             presentedIndex >= 0;
                             presentedIndex--)
                        {
                            SessionCameraOccurrence presented = presentedCandidates[presentedIndex];
                            presented.Output.Session.RemoveIndividualOccurrence(presented.Identity);
                        }
                        ReleaseCandidateCoverage(candidates);
                        DestroyOccurrences(candidates);
                        return false;
                    }
                    presentedCandidates.Add(candidate);
                }

                if (stagingRoot != null)
                {
                    DestroyObject(stagingRoot);
                    stagingRoot = null;
                }
                for (int index = 0; index < _occurrences.Count; index++)
                {
                    SessionCameraOccurrence occurrence = _occurrences[index];
                    if (occurrence.Identity.IsIndividual &&
                        occurrence.Identity.PlayerOccurrenceId != playerOccurrenceId)
                    {
                        continue;
                    }
                    if (!occurrence.Identity.IsIndividual &&
                        (occurrence.Assignment.OccurrenceMode == CameraOccurrenceMode.IndividualPerPlayer ||
                         occurrence.Assignment.MembershipPolicy != CameraMembershipPolicy.ExplicitPlayerSlots))
                    {
                        continue;
                    }
                    if (!IsConfiguredMember(occurrence.Assignment, playerSlotId))
                    {
                        continue;
                    }
                    if (candidates.Contains(occurrence))
                    {
                        continue;
                    }

                    if (!occurrence.ReconcileMember(
                            playerOccurrenceId,
                            playerSlotId,
                            subject))
                    {
                        issue = $"Session Camera Assignment '{occurrence.Assignment.Id}' rejected current Player membership '{playerOccurrenceId}'.";
                        RollbackPlayerCandidates(candidates, presentedCandidates);
                        return false;
                    }

                    if (occurrence.IsReadyForOutput &&
                        (!occurrence.Output.Session.OutputState.HasRetainedNormalOccurrence ||
                         occurrence.Output.Session.OutputState.RetainedNormalOccurrence != occurrence.Identity))
                    {
                        CameraOccurrenceOutputResult presentation =
                            occurrence.Output.Session.PresentNormalOccurrence(
                                occurrence.Identity,
                                occurrence.Composer);
                        if (!presentation.Succeeded)
                        {
                            issue = $"Camera Occurrence '{occurrence.Identity}' could not be applied after membership reconciliation. {presentation.Diagnostic}";
                            RollbackPlayerCandidates(candidates, presentedCandidates);
                            return false;
                        }
                    }
                }

                _occurrences.AddRange(candidates);
                return true;
            }
            catch (Exception exception)
            {
                RollbackPlayerCandidates(candidates, presentedCandidates);
                issue = $"Individual Camera membership reconciliation failed. {exception.GetType().Name}: {exception.Message}";
                return false;
            }
            finally
            {
                if (stagingRoot != null)
                {
                    DestroyObject(stagingRoot);
                }
            }
        }

        internal bool RemovePlayerOccurrence(
            PlayerOccurrenceId playerOccurrenceId,
            out string issue)
        {
            issue = string.Empty;
            if (!playerOccurrenceId.IsValid)
            {
                issue = "Camera membership removal requires an exact Player occurrence identity.";
                return false;
            }

            for (int index = _occurrences.Count - 1; index >= 0; index--)
            {
                SessionCameraOccurrence occurrence = _occurrences[index];
                if (!occurrence.Identity.IsIndividual ||
                    occurrence.Identity.PlayerOccurrenceId != playerOccurrenceId)
                {
                    occurrence.RemoveMember(playerOccurrenceId);
                    continue;
                }

                occurrence.ReleaseSubjectFallbackCoverage();
                CameraOccurrenceOutputResult removal =
                    occurrence.Output.Session.RemoveIndividualOccurrence(occurrence.Identity);
                if (!removal.Succeeded)
                {
                    issue = $"Individual Camera Occurrence '{occurrence.Identity}' could not be removed. {removal.Diagnostic}";
                    return false;
                }

                DestroyObject(occurrence.Root);
                _occurrences.RemoveAt(index);
            }

            return true;
        }

        private SessionCameraOccurrence FindOccurrence(CameraOccurrenceIdentity identity)
        {
            for (int index = 0; index < _occurrences.Count; index++)
            {
                if (_occurrences[index].Identity == identity)
                {
                    return _occurrences[index];
                }
            }
            return null;
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

        private static bool TryMaterializeIndividualOccurrence(
            IndividualAssignmentRuntime individual,
            CameraOutputAuthoring output,
            CameraOccurrenceIdentity identity,
            Transform stagingParent,
            out SessionCameraOccurrence occurrence,
            out string issue)
        {
            occurrence = null;
            GameObject instance = null;
            try
            {
                instance = Object.Instantiate(
                    individual.Definition.RigPrefab,
                    stagingParent,
                    false);
                if (instance == null)
                {
                    issue = $"Camera Definition '{individual.Definition.name}' Rig Prefab instantiation returned null.";
                    return false;
                }
                instance.SetActive(false);
                instance.name = $"Individual Camera Occurrence [{identity}]";

                CameraRigComposer[] composers =
                    instance.GetComponentsInChildren<CameraRigComposer>(true);
                if (composers.Length != 1 || composers[0] == null)
                {
                    issue = $"Materialized Individual Camera Occurrence '{instance.name}' must contain exactly one CameraRigComposer.";
                    DestroyObject(instance);
                    return false;
                }

                CameraRigComposer composer = composers[0];
                if (!composer.TryValidateForApply(out issue) ||
                    composer.CinemachineCamera == null)
                {
                    issue = $"Materialized Individual Camera Occurrence '{instance.name}' is invalid. {issue}";
                    DestroyObject(instance);
                    return false;
                }

                OutputChannels outputChannel = output.CinemachineBrain.ChannelMask;
                int channelMask = (int)outputChannel;
                if (channelMask <= 0 || (channelMask & (channelMask - 1)) != 0)
                {
                    issue = $"Camera Output '{identity.OutputId}' must have one isolated Cinemachine channel before its Individual occurrence is materialized.";
                    DestroyObject(instance);
                    return false;
                }
                composer.CinemachineCamera.OutputChannel = outputChannel;
                composer.CinemachineCamera.enabled = false;
                occurrence = new SessionCameraOccurrence(
                    individual.Assignment,
                    individual.Definition,
                    identity,
                    instance,
                    composer,
                    output);
                issue = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                DestroyObject(instance);
                issue = $"Individual Camera Occurrence '{identity}' materialization failed. {exception.GetType().Name}: {exception.Message}";
                return false;
            }
        }

        private static void ReleaseCandidateCoverage(
            IReadOnlyList<SessionCameraOccurrence> candidates)
        {
            for (int index = candidates.Count - 1; index >= 0; index--)
            {
                candidates[index].ReleaseSubjectFallbackCoverage();
            }
        }

        private static void RollbackPlayerCandidates(
            IReadOnlyList<SessionCameraOccurrence> candidates,
            IReadOnlyList<SessionCameraOccurrence> presentedCandidates)
        {
            for (int index = presentedCandidates.Count - 1; index >= 0; index--)
            {
                SessionCameraOccurrence presented = presentedCandidates[index];
                presented.Output.Session.RemoveIndividualOccurrence(presented.Identity);
            }
            ReleaseCandidateCoverage(candidates);
            DestroyOccurrences(candidates);
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
                runtime = new SessionCameraAssignmentRuntime(
                    Array.Empty<SessionCameraOccurrence>(),
                    Array.Empty<CameraOutputAuthoring>(),
                    Array.Empty<IndividualAssignmentRuntime>(),
                    sessionParent);
                issue = string.Empty;
                return true;
            }

            var assignments = new List<SessionCameraAssignment>(authoredAssignments.Count);
            var definitionsById = new Dictionary<CameraDefinitionId, CameraDefinition>();
            var assignmentIds = new HashSet<SessionCameraAssignmentId>();
            var usedOutputIds = new HashSet<CameraOutputId>();
            var individualAssignments = new List<IndividualAssignmentRuntime>();
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
                     assignment.OccurrenceMode != CameraOccurrenceMode.SharedGroup &&
                     assignment.OccurrenceMode != CameraOccurrenceMode.IndividualPerPlayer) ||
                    (assignment.MembershipPolicy != CameraMembershipPolicy.None &&
                     assignment.MembershipPolicy != CameraMembershipPolicy.ExplicitPlayerSlots) ||
                    (assignment.TargetPolicy != CameraTargetPolicy.NoSubject &&
                     assignment.TargetPolicy != CameraTargetPolicy.MemberActorTargets))
                {
                    issue = $"Session Camera Assignment '{assignment.Id}' is outside this cut's Session/Shared/Individual membership and target contract.";
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
                var outputsBySlot = new Dictionary<PlayerSlotId, CameraOutputAuthoring>();
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

                if (assignment.OccurrenceMode == CameraOccurrenceMode.IndividualPerPlayer)
                {
                    for (int memberOutputIndex = 0;
                         memberOutputIndex < assignment.MemberOutputs.Count;
                         memberOutputIndex++)
                    {
                        CameraPlayerOutputMapping memberOutput =
                            assignment.MemberOutputs[memberOutputIndex];
                        if (!outputs.TryGetOutput(
                                memberOutput.OutputId,
                                out CameraOutputAuthoring mappedOutput,
                                out issue))
                        {
                            issue = $"Individual Camera Assignment '{assignment.Id}' has no exact configured Output for Player Slot '{memberOutput.PlayerSlotId.StableText}'. {issue}";
                            return false;
                        }
                        outputsBySlot.Add(memberOutput.PlayerSlotId, mappedOutput);
                    }
                    individualAssignments.Add(new IndividualAssignmentRuntime(
                        assignment,
                        definition,
                        outputsBySlot));
                }

                assignments.Add(assignment);
            }

            var candidates = new List<SessionCameraOccurrence>(occurrenceCount);
            var activeOutputs = new List<CameraOutputAuthoring>(occurrenceCount);
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
                    if (assignment.OccurrenceMode == CameraOccurrenceMode.IndividualPerPlayer)
                    {
                        continue;
                    }
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
                        Rollback(activeOutputs);
                        DestroyOccurrences(candidates);
                        return false;
                    }
                    activeOutputs.Add(occurrence.Output);

                    CameraOccurrenceOutputResult applyResult = occurrence.Output.Session.PresentNormalOccurrence(
                        occurrence.Identity,
                        occurrence.Composer);
                    if (!applyResult.Succeeded)
                    {
                        issue = $"Session Camera Occurrence '{occurrence.Identity}' could not be applied. {applyResult.Diagnostic}";
                        Rollback(activeOutputs);
                        DestroyOccurrences(candidates);
                        return false;
                    }
                }

                for (int assignmentIndex = 0;
                     assignmentIndex < individualAssignments.Count;
                     assignmentIndex++)
                {
                    IndividualAssignmentRuntime individual =
                        individualAssignments[assignmentIndex];
                    for (int outputIndex = 0;
                         outputIndex < individual.Assignment.Outputs.Count;
                         outputIndex++)
                    {
                        CameraOutputId outputId =
                            individual.Assignment.Outputs[outputIndex].OutputId;
                        if (!outputs.TryGetOutput(
                                outputId,
                                out CameraOutputAuthoring output,
                                out issue) ||
                            !output.Session.TrySetActiveAssignment(
                                individual.Assignment,
                                out issue))
                        {
                            issue = $"Individual Session Camera Assignment '{individual.Assignment.Id}' could not reserve Output '{outputId}'. {issue}";
                            Rollback(activeOutputs);
                            DestroyOccurrences(candidates);
                            return false;
                        }
                        activeOutputs.Add(output);
                    }
                }

                DestroyObject(stagingRoot);
                stagingRoot = null;
                runtime = new SessionCameraAssignmentRuntime(
                    candidates,
                    activeOutputs,
                    individualAssignments,
                    sessionParent);
                issue = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                Rollback(activeOutputs);
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
            for (int index = _occurrences.Count - 1; index >= 0; index--)
            {
                SessionCameraOccurrence occurrence = _occurrences[index];
                occurrence.ReleaseSubjectFallbackCoverage();
            }

            for (int index = _activeOutputs.Count - 1; index >= 0; index--)
            {
                CameraOutputAuthoring output = _activeOutputs[index];
                if (output != null && output.Session != null)
                {
                    output.Session.ResetSessionAssignmentToFallback(out _);
                }
            }

            for (int index = _occurrences.Count - 1; index >= 0; index--)
            {
                DestroyObject(_occurrences[index].Root);
            }
        }

        private static void Rollback(IReadOnlyList<CameraOutputAuthoring> activeOutputs)
        {
            for (int index = activeOutputs.Count - 1; index >= 0; index--)
            {
                CameraOutputAuthoring output = activeOutputs[index];
                if (output?.Session?.OutputState.HasActiveAssignment == true)
                {
                    output.Session.ResetSessionAssignmentToFallback(out _);
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

            IReadOnlyList<SessionCameraAssignment> membershipAssignments =
                assignments.MembershipAssignments;
            for (int occurrenceIndex = 0;
                 occurrenceIndex < membershipAssignments.Count;
                 occurrenceIndex++)
            {
                SessionCameraAssignment assignment =
                    membershipAssignments[occurrenceIndex];
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
                if (!RemoveMember(previousPlayerId, out string removalIssue))
                {
                    LastReconciliationSucceeded = false;
                    Diagnostic = removalIssue;
                    return;
                }
            }

            if (!currentSlot.IsJoined || !currentSlot.PlayerOccurrenceId.IsValid)
            {
                LastReconciliationSucceeded = true;
                Diagnostic = string.Empty;
                return;
            }

            CameraSubject subject = default;
            string diagnostic = string.Empty;
            bool actorEvidenceValid =
                !_actors.TryGetCurrentActorOccurrence(
                    playerSlotId,
                    out PlayerPreparedActorOccurrence actor) ||
                TryResolveSubject(actor, out subject, out diagnostic);
            bool membershipSucceeded = _assignments.ReconcilePlayerOccurrence(
                currentSlot.PlayerOccurrenceId,
                playerSlotId,
                subject,
                out string membershipDiagnostic);
            LastReconciliationSucceeded = actorEvidenceValid && membershipSucceeded;
            Diagnostic = !actorEvidenceValid ? diagnostic : membershipDiagnostic;
            if (!LastReconciliationSucceeded && string.IsNullOrEmpty(Diagnostic))
            {
                Diagnostic = "Current Player Camera membership or Actor Subject reconciliation failed.";
            }
        }

        private bool RemoveMember(PlayerOccurrenceId playerOccurrenceId, out string issue)
        {
            return _assignments.RemovePlayerOccurrence(playerOccurrenceId, out issue);
        }

        private void RecordFailure(PlayerSlotId playerSlotId, Exception exception)
        {
            LastReconciliationSucceeded = false;
            Diagnostic = $"Session Camera membership reconciliation failed for '{playerSlotId.StableText}'. {exception.GetType().Name}: {exception.Message}";
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
            subject = new CameraSubject(
                new CameraSubjectId(
                    $"camera.subject.player-actor:{actor.PreparationToken.StableText}"),
                observation,
                $"Current Session Player Actor for {actor.PlayerSlotId.StableText}");
            if (!subject.IsValid)
            {
                issue = "Current Player Actor did not resolve a valid Camera Subject.";
                return false;
            }
            return true;
        }
    }
}
