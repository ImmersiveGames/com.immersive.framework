using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerBinding;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Editor-only window for passive Player binding authoring validation.
    /// The window is read-only and does not perform binding or scene mutation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F50C Player binding authoring validation editor window with root-cause issue cleanup.")]
    public sealed class PlayerBindingAuthoringValidationWindow : EditorWindow
    {
        private const string WindowTitle = "Player Binding Validation";
        private GameObject explicitRoot;
        private PlayerBindingAuthoringValidationReport report;
        private Vector2 scrollPosition;
        private bool showDerivedIssues;

        [MenuItem("Immersive Framework/Player Binding/Authoring Validation")]
        public static PlayerBindingAuthoringValidationWindow OpenWindow()
        {
            PlayerBindingAuthoringValidationWindow window = GetWindow<PlayerBindingAuthoringValidationWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(560f, 420f);
            window.Show();
            return window;
        }

        public PlayerBindingAuthoringValidationReport CurrentReport => report;

        public void ValidateActiveSceneForDiagnostics()
        {
            report = PlayerBindingAuthoringValidationEditorUtility.ValidateActiveScene("editor-window.validate-active-scene");
            PlayerBindingAuthoringValidationEditorUtility.LogReport(report);
        }

        public void ValidateSelectedRootForDiagnostics()
        {
            report = PlayerBindingAuthoringValidationEditorUtility.ValidateSelectedRoot("editor-window.validate-selected-root");
            PlayerBindingAuthoringValidationEditorUtility.LogReport(report);
        }

        public void ValidateExplicitRootForDiagnostics()
        {
            report = PlayerBindingAuthoringValidationEditorUtility.ValidateRoot(explicitRoot, "editor-window.validate-explicit-root");
            PlayerBindingAuthoringValidationEditorUtility.LogReport(report);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Player Binding Authoring Validation", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Runs the passive Player binding authoring validator. This surface reports readiness and diagnostics only; it does not bind view, bind control, activate camera, activate input, enable movement or spawn actors.",
                MessageType.Info);

            explicitRoot = EditorGUILayout.ObjectField("Validation Root", explicitRoot, typeof(GameObject), true) as GameObject;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate Active Scene"))
                {
                    ValidateActiveSceneForDiagnostics();
                }

                if (GUILayout.Button("Validate Selected Root"))
                {
                    ValidateSelectedRootForDiagnostics();
                }

                if (GUILayout.Button("Validate Root Field"))
                {
                    ValidateExplicitRootForDiagnostics();
                }
            }

            EditorGUILayout.Space();
            DrawReport();
        }

        private void DrawReport()
        {
            if (report == null)
            {
                EditorGUILayout.HelpBox("No validation report yet.", MessageType.None);
                return;
            }

            MessageType summaryType = report.Failed ? MessageType.Error : MessageType.Info;
            EditorGUILayout.HelpBox(report.Succeeded ? "Player binding authoring is ready." : "Player binding authoring is not ready.", summaryType);

            EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("PlayerSlotDeclaration", report.PlayerSlotDeclarationCount.ToString());
            EditorGUILayout.LabelField("PlayerSlotOccupancy", report.PlayerSlotOccupancyCount.ToString());
            EditorGUILayout.LabelField("ActorReadinessBehaviour", report.ActorReadinessBehaviourCount.ToString());
            EditorGUILayout.LabelField("PlayerEntryBehaviour", report.PlayerEntryBehaviourCount.ToString());
            EditorGUILayout.LabelField("PlayerViewBehaviour", report.PlayerViewBehaviourCount.ToString());
            EditorGUILayout.LabelField("PlayerControlBehaviour", report.PlayerControlBehaviourCount.ToString());
            EditorGUILayout.LabelField("Ready For View Binding", report.IsReadyForViewBinding.ToString());
            EditorGUILayout.LabelField("Ready For Control Binding", report.IsReadyForControlBinding.ToString());
            EditorGUILayout.LabelField("Ready For Full Binding", report.IsReadyForFullBinding.ToString());
            EditorGUILayout.LabelField("Blocking Issues", report.BlockingIssueCount.ToString());
            EditorGUILayout.LabelField("Root Cause Issues", report.RootCauseIssueCount.ToString());
            EditorGUILayout.LabelField("Derived Issues", report.DerivedIssueCount.ToString());

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Root Cause Issues", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            if (report.RootCauseIssueCount == 0)
            {
                EditorGUILayout.LabelField("No root cause issues.");
            }
            else
            {
                for (int i = 0; i < report.RootCauseIssues.Count; i++)
                {
                    DrawIssue(i, report.RootCauseIssues[i]);
                }
            }

            if (report.DerivedIssueCount > 0)
            {
                EditorGUILayout.Space();
                showDerivedIssues = EditorGUILayout.Foldout(
                    showDerivedIssues,
                    "Derived technical issues (" + report.DerivedIssueCount + ")",
                    true);

                if (showDerivedIssues)
                {
                    for (int i = 0; i < report.DerivedIssues.Count; i++)
                    {
                        DrawIssue(i, report.DerivedIssues[i]);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawIssue(int index, PlayerBindingAuthoringIssue issue)
        {
            EditorGUILayout.LabelField($"{index}. {(issue.Blocking ? "Error" : "Warning")}: {issue}");
            if (!string.IsNullOrWhiteSpace(issue.Message))
            {
                EditorGUILayout.LabelField("   " + issue.Message, EditorStyles.wordWrappedMiniLabel);
            }
        }
    }
}
