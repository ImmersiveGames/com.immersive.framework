using System;
using UnityEngine;

namespace Immersive.Framework.Reset.Tests
{
    [ExecuteAlways]
    public sealed class ObjectEntryCompositionDisableProbe : MonoBehaviour
    {
        public static Action<GameObject> Enabled;
        public static Action<GameObject> Disabled;

        private void OnEnable() => Enabled?.Invoke(gameObject);

        private void OnDisable() => Disabled?.Invoke(gameObject);
    }
}
