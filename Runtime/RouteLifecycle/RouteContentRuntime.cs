using System;
using System.Collections.Generic;
using Immersive.Framework.Authoring;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.SceneLifecycle;
using UnityEngine;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Minimal owner for notifying Route-scoped scene content.
    /// It does not load scenes, start activities, spawn actors, release content, or own Route identity.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail activated by F3D for Route-local content callbacks.")]
    internal sealed class RouteContentRuntime
    {
        private readonly FrameworkLogger _logger = FrameworkLogger.Create<RouteContentRuntime>();

        internal RouteContentLifecycleDispatchResult ExitRouteContent(RouteAsset route, RouteAsset nextRoute, string source, string reason)
        {
            string resolvedSource = NormalizeSource(source);
            string resolvedReason = NormalizeReason(reason);

            if (route == null || ReferenceEquals(route, nextRoute))
            {
                return RouteContentLifecycleDispatchResult.Skipped(
                    RouteContentLifecyclePhase.Exited,
                    route,
                    nextRoute,
                    resolvedSource,
                    resolvedReason);
            }

            IReadOnlyList<RouteContentBinding> bindings = SceneScopedComponentQuery.GetComponentsInRoutePrimaryScene<RouteContentBinding>(route);
            int bindingCount = 0;
            int receiverCount = 0;
            int failedReceiverCount = 0;

            if (bindings != null)
            {
                for (int i = 0; i < bindings.Count; i++)
                {
                    var binding = bindings[i];
                    if (!IsValidBindingForRoute(binding, route))
                    {
                        continue;
                    }

                    bindingCount++;
                    DispatchRouteContentExited(
                        binding,
                        route,
                        nextRoute,
                        resolvedSource,
                        resolvedReason,
                        out int bindingReceiverCount,
                        out int bindingFailedReceiverCount);
                    receiverCount += bindingReceiverCount;
                    failedReceiverCount += bindingFailedReceiverCount;
                }
            }

            return RouteContentLifecycleDispatchResult.ExecutedWith(
                RouteContentLifecyclePhase.Exited,
                route,
                nextRoute,
                bindingCount,
                receiverCount,
                failedReceiverCount,
                resolvedSource,
                resolvedReason);
        }

        internal RouteContentLifecycleDispatchResult EnterRouteContent(RouteAsset route, RouteAsset previousRoute, string source, string reason)
        {
            string resolvedSource = NormalizeSource(source);
            string resolvedReason = NormalizeReason(reason);

            if (route == null || ReferenceEquals(route, previousRoute))
            {
                return RouteContentLifecycleDispatchResult.Skipped(
                    RouteContentLifecyclePhase.Entered,
                    route,
                    previousRoute,
                    resolvedSource,
                    resolvedReason);
            }

            IReadOnlyList<RouteContentBinding> bindings = SceneScopedComponentQuery.GetComponentsInRoutePrimaryScene<RouteContentBinding>(route);
            int bindingCount = 0;
            int receiverCount = 0;
            int failedReceiverCount = 0;

            if (bindings != null)
            {
                for (int i = 0; i < bindings.Count; i++)
                {
                    var binding = bindings[i];
                    if (!IsValidBindingForRoute(binding, route))
                    {
                        continue;
                    }

                    bindingCount++;
                    DispatchRouteContentEntered(
                        binding,
                        route,
                        previousRoute,
                        resolvedSource,
                        resolvedReason,
                        out int bindingReceiverCount,
                        out int bindingFailedReceiverCount);
                    receiverCount += bindingReceiverCount;
                    failedReceiverCount += bindingFailedReceiverCount;
                }
            }

            return RouteContentLifecycleDispatchResult.ExecutedWith(
                RouteContentLifecyclePhase.Entered,
                route,
                previousRoute,
                bindingCount,
                receiverCount,
                failedReceiverCount,
                resolvedSource,
                resolvedReason);
        }

        private static string NormalizeSource(string source)
        {
            return source.NormalizeTextOrFallback("Unknown");
        }

        private static string NormalizeReason(string reason)
        {
            return reason.NormalizeTextOrFallback("None");
        }

        private static bool IsValidBindingForRoute(RouteContentBinding binding, RouteAsset route)
        {
            return binding != null && binding.MatchesRoute(route);
        }

        private void DispatchRouteContentEntered(
            RouteContentBinding binding,
            RouteAsset route,
            RouteAsset previousRoute,
            string source,
            string reason,
            out int receiverCount,
            out int failedReceiverCount)
        {
            var context = RouteContentLifecycleContext.Entered(route, previousRoute, binding, source, reason);
            DispatchRouteContentLifecycle(
                binding,
                "Entered",
                route,
                true,
                receiver => receiver.OnRouteContentEntered(context),
                out receiverCount,
                out failedReceiverCount);
        }

        private void DispatchRouteContentExited(
            RouteContentBinding binding,
            RouteAsset route,
            RouteAsset nextRoute,
            string source,
            string reason,
            out int receiverCount,
            out int failedReceiverCount)
        {
            var context = RouteContentLifecycleContext.Exited(route, nextRoute, binding, source, reason);
            DispatchRouteContentLifecycle(
                binding,
                "Exited",
                route,
                false,
                receiver => receiver.OnRouteContentExited(context),
                out receiverCount,
                out failedReceiverCount);
        }

        private void DispatchRouteContentLifecycle(
            RouteContentBinding binding,
            string phase,
            RouteAsset route,
            bool parentFirst,
            Action<IRouteContentLifecycleReceiver> dispatch,
            out int receiverCount,
            out int failedReceiverCount)
        {
            receiverCount = 0;
            failedReceiverCount = 0;

            if (binding == null || dispatch == null)
            {
                return;
            }

            MonoBehaviour[] behaviours = binding.GetComponentsInChildren<MonoBehaviour>(true);
            if (behaviours == null || behaviours.Length == 0)
            {
                return;
            }

            int start = parentFirst ? 0 : behaviours.Length - 1;
            int end = parentFirst ? behaviours.Length : -1;
            int step = parentFirst ? 1 : -1;

            for (int i = start; i != end; i += step)
            {
                if (behaviours[i] is not IRouteContentLifecycleReceiver receiver)
                {
                    continue;
                }

                receiverCount++;

                try
                {
                    dispatch(receiver);
                }
                catch (Exception exception)
                {
                    failedReceiverCount++;
                    LogRouteContentReceiverException(binding, phase, route, receiver, exception);
                }
            }
        }

        private void LogRouteContentReceiverException(
            RouteContentBinding binding,
            string phase,
            RouteAsset route,
            IRouteContentLifecycleReceiver receiver,
            Exception exception)
        {
            string receiverType = receiver != null ? receiver.GetType().FullName : "<missing>";
            string routeName = route.ToDiagnosticText(x => x.RouteName);
            string exceptionType = exception != null ? exception.GetType().Name : "<unknown>";
            string exceptionMessage = exception != null ? exception.Message : string.Empty;

            _logger.Error(
                $"Route Content lifecycle receiver failed. phase='{FormatValue(phase)}' route='{FormatValue(routeName)}' object='{FormatValue(binding.ObjectName)}' scene='{FormatValue(binding.SceneName)}' receiver='{FormatValue(receiverType)}' exception='{FormatValue(exceptionType)}' message='{FormatValue(exceptionMessage)}'.");
        }

        private static string FormatValue(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "<empty>"
                : value.Replace("'", "\\'");
        }
    }
}
