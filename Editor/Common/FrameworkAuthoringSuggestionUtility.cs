using System.Text;
using UnityEngine;

namespace Immersive.Framework.Editor.Common
{
    /// <summary>Explicit, deterministic suggestions for authoring text. It never writes serialized data.</summary>
    internal static class FrameworkAuthoringSuggestionUtility
    {
        internal static string SuggestIdentity(Object context, string domain)
        {
            return Build(domain, DescribeContext(context, true));
        }

        internal static string SuggestReason(Object context, string domain)
        {
            return Build(domain, DescribeContext(context, false));
        }

        private static string DescribeContext(Object context, bool includeComponentIndex)
        {
            if (!(context is Component component))
            {
                return context != null ? context.name : "unnamed";
            }

            string path = component.gameObject.name;
            for (Transform current = component.transform.parent; current != null; current = current.parent)
            {
                path = current.name + "-" + path;
            }

            if (!includeComponentIndex)
            {
                return path;
            }

            Component[] siblings = component.GetComponents(component.GetType());
            for (int index = 0; index < siblings.Length; index++)
            {
                if (object.ReferenceEquals(siblings[index], component))
                {
                    return path + "-" + component.GetType().Name + "-" + index;
                }
            }

            return path + "-" + component.GetType().Name;
        }

        private static string Build(string domain, string name)
        {
            string normalizedDomain = Normalize(domain);
            string normalizedName = Normalize(name);
            return string.IsNullOrEmpty(normalizedName)
                ? normalizedDomain
                : normalizedDomain + "." + normalizedName;
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "unnamed";
            }

            var builder = new StringBuilder(value.Length);
            bool previousSeparator = false;
            foreach (char character in value.Trim())
            {
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(char.ToLowerInvariant(character));
                    previousSeparator = false;
                }
                else if (!previousSeparator && builder.Length > 0)
                {
                    builder.Append('-');
                    previousSeparator = true;
                }
            }

            while (builder.Length > 0 && builder[builder.Length - 1] == '-')
            {
                builder.Length--;
            }

            return builder.Length == 0 ? "unnamed" : builder.ToString();
        }
    }
}
