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
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-014 Amendment 002 optional per-declaration composition authoring.")]
    public sealed class ObjectEntryCompositionAuthoring : MonoBehaviour
    {
        [SerializeField] private ObjectEntryCompositionProfile profile;

        public ObjectEntryCompositionProfile Profile => profile;

        public bool TryValidateConfiguration(out string issue)
        {
            issue = string.Empty;
            ObjectEntryDeclaration declaration = GetComponent<ObjectEntryDeclaration>();
            if (declaration == null || !declaration.TryGetObjectEntryId(out _))
            {
                issue = "Composition authoring requires a valid ObjectEntryDeclaration on the same GameObject.";
                return false;
            }

            if (profile == null)
            {
                issue = "Composition authoring requires an explicit Object Entry composition profile.";
                return false;
            }

            return profile.TryValidate(out issue);
        }
    }
}
