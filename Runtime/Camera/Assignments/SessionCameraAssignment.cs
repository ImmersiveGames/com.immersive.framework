using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.Camera
{
    public enum CameraOccurrenceMode { Undefined = 0, SessionScoped = 1, IndividualPerPlayer = 2, SharedGroup = 3 }
    public enum CameraMembershipPolicy { Undefined = 0, None = 1, ExplicitPlayerSlots = 2 }
    public enum CameraTargetPolicy { Undefined = 0, NoSubject = 1, ExplicitWorldTarget = 2, MemberActorTargets = 3 }

    public readonly struct CameraOutputMapping : IEquatable<CameraOutputMapping>
    {
        public CameraOutputMapping(CameraOutputId outputId) { OutputId = outputId; }
        public CameraOutputId OutputId { get; }
        public bool IsValid => OutputId.IsValid;
        public bool Equals(CameraOutputMapping other) => OutputId == other.OutputId;
        public override bool Equals(object obj) => obj is CameraOutputMapping other && Equals(other);
        public override int GetHashCode() => OutputId.GetHashCode();
    }

    public readonly struct CameraPlayerOutputMapping : IEquatable<CameraPlayerOutputMapping>
    {
        public CameraPlayerOutputMapping(PlayerSlotId playerSlotId, CameraOutputId outputId)
        {
            PlayerSlotId = playerSlotId;
            OutputId = outputId;
        }

        public PlayerSlotId PlayerSlotId { get; }
        public CameraOutputId OutputId { get; }
        public bool IsValid => PlayerSlotId.IsValid && OutputId.IsValid;
        public bool Equals(CameraPlayerOutputMapping other) =>
            PlayerSlotId == other.PlayerSlotId && OutputId == other.OutputId;
        public override bool Equals(object obj) =>
            obj is CameraPlayerOutputMapping other && Equals(other);
        public override int GetHashCode() =>
            (PlayerSlotId.GetHashCode() * 397) ^ OutputId.GetHashCode();
    }

    /// <summary>Immutable policy dimensions for one Session's use of a reusable Definition.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-038-B Session Camera Assignment contract.")]
    public sealed class SessionCameraAssignment
    {
        private readonly CameraOutputMapping[] _outputs;
        private readonly PlayerSlotId[] _memberSlots;
        private readonly CameraPlayerOutputMapping[] _memberOutputs;

        public SessionCameraAssignment(SessionCameraAssignmentId id,
            CameraOccurrenceMode occurrenceMode, CameraMembershipPolicy membershipPolicy,
            CameraTargetPolicy targetPolicy, IReadOnlyList<CameraOutputMapping> outputs,
            IReadOnlyList<PlayerSlotId> memberSlots = null,
            IReadOnlyList<CameraPlayerOutputMapping> memberOutputs = null)
        {
            Id = id; OccurrenceMode = occurrenceMode;
            MembershipPolicy = membershipPolicy; TargetPolicy = targetPolicy;
            _outputs = Copy(outputs); _memberSlots = Copy(memberSlots);
            _memberOutputs = Copy(memberOutputs);
        }

        public SessionCameraAssignmentId Id { get; }
        public CameraOccurrenceMode OccurrenceMode { get; }
        public CameraMembershipPolicy MembershipPolicy { get; }
        public CameraTargetPolicy TargetPolicy { get; }
        public IReadOnlyList<CameraOutputMapping> Outputs => Array.AsReadOnly(_outputs);
        public IReadOnlyList<PlayerSlotId> MemberSlots => Array.AsReadOnly(_memberSlots);
        public IReadOnlyList<CameraPlayerOutputMapping> MemberOutputs =>
            Array.AsReadOnly(_memberOutputs);

        public bool TryValidate(out string issue)
        {
            if (!Id.IsValid) issue = "Assignment identity is required.";
            else if (OccurrenceMode != CameraOccurrenceMode.SessionScoped && OccurrenceMode != CameraOccurrenceMode.IndividualPerPlayer && OccurrenceMode != CameraOccurrenceMode.SharedGroup) issue = "Occurrence mode must be explicit.";
            else if (MembershipPolicy != CameraMembershipPolicy.None && MembershipPolicy != CameraMembershipPolicy.ExplicitPlayerSlots) issue = "Membership policy must be explicit.";
            else if (TargetPolicy != CameraTargetPolicy.NoSubject && TargetPolicy != CameraTargetPolicy.ExplicitWorldTarget && TargetPolicy != CameraTargetPolicy.MemberActorTargets) issue = "Target policy must be explicit.";
            else if (_outputs.Length == 0) issue = "At least one explicit Output mapping is required.";
            else if (HasInvalidOrDuplicateOutputs()) issue = "Output mappings must be valid and unique.";
            else if (MembershipPolicy == CameraMembershipPolicy.None && _memberSlots.Length != 0) issue = "Slots cannot be supplied when membership policy is None.";
            else if (MembershipPolicy == CameraMembershipPolicy.ExplicitPlayerSlots && HasInvalidOrDuplicateSlots()) issue = "Explicit membership requires valid, unique Player Slots.";
            else if (OccurrenceMode == CameraOccurrenceMode.IndividualPerPlayer && MembershipPolicy == CameraMembershipPolicy.None) issue = "Individual occurrences require an explicit membership source.";
            else if (OccurrenceMode == CameraOccurrenceMode.IndividualPerPlayer && !HasExactMemberOutputMappings()) issue = "Individual Assignments require one explicit unique Output mapping for every member Player Slot, and every mapped Output must belong to this Assignment.";
            else if (OccurrenceMode != CameraOccurrenceMode.IndividualPerPlayer && _memberOutputs.Length != 0) issue = "Per-Player Output mappings are valid only for Individual Assignments.";
            else if (TargetPolicy == CameraTargetPolicy.MemberActorTargets && MembershipPolicy == CameraMembershipPolicy.None) issue = "Member Actor targets require a membership source.";
            else { issue = string.Empty; return true; }
            return false;
        }

        private bool HasInvalidOrDuplicateOutputs() { var seen = new HashSet<CameraOutputId>(); foreach (var item in _outputs) if (!item.IsValid || !seen.Add(item.OutputId)) return true; return false; }
        private bool HasInvalidOrDuplicateSlots() { var seen = new HashSet<PlayerSlotId>(); foreach (var item in _memberSlots) if (!item.IsValid || !seen.Add(item)) return true; return false; }
        private bool HasExactMemberOutputMappings()
        {
            if (_memberOutputs.Length != _memberSlots.Length || _memberSlots.Length == 0)
            {
                return false;
            }

            var slots = new HashSet<PlayerSlotId>();
            var outputs = new HashSet<CameraOutputId>();
            for (int index = 0; index < _memberOutputs.Length; index++)
            {
                CameraPlayerOutputMapping mapping = _memberOutputs[index];
                if (!mapping.IsValid || !slots.Add(mapping.PlayerSlotId) ||
                    !outputs.Add(mapping.OutputId) ||
                    Array.IndexOf(_memberSlots, mapping.PlayerSlotId) < 0)
                {
                    return false;
                }

                bool outputIsMapped = false;
                for (int outputIndex = 0; outputIndex < _outputs.Length; outputIndex++)
                {
                    if (_outputs[outputIndex].OutputId == mapping.OutputId)
                    {
                        outputIsMapped = true;
                        break;
                    }
                }
                if (!outputIsMapped)
                {
                    return false;
                }
            }

            return slots.Count == _memberSlots.Length && outputs.Count == _outputs.Length;
        }
        private static T[] Copy<T>(IReadOnlyList<T> source) { if (source == null) return Array.Empty<T>(); var result = new T[source.Count]; for (int i = 0; i < result.Length; i++) result[i] = source[i]; return result; }
    }
}
