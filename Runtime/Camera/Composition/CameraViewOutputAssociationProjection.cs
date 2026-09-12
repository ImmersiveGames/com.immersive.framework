using System;
using System.Collections.Generic;
using Immersive.Framework.CameraAuthoring;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Projects authored View-to-Output associations into the existing Session
    /// CameraViewOutputTopology. Simple Shared Camera Composition associations and
    /// an optional advanced policy contribute equally; there is no precedence.
    /// </summary>
    internal static class CameraViewOutputAssociationProjection
    {
        internal static bool TryCreate(
            IReadOnlyList<CameraSharedComposition> compositions,
            IReadOnlyList<CameraViewOutputPolicyAuthoring> policies,
            IReadOnlyList<CameraOutputAuthoring> physicalOutputs,
            out CameraViewOutputTopology topology,
            out IReadOnlyList<CameraViewDefinition> viewDefinitions,
            out string diagnostic)
        {
            topology = null;
            viewDefinitions = Array.Empty<CameraViewDefinition>();
            compositions ??= Array.Empty<CameraSharedComposition>();
            policies ??= Array.Empty<CameraViewOutputPolicyAuthoring>();
            physicalOutputs ??= Array.Empty<CameraOutputAuthoring>();

            CameraViewOutputPolicyAuthoring policy = null;
            int policyCount = 0;
            for (int index = 0; index < policies.Count; index++)
            {
                if (policies[index] == null) continue;
                policyCount++;
                policy = policies[index];
            }
            if (policyCount > 1)
            {
                diagnostic =
                    $"Persistent Content requires at most one Camera View Output Policy, but found '{policyCount}'.";
                return false;
            }

            var views = new List<CameraViewDefinition>();
            var associatedOutputs = new List<CameraOutputDefinition>();
            for (int index = 0; index < compositions.Count; index++)
            {
                CameraSharedComposition composition = compositions[index];
                if (composition == null)
                {
                    diagnostic = $"Shared Camera composition is missing at index '{index}'.";
                    return false;
                }
                views.Add(composition.ViewDefinition);
                associatedOutputs.Add(composition.OutputDefinition);
            }

            if (policy != null)
            {
                if (policy.Bindings == null || policy.Bindings.Count == 0)
                {
                    diagnostic = "Camera View Output Policy requires at least one explicit binding.";
                    return false;
                }
                for (int index = 0; index < policy.Bindings.Count; index++)
                {
                    CameraViewOutputBindingAuthoring authored = policy.Bindings[index];
                    if (authored == null)
                    {
                        diagnostic =
                            $"Camera View Output Policy contains a missing binding at index '{index}'.";
                        return false;
                    }
                    views.Add(authored.ViewDefinition);
                    associatedOutputs.Add(authored.OutputDefinition);
                }
            }

            try
            {
                CameraDefinitionValidation.ValidateViews(views);
                CameraDefinitionValidation.ValidateOutputs(associatedOutputs);
            }
            catch (InvalidOperationException exception)
            {
                diagnostic = exception.Message;
                return false;
            }

            var projected = new List<CameraViewOutputBinding>();
            for (int index = 0; index < compositions.Count; index++)
            {
                if (!compositions[index].TryCreateAssociationBinding(
                        out CameraViewOutputBinding binding,
                        out diagnostic))
                    return false;
                projected.Add(binding);
            }

            if (policy != null)
            {
                for (int index = 0; index < policy.Bindings.Count; index++)
                    projected.Add(policy.Bindings[index].ToBinding());
            }

            if (projected.Count == 0)
            {
                diagnostic =
                    "Persistent Content requires at least one explicit Camera View-to-Output association.";
                return false;
            }

            if (!CameraViewOutputTopology.TryCreate(projected, out topology, out diagnostic))
                return false;

            int physicalCount = 0;
            for (int index = 0; index < physicalOutputs.Count; index++)
            {
                if (physicalOutputs[index] == null)
                {
                    diagnostic =
                        $"Camera Output topology contains a missing authoring reference at index '{index}'.";
                    topology = null;
                    return false;
                }
                physicalCount++;
            }

            if (topology.BindingCount != physicalCount)
            {
                diagnostic =
                    $"Camera View-to-Output policy must bind every active Output exactly once. outputs='{physicalCount}' bindings='{topology.BindingCount}'.";
                topology = null;
                return false;
            }

            for (int index = 0; index < associatedOutputs.Count; index++)
            {
                CameraOutputDefinition definition = associatedOutputs[index];
                bool exact = false;
                for (int physicalIndex = 0; physicalIndex < physicalOutputs.Count; physicalIndex++)
                {
                    CameraOutputAuthoring physical = physicalOutputs[physicalIndex];
                    if (physical != null && ReferenceEquals(physical.OutputDefinition, definition))
                        exact = true;
                }
                if (!exact)
                {
                    diagnostic =
                        "Camera association Output definition has no exact physical Output in this composition.";
                    topology = null;
                    return false;
                }
            }

            viewDefinitions = views.AsReadOnly();
            diagnostic = string.Empty;
            return true;
        }
    }
}
