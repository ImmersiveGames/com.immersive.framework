using System;

namespace Immersive.Framework.Common
{
    internal static class FrameworkStringExtensions
    {
        internal static string NormalizeText(this string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        internal static string NormalizeTextOrFallback(this string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        internal static string ToDiagnosticText(this string value)
        {
            return value.NormalizeTextOrFallback("<none>");
        }

        internal static string ToDiagnosticText(this string value, string fallback)
        {
            return value.NormalizeTextOrFallback(fallback);
        }

        internal static string ToDiagnosticText<T>(this T value, Func<T, string> selector)
            where T : class
        {
            return value == null ? "<none>" : selector(value).ToDiagnosticText();
        }

        internal static string ToDiagnosticText<T>(this T value, Func<T, string> selector, string fallback)
            where T : class
        {
            return value == null ? fallback : selector(value).ToDiagnosticText(fallback);
        }
    }
}
