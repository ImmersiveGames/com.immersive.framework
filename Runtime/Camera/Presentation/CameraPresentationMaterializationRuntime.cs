using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Common;
using Immersive.Framework.RuntimeContent;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Materializes and releases Camera Presentation Rig prefab occurrences under
    /// explicit RuntimeContent ownership.
    ///
    /// This is not a global Camera manager. A caller supplies the exact
    /// RuntimeScopeContext and CameraPresentationDefinition for every occurrence.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "CAMERA-032-B RuntimeContent-backed Camera Presentation Rig materialization.")]
    internal sealed class CameraPresentationMaterializationRuntime
    {
        internal const string ResourceType = "CameraPresentationPrefab";

        private readonly RuntimeContentRuntime _runtimeContentRuntime;

        internal CameraPresentationMaterializationRuntime(
            RuntimeContentRuntime runtimeContentRuntime)
        {
            _runtimeContentRuntime = runtimeContentRuntime ??
                throw new ArgumentNullException(nameof(runtimeContentRuntime));
        }

        internal CameraPresentationMaterializationResult Materialize(
            RuntimeScopeContext scopeContext,
            CameraPresentationDefinition definition,
            Transform physicalParent,
            string source,
            string reason)
        {
            string resolvedSource =
                source.NormalizeTextOrFallback(
                    nameof(CameraPresentationMaterializationRuntime));
            string resolvedReason =
                reason.NormalizeTextOrFallback(
                    "camera-presentation-materialize");

            if (!scopeContext.IsValid)
            {
                return CameraPresentationMaterializationResult.Failure(
                    CameraPresentationMaterializationStatus
                        .RejectedInvalidContext,
                    "Camera Presentation materialization requires a valid RuntimeContent scope context.");
            }

            if (definition == null ||
                !definition.TryValidate(out string definitionIssue))
            {
                return CameraPresentationMaterializationResult.Failure(
                    CameraPresentationMaterializationStatus
                        .RejectedInvalidDefinition,
                    definitionIssue.NormalizeTextOrFallback(
                        "Camera Presentation definition is missing or invalid."));
            }

            RuntimeContentId contentId = CreateContentId(definition);
            var resource = new RuntimeMaterializationResource(
                ResourceType,
                definition.PresentationId.Value,
                definition.name,
                string.Empty);

            if (!_runtimeContentRuntime.TryCreateMaterializationRequest(
                    scopeContext,
                    contentId,
                    resource,
                    resolvedSource,
                    resolvedReason,
                    out RuntimeMaterializationRequest runtimeRequest,
                    out RuntimeScopeTransitionGuardResult guardResult))
            {
                return CameraPresentationMaterializationResult.Failure(
                    CameraPresentationMaterializationStatus
                        .RejectedScopeTransition,
                    guardResult.Message);
            }

            if (_runtimeContentRuntime.TryGetHandle(
                    scopeContext,
                    runtimeRequest.Identity,
                    out _))
            {
                return CameraPresentationMaterializationResult.Failure(
                    CameraPresentationMaterializationStatus
                        .RejectedDuplicateOccurrence,
                    $"Camera Presentation '{definition.name}' is already materialized for owner '{scopeContext.Owner.StableText}'.");
            }

            GameObject stagingRoot = null;
            GameObject rigRoot = null;
            try
            {
                stagingRoot = new GameObject(
                    $"[{runtimeRequest.Identity.StableText}] Camera Presentation Staging");
                stagingRoot.SetActive(false);
                if (physicalParent != null)
                {
                    stagingRoot.transform.SetParent(
                        physicalParent,
                        false);
                }

                rigRoot = Object.Instantiate(
                    definition.RigPrefab,
                    stagingRoot.transform,
                    false);
                if (rigRoot == null)
                {
                    return CameraPresentationMaterializationResult.Failure(
                        CameraPresentationMaterializationStatus
                            .FailedInstantiate,
                        $"Camera Presentation '{definition.name}' Rig Prefab instantiation returned null.");
                }

                rigRoot.SetActive(false);

                if (!TryResolveRig(
                        rigRoot,
                        out CameraRigComposer rigComposer,
                        out string rigIssue))
                {
                    DestroyObject(rigRoot);
                    rigRoot = null;
                    return CameraPresentationMaterializationResult.Failure(
                        CameraPresentationMaterializationStatus
                            .FailedInvalidRigInstance,
                        rigIssue);
                }

                rigRoot.name =
                    $"Camera Presentation [{definition.PresentationId.Value}]";
                rigRoot.transform.SetParent(
                    physicalParent,
                    false);

                DestroyObject(stagingRoot);
                stagingRoot = null;

                // A materialized Presentation Rig is not physically participating
                // until CameraOutputRigApplicator selects its admitted request.
                rigComposer.CinemachineCamera.enabled = false;
                rigRoot.SetActive(true);

                var runtimeHandle = RuntimeContentHandle.Materialized(
                    runtimeRequest.Identity,
                    resolvedSource,
                    resolvedReason);
                RuntimeMaterializationResult applied =
                    _runtimeContentRuntime.ApplyMaterializationResult(
                        RuntimeMaterializationResult.Success(
                            runtimeRequest,
                            runtimeHandle,
                            resolvedSource,
                            resolvedReason,
                            "Camera Presentation Rig prefab materialized."),
                        resolvedSource,
                        resolvedReason);

                if (!applied.Succeeded)
                {
                    DestroyObject(rigRoot);
                    rigRoot = null;
                    return CameraPresentationMaterializationResult.Failure(
                        CameraPresentationMaterializationStatus
                            .FailedRuntimeContentRegistration,
                        applied.Message);
                }

                var presentationRuntime =
                    new CameraPresentationRuntime(definition.name);
                presentationRuntime.Configure(
                    definition.OutputDefinition,
                    definition.SubjectPolicy,
                    rigComposer,
                    definition.RequestPrecedence);

                var handle =
                    new CameraPresentationMaterializationHandle(
                        definition,
                        scopeContext,
                        runtimeRequest,
                        runtimeHandle,
                        rigRoot,
                        rigComposer,
                        presentationRuntime);

                return CameraPresentationMaterializationResult.Success(handle);
            }
            catch (Exception exception)
            {
                if (rigRoot != null)
                {
                    DestroyObject(rigRoot);
                }

                return CameraPresentationMaterializationResult.Failure(
                    CameraPresentationMaterializationStatus.FailedInstantiate,
                    $"Camera Presentation materialization failed. exception='{exception.GetType().Name.ToDiagnosticText()}' message='{exception.Message.ToDiagnosticText()}'.");
            }
            finally
            {
                if (stagingRoot != null)
                {
                    DestroyObject(stagingRoot);
                }
            }
        }

        internal CameraPresentationMaterializationResult Release(
            CameraPresentationMaterializationHandle handle,
            string source,
            string reason)
        {
            string resolvedSource =
                source.NormalizeTextOrFallback(
                    nameof(CameraPresentationMaterializationRuntime));
            string resolvedReason =
                reason.NormalizeTextOrFallback(
                    "camera-presentation-release");

            if (handle == null)
            {
                return CameraPresentationMaterializationResult.Failure(
                    CameraPresentationMaterializationStatus
                        .RejectedInvalidContext,
                    "Camera Presentation release requires an explicit materialization handle.");
            }

            if (handle.IsReleased)
            {
                return CameraPresentationMaterializationResult
                    .AlreadyReleased(handle);
            }

            if (!handle.PresentationRuntime.StopPresentation())
            {
                return CameraPresentationMaterializationResult.Failure(
                    CameraPresentationMaterializationStatus
                        .FailedPresentationTeardown,
                    handle.PresentationRuntime.Snapshot.LastBlockingIssue
                        .NormalizeTextOrFallback(
                            "Camera Presentation runtime teardown failed."),
                    handle);
            }

            RuntimeReleaseResult logicalRelease =
                _runtimeContentRuntime.ReleaseHandleLogically(
                    handle.ScopeContext,
                    handle.RuntimeContentIdentity,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    resolvedSource,
                    resolvedReason);

            if (!logicalRelease.Succeeded)
            {
                return CameraPresentationMaterializationResult.Failure(
                    CameraPresentationMaterializationStatus
                        .FailedRuntimeContentRelease,
                    logicalRelease.Message,
                    handle);
            }

            handle.PresentationRuntime.SetEnabled(false);
            handle.PresentationRuntime.Dispose();

            if (handle.RigRoot != null)
            {
                handle.RigRoot.SetActive(false);
                DestroyObject(handle.RigRoot);
            }

            handle.MarkReleased();
            return CameraPresentationMaterializationResult.Success(handle);
        }

        internal static RuntimeContentId CreateContentId(
            CameraPresentationDefinition definition)
        {
            if (definition == null || !definition.HasValidId)
            {
                throw new ArgumentException(
                    "Camera Presentation RuntimeContent identity requires a valid definition.",
                    nameof(definition));
            }

            return RuntimeContentId.From(
                $"camera-presentation:{definition.PresentationId.Value}");
        }

        private static bool TryResolveRig(
            GameObject rigRoot,
            out CameraRigComposer rigComposer,
            out string issue)
        {
            rigComposer = null;

            if (rigRoot == null)
            {
                issue =
                    "Camera Presentation materialization requires a created Rig root.";
                return false;
            }

            CameraRigComposer[] composers =
                rigRoot.GetComponentsInChildren<CameraRigComposer>(true);
            if (composers.Length != 1 || composers[0] == null)
            {
                issue =
                    $"Materialized Camera Presentation Rig must contain exactly one CameraRigComposer. Found '{composers.Length}'.";
                return false;
            }

            if (rigRoot.GetComponentInChildren<CameraOutputAuthoring>(true) != null)
            {
                issue =
                    "Materialized Camera Presentation Rig must not contain CameraOutputAuthoring.";
                return false;
            }

            if (rigRoot.GetComponentInChildren<CameraSharedComposition>(true) != null)
            {
                issue =
                    "Materialized Camera Presentation Rig must not contain CameraSharedComposition.";
                return false;
            }

            rigComposer = composers[0];
            if (!rigComposer.TryValidateForApply(out issue))
            {
                issue =
                    $"Materialized Camera Presentation Rig has invalid CameraRigComposer configuration. {issue}";
                rigComposer = null;
                return false;
            }

            if (rigComposer.CinemachineCamera == null)
            {
                issue =
                    "Materialized Camera Presentation Rig requires an existing Apply/Rebuild-generated CinemachineCamera.";
                rigComposer = null;
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private static void DestroyObject(Object value)
        {
            if (value == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(value);
            }
            else
            {
                Object.DestroyImmediate(value);
            }
        }
    }
}
