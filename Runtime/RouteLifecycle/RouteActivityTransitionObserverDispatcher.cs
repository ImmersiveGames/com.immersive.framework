using System;
using Immersive.Framework.Authoring;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.SceneLifecycle;
using UnityEngine;

namespace Immersive.Framework.RouteLifecycle
{
    internal sealed class RouteActivityTransitionObserverDispatcher
    {
        private readonly FrameworkLogger _logger = FrameworkLogger.Create<RouteActivityTransitionObserverDispatcher>();

        internal void Dispatch(
            RouteContentDiscoveryScope scope,
            ActivityAsset previousActivity,
            ActivityAsset currentActivity,
            string source,
            string reason)
        {
            RouteAsset route = scope.Route;
            if (route == null)
            {
                return;
            }

            var context = new RouteActivityTransitionContext(
                previousActivity,
                currentActivity,
                source,
                reason);
            var contributions = SceneCompositionComponentQuery.GetComponents<RouteContentContribution>(scope);
            for (int i = 0; i < contributions.Count; i++)
            {
                RouteContentContribution contribution = contributions[i];
                if (contribution == null || !contribution.MatchesRoute(route))
                {
                    continue;
                }

                MonoBehaviour[] behaviours = contribution.GetComponentsInChildren<MonoBehaviour>(true);
                for (int j = 0; j < behaviours.Length; j++)
                {
                    if (behaviours[j] is not IRouteActivityTransitionObserver observer)
                    {
                        continue;
                    }

                    try
                    {
                        observer.OnActivityTransitionCommitted(context);
                    }
                    catch (Exception exception)
                    {
                        string observerType = observer.GetType().FullName;
                        _logger.Error(
                            $"Route Activity transition observer failed. route='{route.RouteName}' observer='{observerType}' exception='{exception.GetType().Name}' message='{exception.Message}'.");
                    }
                }
            }
        }
    }
}
