using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Immutable passive collection of Content Anchor declarations.
    /// The set performs local de-duplication and records diagnostic issues, but it does not discover scene objects,
    /// validate authoring globally, register runtime services, materialize content or block lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Passive Content Anchor Set introduced by F7E.")]
    public readonly struct ContentAnchorSet
    {
        private readonly ContentAnchorDeclaration[] _declarations;
        private readonly ContentAnchorSetIssue[] _issues;

        public ContentAnchorSet(IReadOnlyList<ContentAnchorDeclaration> declarations)
        {
            if (declarations == null || declarations.Count == 0)
            {
                _declarations = Array.Empty<ContentAnchorDeclaration>();
                _issues = Array.Empty<ContentAnchorSetIssue>();
                return;
            }

            var uniqueDeclarations = new List<ContentAnchorDeclaration>(declarations.Count);
            var detectedIssues = new List<ContentAnchorSetIssue>();
            var identityKeys = new HashSet<string>(StringComparer.Ordinal);
            var anchorIdKeys = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < declarations.Count; i++)
            {
                var declaration = declarations[i];
                if (!declaration.IsValid)
                {
                    detectedIssues.Add(new ContentAnchorSetIssue(
                        ContentAnchorSetIssueKind.InvalidDeclaration,
                        $"index:{i}",
                        declaration,
                        "Content Anchor declaration is invalid and was not added to the set."));
                    continue;
                }

                string identityKey = declaration.StableText;
                if (identityKeys.Contains(identityKey))
                {
                    detectedIssues.Add(new ContentAnchorSetIssue(
                        ContentAnchorSetIssueKind.DuplicateIdentity,
                        identityKey,
                        declaration,
                        "Content Anchor declaration identity already exists in the set; duplicate declaration was not added."));
                    continue;
                }

                string anchorIdKey = CreateAnchorIdKey(declaration);
                if (anchorIdKeys.Contains(anchorIdKey))
                {
                    detectedIssues.Add(new ContentAnchorSetIssue(
                        ContentAnchorSetIssueKind.DuplicateAnchorId,
                        anchorIdKey,
                        declaration,
                        "Content Anchor id already exists for the same owner and scope; duplicate declaration was not added."));
                    continue;
                }

                identityKeys.Add(identityKey);
                anchorIdKeys.Add(anchorIdKey);
                uniqueDeclarations.Add(declaration);
            }

            _declarations = uniqueDeclarations.Count == 0
                ? Array.Empty<ContentAnchorDeclaration>()
                : uniqueDeclarations.ToArray();
            _issues = detectedIssues.Count == 0
                ? Array.Empty<ContentAnchorSetIssue>()
                : detectedIssues.ToArray();
        }

        public IReadOnlyList<ContentAnchorDeclaration> Declarations => _declarations ?? Array.Empty<ContentAnchorDeclaration>();

        public IReadOnlyList<ContentAnchorSetIssue> Issues => _issues ?? Array.Empty<ContentAnchorSetIssue>();

        public int Count => Declarations.Count;

        public int IssueCount => Issues.Count;

        public bool IsEmpty => Count == 0;

        public bool HasAnchors => Count > 0;

        public bool HasIssues => IssueCount > 0;

        public int RouteCount => CountByScope(ContentAnchorScope.Route);

        public int ActivityCount => CountByScope(ContentAnchorScope.Activity);

        public int LocalCount => CountByScope(ContentAnchorScope.Local);

        public int RootCount => CountByKind(ContentAnchorKind.Root);

        public int SlotCount => CountByKind(ContentAnchorKind.Slot);

        public int PointCount => CountByKind(ContentAnchorKind.Point);

        public int RequiredCount => CountByRequiredness(ContentAnchorRequiredness.Required);

        public int OptionalCount => CountByRequiredness(ContentAnchorRequiredness.Optional);

        public int InvalidDeclarationIssueCount => CountIssuesByKind(ContentAnchorSetIssueKind.InvalidDeclaration);

        public int DuplicateIdentityIssueCount => CountIssuesByKind(ContentAnchorSetIssueKind.DuplicateIdentity);

        public int DuplicateAnchorIdIssueCount => CountIssuesByKind(ContentAnchorSetIssueKind.DuplicateAnchorId);

        public bool Contains(ContentAnchorDeclaration declaration)
        {
            return declaration.IsValid && TryGetByIdentity(declaration.StableText, out _);
        }

        public bool Contains(ContentAnchorScope scope, string ownerStableText, string anchorId)
        {
            return TryGetByAnchorId(scope, ownerStableText, anchorId, out _);
        }

        public bool Contains(ContentAnchorBindingRequest request)
        {
            return TryGetByBindingRequest(request, out _);
        }

        public bool TryGetByIdentity(string stableText, out ContentAnchorDeclaration declaration)
        {
            string normalized = Normalize(stableText);
            if (string.IsNullOrWhiteSpace(normalized) || !HasAnchors)
            {
                declaration = default;
                return false;
            }

            IReadOnlyList<ContentAnchorDeclaration> items = Declarations;
            for (int i = 0; i < items.Count; i++)
            {
                if (string.Equals(items[i].StableText, normalized, StringComparison.Ordinal))
                {
                    declaration = items[i];
                    return true;
                }
            }

            declaration = default;
            return false;
        }

        public bool TryGetByAnchorId(
            ContentAnchorScope scope,
            string ownerStableText,
            string anchorId,
            out ContentAnchorDeclaration declaration)
        {
            if (scope == ContentAnchorScope.Unknown || string.IsNullOrWhiteSpace(anchorId) || !HasAnchors)
            {
                declaration = default;
                return false;
            }

            string owner = Normalize(ownerStableText);
            string id = Normalize(anchorId);
            IReadOnlyList<ContentAnchorDeclaration> items = Declarations;
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item.Scope == scope
                    && string.Equals(item.Owner.StableText, owner, StringComparison.Ordinal)
                    && string.Equals(item.AnchorId.StableText, id, StringComparison.Ordinal))
                {
                    declaration = item;
                    return true;
                }
            }

            declaration = default;
            return false;
        }

        public bool TryGetByBindingRequest(
            ContentAnchorBindingRequest request,
            out ContentAnchorDeclaration declaration)
        {
            if (!request.IsValid || !HasAnchors)
            {
                declaration = default;
                return false;
            }

            if (!TryGetByAnchorId(
                    request.AnchorScope,
                    request.AnchorOwner.StableText,
                    request.AnchorId.StableText,
                    out declaration))
            {
                return false;
            }

            if (declaration.Kind != request.AnchorKind)
            {
                declaration = default;
                return false;
            }

            return true;
        }

        public IReadOnlyList<ContentAnchorDeclaration> GetByScope(ContentAnchorScope scope)
        {
            if (scope == ContentAnchorScope.Unknown || !HasAnchors)
            {
                return Array.Empty<ContentAnchorDeclaration>();
            }

            return Filter(declaration => declaration.Scope == scope);
        }

        public IReadOnlyList<ContentAnchorDeclaration> GetByKind(ContentAnchorKind kind)
        {
            if (kind == ContentAnchorKind.Unknown || !HasAnchors)
            {
                return Array.Empty<ContentAnchorDeclaration>();
            }

            return Filter(declaration => declaration.Kind == kind);
        }

        public IReadOnlyList<ContentAnchorDeclaration> GetByRequiredness(ContentAnchorRequiredness requiredness)
        {
            if (!HasAnchors)
            {
                return Array.Empty<ContentAnchorDeclaration>();
            }

            var normalized = NormalizeRequiredness(requiredness);
            return Filter(declaration => NormalizeRequiredness(declaration.Requiredness) == normalized);
        }

        public int CountByScope(ContentAnchorScope scope)
        {
            if (scope == ContentAnchorScope.Unknown || !HasAnchors)
            {
                return 0;
            }

            int count = 0;
            IReadOnlyList<ContentAnchorDeclaration> items = Declarations;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Scope == scope)
                {
                    count++;
                }
            }

            return count;
        }

        public int CountByKind(ContentAnchorKind kind)
        {
            if (kind == ContentAnchorKind.Unknown || !HasAnchors)
            {
                return 0;
            }

            int count = 0;
            IReadOnlyList<ContentAnchorDeclaration> items = Declarations;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Kind == kind)
                {
                    count++;
                }
            }

            return count;
        }

        public int CountByRequiredness(ContentAnchorRequiredness requiredness)
        {
            if (!HasAnchors)
            {
                return 0;
            }

            var normalized = NormalizeRequiredness(requiredness);
            int count = 0;
            IReadOnlyList<ContentAnchorDeclaration> items = Declarations;
            for (int i = 0; i < items.Count; i++)
            {
                if (NormalizeRequiredness(items[i].Requiredness) == normalized)
                {
                    count++;
                }
            }

            return count;
        }

        public int CountIssuesByKind(ContentAnchorSetIssueKind issueKind)
        {
            if (issueKind == ContentAnchorSetIssueKind.Unknown || !HasIssues)
            {
                return 0;
            }

            int count = 0;
            IReadOnlyList<ContentAnchorSetIssue> items = Issues;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Kind == issueKind)
                {
                    count++;
                }
            }

            return count;
        }

        public string DiagnosticMessage
        {
            get
            {
                if (!HasAnchors && !HasIssues)
                {
                    return "Content Anchor Set is empty.";
                }

                return $"Content Anchor Set registered {Count} anchor(s). {ToDiagnosticString()}";
            }
        }

        public string ToDiagnosticString(int maxDeclarations = 8, int maxIssues = 8)
        {
            var builder = new StringBuilder();
            builder.Append($"anchors='{Count}' route='{RouteCount}' activity='{ActivityCount}' local='{LocalCount}' root='{RootCount}' slot='{SlotCount}' point='{PointCount}' required='{RequiredCount}' optional='{OptionalCount}' issues='{IssueCount}' duplicateIdentity='{DuplicateIdentityIssueCount}' duplicateAnchorId='{DuplicateAnchorIdIssueCount}' invalid='{InvalidDeclarationIssueCount}'");

            AppendDeclarations(builder, maxDeclarations);
            AppendIssues(builder, maxIssues);
            return builder.ToString();
        }

        public static ContentAnchorSet Empty()
        {
            return new ContentAnchorSet(Array.Empty<ContentAnchorDeclaration>());
        }

        public static ContentAnchorSet FromDeclarations(IReadOnlyList<ContentAnchorDeclaration> declarations)
        {
            return new ContentAnchorSet(declarations);
        }

        private static string CreateAnchorIdKey(ContentAnchorDeclaration declaration)
        {
            return $"{declaration.Scope}:{declaration.Owner.StableText}:{declaration.AnchorId.StableText}";
        }

        private static ContentAnchorRequiredness NormalizeRequiredness(ContentAnchorRequiredness requiredness)
        {
            return requiredness == ContentAnchorRequiredness.Required
                ? ContentAnchorRequiredness.Required
                : ContentAnchorRequiredness.Optional;
        }

        private IReadOnlyList<ContentAnchorDeclaration> Filter(Func<ContentAnchorDeclaration, bool> predicate)
        {
            if (predicate == null || !HasAnchors)
            {
                return Array.Empty<ContentAnchorDeclaration>();
            }

            var matches = new List<ContentAnchorDeclaration>();
            IReadOnlyList<ContentAnchorDeclaration> items = Declarations;
            for (int i = 0; i < items.Count; i++)
            {
                var declaration = items[i];
                if (predicate(declaration))
                {
                    matches.Add(declaration);
                }
            }

            return matches.Count == 0
                ? Array.Empty<ContentAnchorDeclaration>()
                : matches.ToArray();
        }

        private void AppendDeclarations(StringBuilder builder, int maxDeclarations)
        {
            if (!HasAnchors)
            {
                return;
            }

            int limit = Math.Max(0, maxDeclarations);
            int shown = Math.Min(limit, Count);
            builder.Append(" details=[");
            for (int i = 0; i < shown; i++)
            {
                if (i > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append(Declarations[i].ToDiagnosticString());
            }

            builder.Append("]");
            if (Count > shown)
            {
                builder.Append($" omitted='{Count - shown}'");
            }
        }

        private void AppendIssues(StringBuilder builder, int maxIssues)
        {
            if (!HasIssues)
            {
                return;
            }

            int limit = Math.Max(0, maxIssues);
            int shown = Math.Min(limit, IssueCount);
            builder.Append(" issueDetails=[");
            for (int i = 0; i < shown; i++)
            {
                if (i > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append(Issues[i].ToDiagnosticString());
            }

            builder.Append("]");
            if (IssueCount > shown)
            {
                builder.Append($" omittedIssues='{IssueCount - shown}'");
            }
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
