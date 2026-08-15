using System.Reflection;
using ExcelHell.Prototype;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Application
{
    /// <summary>
    /// Presentation-only adapter between the frozen prototype UI and the application shell.
    /// Keeps shell concerns out of the graybox gameplay class.
    /// </summary>
    [DefaultExecutionOrder(900)]
    public sealed class PrototypeApplicationBridge : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo LocalizationField = typeof(ExcelHellPrototype).GetField("loc", Flags);
        private static readonly FieldInfo HelpTextField = typeof(ExcelHellPrototype).GetField("helpText", Flags);
        private static readonly MethodInfo RefreshAllMethod = typeof(ExcelHellPrototype).GetMethod("RefreshAll", Flags);

        private ExcelHellPrototype prototype;
        private Button menuButton;
        private Text menuLabel;
        private string appliedLanguage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<PrototypeApplicationBridge>() != null) return;
            var helper = new GameObject("EXEL HELL Application Bridge").AddComponent<PrototypeApplicationBridge>();
            DontDestroyOnLoad(helper.gameObject);
        }

        private void LateUpdate()
        {
            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != prototype) Bind(current);
            if (prototype == null) return;

            var desiredLanguage = ExcelHellApplication.CurrentLanguageCode;
            if (desiredLanguage != appliedLanguage)
                ApplyLanguage(desiredLanguage);

            if (menuLabel != null)
                menuLabel.text = desiredLanguage == "en" ? "MENU" : "МЕНЮ";
        }

        private void Bind(ExcelHellPrototype owner)
        {
            prototype = owner;
            menuButton = null;
            menuLabel = null;
            appliedLanguage = null;
            if (prototype == null) return;

            var buttons = prototype.GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                if (button.gameObject.name == "ui.language")
                {
                    menuButton = button;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(ExcelHellApplication.OpenGameplayMenu);
                    menuLabel = button.GetComponentInChildren<Text>(true);
                }
                else if (button.gameObject.name == "ui.reset")
                {
                    button.gameObject.SetActive(false);
                }
                else if (button.gameObject.name == "ui.submit")
                {
                    var rect = button.GetComponent<RectTransform>();
                    if (rect != null)
                        rect.sizeDelta = new Vector2(600f, rect.sizeDelta.y);
                }
            }

            var helpText = HelpTextField?.GetValue(prototype) as Text;
            if (helpText != null) helpText.gameObject.SetActive(false);

            ApplyLanguage(ExcelHellApplication.CurrentLanguageCode);
        }

        private void ApplyLanguage(string code)
        {
            if (prototype == null) return;
            var localization = LocalizationField?.GetValue(prototype) as PrototypeLocalization;
            if (localization == null) return;

            var wantsEnglish = code == "en";
            var isEnglish = localization.Language == PrototypeLanguage.English;
            if (wantsEnglish != isEnglish)
                localization.ToggleLanguage();

            appliedLanguage = wantsEnglish ? "en" : "ru";
            RefreshAllMethod?.Invoke(prototype, null);
        }
    }
}
