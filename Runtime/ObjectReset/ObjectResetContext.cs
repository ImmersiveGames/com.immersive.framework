using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ObjectEntry;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Passive context passed to one Object Reset participant dispatch.
    /// It exposes the resolved logical ObjectEntry target only; it is not a service locator and does not expose Unity objects.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F14C Object Reset participant context; logical target only.")]
    public readonly struct ObjectResetContext : IEquatable<ObjectResetContext>
    {
        public ObjectResetContext(
            ObjectResetRequest request,
            ObjectEntryDescriptor resolvedTarget,
            ObjectResetParticipantDescriptor participant)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Object Reset context requires a valid request.", nameof(request));
            }

            if (!request.Target.Matches(resolvedTarget))
            {
                throw new ArgumentException("Object Reset context requires a resolved target matching the request target.", nameof(resolvedTarget));
            }

            if (!participant.IsValid)
            {
                throw new ArgumentException("Object Reset context requires a valid participant descriptor.", nameof(participant));
            }

            if (!participant.SupportsResolvedTarget(request, resolvedTarget))
            {
                throw new ArgumentException("Object Reset participant descriptor does not support the resolved request target.", nameof(participant));
            }

            Request = request;
            ResolvedTarget = resolvedTarget;
            Participant = participant;
        }

        public ObjectResetRequest Request { get; }

        public ObjectEntryDescriptor ResolvedTarget { get; }

        public ObjectResetParticipantDescriptor Participant { get; }

        public ObjectResetTarget Target => Request.Target;

        public ObjectEntryId ObjectEntryId => Target.ObjectEntryId;

        public ObjectEntryScope Scope => Target.Scope;

        public ObjectResetParticipantId ParticipantId => Participant.ParticipantId;

        public ObjectResetParticipantRequiredness Requiredness => Participant.Requiredness;

        public bool IsRequired => Participant.IsRequired;

        public bool IsOptional => Participant.IsOptional;

        public string Source => Request.Source;

        public string Reason => Request.Reason;

        public bool IsValid => Request.IsValid
            && Request.Target.Matches(ResolvedTarget)
            && Participant.IsValid
            && Participant.SupportsResolvedTarget(Request, ResolvedTarget);

        public bool Equals(ObjectResetContext other)
        {
            return Request.Equals(other.Request)
                && ResolvedTarget.Equals(other.ResolvedTarget)
                && Participant.Equals(other.Participant);
        }

        public override bool Equals(object obj)
        {
            return obj is ObjectResetContext other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Request.GetHashCode();
                hashCode = hashCode * 397 ^ ResolvedTarget.GetHashCode();
                hashCode = hashCode * 397 ^ Participant.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            return $"request=({Request.ToDiagnosticString()}) participant=({Participant.ToDiagnosticString()}) resolvedTarget=({ResolvedTarget})";
        }

        public static bool operator ==(ObjectResetContext left, ObjectResetContext right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ObjectResetContext left, ObjectResetContext right)
        {
            return !left.Equals(right);
        }
    }
}
