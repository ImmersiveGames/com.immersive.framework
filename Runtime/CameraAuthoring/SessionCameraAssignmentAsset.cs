using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Camera;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.CameraAuthoring
{
    [Serializable]
    public sealed class SessionCameraMemberOutputAuthoring
    {
        [SerializeField] private PlayerSlotProfile playerSlotProfile;
        [SerializeField] private CameraOutputDefinition outputDefinition;

        public PlayerSlotProfile PlayerSlotProfile => playerSlotProfile;
        public CameraOutputDefinition OutputDefinition => outputDefinition;

        public void Configure(
            PlayerSlotProfile slotProfile,
            CameraOutputDefinition cameraOutput)
        {
            playerSlotProfile = slotProfile;
            outputDefinition = cameraOutput;
        }
    }

    /// <summary>Reusable authored configuration and policy for a Session Camera Assignment.</summary>
    [CreateAssetMenu(fileName = "Session Camera Assignment", menuName = "Immersive Framework/Camera/Session Camera Assignment", order = 20)]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-038 Session Camera Assignment asset authoring.")]
    public sealed class SessionCameraAssignmentAsset : ScriptableObject
    {
        [SerializeField, HideInInspector] private string assignmentId;
        [SerializeField] private GameObject rigPrefab;
        [SerializeField] private CameraOccurrenceMode occurrenceMode = CameraOccurrenceMode.SessionScoped;
        [SerializeField] private CameraMembershipPolicy membershipPolicy = CameraMembershipPolicy.None;
        [SerializeField] private CameraTargetPolicy targetPolicy = CameraTargetPolicy.NoSubject;
        [SerializeField] private List<PlayerSlotProfile> memberSlots = new List<PlayerSlotProfile>();
        [SerializeField] private List<CameraOutputDefinition> outputDefinitions = new List<CameraOutputDefinition>();
        [SerializeField] private List<SessionCameraMemberOutputAuthoring> individualMemberOutputMappings = new List<SessionCameraMemberOutputAuthoring>();

        public SessionCameraAssignmentAsset()
        {
            assignmentId = Guid.NewGuid().ToString("N");
        }

        public SessionCameraAssignmentId AssignmentId => new SessionCameraAssignmentId(assignmentId);
        public GameObject RigPrefab => rigPrefab;
        public IReadOnlyList<CameraOutputDefinition> OutputDefinitions =>
            outputDefinitions ?? (IReadOnlyList<CameraOutputDefinition>)Array.Empty<CameraOutputDefinition>();

        public bool TryBuild(out SessionCameraAssignment assignment, out string issue)
        {
            assignment = null;
            if (!AssignmentId.IsValid)
            {
                issue = "Session Camera Assignment requires an explicit identity.";
                return false;
            }

            if (rigPrefab == null)
            {
                issue = "Session Camera Assignment requires an explicit Rig Prefab.";
                return false;
            }

            CameraRigComposer[] composers = rigPrefab.GetComponentsInChildren<CameraRigComposer>(true);
            if (composers.Length != 1 || composers[0] == null)
            {
                issue = $"Session Camera Assignment Rig Prefab '{rigPrefab.name}' must contain exactly one CameraRigComposer. Found '{composers.Length}'.";
                return false;
            }
            if (rigPrefab.GetComponentInChildren<CameraOutputAuthoring>(true) != null)
            {
                issue = $"Session Camera Assignment Rig Prefab '{rigPrefab.name}' must not contain CameraOutputAuthoring.";
                return false;
            }
            bool usesGroupBehavior =
                composers[0].BehaviorDefinition is GroupCameraRigBehaviorDefinition;
            if (usesGroupBehavior &&
                (occurrenceMode != CameraOccurrenceMode.SharedGroup ||
                 membershipPolicy != CameraMembershipPolicy.ExplicitPlayerSlots ||
                 targetPolicy != CameraTargetPolicy.MemberActorTargets))
            {
                issue = $"Session Camera Assignment Rig Prefab '{rigPrefab.name}' uses Group behavior, which requires SharedGroup occurrence mode, ExplicitPlayerSlots membership, and MemberActorTargets.";
                return false;
            }
            if (usesGroupBehavior &&
                !CameraGroupProvenance.Validate(composers[0], true, out issue))
            {
                issue = $"Session Camera Assignment Rig Prefab '{rigPrefab.name}' has invalid Group materialization. {issue}";
                return false;
            }
            if (!composers[0].TryValidateForApply(out issue))
            {
                issue = $"Session Camera Assignment Rig Prefab '{rigPrefab.name}' is invalid. {issue}";
                return false;
            }
            if (composers[0].CinemachineCamera == null)
            {
                issue = $"Session Camera Assignment Rig Prefab '{rigPrefab.name}' must contain its materialized CinemachineCamera.";
                return false;
            }
            if (targetPolicy == CameraTargetPolicy.NoSubject &&
                (composers[0].EffectiveFollowRequirement != CameraTargetRequirement.NotUsed ||
                 composers[0].EffectiveLookAtRequirement != CameraTargetRequirement.NotUsed))
            {
                issue = $"Session Camera Assignment Rig Prefab '{rigPrefab.name}' requires a Subject but its Target Policy is NoSubject.";
                return false;
            }
            if (targetPolicy == CameraTargetPolicy.MemberActorTargets &&
                composers[0].EffectiveFollowRequirement == CameraTargetRequirement.NotUsed &&
                composers[0].EffectiveLookAtRequirement == CameraTargetRequirement.NotUsed)
            {
                issue = $"Session Camera Assignment Rig Prefab '{rigPrefab.name}' does not consume the Assignment's member Actor Subjects.";
                return false;
            }

            if ((occurrenceMode != CameraOccurrenceMode.SessionScoped &&
                 occurrenceMode != CameraOccurrenceMode.SharedGroup &&
                 occurrenceMode != CameraOccurrenceMode.IndividualPerPlayer) ||
                (membershipPolicy != CameraMembershipPolicy.None &&
                 membershipPolicy != CameraMembershipPolicy.ExplicitPlayerSlots) ||
                (targetPolicy != CameraTargetPolicy.NoSubject &&
                 targetPolicy != CameraTargetPolicy.MemberActorTargets))
            {
                issue = "This Session Camera cut supports Session, Shared or Individual Assignments with no membership or explicit Player Slot membership, and no-Subject or member-Actor targets.";
                return false;
            }

            var authoredMemberSlots = new List<PlayerSlotId>(memberSlots != null ? memberSlots.Count : 0);
            if (memberSlots != null)
            {
                var uniqueMemberSlots = new HashSet<PlayerSlotId>();
                for (int index = 0; index < memberSlots.Count; index++)
                {
                    PlayerSlotProfile profile = memberSlots[index];
                    if (profile == null)
                    {
                        issue = $"Session Camera Assignment '{AssignmentId}' has a missing member Player Slot at index '{index}'.";
                        return false;
                    }

                    if (!profile.TryGetPlayerSlotId(out PlayerSlotId playerSlotId, out issue))
                    {
                        issue = $"Session Camera Assignment '{AssignmentId}' has invalid member Player Slot at index '{index}'. {issue}";
                        return false;
                    }
                    if (!uniqueMemberSlots.Add(playerSlotId))
                    {
                        issue = $"Session Camera Assignment '{AssignmentId}' has duplicate member Player Slot '{playerSlotId}'.";
                        return false;
                    }
                    authoredMemberSlots.Add(playerSlotId);
                }
            }

            IReadOnlyList<CameraOutputDefinition> authoredOutputs = OutputDefinitions;
            var mappings = new CameraOutputMapping[authoredOutputs.Count];
            for (int index = 0; index < authoredOutputs.Count; index++)
            {
                CameraOutputDefinition output = authoredOutputs[index];
                if (output == null || !output.HasValidId)
                {
                    issue = $"Session Camera Assignment '{AssignmentId}' has a missing or invalid Output mapping at index '{index}'.";
                    return false;
                }
                mappings[index] = new CameraOutputMapping(output.OutputId);
            }

            var authoredMemberOutputs = new List<CameraPlayerOutputMapping>(
                individualMemberOutputMappings != null
                    ? individualMemberOutputMappings.Count
                    : 0);
            if (individualMemberOutputMappings != null)
            {
                for (int index = 0; index < individualMemberOutputMappings.Count; index++)
                {
                    SessionCameraMemberOutputAuthoring memberOutput =
                        individualMemberOutputMappings[index];
                    if (memberOutput == null || memberOutput.PlayerSlotProfile == null ||
                        memberOutput.OutputDefinition == null ||
                        !memberOutput.OutputDefinition.HasValidId)
                    {
                        issue = $"Individual Session Camera Assignment '{AssignmentId}' has an incomplete Player Slot to Output mapping at index '{index}'.";
                        return false;
                    }

                    if (!memberOutput.PlayerSlotProfile.TryGetPlayerSlotId(
                            out PlayerSlotId playerSlotId,
                            out issue))
                    {
                        issue = $"Individual Session Camera Assignment '{AssignmentId}' has an invalid Player Slot at mapping index '{index}'. {issue}";
                        return false;
                    }

                    bool outputBelongsToAssignment = false;
                    for (int outputIndex = 0;
                         outputIndex < authoredOutputs.Count;
                         outputIndex++)
                    {
                        if (ReferenceEquals(
                                authoredOutputs[outputIndex],
                                memberOutput.OutputDefinition))
                        {
                            outputBelongsToAssignment = true;
                            break;
                        }
                    }
                    if (!outputBelongsToAssignment)
                    {
                        issue = $"Individual Session Camera Assignment '{AssignmentId}' maps Player Slot '{playerSlotId.StableText}' to an Output that is not explicitly listed by the Assignment.";
                        return false;
                    }

                    authoredMemberOutputs.Add(new CameraPlayerOutputMapping(
                        playerSlotId,
                        memberOutput.OutputDefinition.OutputId));
                }
            }

            assignment = new SessionCameraAssignment(
                AssignmentId,
                occurrenceMode,
                membershipPolicy,
                targetPolicy,
                mappings,
                authoredMemberSlots,
                authoredMemberOutputs);
            if (!assignment.TryValidate(out issue))
            {
                assignment = null;
                return false;
            }

            issue = string.Empty;
            return true;
        }
    }
}
