using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.CameraAuthoring;
using Unity.Cinemachine;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Runtime-only Cinemachine channel isolation for one Session's explicit
    /// physical Camera Outputs and the Presentation occurrences that target them.
    ///
    /// Channel identity is occurrence-local routing state. It is not serialized
    /// into CameraOutputDefinition or CameraPresentationDefinition.
    /// </summary>
    internal static class CameraCinemachineOutputChannelIsolation
    {
        internal const int MaxOutputCount = 16;

        internal static bool TryResolveChannel(
            int outputIndex,
            out OutputChannels channel,
            out string issue)
        {
            channel = default;
            if (outputIndex < 0 || outputIndex >= MaxOutputCount)
            {
                issue =
                    $"Cinemachine Output channel allocation requires an index from '0' to '{MaxOutputCount - 1}'. index='{outputIndex}'.";
                return false;
            }

            channel = (OutputChannels)(1 << outputIndex);
            issue = string.Empty;
            return true;
        }

        internal static bool TryConfigureOutput(
            CameraOutputAuthoring output,
            int outputIndex,
            out string issue)
        {
            if (output == null)
            {
                issue =
                    "Cinemachine Output channel isolation requires an exact CameraOutputAuthoring occurrence.";
                return false;
            }

            if (!TryResolveChannel(
                    outputIndex,
                    out OutputChannels channel,
                    out issue))
            {
                return false;
            }

            if (output.CinemachineBrain == null)
            {
                issue =
                    $"Camera Output '{output.OutputIdText}' requires an explicit CinemachineBrain for Output channel isolation.";
                return false;
            }

            if (output.DefaultCameraRig == null ||
                output.DefaultCameraRig.CinemachineCamera == null)
            {
                issue =
                    $"Camera Output '{output.OutputIdText}' requires a materialized Default Rig CinemachineCamera for Output channel isolation.";
                return false;
            }

            output.CinemachineBrain.ChannelMask = channel;
            output.DefaultCameraRig.CinemachineCamera.OutputChannel = channel;
            issue = string.Empty;
            return true;
        }

        internal static bool TryConfigurePresentation(
            CameraOutputAuthoring output,
            CameraRigComposer presentationRig,
            out string issue)
        {
            if (output == null || output.CinemachineBrain == null)
            {
                issue =
                    "Camera Presentation Output channel isolation requires an exact Session Output with CinemachineBrain.";
                return false;
            }

            OutputChannels channel =
                output.CinemachineBrain.ChannelMask;
            if (!IsSingleChannel(channel))
            {
                issue =
                    $"Camera Output '{output.OutputIdText}' must expose exactly one Session-assigned Cinemachine Output channel. channelMask='{channel}'.";
                return false;
            }

            if (presentationRig == null ||
                presentationRig.CinemachineCamera == null)
            {
                issue =
                    "Camera Presentation Output channel isolation requires a materialized Rig CinemachineCamera.";
                return false;
            }

            presentationRig.CinemachineCamera.OutputChannel = channel;
            issue = string.Empty;
            return true;
        }

        private static bool IsSingleChannel(
            OutputChannels channel)
        {
            int value = (int)channel;
            return value > 0 &&
                value <= (1 << (MaxOutputCount - 1)) &&
                (value & (value - 1)) == 0;
        }
    }

    /// <summary>
    /// Session-owned physical materialization boundary for explicitly configured
    /// Camera Output prefabs.
    ///
    /// It owns only the prefab occurrences and their CameraOutputSessionTopology.
    /// Request arbitration, Default semantics and physical Rig application remain
    /// in the existing Camera Output runtime.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "CAMERA-032-D Session-owned physical Camera Output prefab materialization.")]
    internal sealed class CameraSessionOutputMaterializationRuntime :
        IDisposable
    {
        private readonly GameObject[] _instances;
        private bool _disposed;

        private CameraSessionOutputMaterializationRuntime(
            GameObject[] instances,
            CameraOutputSessionTopology topology)
        {
            _instances = instances ??
                throw new ArgumentNullException(nameof(instances));
            Topology = topology ??
                throw new ArgumentNullException(nameof(topology));
        }

        internal CameraOutputSessionTopology Topology { get; }

        internal int OutputCount => _instances.Length;

        internal bool IsDisposed => _disposed;

        internal static bool TryCreate(
            CameraSessionConfiguration configuration,
            Transform physicalParent,
            out CameraSessionOutputMaterializationRuntime runtime,
            out string diagnostic)
        {
            runtime = null;

            if (configuration == null)
            {
                diagnostic =
                    "Game Application requires an explicit Camera Session configuration.";
                return false;
            }

            if (!configuration.TryValidate(out diagnostic))
            {
                return false;
            }

            IReadOnlyList<GameObject> prefabs =
                configuration.OutputPrefabs;
            var instances = new GameObject[prefabs.Count];
            var outputs =
                new CameraOutputAuthoring[prefabs.Count];
            GameObject stagingRoot = null;

            try
            {
                stagingRoot =
                    new GameObject("[Camera Session] Output Staging");
                stagingRoot.SetActive(false);
                if (physicalParent != null)
                {
                    stagingRoot.transform.SetParent(
                        physicalParent,
                        false);
                }

                for (int index = 0;
                     index < prefabs.Count;
                     index++)
                {
                    GameObject instance =
                        Object.Instantiate(
                            prefabs[index],
                            stagingRoot.transform,
                            false);
                    if (instance == null)
                    {
                        diagnostic =
                            $"Camera Session Output prefab '{prefabs[index].name}' instantiation returned null.";
                        DestroyInstances(instances);
                        return false;
                    }

                    instance.SetActive(false);
                    instances[index] = instance;

                    CameraOutputAuthoring[] authoredOutputs =
                        instance.GetComponentsInChildren<
                            CameraOutputAuthoring>(true);
                    if (authoredOutputs.Length != 1 ||
                        authoredOutputs[0] == null)
                    {
                        diagnostic =
                            $"Materialized Camera Session Output '{prefabs[index].name}' must contain exactly one CameraOutputAuthoring. Found '{authoredOutputs.Length}'.";
                        DestroyInstances(instances);
                        return false;
                    }

                    outputs[index] = authoredOutputs[0];
                }

                for (int index = 0;
                     index < outputs.Length;
                     index++)
                {
                    if (!CameraCinemachineOutputChannelIsolation
                            .TryConfigureOutput(
                                outputs[index],
                                index,
                                out string channelIssue))
                    {
                        diagnostic =
                            $"Materialized Camera Session Output '{prefabs[index].name}' could not establish Cinemachine Output channel isolation. {channelIssue}";
                        DestroyInstances(instances);
                        return false;
                    }
                }

                for (int index = 0;
                     index < instances.Length;
                     index++)
                {
                    GameObject instance = instances[index];
                    instance.name =
                        $"Camera Output [{outputs[index].OutputIdText}]";
                    instance.transform.SetParent(
                        physicalParent,
                        false);
                }

                DestroyObject(stagingRoot);
                stagingRoot = null;

                for (int index = 0;
                     index < instances.Length;
                     index++)
                {
                    instances[index].SetActive(true);
                }

                if (!CameraOutputSessionTopology.TryCreate(
                        outputs,
                        out CameraOutputSessionTopology topology,
                        out diagnostic))
                {
                    DestroyInstances(instances);
                    return false;
                }

                runtime =
                    new CameraSessionOutputMaterializationRuntime(
                        instances,
                        topology);
                diagnostic =
                    $"Camera Session materialized '{instances.Length}' explicit Output prefab occurrence(s).";
                return true;
            }
            catch (Exception exception)
            {
                DestroyInstances(instances);
                diagnostic =
                    $"Camera Session Output materialization failed. exception='{exception.GetType().Name}' message='{exception.Message}'.";
                return false;
            }
            finally
            {
                if (stagingRoot != null)
                {
                    DestroyObject(stagingRoot);
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Topology.Dispose();

            for (int index = _instances.Length - 1;
                 index >= 0;
                 index--)
            {
                GameObject instance = _instances[index];
                if (instance == null)
                {
                    continue;
                }

                instance.SetActive(false);
                DestroyObject(instance);
            }
        }

        private static void DestroyInstances(
            IReadOnlyList<GameObject> instances)
        {
            if (instances == null)
            {
                return;
            }

            for (int index = instances.Count - 1;
                 index >= 0;
                 index--)
            {
                GameObject instance = instances[index];
                if (instance == null)
                {
                    continue;
                }

                instance.SetActive(false);
                DestroyObject(instance);
            }
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
