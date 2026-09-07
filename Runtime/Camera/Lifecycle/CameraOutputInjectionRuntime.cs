using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Session topology dependency injector for camera consumers in loaded scenes.
    /// Every consumer declares an Output ID and receives only that exact output.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    internal sealed class CameraOutputInjectionRuntime : IDisposable
    {
        private readonly CameraOutputSessionTopology _topology;
        private readonly CameraViewOutputTopology _viewOutputs;
        private readonly Dictionary<ICameraOutputSessionConsumer, AttachedConsumer>
            _attachedConsumers =
                new Dictionary<ICameraOutputSessionConsumer, AttachedConsumer>(
                    ConsumerReferenceComparer.Instance);

        internal CameraOutputInjectionRuntime(
            CameraOutputSessionTopology topology,
            CameraViewOutputTopology viewOutputs)
        {
            _topology = topology ?? throw new ArgumentNullException(nameof(topology));
            _viewOutputs = viewOutputs ?? throw new ArgumentNullException(nameof(viewOutputs));
            SceneManager.sceneLoaded += OnSceneLoaded;

            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                AttachScene(SceneManager.GetSceneAt(index));
            }
        }

        public void Dispose()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _attachedConsumers.Clear();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            AttachScene(scene);
        }

        internal void AttachScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            AttachRoots(scene.GetRootGameObjects());
        }

        internal void AttachRoots(IReadOnlyList<GameObject> roots)
        {
            if (roots == null)
            {
                return;
            }

            for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
            {
                GameObject root = roots[rootIndex];
                if (root == null)
                {
                    continue;
                }

                MonoBehaviour[] behaviours = root
                    .GetComponentsInChildren<MonoBehaviour>(true);

                for (int index = 0; index < behaviours.Length; index++)
                {
                    if (behaviours[index] is ICameraOutputSessionConsumer consumer)
                    {
                        AttachExact(consumer);
                    }
                }
            }
        }

        internal bool AttachExact(
            ICameraOutputSessionConsumer consumer,
            out string diagnostic)
        {
            CameraOutputInjectionResult result =
                AttachExact(consumer);
            diagnostic = result.Diagnostic;
            return result.Succeeded;
        }

        internal CameraOutputInjectionResult AttachExact(
            ICameraOutputSessionConsumer consumer)
        {
            if (consumer == null)
            {
                return CameraOutputInjectionResult.Rejected(
                    CameraOutputInjectionStatus.RejectedMissingConsumer,
                    "Camera Output injection requires a consumer.");
            }
            CameraOutputId requestedOutputId = consumer.RequestedOutputId;
            if (TryGetPreviousResult(
                    consumer,
                    requestedOutputId,
                    out CameraOutputInjectionResult previousResult))
            {
                return previousResult.Succeeded
                    ? CameraOutputInjectionResult.Preserved()
                    : previousResult;
            }
            ForgetAttachment(consumer);
            if (!_topology.TryGetOutput(
                    requestedOutputId,
                    out CameraOutputAuthoring output,
                    out string diagnostic))
            {
                consumer.DetachOutputSession(
                    diagnostic);
                CameraOutputInjectionStatus status =
                    requestedOutputId.IsValid
                        ? CameraOutputInjectionStatus.RejectedUnknownOutput
                        : CameraOutputInjectionStatus.RejectedInvalidOutputId;
                CameraOutputInjectionResult rejected =
                    CameraOutputInjectionResult.Rejected(
                        status,
                        diagnostic);
                RememberAttachment(
                    consumer,
                    requestedOutputId,
                    rejected);
                return rejected;
            }
            if (consumer is ICameraViewOutputBindingConsumer viewConsumer &&
                !_viewOutputs.TryGetBinding(
                    viewConsumer.RequestedViewId,
                    requestedOutputId,
                    out _))
            {
                diagnostic =
                    $"Camera consumer requires an explicit View '{viewConsumer.RequestedViewId}' to Output '{consumer.RequestedOutputId}' binding in the active composition policy.";
                consumer.DetachOutputSession(diagnostic);
                CameraOutputInjectionResult rejected =
                    CameraOutputInjectionResult.Rejected(
                        CameraOutputInjectionStatus.RejectedViewOutputBinding,
                        diagnostic);
                RememberAttachment(
                    consumer,
                    requestedOutputId,
                    rejected);
                return rejected;
            }
            consumer.AttachOutputSession(output);
            CameraOutputInjectionResult attached =
                CameraOutputInjectionResult.Attached();
            RememberAttachment(
                consumer,
                requestedOutputId,
                attached);
            return attached;
        }

        private bool TryGetPreviousResult(
            ICameraOutputSessionConsumer consumer,
            CameraOutputId requestedOutputId,
            out CameraOutputInjectionResult result)
        {
            if (_attachedConsumers.TryGetValue(
                    consumer,
                    out AttachedConsumer attached) &&
                attached.OutputId == requestedOutputId)
            {
                result = attached.Result;
                return true;
            }

            result = default;
            return false;
        }

        private void RememberAttachment(
            ICameraOutputSessionConsumer consumer,
            CameraOutputId requestedOutputId,
            CameraOutputInjectionResult result)
        {
            _attachedConsumers[consumer] =
                new AttachedConsumer(
                    requestedOutputId,
                    result);
        }

        private void ForgetAttachment(
            ICameraOutputSessionConsumer consumer)
        {
            _attachedConsumers.Remove(consumer);
        }

        private readonly struct AttachedConsumer
        {
            internal AttachedConsumer(
                CameraOutputId outputId,
                CameraOutputInjectionResult result)
            {
                OutputId = outputId;
                Result = result;
            }

            internal CameraOutputId OutputId { get; }

            internal CameraOutputInjectionResult Result { get; }
        }

        private sealed class ConsumerReferenceComparer :
            IEqualityComparer<ICameraOutputSessionConsumer>
        {
            internal static readonly ConsumerReferenceComparer Instance =
                new ConsumerReferenceComparer();

            public bool Equals(
                ICameraOutputSessionConsumer left,
                ICameraOutputSessionConsumer right) =>
                ReferenceEquals(left, right);

            public int GetHashCode(
                ICameraOutputSessionConsumer consumer) =>
                RuntimeHelpers.GetHashCode(consumer);
        }
    }

    internal enum CameraOutputInjectionStatus
    {
        None = 0,
        Attached = 1,
        Preserved = 2,
        RejectedMissingConsumer = 3,
        RejectedInvalidOutputId = 4,
        RejectedUnknownOutput = 5,
        RejectedViewOutputBinding = 6
    }

    internal readonly struct CameraOutputInjectionResult
    {
        private CameraOutputInjectionResult(
            CameraOutputInjectionStatus status,
            bool succeeded,
            string diagnostic)
        {
            Status = status;
            Succeeded = succeeded;
            Diagnostic = diagnostic ?? string.Empty;
        }

        internal CameraOutputInjectionStatus Status { get; }

        internal bool Succeeded { get; }

        internal string Diagnostic { get; }

        internal static CameraOutputInjectionResult Attached() =>
            new CameraOutputInjectionResult(
                CameraOutputInjectionStatus.Attached,
                true,
                string.Empty);

        internal static CameraOutputInjectionResult Preserved() =>
            new CameraOutputInjectionResult(
                CameraOutputInjectionStatus.Preserved,
                true,
                string.Empty);

        internal static CameraOutputInjectionResult Rejected(
            CameraOutputInjectionStatus status,
            string diagnostic) =>
            new CameraOutputInjectionResult(
                status,
                false,
                diagnostic);
    }
}
