using Immersive.Framework.CameraAuthoring;

namespace Immersive.Framework.Camera
{
    /// <summary>
    /// Typed reference to reusable rig intent and/or one materialized rig instance.
    /// It carries evidence only and does not activate the rig.
    /// </summary>
    public readonly struct CameraRigReference
    {
        public CameraRigReference(CameraRigRecipe recipe, CameraRigComposer composer)
        {
            Recipe = recipe;
            Composer = composer;
        }

        public CameraRigRecipe Recipe { get; }

        public CameraRigComposer Composer { get; }

        public bool HasRecipe => Recipe != null;

        public bool HasComposer => Composer != null;

        public bool IsValid => HasRecipe || HasComposer;

        public static CameraRigReference FromRecipe(CameraRigRecipe recipe)
        {
            return new CameraRigReference(recipe, null);
        }

        public static CameraRigReference FromComposer(CameraRigComposer composer)
        {
            return new CameraRigReference(composer != null ? composer.Recipe : null, composer);
        }

        public static CameraRigReference From(CameraRigRecipe recipe, CameraRigComposer composer)
        {
            return new CameraRigReference(recipe, composer);
        }
    }
}
