using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Immersive.Framework.ApiStatus;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Process-local operational token for one exact authored definition instance.
    /// Distinguishes two Unity objects that share a stable RouteId/ActivityId. Not a stable identity,
    /// not persisted, and not a global lookup surface.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IF-ID-05 process-local definition token for operational ownership.")]
    public readonly struct RuntimeDefinitionToken : IEquatable<RuntimeDefinitionToken>
    {
        private static long _sNextValue;

        private readonly long _value;

        private RuntimeDefinitionToken(long value)
        {
            _value = value;
        }

        /// <summary>
        /// True when this token was minted for a concrete definition instance.
        /// </summary>
        public bool IsValid => _value != 0L;

        public long Value => _value;

        public string StableText => IsValid ? $"def-{_value}" : "def-none";

        /// <summary>
        /// Mints or reuses the process-local token for the exact managed identity of
        /// <paramref name="definition"/>. Requires a non-null, alive Unity object.
        /// </summary>
        public static RuntimeDefinitionToken FromUnityObject(Object definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            // RuntimeHelpers identity hash is process-local and tied to the managed object
            // instance. Combined with a monotonic mint table keyed by CLR object identity
            // (ConditionalWeakTable would require static state shared as a registry).
            // Instead mint uniquely per call only when we cannot attach state to the object:
            // we pack identity hash with a per-process sequence derived from first-seen identity.
            return FromManagedIdentity(definition);
        }

        /// <summary>
        /// Creates a unique process-local token that is not bound to a Unity object.
        /// Intended for smoke/tests that need operational uniqueness without an asset instance.
        /// </summary>
        public static RuntimeDefinitionToken MintAnonymous()
        {
            long value = Interlocked.Increment(ref _sNextValue);
            if (value == 0L)
            {
                value = Interlocked.Increment(ref _sNextValue);
            }

            return new RuntimeDefinitionToken(value);
        }

        public bool Equals(RuntimeDefinitionToken other) => _value == other._value;

        public override bool Equals(object obj) =>
            obj is RuntimeDefinitionToken other && Equals(other);

        public override int GetHashCode() => _value.GetHashCode();

        public override string ToString() => StableText;

        public static bool operator ==(RuntimeDefinitionToken left, RuntimeDefinitionToken right) =>
            left.Equals(right);

        public static bool operator !=(RuntimeDefinitionToken left, RuntimeDefinitionToken right) =>
            !left.Equals(right);

        private static RuntimeDefinitionToken FromManagedIdentity(object definition)
        {
            // Two different managed instances must never share a token. Identity hash alone is
            // not collision-proof, so combine with a unique mint counter stored weakly per object.
            long tokenValue = DefinitionTokenTable.GetOrAdd(definition);
            return new RuntimeDefinitionToken(tokenValue);
        }

        /// <summary>
        /// Private process-local association of managed objects to minted token values.
        /// Not a framework service locator: no reverse resolution, no public API, GC-safe.
        /// </summary>
        private static class DefinitionTokenTable
        {
            private static readonly ConditionalWeakTable<object, Holder> Table =
                new ConditionalWeakTable<object, Holder>();

            internal static long GetOrAdd(object definition)
            {
                Holder holder = Table.GetValue(
                    definition,
                    _ =>
                    {
                        long value = Interlocked.Increment(ref _sNextValue);
                        if (value == 0L)
                        {
                            value = Interlocked.Increment(ref _sNextValue);
                        }

                        return new Holder(value);
                    });
                return holder.Value;
            }

            private sealed class Holder
            {
                internal Holder(long value)
                {
                    Value = value;
                }

                internal long Value { get; }
            }
        }
    }
}
