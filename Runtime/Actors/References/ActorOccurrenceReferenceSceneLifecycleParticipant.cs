using System.Collections.Generic;
using Immersive.Framework.SceneLifecycle;
using UnityEngine;

namespace Immersive.Framework.Actors
{
    /// <summary>Vincula consumidores/publicadores somente por raízes explicitamente compostas.</summary>
    internal sealed class ActorOccurrenceReferenceSceneLifecycleParticipant :
        ISceneLifecycleParticipant,
        ISceneLifecycleReleaseCompensationParticipant
    {
        private readonly ActorOccurrenceReferenceRuntime _runtime;
        private sealed class ScopeMembers
        {
            internal readonly HashSet<ActorOccurrenceReferenceBinding> Bindings = new();
            internal readonly HashSet<ActorOccurrencePublication> Publishers = new();
            internal readonly HashSet<ActorOccurrencePublicationScope> PublicationScopes = new();
        }

        private readonly Dictionary<SceneCompositionScope, ScopeMembers> _scopes = new();
        private readonly Dictionary<ActorOccurrenceReferenceBinding, int> _bindings = new();
        private readonly Dictionary<ActorOccurrencePublication, int> _publishers = new();
        private readonly Dictionary<ActorOccurrencePublicationScope, int> _publicationScopes = new();

        internal ActorOccurrenceReferenceSceneLifecycleParticipant(ActorOccurrenceReferenceRuntime runtime)
        {
            _runtime = runtime;
        }

        public SceneCompositionResult OnSceneAvailable(SceneCompositionScope scope, IReadOnlyList<GameObject> roots)
        {
            if (roots == null)
                return SceneCompositionResult.Rejected(scope, SceneCompositionOperation.Available,
                    "Actor occurrence composition requires explicit scene roots.");
            if (!_scopes.TryGetValue(scope, out ScopeMembers members))
            {
                members = new ScopeMembers();
                _scopes.Add(scope, members);
            }

            var currentBindings = new HashSet<ActorOccurrenceReferenceBinding>();
            var currentPublishers = new HashSet<ActorOccurrencePublication>();
            var currentPublicationScopes = new HashSet<ActorOccurrencePublicationScope>();
            for (int index = 0; index < roots.Count; index++)
            {
                GameObject root = roots[index];
                if (root == null) continue;
                ActorOccurrenceReferenceBinding[] bindings = root.GetComponentsInChildren<ActorOccurrenceReferenceBinding>(true);
                for (int i = 0; i < bindings.Length; i++)
                    if (bindings[i] != null) currentBindings.Add(bindings[i]);
                ActorOccurrencePublication[] publishers = root.GetComponentsInChildren<ActorOccurrencePublication>(true);
                for (int i = 0; i < publishers.Length; i++)
                    if (publishers[i] != null) currentPublishers.Add(publishers[i]);
                ActorOccurrencePublicationScope[] scopes = root.GetComponentsInChildren<ActorOccurrencePublicationScope>(true);
                for (int i = 0; i < scopes.Length; i++)
                    if (scopes[i] != null) currentPublicationScopes.Add(scopes[i]);
            }

            Reconcile(members.Bindings, currentBindings, Retain, Release);
            Reconcile(members.Publishers, currentPublishers, Retain, Release);
            Reconcile(members.PublicationScopes, currentPublicationScopes, Retain, Release);
            return SceneCompositionResult.Completed(scope, SceneCompositionOperation.Available,
                "Actor occurrence reference components received the active Framework host runtime.");
        }

        public SceneCompositionResult OnSceneReleasing(
            SceneCompositionScope scope,
            IReadOnlyList<GameObject> roots,
            string reason)
        {
            if (_scopes.TryGetValue(scope, out ScopeMembers members))
            {
                _scopes.Remove(scope);
                ReleaseAll(members.Bindings, Release);
                ReleaseAll(members.Publishers, Release);
                ReleaseAll(members.PublicationScopes, Release);
            }
            return SceneCompositionResult.Completed(scope, SceneCompositionOperation.Releasing,
                "Actor occurrence reference components released their composition bindings.");
        }

        void ISceneLifecycleReleaseCompensationParticipant.OnSceneReleaseCompensated(SceneCompositionScope scope)
        {
            if (!_scopes.TryGetValue(scope, out ScopeMembers members)) return;
            foreach (ActorOccurrencePublicationScope publicationScope in members.PublicationScopes)
                _runtime.RestoreScopePublications(publicationScope);
        }

        private static void Reconcile<T>(HashSet<T> retained, HashSet<T> current, System.Action<T> retain, System.Action<T> release)
        {
            var removed = new List<T>();
            foreach (T item in retained)
                if (!current.Contains(item)) removed.Add(item);
            for (int index = 0; index < removed.Count; index++)
            {
                retained.Remove(removed[index]);
                release(removed[index]);
            }
            foreach (T item in current)
            {
                if (!retained.Add(item)) continue;
                retain(item);
            }
        }

        private static void ReleaseAll<T>(HashSet<T> retained, System.Action<T> release)
        {
            foreach (T item in retained) release(item);
            retained.Clear();
        }

        private void Retain(ActorOccurrenceReferenceBinding binding)
        {
            if (binding == null) return;
            _bindings.TryGetValue(binding, out int count);
            _bindings[binding] = count + 1;
            if (count == 0) _runtime.BindConsumer(binding);
        }

        private void Release(ActorOccurrenceReferenceBinding binding)
        {
            if (binding == null || !_bindings.TryGetValue(binding, out int count)) return;
            if (count > 1)
            {
                _bindings[binding] = count - 1;
                return;
            }
            _bindings.Remove(binding);
            _runtime.UnbindConsumer(binding);
        }

        private void Retain(ActorOccurrencePublication publisher)
        {
            if (publisher == null) return;
            _publishers.TryGetValue(publisher, out int count);
            _publishers[publisher] = count + 1;
            if (count == 0) publisher.BindRuntime(_runtime);
        }

        private void Release(ActorOccurrencePublication publisher)
        {
            if (publisher == null || !_publishers.TryGetValue(publisher, out int count)) return;
            if (count > 1)
            {
                _publishers[publisher] = count - 1;
                return;
            }
            _publishers.Remove(publisher);
            publisher.UnbindRuntime(_runtime);
        }

        private void Retain(ActorOccurrencePublicationScope scope)
        {
            if (scope == null) return;
            _publicationScopes.TryGetValue(scope, out int count);
            _publicationScopes[scope] = count + 1;
            if (count == 0) scope.BindRuntime(_runtime);
        }

        private void Release(ActorOccurrencePublicationScope scope)
        {
            if (scope == null || !_publicationScopes.TryGetValue(scope, out int count)) return;
            if (count > 1)
            {
                _publicationScopes[scope] = count - 1;
                return;
            }
            _publicationScopes.Remove(scope);
            scope.UnbindRuntime(_runtime);
        }
    }
}
