using System;
using Immersive.Framework.Common;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.UnityInput
{
    /// <summary>
    /// Stable reference to one InputActionMap inside an InputActionAsset.
    ///
    /// The source asset is authoring evidence. Runtime resolution is performed
    /// against the exact PlayerInput.actions instance by map GUID, so PlayerInput
    /// action-asset copies and map renames remain valid.
    /// </summary>
    [Serializable]
    public struct PlayerInputActionMapReference
    {
        [SerializeField]
        private InputActionAsset actionAsset;

        [SerializeField]
        private string actionMapId;

        [SerializeField]
        private string cachedActionMapName;

        public InputActionAsset ActionAsset => actionAsset;

        public string ActionMapId => actionMapId.NormalizeText();

        public string CachedActionMapName =>
            cachedActionMapName.NormalizeText();

        public bool IsConfigured =>
            actionAsset != null &&
            TryGetMapId(out _);

        public static PlayerInputActionMapReference From(
            InputActionMap actionMap)
        {
            if (actionMap == null ||
                actionMap.asset == null)
            {
                return default;
            }

            return new PlayerInputActionMapReference
            {
                actionAsset = actionMap.asset,
                actionMapId = actionMap.id.ToString("D"),
                cachedActionMapName = actionMap.name.NormalizeText()
            };
        }

        public bool TryResolve(
            out InputActionMap actionMap,
            out string diagnostic)
        {
            return TryResolve(
                actionAsset,
                out actionMap,
                out diagnostic);
        }

        public bool TryResolve(
            InputActionAsset targetAsset,
            out InputActionMap actionMap,
            out string diagnostic)
        {
            actionMap = null;

            if (actionAsset == null)
            {
                diagnostic =
                    "Action Map reference requires an authoring InputActionAsset.";
                return false;
            }

            if (!TryGetMapId(
                    out Guid mapId))
            {
                diagnostic =
                    "Action Map reference requires a valid map GUID.";
                return false;
            }

            InputActionMap sourceMap =
                actionAsset.FindActionMap(
                    mapId);

            if (sourceMap == null)
            {
                diagnostic =
                    $"Action Map GUID '{mapId:D}' is not present in the authoring InputActionAsset '{actionAsset.name}'.";
                return false;
            }

            if (targetAsset == null)
            {
                diagnostic =
                    "Action Map resolution requires a target InputActionAsset.";
                return false;
            }

            actionMap =
                targetAsset.FindActionMap(
                    mapId);

            if (actionMap == null)
            {
                diagnostic =
                    $"Action Map GUID '{mapId:D}' from '{actionAsset.name}' is not present in target InputActionAsset '{targetAsset.name}'. Name fallback is not used.";
                return false;
            }

            diagnostic = string.Empty;
            return true;
        }

        public bool HasSameIdentity(
            PlayerInputActionMapReference other)
        {
            return TryGetMapId(
                    out Guid currentId) &&
                other.TryGetMapId(
                    out Guid otherId) &&
                currentId == otherId;
        }

        public bool HasSameIdentity(
            InputActionMap actionMap)
        {
            return actionMap != null &&
                TryGetMapId(
                    out Guid currentId) &&
                currentId == actionMap.id;
        }

        public string ToDiagnosticString()
        {
            return
                $"asset='{(actionAsset != null ? actionAsset.name : "<none>")}' " +
                $"mapId='{ActionMapId}' " +
                $"cachedName='{CachedActionMapName}'";
        }

        private bool TryGetMapId(
            out Guid mapId)
        {
            return Guid.TryParse(
                ActionMapId,
                out mapId) &&
                mapId != Guid.Empty;
        }
    }
}
