namespace Immersive.Framework.Authoring
{
    internal static class AuthoringIdentityText
    {
        internal static bool IsValid(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            bool previousWasSeparator = true;
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                bool isAlphaNumeric =
                    character >= 'a' && character <= 'z' ||
                    character >= '0' && character <= '9';
                if (isAlphaNumeric)
                {
                    previousWasSeparator = false;
                    continue;
                }

                bool isSeparator = character == '.' || character == '-';
                if (!isSeparator || previousWasSeparator)
                {
                    return false;
                }

                previousWasSeparator = true;
            }

            return !previousWasSeparator;
        }
    }
}
