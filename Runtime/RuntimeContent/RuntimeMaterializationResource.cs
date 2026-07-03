using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Materializer-facing resource descriptor for one runtime materialization request.
    /// It carries explicit resource input data only; it is not runtime content identity and does not resolve assets by itself.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8G resource descriptor for RuntimeMaterializationRequest; no asset loading or UnityEngine reference.")]
    public readonly struct RuntimeMaterializationResource : IEquatable<RuntimeMaterializationResource>
    {
        public RuntimeMaterializationResource(
            string resourceType,
            string resourceKey,
            string resourceName,
            string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourceType))
            {
                throw new ArgumentException("Runtime materialization resource type cannot be null, empty or whitespace.", nameof(resourceType));
            }

            if (string.IsNullOrWhiteSpace(resourceKey))
            {
                throw new ArgumentException("Runtime materialization resource key cannot be null, empty or whitespace.", nameof(resourceKey));
            }

            ResourceType = Normalize(resourceType);
            ResourceKey = Normalize(resourceKey);
            ResourceName = Normalize(resourceName);
            ResourcePath = Normalize(resourcePath);
        }

        public string ResourceType { get; }

        public string ResourceKey { get; }

        public string ResourceName { get; }

        public string ResourcePath { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(ResourceType)
            && !string.IsNullOrWhiteSpace(ResourceKey);

        public string StableText => $"{ResourceType}:{ResourceKey}";

        public bool HasResourceName => !string.IsNullOrWhiteSpace(ResourceName);

        public bool HasResourcePath => !string.IsNullOrWhiteSpace(ResourcePath);

        public bool Equals(RuntimeMaterializationResource other)
        {
            return string.Equals(ResourceType, other.ResourceType, StringComparison.Ordinal)
                && string.Equals(ResourceKey, other.ResourceKey, StringComparison.Ordinal)
                && string.Equals(ResourceName, other.ResourceName, StringComparison.Ordinal)
                && string.Equals(ResourcePath, other.ResourcePath, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is RuntimeMaterializationResource other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(ResourceType ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(ResourceKey ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(ResourceName ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(ResourcePath ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return StableText;
        }

        public string ToDiagnosticString()
        {
            string nameText = HasResourceName ? ResourceName : "<none>";
            string pathText = HasResourcePath ? ResourcePath : "<none>";
            return $"resource='{StableText}' resourceName='{nameText}' resourcePath='{pathText}'";
        }

        public static RuntimeMaterializationResource From(
            string resourceType,
            string resourceKey,
            string resourceName,
            string resourcePath)
        {
            return new RuntimeMaterializationResource(
                resourceType,
                resourceKey,
                resourceName,
                resourcePath);
        }

        public static bool operator ==(RuntimeMaterializationResource left, RuntimeMaterializationResource right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RuntimeMaterializationResource left, RuntimeMaterializationResource right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
