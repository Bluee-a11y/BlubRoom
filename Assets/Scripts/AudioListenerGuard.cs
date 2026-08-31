using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClubhousePC
{
    public sealed class AudioListenerGuard : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindObjectOfType<AudioListenerGuard>() != null) return;
            var guard = new GameObject("Audio Listener Guard").AddComponent<AudioListenerGuard>();
            DontDestroyOnLoad(guard.gameObject);
        }

        private void LateUpdate()
        {
            var listeners = FindObjectsOfType<AudioListener>(true);
            if (listeners.Length == 0) return;
            AudioListener keep = null;
            var mainCamera = Camera.main;
            if (mainCamera != null) keep = mainCamera.GetComponent<AudioListener>();
            if (keep == null)
                foreach (var listener in listeners)
                    if (listener.enabled) { keep = listener; break; }
            keep ??= listeners[0];
            foreach (var listener in listeners)
                listener.enabled = listener == keep;
        }
    }
}
