using UnityEngine;
using UnityEngine.Events;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.SceneLifecycle
{
    /// <summary>
    /// Inspector-authored callbacks for the official Scene Lifecycle.
    /// The framework invokes these callbacks when the containing scene becomes
    /// available or is about to be released; this component never observes Unity
    /// scene events by itself.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Scene Lifecycle Events")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Scene lifecycle callbacks are an authoring bridge over the internal SceneLifecycleRuntime.")]
    public sealed class SceneLifecycleEvents : MonoBehaviour
    {
        [Header("Scene Lifecycle")]
        [SerializeField] private UnityEvent available = new UnityEvent();
        [SerializeField] private UnityEvent releasing = new UnityEvent();

        [Header("Advanced / Debug")]
        [SerializeField] private string lastEvent = "Initial";
        [SerializeField] private int availableCount;
        [SerializeField] private int releasingCount;

        private bool _isAvailable;

        public UnityEvent Available => available;

        public UnityEvent Releasing => releasing;

        public string LastEvent => lastEvent;

        public int AvailableCount => availableCount;

        public int ReleasingCount => releasingCount;

        internal void NotifySceneAvailable()
        {
            if (_isAvailable)
            {
                return;
            }

            _isAvailable = true;
            availableCount++;
            lastEvent = "Available";
            available?.Invoke();
        }

        internal void NotifySceneReleasing()
        {
            if (!_isAvailable)
            {
                return;
            }

            _isAvailable = false;
            releasingCount++;
            lastEvent = "Releasing";
            releasing?.Invoke();
        }
    }
}
