using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Explicit target-source descriptor used by camera product authoring and diagnostics.
    /// A descriptor may include a logical source id for future runtime scopes, but it must not use object names or hierarchy paths as functional identity.
    /// </summary>
    public readonly struct CameraTargetSourceDescriptor
    {
        public CameraTargetSourceDescriptor(
            CameraTargetSourceKind kind,
            Object sourceObject,
            string logicalSourceId,
            string diagnosticLabel)
        {
            Kind = kind;
            SourceObject = sourceObject;
            LogicalSourceId = logicalSourceId.NormalizeText();
            DiagnosticLabel = diagnosticLabel.NormalizeText();
        }

        public CameraTargetSourceKind Kind { get; }

        public Object SourceObject { get; }

        public string LogicalSourceId { get; }

        public string DiagnosticLabel { get; }

        public bool HasSourceObject => SourceObject != null;

        public bool HasLogicalSourceId => !string.IsNullOrWhiteSpace(LogicalSourceId);

        public bool IsNone => Kind == CameraTargetSourceKind.None;

        public static CameraTargetSourceDescriptor None()
        {
            return new CameraTargetSourceDescriptor(CameraTargetSourceKind.None, null, string.Empty, string.Empty);
        }

        public static CameraTargetSourceDescriptor ExplicitTransform(Transform transform, string diagnosticLabel = "")
        {
            return new CameraTargetSourceDescriptor(CameraTargetSourceKind.ExplicitTransform, transform, string.Empty, diagnosticLabel);
        }

        public static CameraTargetSourceDescriptor PlayerComposer(Object playerComposer, string diagnosticLabel = "")
        {
            return new CameraTargetSourceDescriptor(CameraTargetSourceKind.PlayerComposer, playerComposer, string.Empty, diagnosticLabel);
        }

        public static CameraTargetSourceDescriptor Logical(CameraTargetSourceKind kind, string logicalSourceId, string diagnosticLabel = "")
        {
            return new CameraTargetSourceDescriptor(kind, null, logicalSourceId, diagnosticLabel);
        }
    }
}
