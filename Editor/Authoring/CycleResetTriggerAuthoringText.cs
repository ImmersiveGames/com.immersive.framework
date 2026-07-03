using System;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    internal static class CycleResetTriggerAuthoringText
    {
        private static readonly string[] FutureResetVocabulary =
        {
            "object",
            "component",
            "player",
            "actor",
            "transform",
            "rigidbody",
            "animator",
            "pool",
            "snapshot",
            "save",
            "reload",
            "scene reload",
            "checkpoint"
        };

        internal static bool ContainsFutureResetVocabulary(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            for (int i = 0; i < FutureResetVocabulary.Length; i++)
            {
                if (value.IndexOf(FutureResetVocabulary[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
