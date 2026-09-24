using System;
using System.Collections.Generic;
using Immersive.Framework.Camera;

namespace Immersive.Framework.CameraAuthoring
{
    /// <summary>Validates an explicit definition scope without resolving assets through IDs.</summary>
    public static class CameraDefinitionValidation
    {
        public static void ValidateOutputs(IEnumerable<CameraOutputDefinition> definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            var identities = new Dictionary<CameraOutputId, CameraOutputDefinition>();
            foreach (var definition in definitions)
            {
                if (definition == null || !definition.HasValidId)
                    throw new InvalidOperationException("Missing or invalid Camera Output definition.");
                if (identities.TryGetValue(definition.OutputId, out var previous) &&
                    !ReferenceEquals(previous, definition))
                    throw new InvalidOperationException(
                        $"Camera Output definition stable ID collision: '{previous.name}' and '{definition.name}' share '{definition.OutputId}'.");
                identities[definition.OutputId] = definition;
            }
        }
    }
}
