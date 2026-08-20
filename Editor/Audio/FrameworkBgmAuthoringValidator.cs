
using System;
using Immersive.Framework.Audio;
using Immersive.Framework.Editor.Validation;
using Immersive.Framework.RouteLifecycle;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Audio
{
    internal static class FrameworkBgmAuthoringValidator
    {
        internal static FrameworkAuthoringValidationReport ValidateRouteBinding(
            FrameworkRouteBgmBinding binding)
        {
            var report = new FrameworkAuthoringValidationReport();

            if (binding == null)
            {
                report.AddError(
                    "Route BGM Binding is missing.",
                    null);
                return report;
            }

            RouteContentBinding routeContent =
                binding.GetComponentInParent<RouteContentBinding>(true);

            if (routeContent == null)
            {
                report.AddError(
                    "Route BGM Binding must be placed on or below a Route Content Binding so Route lifecycle callbacks can reach it.",
                    binding);
                return report;
            }

            if (routeContent.Route == null)
            {
                report.AddError(
                    "The owning Route Content Binding has no Route assigned.",
                    routeContent);
                return report;
            }

            FrameworkActivityBgmBinding startupBinding =
                binding.StartupActivityBgmBinding;

            if (startupBinding != null)
            {
                if (!routeContent.Route.HasStartupActivity ||
                    routeContent.Route.StartupActivity == null)
                {
                    report.AddWarning(
                        "Startup Activity BGM is assigned, but the owning Route has no Startup Activity. The reference will not be used on Route entry.",
                        binding);
                }
                else if (startupBinding.AssignedActivity != null &&
                         !ReferenceEquals(
                             startupBinding.AssignedActivity,
                             routeContent.Route.StartupActivity))
                {
                    report.AddWarning(
                        $"Startup Activity BGM targets '{startupBinding.AssignedActivity.ActivityName}', but the owning Route starts '{routeContent.Route.StartupActivity.ActivityName}'. The explicit Startup Activity BGM intent will be ignored and the pending Route BGM intent will be evaluated.",
                        startupBinding);
                }
            }

            return report;
        }

        internal static FrameworkAuthoringValidationReport ValidateActivityBinding(
            FrameworkActivityBgmBinding binding)
        {
            var report = new FrameworkAuthoringValidationReport();

            if (binding == null)
            {
                report.AddError(
                    "Activity BGM Binding is missing.",
                    null);
                return report;
            }

            if (!Enum.IsDefined(
                    typeof(FrameworkBgmActivityPolicy),
                    binding.Policy))
            {
                report.AddError(
                    "Activity BGM Policy has an invalid serialized value.",
                    binding);
            }

            return report;
        }

        internal static FrameworkAuthoringValidationReport ValidateDirector(
            FrameworkBgmDirector director)
        {
            var report = new FrameworkAuthoringValidationReport();

            if (director == null)
            {
                report.AddError(
                    "BGM Director is missing.",
                    null);
                return report;
            }

            var serializedDirector =
                new SerializedObject(director);

            SerializedProperty audioRuntimeHost =
                serializedDirector.FindProperty(
                    "audioRuntimeHost");

            if (audioRuntimeHost == null ||
                audioRuntimeHost.objectReferenceValue == null)
            {
                report.AddError(
                    "Audio Runtime Host is required. Assign the explicit physical playback authority used by the BGM Director.",
                    director);
            }

            return report;
        }
    }
}
