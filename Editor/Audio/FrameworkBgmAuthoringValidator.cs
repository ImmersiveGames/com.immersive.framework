
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

            if (!Enum.IsDefined(
                    typeof(FrameworkBgmRoutePolicy),
                    binding.Policy))
            {
                report.AddError(
                    "Route BGM Policy has an invalid serialized value.",
                    binding);
            }
            else if (binding.Policy == FrameworkBgmRoutePolicy.PlayOwn &&
                     binding.RouteBgm == null)
            {
                report.AddError(
                    "Route BGM Policy Play Own requires a Route BGM cue.",
                    binding);
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
