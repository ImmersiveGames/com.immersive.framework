using System;
using System.Collections.Generic;
using Immersive.Framework.SceneLifecycle;
using UnityEngine;

namespace Immersive.Framework.PersistentContent.Tests
{
    internal sealed class HostSessionLifecycleTestProbe : MonoBehaviour, ISceneLifecycleParticipant
    {
        private static readonly List<Call> RecordedCalls = new();
        private static readonly List<GameObject> Roots = new();
        private static string _rejectParticipant;
        private static SceneCompositionScopeKind _rejectScopeKind;
        private static int _remainingRejections;

        [SerializeField]
        private string participantKey;

        internal static IReadOnlyList<GameObject> CreatedRoots => Roots;

        internal void Configure(string key)
        {
            participantKey = key;
        }

        internal static void RejectRelease(
            string participantKey,
            SceneCompositionScopeKind scopeKind,
            int rejectionCount)
        {
            _rejectParticipant = participantKey;
            _rejectScopeKind = scopeKind;
            _remainingRejections = Math.Max(0, rejectionCount);
        }

        internal static IReadOnlyList<Call> CallsFor(SceneCompositionOperation operation)
        {
            var calls = new List<Call>();
            for (int index = 0; index < RecordedCalls.Count; index++)
            {
                if (RecordedCalls[index].Operation == operation)
                {
                    calls.Add(RecordedCalls[index]);
                }
            }

            return calls;
        }

        internal static void ClearRecords()
        {
            RecordedCalls.Clear();
            Roots.Clear();
            _rejectParticipant = string.Empty;
            _rejectScopeKind = SceneCompositionScopeKind.Session;
            _remainingRejections = 0;
        }

        public SceneCompositionResult OnSceneAvailable(
            SceneCompositionScope scope,
            IReadOnlyList<GameObject> roots)
        {
            Record(SceneCompositionOperation.Available, scope, roots);
            return SceneCompositionResult.Completed(
                scope,
                SceneCompositionOperation.Available,
                participantKey + " accepted Session composition.");
        }

        public SceneCompositionResult OnSceneReleasing(
            SceneCompositionScope scope,
            IReadOnlyList<GameObject> roots,
            string reason)
        {
            Record(SceneCompositionOperation.Releasing, scope, roots);
            if (_remainingRejections > 0 &&
                scope.Kind == _rejectScopeKind &&
                string.Equals(participantKey, _rejectParticipant, StringComparison.Ordinal))
            {
                _remainingRejections--;
                return SceneCompositionResult.Rejected(
                    scope,
                    SceneCompositionOperation.Releasing,
                    participantKey + " rejected " + scope.Kind + " release once.");
            }

            return SceneCompositionResult.Completed(
                scope,
                SceneCompositionOperation.Releasing,
                participantKey + " released " + scope.Kind + " composition.");
        }

        private void Record(
            SceneCompositionOperation operation,
            SceneCompositionScope scope,
            IReadOnlyList<GameObject> roots)
        {
            if (!Roots.Contains(gameObject))
            {
                Roots.Add(gameObject);
            }

            RecordedCalls.Add(new Call(participantKey, operation, scope, roots));
        }

        internal sealed class Call
        {
            internal Call(
                string participantKey,
                SceneCompositionOperation operation,
                SceneCompositionScope scope,
                IReadOnlyList<GameObject> roots)
            {
                ParticipantKey = participantKey;
                Operation = operation;
                Scope = scope;
                Roots = roots;
            }

            internal string ParticipantKey { get; }

            internal SceneCompositionOperation Operation { get; }

            internal SceneCompositionScope Scope { get; }

            internal IReadOnlyList<GameObject> Roots { get; }
        }
    }
}
