namespace Immersive.Framework.Common
{
    internal static class ParticipantValidation
    {
        internal static bool IsDefined(ParticipantRequiredness requiredness)
        {
            return FrameworkEnumValidation.IsDefinedAndNot(requiredness, ParticipantRequiredness.Unknown);
        }

        internal static void ThrowIfUndefined(ParticipantRequiredness requiredness, string paramName)
        {
            FrameworkEnumValidation.ThrowIfUndefinedOr(
                requiredness,
                ParticipantRequiredness.Unknown,
                paramName,
                "Participant requiredness must be explicit.");
        }
    }
}
