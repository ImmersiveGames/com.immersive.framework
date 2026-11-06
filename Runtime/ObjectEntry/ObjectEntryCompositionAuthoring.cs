using System;
using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>
    /// Optional authoring attached to the exact ObjectEntryDeclaration GameObject.
    /// This component does not open a Composer or infer an owner from the scene.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ObjectEntryDeclaration))]
    [AddComponentMenu("Immersive Framework/Object Entry/Composition Authoring")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-014 Amendments 002/003 optional Object Entry composition authoring and exact-occurrence access handoff.")]
    public sealed class ObjectEntryCompositionAuthoring : MonoBehaviour
    {
        [SerializeField] private ObjectEntryCompositionProfile profile;
        [SerializeField] private bool allowDynamicContributions;

        [NonSerialized] private ObjectEntryCompositionAccessGrant compositionAccessGrant;

        public ObjectEntryCompositionProfile Profile => profile;

        public bool AllowsDynamicContributions => allowDynamicContributions;

        /// <summary>Returns a currently valid Runtime Content grant for this exact admitted occurrence.</summary>
        /// <remarks>Call on Unity's main thread. Invalid grants are revoked and cannot become current again.</remarks>
        public bool TryGetCompositionAccessGrant(
            out ObjectEntryCompositionAccessGrant grant,
            out string issue)
        {
            grant = compositionAccessGrant;
            issue = string.Empty;
            if (grant != null && grant.TryValidate(out issue))
            {
                return true;
            }

            if (ReferenceEquals(compositionAccessGrant, grant))
            {
                compositionAccessGrant = null;
            }
            grant = null;

            if (string.IsNullOrEmpty(issue))
            {
                issue = allowDynamicContributions
                    ? "Object Entry composition access is unavailable until the exact owner occurrence commits and its Base Profile, when present, activates."
                    : "Dynamic Object Entry composition is not enabled for this authoring.";
            }
            return false;
        }

        public bool TryValidateConfiguration(out string issue)
        {
            issue = string.Empty;
            ObjectEntryDeclaration declaration = GetComponent<ObjectEntryDeclaration>();
            if (declaration == null || !declaration.TryGetObjectEntryId(out _))
            {
                issue = "Composition authoring requires a valid ObjectEntryDeclaration on the same GameObject.";
                return false;
            }

            if (ReferenceEquals(profile, null))
            {
                if (allowDynamicContributions)
                {
                    return true;
                }

                issue = "Composition authoring requires an explicit Object Entry composition profile unless dynamic contributions are enabled.";
                return false;
            }

            if (profile == null)
            {
                issue = "The configured Object Entry composition profile was destroyed and cannot provide the Base contribution.";
                return false;
            }

            return profile.TryValidate(out issue);
        }

        internal bool TryBindCompositionAccessGrant(
            ObjectEntryCompositionAccessGrant grant,
            out string issue)
        {
            if (grant == null)
            {
                issue = "Runtime Content cannot bind a null Object Entry composition access grant.";
                return false;
            }

            if (compositionAccessGrant != null && !ReferenceEquals(compositionAccessGrant, grant))
            {
                issue = "Object Entry composition authoring is already bound to another occurrence grant.";
                return false;
            }

            compositionAccessGrant = grant;
            issue = string.Empty;
            return true;
        }

        internal void UnbindCompositionAccessGrant(ObjectEntryCompositionAccessGrant grant)
        {
            if (ReferenceEquals(compositionAccessGrant, grant))
            {
                compositionAccessGrant = null;
            }
        }

        internal bool IsBoundToCompositionAccessGrant(ObjectEntryCompositionAccessGrant grant) =>
            ReferenceEquals(compositionAccessGrant, grant);
    }
}
