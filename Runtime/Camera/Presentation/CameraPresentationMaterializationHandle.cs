using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Internal ownership handle for one materialized Camera Presentation occurrence.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "CAMERA-032-B materialized Camera Presentation occurrence handle.")]
    internal sealed class CameraPresentationMaterializationHandle
    {
        private bool _released;

        internal CameraPresentationMaterializationHandle(
            CameraPresentationDefinition definition,
            RuntimeScopeContext scopeContext,
            RuntimeMaterializationRequest runtimeContentRequest,
            RuntimeContentHandle runtimeContentHandle,
            GameObject rigRoot,
            CameraRigComposer rigComposer,
            CameraPresentationRuntime presentationRuntime)
        {
            Definition = definition ??
                throw new ArgumentNullException(nameof(definition));
            if (!scopeContext.IsValid)
            {
                throw new ArgumentException(
                    "Camera Presentation materialization handle requires a valid scope context.",
                    nameof(scopeContext));
            }

            if (!runtimeContentRequest.IsValid)
            {
                throw new ArgumentException(
                    "Camera Presentation materialization handle requires a valid RuntimeContent request.",
                    nameof(runtimeContentRequest));
            }

            RuntimeContentHandle = runtimeContentHandle ??
                throw new ArgumentNullException(nameof(runtimeContentHandle));
            RigRoot = rigRoot ??
                throw new ArgumentNullException(nameof(rigRoot));
            RigComposer = rigComposer ??
                throw new ArgumentNullException(nameof(rigComposer));
            PresentationRuntime = presentationRuntime ??
                throw new ArgumentNullException(nameof(presentationRuntime));

            ScopeContext = scopeContext;
            RuntimeContentRequest = runtimeContentRequest;
        }

        internal CameraPresentationDefinition Definition { get; }

        internal CameraPresentationId PresentationId =>
            Definition.PresentationId;

        internal RuntimeScopeContext ScopeContext { get; }

        internal RuntimeMaterializationRequest RuntimeContentRequest { get; }

        internal RuntimeContentHandle RuntimeContentHandle { get; }

        internal RuntimeContentIdentity RuntimeContentIdentity =>
            RuntimeContentRequest.Identity;

        internal GameObject RigRoot { get; }

        internal CameraRigComposer RigComposer { get; }

        internal CameraPresentationRuntime PresentationRuntime { get; }

        internal bool IsReleased => _released;

        internal void MarkReleased()
        {
            _released = true;
        }
    }
}
