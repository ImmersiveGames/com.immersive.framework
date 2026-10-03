using System;
using Immersive.Framework.Common;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.Framework.SceneLifecycle
{
    internal enum SceneCompositionScopeKind
    {
        Invalid = 0,
        Scene = 10,
        Session = 20
    }

    internal enum SceneCompositionOperation
    {
        Available = 10,
        Releasing = 20
    }

    /// <summary>
    /// Identifies the owner lifetime for a bounded set of authored roots.
    /// Scene handles and Session owners are distinct scope kinds.
    /// </summary>
    internal readonly struct SceneCompositionScope : IEquatable<SceneCompositionScope>
    {
        private readonly ulong _sceneIdentity;
        private readonly EntityId _sessionOwnerIdentity;
        private readonly Scene _scene;

        private SceneCompositionScope(
            SceneCompositionScopeKind kind,
            ulong sceneIdentity,
            EntityId sessionOwnerIdentity,
            Scene scene)
        {
            Kind = kind;
            _sceneIdentity = sceneIdentity;
            _sessionOwnerIdentity = sessionOwnerIdentity;
            _scene = scene;
        }

        internal SceneCompositionScopeKind Kind { get; }

        internal bool IsValid => Kind switch
        {
            SceneCompositionScopeKind.Scene => _sceneIdentity != 0,
            SceneCompositionScopeKind.Session => !_sessionOwnerIdentity.Equals(default(EntityId)),
            _ => false
        };

        internal Scene Scene => Kind == SceneCompositionScopeKind.Scene ? _scene : default;

        internal string Label => Kind switch
        {
            SceneCompositionScopeKind.Scene => _scene.IsValid()
                ? _scene.name.NormalizeTextOrFallback("<unnamed>")
                : "<invalid-scene>",
            SceneCompositionScopeKind.Session => "Session",
            _ => "<invalid-scope>"
        };

        internal static SceneCompositionScope ForScene(Scene scene)
        {
            if (!scene.IsValid())
            {
                return default;
            }

            return new SceneCompositionScope(
                SceneCompositionScopeKind.Scene,
                scene.handle.GetRawData(),
                default,
                scene);
        }

        internal static SceneCompositionScope ForSession(UnityEngine.Object sessionOwner)
        {
            if (sessionOwner == null)
            {
                return default;
            }

            return new SceneCompositionScope(
                SceneCompositionScopeKind.Session,
                0,
                sessionOwner.GetEntityId(),
                default);
        }

        public bool Equals(SceneCompositionScope other)
        {
            if (Kind != other.Kind) return false;
            return Kind switch
            {
                SceneCompositionScopeKind.Scene => _sceneIdentity == other._sceneIdentity,
                SceneCompositionScopeKind.Session => _sessionOwnerIdentity.Equals(other._sessionOwnerIdentity),
                _ => true
            };
        }

        public override bool Equals(object obj) =>
            obj is SceneCompositionScope other && Equals(other);

        public override int GetHashCode()
        {
            int identityHash = Kind switch
            {
                SceneCompositionScopeKind.Scene => _sceneIdentity.GetHashCode(),
                SceneCompositionScopeKind.Session => _sessionOwnerIdentity.GetHashCode(),
                _ => 0
            };
            return unchecked(((int)Kind * 397) ^ identityHash);
        }
    }

    /// <summary>Common structured outcome for one composition participant and scope.</summary>
    internal readonly struct SceneCompositionResult
    {
        private SceneCompositionResult(
            SceneCompositionScope scope,
            SceneCompositionOperation operation,
            bool succeeded,
            string status,
            string diagnostic)
        {
            Scope = scope;
            Operation = operation;
            Succeeded = succeeded;
            Status = status ?? string.Empty;
            Diagnostic = diagnostic.NormalizeText();
        }

        internal SceneCompositionScope Scope { get; }

        internal SceneCompositionOperation Operation { get; }

        internal bool Succeeded { get; }

        internal string Status { get; }

        internal string Diagnostic { get; }

        internal static SceneCompositionResult Completed(
            SceneCompositionScope scope,
            SceneCompositionOperation operation,
            string diagnostic) =>
            new SceneCompositionResult(scope, operation, true, "Completed", diagnostic);

        internal static SceneCompositionResult Rejected(
            SceneCompositionScope scope,
            SceneCompositionOperation operation,
            string diagnostic) =>
            new SceneCompositionResult(scope, operation, false, "Rejected", diagnostic);
    }
}
