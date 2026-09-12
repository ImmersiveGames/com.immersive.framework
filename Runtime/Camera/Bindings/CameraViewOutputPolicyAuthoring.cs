using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.CameraAuthoring;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    [Serializable]
    public sealed class CameraViewOutputBindingAuthoring
    {
        [SerializeField] private CameraViewDefinition viewDefinition;
        [SerializeField] private CameraOutputDefinition outputDefinition;
        [SerializeField] private Rect viewport = new Rect(0f, 0f, 1f, 1f);

        public CameraViewDefinition ViewDefinition => viewDefinition;
        public string ViewIdText => viewDefinition != null && viewDefinition.HasValidId ? viewDefinition.ViewId.Value : string.Empty;
        public CameraOutputDefinition OutputDefinition => outputDefinition;
        public string OutputIdText => outputDefinition != null && outputDefinition.HasValidId ? outputDefinition.OutputId.Value : string.Empty;
        public Rect Viewport => viewport;

        public CameraViewOutputBinding ToBinding() =>
            new CameraViewOutputBinding(
                viewDefinition.ViewId,
                outputDefinition.OutputId,
                new CameraViewport(viewport.x, viewport.y, viewport.width, viewport.height));

        public void Configure(CameraViewDefinition view, CameraOutputDefinition output, CameraViewport targetViewport)
        {
            viewDefinition = view;
            outputDefinition = output;
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

        public IReadOnlyList<CameraViewDefinition> ViewDefinitions
        {
            get
            {
                var result = new List<CameraViewDefinition>();
                if (bindings != null)
                    foreach (var binding in bindings) result.Add(binding?.ViewDefinition);
                return result.AsReadOnly();
            }
        }

        public bool TryValidateOutputs(IReadOnlyList<CameraOutputAuthoring> outputs, out string diagnostic)
        {
            if (outputs == null || bindings == null)
            {
                diagnostic = "Camera policy requires explicit Output definitions and physical Outputs.";
                return false;
            }
            foreach (var binding in bindings)
            {
                bool exact = false;
                if (binding?.OutputDefinition != null && binding.OutputDefinition.HasValidId)
                    foreach (var output in outputs)
                        if (output != null && ReferenceEquals(output.OutputDefinition, binding.OutputDefinition))
                            exact = true;
                if (!exact)
                {
                    diagnostic = "Camera policy Output definition has no exact physical Output in this composition.";
                    return false;
                }
            }
            diagnostic = string.Empty;
            return true;
        }

        public bool TryBuildTopology(out CameraViewOutputTopology topology, out string diagnostic)
        {
            if (bindings == null || bindings.Count == 0)
            {
                topology = null;
                diagnostic = "Camera View Output Policy requires at least one explicit binding.";
                return false;
            }

            var views = new List<CameraViewDefinition>();
            var outputs = new List<CameraOutputDefinition>();
            foreach (var binding in bindings)
            {
                views.Add(binding?.ViewDefinition);
                outputs.Add(binding?.OutputDefinition);
            }
            try
            {
                CameraDefinitionValidation.ValidateViews(views);
                CameraDefinitionValidation.ValidateOutputs(outputs);
            }
            catch (InvalidOperationException exception)
            {
                topology = null;
                diagnostic = exception.Message;
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

        public void Configure(IReadOnlyList<CameraViewOutputBindingAuthoring> configuredBindings)
        {
            if (configuredBindings == null) throw new ArgumentNullException(nameof(configuredBindings));
            bindings = new List<CameraViewOutputBindingAuthoring>(configuredBindings);
        }
    }
}
