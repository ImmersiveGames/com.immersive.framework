using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.RuntimeContent;
using Immersive.Logging.Records;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Bridges Game Flow lifecycle ownership to materialized Camera Presentation
    /// occurrences. Route and Activity scopes remain the lifecycle authority;
    /// Camera only owns the occurrences registered under those existing scopes.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "CAMERA-032-C Route/Activity Camera Presentation lifecycle bridge.")]
    internal sealed class CameraPresentationLifecycleRuntime : IDisposable
    {
        private readonly CameraPresentationMaterializationRuntime _materializer;
        private readonly CameraOutputSessionTopology _outputTopology;
        private readonly CameraSubjectAvailabilityContext _availability;
        private readonly Transform _physicalParent;
        private readonly FrameworkLogger _logger =
            FrameworkLogger.Create<CameraPresentationLifecycleRuntime>();
        private readonly Dictionary<Object, List<CameraPresentationMaterializationHandle>>
            _activeByOwner =
                new Dictionary<Object, List<CameraPresentationMaterializationHandle>>();

        private bool _disposed;

        internal CameraPresentationLifecycleRuntime(
            CameraPresentationMaterializationRuntime materializer,
            CameraOutputSessionTopology outputTopology,
            CameraSubjectAvailabilityContext availability,
            Transform physicalParent)
        {
            _materializer = materializer ??
                throw new ArgumentNullException(nameof(materializer));
            _outputTopology = outputTopology ??
                throw new ArgumentNullException(nameof(outputTopology));
            _availability = availability ??
                throw new ArgumentNullException(nameof(availability));
            _physicalParent = physicalParent;
        }

        internal int ActiveOwnerCount => _activeByOwner.Count;

        internal bool TryEnterRoute(
            RouteAsset route,
            RuntimeScopeContext context,
            string source,
            string reason,
            out string issue)
        {
            if (route == null)
            {
                issue = "Route Camera Presentation entry requires a Route.";
                return false;
            }

            return TryEnter(
                route,
                "Route",
                route.RouteName,
                route.CameraPresentations,
                context,
                source,
                reason,
                out issue);
        }

        internal bool TryExitRoute(
            RouteAsset route,
            string source,
            string reason,
            out string issue)
        {
            return TryExit(
                route,
                "Route",
                route != null ? route.RouteName : string.Empty,
                source,
                reason,
                out issue);
        }

        internal bool TryEnterActivity(
            ActivityAsset activity,
            RuntimeScopeContext context,
            string source,
            string reason,
            out string issue)
        {
            if (activity == null)
            {
                issue = "Activity Camera Presentation entry requires an Activity.";
                return false;
            }

            return TryEnter(
                activity,
                "Activity",
                activity.ActivityName,
                activity.CameraPresentations,
                context,
                source,
                reason,
                out issue);
        }

        internal bool TryExitActivity(
            ActivityAsset activity,
            string source,
            string reason,
            out string issue)
        {
            return TryExit(
                activity,
                "Activity",
                activity != null ? activity.ActivityName : string.Empty,
                source,
                reason,
                out issue);
        }

        private bool TryEnter(
            Object owner,
            string ownerKind,
            string ownerName,
            IReadOnlyList<CameraPresentationDefinition> definitions,
            RuntimeScopeContext context,
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            if (_disposed)
            {
                issue = "Camera Presentation lifecycle is already disposed.";
                return false;
            }

            if (owner == null)
            {
                issue = $"{ownerKind} Camera Presentation entry requires an exact authored owner.";
                return false;
            }

            if (definitions == null || definitions.Count == 0)
            {
                return true;
            }

            if (!context.IsValid)
            {
                issue =
                    $"{ownerKind} Camera Presentations require the active RuntimeContent scope context.";
                return false;
            }

            if (_activeByOwner.ContainsKey(owner))
            {
                issue =
                    $"{ownerKind} '{ownerName}' already owns materialized Camera Presentations for the current occurrence.";
                return false;
            }

            if (!TryValidateDefinitions(
                    ownerKind,
                    ownerName,
                    definitions,
                    out issue))
            {
                return false;
            }

            var created =
                new List<CameraPresentationMaterializationHandle>(
                    definitions.Count);

            for (int index = 0; index < definitions.Count; index++)
            {
                CameraPresentationDefinition definition =
                    definitions[index];

                CameraPresentationMaterializationResult materialized =
                    _materializer.Materialize(
                        context,
                        definition,
                        _physicalParent,
                        source,
                        reason);
                if (!materialized.Succeeded ||
                    materialized.Handle == null)
                {
                    issue =
                        $"{ownerKind} '{ownerName}' Camera Presentation '{definition.name}' materialization failed. {materialized.Issue}";
                    RollbackCreated(created, source, reason);
                    return false;
                }

                CameraPresentationMaterializationHandle handle =
                    materialized.Handle;
                created.Add(handle);

                if (!_outputTopology.TryGetOutput(
                        definition.OutputDefinition.OutputId,
                        out CameraOutputAuthoring output,
                        out string outputIssue))
                {
                    issue =
                        $"{ownerKind} '{ownerName}' Camera Presentation '{definition.name}' targets an unavailable Session Output. {outputIssue}";
                    RollbackCreated(created, source, reason);
                    return false;
                }

                try
                {
                    handle.PresentationRuntime.AttachOutputSession(output);
                    handle.PresentationRuntime
                        .AttachCameraSubjectAvailability(_availability);
                    handle.PresentationRuntime.SetEnabled(true);
                }
                catch (Exception exception)
                {
                    issue =
                        $"{ownerKind} '{ownerName}' Camera Presentation '{definition.name}' activation failed. {exception.Message}";
                    RollbackCreated(created, source, reason);
                    return false;
                }

                _logger.Debug(
                    $"{ownerKind} Camera Presentation materialized.",
                    LogFields.Field("owner", ownerName),
                    LogFields.Field("presentation", definition.name),
                    LogFields.Field(
                        "presentationId",
                        definition.PresentationId.Value),
                    LogFields.Field(
                        "output",
                        definition.OutputDefinition.OutputId.Value),
                    LogFields.Field(
                        "transitionMode",
                        definition.TransitionMode),
                    LogFields.Field(
                        "requestPrecedence",
                        definition.RequestPrecedence),
                    LogFields.Field(
                        "runtimeScope",
                        context.Scope));
            }

            _activeByOwner.Add(owner, created);
            return true;
        }

        private bool TryExit(
            Object owner,
            string ownerKind,
            string ownerName,
            string source,
            string reason,
            out string issue)
        {
            issue = string.Empty;
            if (owner == null)
            {
                return true;
            }

            if (!_activeByOwner.TryGetValue(
                    owner,
                    out List<CameraPresentationMaterializationHandle> handles))
            {
                return true;
            }

            for (int index = handles.Count - 1; index >= 0; index--)
            {
                CameraPresentationMaterializationHandle handle =
                    handles[index];
                CameraPresentationMaterializationResult released =
                    _materializer.Release(
                        handle,
                        source,
                        reason);
                if (!released.Succeeded)
                {
                    issue =
                        $"{ownerKind} '{ownerName}' Camera Presentation release failed. presentation='{handle.Definition?.name ?? "<unknown>"}' issue='{released.Issue}'.";
                    _logger.Warning(
                        $"{ownerKind} Camera Presentation release failed.",
                        LogFields.Field("owner", ownerName),
                        LogFields.Field(
                            "presentation",
                            handle.Definition != null
                                ? handle.Definition.name
                                : "<unknown>"),
                        LogFields.Field("issue", released.Issue));
                    return false;
                }

                _logger.Debug(
                    $"{ownerKind} Camera Presentation released.",
                    LogFields.Field("owner", ownerName),
                    LogFields.Field(
                        "presentation",
                        handle.Definition != null
                            ? handle.Definition.name
                            : "<unknown>"));
            }

            _activeByOwner.Remove(owner);
            return true;
        }

        private static bool TryValidateDefinitions(
            string ownerKind,
            string ownerName,
            IReadOnlyList<CameraPresentationDefinition> definitions,
            out string issue)
        {
            var definitionsSeen =
                new HashSet<CameraPresentationDefinition>();
            var idsSeen =
                new HashSet<CameraPresentationId>();

            for (int index = 0; index < definitions.Count; index++)
            {
                CameraPresentationDefinition definition =
                    definitions[index];
                if (definition == null)
                {
                    issue =
                        $"{ownerKind} '{ownerName}' Camera Presentations[{index}] is missing.";
                    return false;
                }

                if (!definitionsSeen.Add(definition))
                {
                    issue =
                        $"{ownerKind} '{ownerName}' repeats Camera Presentation definition '{definition.name}'.";
                    return false;
                }

                if (!definition.TryValidate(
                        out string definitionIssue))
                {
                    issue =
                        $"{ownerKind} '{ownerName}' Camera Presentation '{definition.name}' is invalid. {definitionIssue}";
                    return false;
                }

                if (!idsSeen.Add(definition.PresentationId))
                {
                    issue =
                        $"{ownerKind} '{ownerName}' contains duplicate CameraPresentationId '{definition.PresentationId}'.";
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }

        private void RollbackCreated(
            List<CameraPresentationMaterializationHandle> created,
            string source,
            string reason)
        {
            for (int index = created.Count - 1; index >= 0; index--)
            {
                _materializer.Release(
                    created[index],
                    source,
                    reason);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Object[] owners =
                new Object[_activeByOwner.Count];
            _activeByOwner.Keys.CopyTo(owners, 0);
            for (int index = owners.Length - 1; index >= 0; index--)
            {
                TryExit(
                    owners[index],
                    "Lifecycle",
                    owners[index] != null
                        ? owners[index].name
                        : string.Empty,
                    nameof(CameraPresentationLifecycleRuntime),
                    "camera-presentation-lifecycle-dispose",
                    out _);
            }

            _disposed = true;
        }
    }
}
