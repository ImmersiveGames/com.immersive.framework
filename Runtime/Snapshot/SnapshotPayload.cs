using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Snapshot
{
    /// <summary>
    /// API status: Experimental. Immutable backend-agnostic payload carried by a Snapshot envelope.
    /// It does not define JSON, files, PlayerPrefs, cloud storage or premium backend details.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21B Snapshot payload primitive; no persistence backend.")]
    public readonly struct SnapshotPayload : IEquatable<SnapshotPayload>
    {
        private readonly byte[] _bytes;

        public SnapshotPayload(SnapshotPayloadFormat format, IReadOnlyList<byte> bytes, string mediaType)
        {
            ValidateFormat(format);

            byte[] copiedBytes = CopyBytes(bytes);
            if (format == SnapshotPayloadFormat.Empty && copiedBytes.Length > 0)
            {
                throw new ArgumentException("Snapshot empty payload cannot carry bytes.", nameof(bytes));
            }

            if (format != SnapshotPayloadFormat.Empty && copiedBytes.Length == 0)
            {
                throw new ArgumentException("Snapshot non-empty payload must carry at least one byte.", nameof(bytes));
            }

            Format = format;
            _bytes = copiedBytes;
            MediaType = Normalize(mediaType);
        }

        public SnapshotPayloadFormat Format { get; }

        public string MediaType { get; }

        public IReadOnlyList<byte> Bytes => _bytes ?? Array.Empty<byte>();

        public int ByteCount => Bytes.Count;

        public bool IsEmpty => Format == SnapshotPayloadFormat.Empty;

        public bool HasBytes => ByteCount > 0;

        public bool HasMediaType => !string.IsNullOrWhiteSpace(MediaType);

        public bool IsValid => Format != SnapshotPayloadFormat.Unknown
            && (Format == SnapshotPayloadFormat.Empty && ByteCount == 0
                || Format != SnapshotPayloadFormat.Empty && ByteCount > 0);

        public byte[] ToByteArray()
        {
            IReadOnlyList<byte> items = Bytes;
            if (items.Count == 0)
            {
                return Array.Empty<byte>();
            }

            byte[] copy = new byte[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                copy[i] = items[i];
            }

            return copy;
        }

        public bool Equals(SnapshotPayload other)
        {
            return Format == other.Format
                && string.Equals(MediaType, other.MediaType, StringComparison.Ordinal)
                && SequenceEquals(Bytes, other.Bytes);
        }

        public override bool Equals(object obj)
        {
            return obj is SnapshotPayload other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Format;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(MediaType ?? string.Empty);

                IReadOnlyList<byte> items = Bytes;
                for (int i = 0; i < items.Count; i++)
                {
                    hashCode = hashCode * 397 ^ items[i];
                }

                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            string mediaTypeText = HasMediaType ? MediaType : "<none>";
            return $"format='{Format}' bytes='{ByteCount}' mediaType='{mediaTypeText}'";
        }

        public static SnapshotPayload Empty()
        {
            return new SnapshotPayload(SnapshotPayloadFormat.Empty, Array.Empty<byte>(), string.Empty);
        }

        public static SnapshotPayload FromBytes(SnapshotPayloadFormat format, IReadOnlyList<byte> bytes, string mediaType)
        {
            return new SnapshotPayload(format, bytes, mediaType);
        }

        public static SnapshotPayload FromText(string text, string mediaType)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentException("Snapshot text payload cannot be null or empty. Use Empty for an explicit empty payload.", nameof(text));
            }

            return new SnapshotPayload(
                SnapshotPayloadFormat.Text,
                Encoding.UTF8.GetBytes(text),
                mediaType);
        }

        public static bool operator ==(SnapshotPayload left, SnapshotPayload right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SnapshotPayload left, SnapshotPayload right)
        {
            return !left.Equals(right);
        }

        private static void ValidateFormat(SnapshotPayloadFormat format)
        {
            if (!Enum.IsDefined(typeof(SnapshotPayloadFormat), format) || format == SnapshotPayloadFormat.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(format), format, "Snapshot payload format must be explicit.");
            }
        }

        private static byte[] CopyBytes(IReadOnlyList<byte> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<byte>();
            }

            byte[] copy = new byte[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                copy[i] = source[i];
            }

            return copy;
        }

        private static bool SequenceEquals(IReadOnlyList<byte> left, IReadOnlyList<byte> right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null || left.Count != right.Count)
            {
                return false;
            }

            for (int i = 0; i < left.Count; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
