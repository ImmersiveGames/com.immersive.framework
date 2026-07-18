using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Explicit UIGlobal registration point for the Session Local Player provisioning endpoint.
    /// It carries no provisioning behavior and is resolved only through the GameApplication-owned
    /// UIGlobal composition root.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu(
        "Immersive Framework/Player/Local Player Provisioning Host Registration")]
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "Explicit UIGlobal registration for Local Player provisioning bootstrap composition.")]
    public sealed class LocalPlayerProvisioningHostRegistration : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Explicit Local Player provisioning authoring owned by this Game Application's UIGlobal composition root.")]
        private LocalPlayerProvisioningAuthoring provisioningAuthoring;

        public LocalPlayerProvisioningAuthoring ProvisioningAuthoring =>
            provisioningAuthoring;

        public bool TryResolveAuthoring(
            out LocalPlayerProvisioningAuthoring authoring,
            out string issue)
        {
            authoring = provisioningAuthoring;
            if (authoring == null)
            {
                issue =
                    "Local Player Provisioning Host Registration requires an explicit LocalPlayerProvisioningAuthoring reference.";
                return false;
            }

            if (authoring.gameObject == null ||
                !authoring.gameObject.scene.IsValid() ||
                !authoring.gameObject.scene.isLoaded)
            {
                authoring = null;
                issue =
                    "Registered Local Player Provisioning Authoring is not a loaded scene object.";
                return false;
            }

            issue = string.Empty;
            return true;
        }
    }
}
