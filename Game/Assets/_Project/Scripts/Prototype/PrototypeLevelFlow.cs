using System;
using System.Reflection;
using ExcelHell.Application;
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
        private bool completionSaved;

        public bool HasPrototype => prototype != null;
        public bool ReportAcceptedForPresentation => ReportAccepted();
        public bool IsLastLevel => PrototypeLevelRuntime.IsLast;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<PrototypeLevelFlow>() != null) return;
            var flow = new GameObject("EXEL HELL Level Flow").AddComponent<PrototypeLevelFlow>();
            DontDestroyOnLoad(flow.gameObject);
        }

        private void LateUpdate()
        {
            if (ExcelHellApplication.ShellAvailable && !ExcelHellApplication.GameplayActive)
            {
                prototype = null;
                return;
            }

            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != prototype)
            {
                prototype = current;
                completionSaved = false;
            }
            if (prototype == null) return;

            if (!PrototypeLevelRuntime.Current.RefEnabled)
            {
                var hadAnomalyTelegraph = PendingSpawnField?.GetValue(prototype) != null || CurrentIntentField?.GetValue(prototype) != null;
                PendingSpawnField?.SetValue(prototype, null);
                CurrentIntentField?.SetValue(prototype, null);
                if (hadAnomalyTelegraph) RefreshAllMethod?.Invoke(prototype, null);

                // Kept only as a debug value inside the hidden developer rail.
                var intentText = IntentTextField?.GetValue(prototype) as Text;
                if (intentText != null) intentText.text = "АНОМАЛИЙ НЕТ / NO ANOMALIES";
            }

            if (!completionSaved && PrototypeLevelRuntime.IsLast && ReportAccepted())
            {
                completionSaved = true;
                ExcelHellApplication.NotifyCampaignCompleted(PrototypeLevelRuntime.CurrentIndex);
            }
        }

        public void AdvanceFromPresentation()
        {
            if (prototype == null || !ReportAccepted() || PrototypeLevelRuntime.IsLast) return;
            if (!PrototypeLevelRuntime.Advance()) return;

            ExcelHellApplication.NotifyLevelAdvanced(PrototypeLevelRuntime.CurrentIndex);
            var old = prototype;
            prototype = null;
            completionSaved = false;
            if (old != null) Destroy(old.gameObject);
            new GameObject("EXEL HELL Prototype").AddComponent<ExcelHellPrototype>();
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
    }
}
