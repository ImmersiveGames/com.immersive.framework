using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Actors
{
    /// <summary>Read-only runtime view of one live Actor Group.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-051 runtime Group snapshot.")]
    public readonly struct ActorGroupSnapshot
    {
        internal ActorGroupSnapshot(GroupId groupId, GroupProfile profile)
        {
            GroupId = groupId;
            Profile = profile;
        }

        public GroupId GroupId { get; }

        public GroupProfile Profile { get; }

        public bool HasProfile => Profile != null;

        public bool IsValid => GroupId.IsValid;
    }
}
