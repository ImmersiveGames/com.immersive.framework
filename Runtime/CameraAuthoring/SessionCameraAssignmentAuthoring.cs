using System;
using System.Collections.Generic;
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

    /// <summary>Authoring for a Session Camera Assignment.</summary>
    [Serializable]
    public sealed class SessionCameraAssignmentAuthoring
    {
        [SerializeField, Tooltip("Unique stable identity for this Session Camera Assignment.")] private string assignmentId = string.Empty;
        [SerializeField] private CameraDefinition definition;
        [SerializeField] private CameraOccurrenceMode occurrenceMode = CameraOccurrenceMode.SessionScoped;
        [SerializeField] private CameraMembershipPolicy membershipPolicy = CameraMembershipPolicy.None;
        [SerializeField] private CameraTargetPolicy targetPolicy = CameraTargetPolicy.NoSubject;
        [SerializeField] private List<PlayerSlotProfile> memberSlots = new List<PlayerSlotProfile>();
        [SerializeField] private List<CameraOutputDefinition> outputDefinitions = new List<CameraOutputDefinition>();
        [SerializeField] private List<SessionCameraMemberOutputAuthoring> individualMemberOutputMappings = new List<SessionCameraMemberOutputAuthoring>();

        public SessionCameraAssignmentId AssignmentId => new SessionCameraAssignmentId(assignmentId);
        public CameraDefinition Definition => definition;
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

            if (definition == null || !definition.HasValidId)
            {
                issue = "Session Camera Assignment requires an exact Camera Definition with a valid identity.";
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
                definition.DefinitionId,
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
