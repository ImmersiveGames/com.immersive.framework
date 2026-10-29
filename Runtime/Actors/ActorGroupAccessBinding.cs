using Immersive.Framework.ApiStatus;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.Actors
{
    /// <summary>Explicit access to Groups owned by the Framework composition that contains this binding.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Actors/Actor Group Access")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-051 owner-bound Group creation and query surface.")]
    public sealed class ActorGroupAccessBinding : MonoBehaviour
    {
        private ActorGroupRuntime _runtime;
        private RuntimeContentOwner _owner;

        public bool IsAccessAvailable => _runtime != null && _owner.IsValid;

        public bool TryCreateGroup(
            GroupDefinitionAsset definition,
            out ActorGroupSnapshot group,
            out string diagnostic)
        {
            if (definition == null)
            {
                group = default;
                diagnostic = "A Group Definition is required.";
                return false;
            }

            if (!definition.TryValidate(out diagnostic) ||
                !definition.TryGetGroupId(out GroupId groupId, out diagnostic))
            {
                group = default;
                return false;
            }

            return TryCreateGroup(groupId, definition.GroupProfile, out group, out diagnostic);
        }

        public bool TryCreateGroup(
            GroupId groupId,
            GroupProfile profile,
            out ActorGroupSnapshot group,
            out string diagnostic)
        {
            if (!TryGetBoundRuntime(out ActorGroupRuntime runtime, out RuntimeContentOwner owner, out diagnostic))
            {
                group = default;
                return false;
            }

            return runtime.TryCreateGroup(owner, groupId, profile, out group, out diagnostic);
        }

        public bool TryGetGroup(
            GroupId groupId,
            out ActorGroupSnapshot group,
            out string diagnostic)
        {
            if (!TryGetBoundRuntime(out ActorGroupRuntime runtime, out _, out diagnostic))
            {
                group = default;
                return false;
            }

            return runtime.TryGetGroup(groupId, out group, out diagnostic);
        }

        internal bool CanBindRuntime(
            ActorGroupRuntime runtime,
            RuntimeContentOwner owner,
            out string diagnostic)
        {
            diagnostic = string.Empty;
            if (runtime == null || !owner.IsValid)
            {
                diagnostic = "Actor Group access binding requires an exact runtime and valid lifecycle owner.";
                return false;
            }

            if (_runtime != null)
            {
                diagnostic = _runtime == runtime && _owner == owner
                    ? "Actor Group access binding is already bound to this owner."
                    : $"Actor Group access binding is already bound to owner '{_owner.StableText}'.";
                return false;
            }

            return true;
        }

        internal bool TryBindRuntime(
            ActorGroupRuntime runtime,
            RuntimeContentOwner owner,
            out string diagnostic)
        {
            if (!CanBindRuntime(runtime, owner, out diagnostic)) return false;
            _runtime = runtime;
            _owner = owner;
            return true;
        }

        internal void UnbindRuntime(ActorGroupRuntime runtime, RuntimeContentOwner owner)
        {
            if (_runtime != runtime || _owner != owner) return;
            _runtime = null;
            _owner = default;
        }

        private bool TryGetBoundRuntime(
            out ActorGroupRuntime runtime,
            out RuntimeContentOwner owner,
            out string diagnostic)
        {
            runtime = _runtime;
            owner = _owner;
            if (runtime == null || !owner.IsValid)
            {
                diagnostic = "Actor Group access is unavailable until its Framework owner is committed.";
                return false;
            }

            diagnostic = string.Empty;
            return true;
        }
    }
}
