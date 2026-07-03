using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.CycleReset
{
    /// <summary>
    /// API status: Experimental. Passive context passed to one Cycle Reset participant dispatch.
    /// It exposes lifecycle request data only; it is not a service locator and does not expose reset targets beyond Route/Activity cycle context.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F11A Cycle Reset participant context; no object/component/player target.")]
    public readonly struct CycleResetContext : IEquatable<CycleResetContext>
    {
        public CycleResetContext(CycleResetRequest request, CycleResetParticipantDescriptor participant)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Cycle Reset context requires a valid request.", nameof(request));
            }

            if (!participant.IsValid)
            {
                throw new ArgumentException("Cycle Reset context requires a valid participant descriptor.", nameof(participant));
            }

            if (!participant.SupportsRequest(request))
            {
                throw new ArgumentException("Cycle Reset participant descriptor does not support the request scope.", nameof(participant));
            }

            Request = request;
            Participant = participant;
        }

        public CycleResetRequest Request { get; }

        public CycleResetParticipantDescriptor Participant { get; }

        public CycleResetScope RequestScope => Request.Scope;

        public CycleResetScope ParticipantScope => Participant.ParticipantScope;

        public CycleResetParticipantId ParticipantId => Participant.ParticipantId;

        public CycleResetParticipantRequiredness Requiredness => Participant.Requiredness;

        public RouteAsset ActiveRoute => Request.ActiveRoute;

        public ActivityAsset ActiveActivity => Request.ActiveActivity;

        public bool IsRouteReset => Request.IsRouteReset;

        public bool IsActivityReset => Request.IsActivityReset;

        public bool IsRequired => Participant.IsRequired;

        public bool IsOptional => Participant.IsOptional;

        public string Source => Request.Source;

        public string Reason => Request.Reason;

        public bool IsValid => Request.IsValid && Participant.IsValid && Participant.SupportsRequest(Request);

        public bool Equals(CycleResetContext other)
        {
            return Request.Equals(other.Request) && Participant.Equals(other.Participant);
        }

        public override bool Equals(object obj)
        {
            return obj is CycleResetContext other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return Request.GetHashCode() * 397 ^ Participant.GetHashCode();
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            return $"request=({Request.ToDiagnosticString()}) participant=({Participant.ToDiagnosticString()})";
        }

        public static bool operator ==(CycleResetContext left, CycleResetContext right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CycleResetContext left, CycleResetContext right)
        {
            return !left.Equals(right);
        }
    }
}
