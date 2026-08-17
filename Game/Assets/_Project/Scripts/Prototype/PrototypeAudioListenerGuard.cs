using UnityEngine;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Runtime-only listener for the UI-driven production scenes. Gameplay/Menu do not require a camera,
    /// so the audio pass must not assume the usual Main Camera + AudioListener setup exists.
    /// </summary>
    [DefaultExecutionOrder(2390)]
    public sealed class PrototypeAudioListenerGuard : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureListener()
        {
            if (PrototypeAuthoringMode.Active) return;

            var listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var existing in listeners)
            {
                if (existing != null && existing.enabled && existing.gameObject.activeInHierarchy)
                {
                    Debug.Log($"[AUDIO] listener ready on '{existing.gameObject.name}'.");
                    return;
                }
            }

            var director = FindFirstObjectByType<PrototypeAudioDirector>(FindObjectsInactive.Include);
            GameObject host;
            if (director != null)
            {
                host = director.gameObject;
            }
            else
            {
                host = new GameObject("[PRESENTATION] Audio Listener");
                DontDestroyOnLoad(host);
            }

            var listener = host.GetComponent<AudioListener>() ?? host.AddComponent<AudioListener>();
            listener.enabled = true;
            Debug.Log($"[AUDIO] no scene AudioListener found; installed runtime 2D listener on '{host.name}'.");
        }
    }
}
