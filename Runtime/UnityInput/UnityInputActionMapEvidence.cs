
using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine.InputSystem;

namespace Immersive.Framework.UnityInput
{
    /// <summary>
    /// API status: Experimental. Passive evidence snapshot for action maps available on a Unity InputActionAsset.
    /// It never switches action maps, enables input, disables input or owns PlayerInput.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32B Unity Input action map evidence snapshot.")]
    public sealed class UnityInputActionMapEvidence
    {
        private readonly UnityInputActionMapName[] _actionMaps;

        private UnityInputActionMapEvidence(UnityInputActionMapName[] actionMaps, bool hasActionAsset, string source, string reason)
        {
            _actionMaps = actionMaps ?? Array.Empty<UnityInputActionMapName>();
            HasActionAsset = hasActionAsset;
            Source = source.NormalizeTextOrFallback(nameof(UnityInputActionMapEvidence));
            Reason = reason.NormalizeText();
        }

        public IReadOnlyList<UnityInputActionMapName> ActionMaps => _actionMaps;

        public int ActionMapCount => _actionMaps.Length;

        public bool HasActionAsset { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool SwitchesActionMaps => false;

        public bool AppliesInputBehavior => false;

        public bool ActivatesPlayerInput => false;

        public bool Contains(UnityInputActionMapName actionMap)
        {
            if (!actionMap.IsValid)
            {
                return false;
            }

            for (int i = 0; i < _actionMaps.Length; i++)
            {
                if (_actionMaps[i] == actionMap)
                {
                    return true;
                }
            }

            return false;
        }

        public string ToDiagnosticString()
        {
            var builder = new StringBuilder();
            builder.Append("hasActionAsset='").Append(HasActionAsset).Append("'");
            builder.Append(" actionMaps='").Append(ActionMapCount).Append("'");
            builder.Append(" actionMapSwitching='").Append(SwitchesActionMaps).Append("'");
            builder.Append(" inputBehavior='").Append(AppliesInputBehavior).Append("'");
            builder.Append(" playerInputActivation='").Append(ActivatesPlayerInput).Append("'");
            for (int i = 0; i < _actionMaps.Length; i++)
            {
                builder.Append(" actionMap[").Append(i).Append("]='").Append(_actionMaps[i]).Append("'");
            }

            return builder.ToString();
        }

        public static UnityInputActionMapEvidence FromActionMapNames(
            IEnumerable<string> actionMapNames,
            string source,
            string reason)
        {
            return FromActionMapNames(actionMapNames, true, source, reason);
        }

        public static UnityInputActionMapEvidence MissingActionAsset(string source, string reason)
        {
            return new UnityInputActionMapEvidence(Array.Empty<UnityInputActionMapName>(), false, source, reason);
        }

        public static UnityInputActionMapEvidence FromInputActionAsset(
            InputActionAsset actionAsset,
            string source,
            string reason)
        {
            if (actionAsset == null)
            {
                return MissingActionAsset(source, reason);
            }

            var names = new List<string>();
            foreach (InputActionMap actionMap in actionAsset.actionMaps)
            {
                if (actionMap != null)
                {
                    names.Add(actionMap.name);
                }
            }

            return FromActionMapNames(names, true, source, reason);
        }

        private static UnityInputActionMapEvidence FromActionMapNames(
            IEnumerable<string> actionMapNames,
            bool hasActionAsset,
            string source,
            string reason)
        {
            var names = new List<UnityInputActionMapName>();
            if (actionMapNames != null)
            {
                foreach (string actionMapName in actionMapNames)
                {
                    UnityInputActionMapName name = UnityInputActionMapName.From(actionMapName);
                    if (!name.IsValid || ContainsName(names, name))
                    {
                        continue;
                    }

                    names.Add(name);
                }
            }

            return new UnityInputActionMapEvidence(names.ToArray(), hasActionAsset, source, reason);
        }

        private static bool ContainsName(IReadOnlyList<UnityInputActionMapName> names, UnityInputActionMapName name)
        {
            for (int i = 0; i < names.Count; i++)
            {
                if (names[i] == name)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
