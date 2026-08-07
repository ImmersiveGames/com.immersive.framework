using System.IO;
using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Editor.Authoring;
using Immersive.Framework.Editor.Editor.Validation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Authoring.Editor.Tests
{
    public sealed class IdentityAuthoringValidationTests
    {
        private const string TempRoot = "Assets/_IF_ID_IdentityValidationTemp";

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(TempRoot))
            {
                AssetDatabase.DeleteAsset(TempRoot);
            }

            AssetDatabase.Refresh();
        }

        [Test]
        public void DefinitionLocal_Collision_ContextIsConflictingAsset()
        {
            EnsureTempRoot();
            const string sharedId = "ifid.route.collision.local";
            RouteAsset selected = CreateRouteAsset("SelectedRoute.asset", sharedId);
            RouteAsset conflicting = CreateRouteAsset("ConflictingRoute.asset", sharedId);

            FrameworkAuthoringValidationReport report =
                FrameworkIdentityAuthoringValidator.ValidateRouteDefinitionLocal(
                    selected,
                    FrameworkValidationMode.Standard);

            Assert.That(report.ErrorCount, Is.GreaterThan(0));
            bool foundConflictingContext = false;
            for (int index = 0; index < report.Issues.Count; index++)
            {
                FrameworkAuthoringValidationIssue issue = report.Issues[index];
                if (issue.Severity != FrameworkAuthoringValidationSeverity.Error)
                {
                    continue;
                }

                if (issue.Context == conflicting)
                {
                    foundConflictingContext = true;
                    Assert.That(issue.Message, Does.Contain("Definition-local"));
                    Assert.That(issue.Message, Does.Contain(sharedId));
                }

                Assert.That(
                    issue.Context,
                    Is.Not.EqualTo(selected),
                    "Collision findings should navigate to the other asset, not only the selected one.");
            }

            Assert.That(foundConflictingContext, Is.True);
        }

        [Test]
        public void DefinitionLocal_UnrelatedProjectCollision_DoesNotBlockSelected()
        {
            EnsureTempRoot();
            RouteAsset selected = CreateRouteAsset(
                "CleanRoute.asset",
                "ifid.route.clean.unique");
            CreateRouteAsset("UnrelatedA.asset", "ifid.route.unrelated.shared");
            CreateRouteAsset("UnrelatedB.asset", "ifid.route.unrelated.shared");

            FrameworkAuthoringValidationReport local =
                FrameworkIdentityAuthoringValidator.ValidateRouteDefinitionLocal(
                    selected,
                    FrameworkValidationMode.Standard);

            Assert.That(local.ErrorCount, Is.EqualTo(0));

            FrameworkAuthoringValidationReport project =
                FrameworkIdentityAuthoringValidator.ValidateProjectIdentityAudit(
                    FrameworkValidationMode.Standard);

            Assert.That(project.ErrorCount, Is.GreaterThan(0));
            bool reportedUnrelated = false;
            for (int index = 0; index < project.Issues.Count; index++)
            {
                if (project.Issues[index].Message.Contains("ifid.route.unrelated.shared") &&
                    project.Issues[index].Message.Contains("Project audit"))
                {
                    reportedUnrelated = true;
                    break;
                }
            }

            Assert.That(reportedUnrelated, Is.True);
        }

        [Test]
        public void RegenerateStableId_ChangesOnlySelectedRoute_AndSupportsUndo()
        {
            EnsureTempRoot();
            const string originalId = "ifid.route.regenerate.source";
            RouteAsset route = CreateRouteAsset("RegenerateRoute.asset", originalId);
            string path = AssetDatabase.GetAssetPath(route);

            Assert.That(
                FrameworkIdentityAuthoringValidator.TryRegenerateStableId(
                    route,
                    out string previousId,
                    out string newId,
                    out string issue),
                Is.True,
                issue);

            Assert.That(previousId, Is.EqualTo(originalId));
            Assert.That(newId, Is.Not.EqualTo(originalId));
            Assert.That(newId, Is.Not.Empty);

            RouteAsset reloaded = AssetDatabase.LoadAssetAtPath<RouteAsset>(path);
            Assert.That(reloaded.RouteId.StableText, Is.EqualTo(newId));

            Undo.PerformUndo();
            reloaded = AssetDatabase.LoadAssetAtPath<RouteAsset>(path);
            Assert.That(reloaded.RouteId.StableText, Is.EqualTo(originalId));
        }

        private static void EnsureTempRoot()
        {
            if (!AssetDatabase.IsValidFolder(TempRoot))
            {
                AssetDatabase.CreateFolder("Assets", "_IF_ID_IdentityValidationTemp");
            }
        }

        private static RouteAsset CreateRouteAsset(string fileName, string routeId)
        {
            string assetPath = Path.Combine(TempRoot, fileName).Replace('\\', '/');
            RouteAsset route = ScriptableObject.CreateInstance<RouteAsset>();
            AssetDatabase.CreateAsset(route, assetPath);
            var serialized = new SerializedObject(route);
            serialized.FindProperty("routeId").stringValue = routeId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(route);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<RouteAsset>(assetPath);
        }
    }
}
