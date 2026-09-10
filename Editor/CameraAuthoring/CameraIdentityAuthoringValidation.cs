using Immersive.Framework.Camera;

namespace Immersive.Framework.Editor.CameraAuthoring
{
    internal static class CameraIdentityAuthoringValidation
    {
        internal static string OutputIdIssue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Output Id is missing.";
            return new CameraOutputId(value).IsValid ? null : "Output Id is invalid.";
        }

        internal static string ViewIdIssue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "View Id is missing.";
            return new CameraViewId(value).IsValid ? null : "View Id is invalid.";
        }

        internal static string OutputReferenceIssue(string value, CameraOutputAuthoringTopology topology)
        {
            string issue = OutputIdIssue(value);
            if (issue != null) return issue;
            if (topology == null || !topology.IsResolved) return null;
            int count = topology.Count(new CameraOutputId(value));
            if (count == 0) return $"Output Id '{value}' is dangling in the active Session Output topology.";
            return count == 1 ? null : $"Output Id '{value}' is ambiguous: {count} definitions in the active Session Output topology.";
        }
    }
}
