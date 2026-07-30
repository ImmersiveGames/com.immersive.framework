using Immersive.Framework.Authoring;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.Editor.Common;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    /// <summary>
    /// Explicit, deterministic Content Anchor identity suggestions.
    /// This utility never mutates serialized data.
    /// </summary>
    internal static class ContentAnchorAuthoringSuggestionUtility
    {
        internal static string SuggestRouteAnchorId(
            RouteContentAnchor context,
            RouteAsset owner,
            ContentAnchorKind kind)
        {
            string ownerName =
                owner != null
                    ? owner.name
                    : "unassigned-route";

            return FrameworkAuthoringSuggestionUtility.SuggestIdentity(
                context,
                $"content-anchor.route.{ownerName}.{kind}");
        }

        internal static string SuggestActivityAnchorId(
            ActivityContentAnchor context,
            ActivityAsset owner,
            ContentAnchorKind kind)
        {
            string ownerName =
                owner != null
                    ? owner.name
                    : "unassigned-activity";

            return FrameworkAuthoringSuggestionUtility.SuggestIdentity(
                context,
                $"content-anchor.activity.{ownerName}.{kind}");
        }
    }
}
