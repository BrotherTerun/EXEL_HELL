using UnityEngine;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Draws a fully opaque backing plate behind the existing tutorial panel.
    /// Tutorial text and controls remain owned by PrototypePlaytestUsability.
    /// </summary>
    public sealed class PrototypeTutorialOpaqueBackground : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<PrototypeTutorialOpaqueBackground>() != null) return;
            var background = new GameObject("EXCEL HELL Tutorial Opaque Background")
                .AddComponent<PrototypeTutorialOpaqueBackground>();
            DontDestroyOnLoad(background.gameObject);
        }

        private void OnGUI()
        {
            if (!PrototypePlaytestUsability.TutorialOpen || PrototypeLevelRuntime.CurrentIndex != 0) return;

            // Larger GUI.depth values render behind the tutorial's default IMGUI depth.
            GUI.depth = 10;

            var width = Mathf.Min(760f, Screen.width - 80f);
            var height = Mathf.Min(500f, Screen.height - 80f);
            var rect = new Rect(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width,
                height);

            var previousColor = GUI.color;
            GUI.color = new Color(0.94f, 0.95f, 0.96f, 1f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = previousColor;
        }
    }
}
