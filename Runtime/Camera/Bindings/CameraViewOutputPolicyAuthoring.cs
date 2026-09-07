using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    [Serializable]
    public sealed class CameraViewOutputBindingAuthoring
    {
        [SerializeField] private string viewId;
        [SerializeField] private string outputId;
        [SerializeField] private Rect viewport = new Rect(0f, 0f, 1f, 1f);

        public string ViewIdText => viewId.NormalizeText();
        public string OutputIdText => outputId.NormalizeText();
        public Rect Viewport => viewport;

        public CameraViewOutputBinding ToBinding() =>
            new CameraViewOutputBinding(
                new CameraViewId(ViewIdText),
                new CameraOutputId(OutputIdText),
                new CameraViewport(viewport.x, viewport.y, viewport.width, viewport.height));

        public void Configure(CameraViewId targetViewId, CameraOutputId targetOutputId, CameraViewport targetViewport)
        {
            viewId = targetViewId.Value;
            outputId = targetOutputId.Value;
            viewport = targetViewport.ToRect();
        }
    }

    /// <summary>Designer-facing explicit View-to-Output and normalized viewport policy.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Camera/Camera View Output Policy")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "CAMERA-026-H explicit Camera composition policy.")]
    public sealed class CameraViewOutputPolicyAuthoring : MonoBehaviour
    {
        [SerializeField] private List<CameraViewOutputBindingAuthoring> bindings =
            new List<CameraViewOutputBindingAuthoring>();

        public IReadOnlyList<CameraViewOutputBindingAuthoring> Bindings => bindings;

        public bool TryBuildTopology(out CameraViewOutputTopology topology, out string diagnostic)
        {
            if (bindings == null || bindings.Count == 0)
            {
                topology = null;
                diagnostic = "Camera View Output Policy requires at least one explicit binding.";
                return false;
            }

            var resolved = new CameraViewOutputBinding[bindings.Count];
            for (int index = 0; index < bindings.Count; index++)
            {
                if (bindings[index] == null)
                {
                    topology = null;
                    diagnostic = $"Camera View Output Policy contains a missing binding at index '{index}'.";
                    return false;
                }
                resolved[index] = bindings[index].ToBinding();
            }
            return CameraViewOutputTopology.TryCreate(resolved, out topology, out diagnostic);
        }

        public void Configure(IReadOnlyList<CameraViewOutputBinding> configuredBindings)
        {
            if (configuredBindings == null) throw new ArgumentNullException(nameof(configuredBindings));
            bindings = new List<CameraViewOutputBindingAuthoring>(configuredBindings.Count);
            for (int index = 0; index < configuredBindings.Count; index++)
            {
                var authored = new CameraViewOutputBindingAuthoring();
                authored.Configure(
                    configuredBindings[index].ViewId,
                    configuredBindings[index].OutputId,
                    configuredBindings[index].Viewport);
                bindings.Add(authored);
            }
        }
    }
}
