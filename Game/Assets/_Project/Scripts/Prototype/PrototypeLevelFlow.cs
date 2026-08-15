using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    public sealed class PrototypeLevelFlow : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo PendingSpawnField = typeof(ExcelHellPrototype).GetField("pendingSpawnIntent", Flags);
        private static readonly FieldInfo CurrentIntentField = typeof(ExcelHellPrototype).GetField("currentIntent", Flags);
        private static readonly FieldInfo IntentTextField = typeof(ExcelHellPrototype).GetField("intentText", Flags);
        private static readonly FieldInfo StatusTextField = typeof(ExcelHellPrototype).GetField("statusText", Flags);
        private static readonly MethodInfo RefreshAllMethod = typeof(ExcelHellPrototype).GetMethod("RefreshAll", Flags);

        private ExcelHellPrototype prototype;
        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<PrototypeLevelFlow>() != null) return;
            var flow = new GameObject("EXEL HELL Level Flow").AddComponent<PrototypeLevelFlow>();
            DontDestroyOnLoad(flow.gameObject);
        }

        private void LateUpdate()
        {
            if (prototype == null)
                prototype = FindFirstObjectByType<ExcelHellPrototype>();
            if (prototype == null) return;

            if (!PrototypeLevelRuntime.Current.RefEnabled)
            {
                var hadAnomalyTelegraph = PendingSpawnField?.GetValue(prototype) != null ||
                                          CurrentIntentField?.GetValue(prototype) != null;
                PendingSpawnField?.SetValue(prototype, null);
                CurrentIntentField?.SetValue(prototype, null);
                if (hadAnomalyTelegraph)
                    RefreshAllMethod?.Invoke(prototype, null);

                var intentText = IntentTextField?.GetValue(prototype) as Text;
                if (intentText != null)
                    intentText.text = "АНОМАЛИЙ НЕТ / NO ANOMALIES";
            }
        }

        private bool ReportAccepted()
        {
            if (prototype == null) return false;
            var statusText = StatusTextField?.GetValue(prototype) as Text;
            if (statusText == null) return false;
            var text = statusText.text ?? string.Empty;
            return text.IndexOf("ОТЧЁТ ПРИНЯТ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("REPORT ACCEPTED", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void OnGUI()
        {
            EnsureStyles();
            var level = PrototypeLevelRuntime.Current;
            GUI.Label(new Rect(18, Screen.height - 48, 820, 34),
                $"УРОВЕНЬ {PrototypeLevelRuntime.CurrentIndex + 1}/{PrototypeLevelCatalog.Count}: {level.NameRu}  /  {level.NameEn}", labelStyle);

            if (!ReportAccepted()) return;

            if (PrototypeLevelRuntime.IsLast)
            {
                GUI.Label(new Rect(Screen.width - 450, Screen.height - 48, 430, 34),
                    "ТЕСТ ЗАВЕРШЁН / PLAYTEST COMPLETE", labelStyle);
                return;
            }

            if (GUI.Button(new Rect(Screen.width - 360, Screen.height - 58, 340, 42),
                    "СЛЕДУЮЩИЙ УРОВЕНЬ / NEXT LEVEL", buttonStyle))
            {
                if (!PrototypeLevelRuntime.Advance()) return;
                var old = prototype;
                prototype = null;
                Destroy(old.gameObject);
                new GameObject("EXEL HELL Prototype").AddComponent<ExcelHellPrototype>();
            }
        }

        private void EnsureStyles()
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft
                };
            }

            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 15,
                    fontStyle = FontStyle.Bold
                };
            }
        }
    }
}
