using Immersive.Framework.Editor.Validation;
using UnityEditor.SceneTemplate;
using UnityEngine.SceneManagement;
namespace Immersive.Framework.Editor.SceneTemplates.PersistentContent
{
    /// <summary>
    /// Non-mutating verification pipeline for the official Persistent Content
    /// Scene Template.
    ///
    /// The source scene owns the composition. This pipeline only proves that the
    /// instantiated scene preserved the required contracts. It never creates,
    /// repairs, saves or assigns consumer assets.
    /// </summary>
    public sealed class PersistentContentSceneTemplatePipeline :
        ISceneTemplatePipeline
    {
        public bool IsValidTemplateForInstantiation(
            SceneTemplateAsset sceneTemplateAsset)
        {
            return sceneTemplateAsset != null &&
                   sceneTemplateAsset.templateScene != null;
        }

        public void BeforeTemplateInstantiation(
            SceneTemplateAsset sceneTemplateAsset,
            bool isAdditive,
            string sceneName)
        {
            // Intentionally empty. The source scene is already the complete
            // authoring authority and requires no pre-instantiation mutation.
        }

        public void AfterTemplateInstantiation(
            SceneTemplateAsset sceneTemplateAsset,
            Scene scene,
            bool isAdditive,
            string sceneName)
        {
            FrameworkAuthoringValidationReport report =
                FrameworkAuthoringValidator
                    .ValidatePersistentContentTemplateScene(
                        scene,
                        sceneTemplateAsset);

            FrameworkAuthoringValidationGui.LogReport(
                "Persistent Content Template Instantiation",
                report);
        }
    }
}
