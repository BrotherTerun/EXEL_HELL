using System;
using ExcelHell.Prototype;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Application
{
    /// <summary>
    /// Final jam-release cleanup for the application shell.
    /// Removes unfinished slot-oriented save/load affordances and keeps the system Help screen
    /// sourced from the same canonical ui.help text as the in-game '?' window.
    /// </summary>
    [DefaultExecutionOrder(32500)]
    public sealed class PrototypeApplicationReleaseCleanup : MonoBehaviour
    {
        private ExcelHellApplication application;
        private Canvas appCanvas;
        private string lastLanguage;
        private float nextRefreshAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeApplicationReleaseCleanup>(FindObjectsInactive.Include) != null) return;

            var root = new GameObject("[PRESENTATION] Application Release Cleanup");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeApplicationReleaseCleanup>();
        }

        private void Update()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (Time.unscaledTime < nextRefreshAt) return;
            nextRefreshAt = Time.unscaledTime + 0.25f;

            var current = FindFirstObjectByType<ExcelHellApplication>(FindObjectsInactive.Include);
            if (current != application)
            {
                application = current;
                appCanvas = null;
                lastLanguage = null;
            }

            if (application == null) return;
            if (appCanvas == null)
            {
                foreach (var canvas in application.GetComponentsInChildren<Canvas>(true))
                {
                    if (canvas.gameObject.name != "Application Canvas") continue;
                    appCanvas = canvas;
                    break;
                }
            }
            if (appCanvas == null) return;

            RemoveSaveLoadButtons();
            SyncHelpBody();
        }

        private void RemoveSaveLoadButtons()
        {
            foreach (var button in appCanvas.GetComponentsInChildren<Button>(true))
            {
                if (button == null) continue;
                var text = button.GetComponentInChildren<Text>(true);
                if (text == null) continue;

                var label = (text.text ?? string.Empty).Trim().ToUpperInvariant();
                if (!IsSaveLoadLabel(label)) continue;

                button.gameObject.SetActive(false);
            }

            // The old slot screen is intentionally kept constructed for compatibility, but is not reachable.
            var loadScreen = FindDirectChild(appCanvas.transform, "Load Game");
            if (loadScreen != null && loadScreen.gameObject.activeSelf)
                loadScreen.gameObject.SetActive(false);
        }

        private static bool IsSaveLoadLabel(string label)
        {
            return label == "СОХРАНИТЬ" ||
                   label == "SAVE" ||
                   label == "ЗАГРУЗИТЬ" ||
                   label == "LOAD" ||
                   label == "ЗАГРУЗИТЬ СЛОТ" ||
                   label == "LOAD SLOT" ||
                   label == "УДАЛИТЬ СОХРАНЕНИЕ" ||
                   label == "DELETE SAVE";
        }

        private void SyncHelpBody()
        {
            var language = ExcelHellApplication.CurrentLanguageCode ?? "ru";
            if (string.Equals(language, lastLanguage, StringComparison.OrdinalIgnoreCase) &&
                FindHelpBody() is { text.Length: > 0 })
                return;

            var body = FindHelpBody();
            if (body == null) return;

            var localization = new PrototypeLocalization();
            if (string.Equals(language, "en", StringComparison.OrdinalIgnoreCase))
                localization.ToggleLanguage();

            body.text = localization.Get("ui.help");
            body.alignment = TextAnchor.UpperLeft;
            body.fontSize = 16;
            lastLanguage = language;
        }

        private Text FindHelpBody()
        {
            if (appCanvas == null) return null;
            var helpScreen = FindDirectChild(appCanvas.transform, "Help");
            var panel = FindDirectChild(helpScreen, "Panel");
            if (panel == null) return null;

            var directTextIndex = 0;
            for (var i = 0; i < panel.childCount; i++)
            {
                var text = panel.GetChild(i).GetComponent<Text>();
                if (text == null) continue;
                if (directTextIndex == 1) return text;
                directTextIndex++;
            }
            return null;
        }

        private static RectTransform FindDirectChild(Transform parent, string name)
        {
            if (parent == null) return null;
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i) as RectTransform;
                if (child != null && child.gameObject.name == name) return child;
            }
            return null;
        }
    }
}
