using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Small narrative-consistency patch kept separate from the upcoming HUD redesign.
    /// 1) Connected department dialogue keeps a stable authored speaker.
    /// 2) The first L2 CELL MESSAGE owns the canonical cyan/turquoise visual language.
    /// Presentation only: no gameplay or narrative trigger mutation.
    /// </summary>
    [DefaultExecutionOrder(2165)]
    public sealed class PrototypeNarrativeConsistencyPatch : MonoBehaviour
    {
        private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly string[] DepartmentNamesRu =
            { "Ирина", "Макс", "Лена", "Антон", "Вика", "Денис", "Катя", "Саша" };
        private static readonly string[] DepartmentNamesEn =
            { "Ira", "Max", "Lena", "Anton", "Vika", "Denis", "Katya", "Sasha" };

        private static readonly Color Cyan = new(0.01f, 0.92f, 0.92f, 1f);
        private static readonly Color DimTurquoise = new(0.035f, 0.22f, 0.22f, 0.88f);

        private PrototypeProductionHud hud;
        private FieldInfo departmentMessagesField;
        private FieldInfo chatSenderField;
        private FieldInfo chatMessageField;
        private float nextChatPass;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeNarrativeConsistencyPatch>() != null) return;
            var root = new GameObject("[PRESENTATION] Narrative Consistency Patch");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeNarrativeConsistencyPatch>();
        }

        private void LateUpdate()
        {
            PatchL2CellMessagePalette();

            if (Time.unscaledTime < nextChatPass) return;
            nextChatPass = Time.unscaledTime + 0.12f;
            PatchDepartmentSpeakers();
        }

        private void PatchDepartmentSpeakers()
        {
            var current = FindFirstObjectByType<PrototypeProductionHud>();
            if (current != hud)
            {
                hud = current;
                departmentMessagesField = null;
                chatSenderField = null;
                chatMessageField = null;
            }
            if (hud == null) return;

            departmentMessagesField ??= typeof(PrototypeProductionHud).GetField("departmentMessages", InstancePrivate);
            if (departmentMessagesField?.GetValue(hud) is not IEnumerable messages) return;

            foreach (var entry in messages)
            {
                if (entry == null) continue;
                var type = entry.GetType();
                chatSenderField ??= type.GetField("Sender", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                chatMessageField ??= type.GetField("Message", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (chatSenderField == null || chatMessageField == null) return;

                var message = chatMessageField.GetValue(entry) as string ?? string.Empty;
                var speakerKey = SpeakerGroupFor(message);
                if (speakerKey == null) continue;

                var names = IsRussian() ? DepartmentNamesRu : DepartmentNamesEn;
                var index = (int)(StableHash("dept-speaker:" + speakerKey) % (uint)names.Length);
                chatSenderField.SetValue(entry, names[index]);
            }
        }

        private static string SpeakerGroupFor(string message)
        {
            if (string.IsNullOrEmpty(message)) return null;

            // L2: the person who asks after Morozov is also the one who wrote to him.
            if (message == "А Морозов сегодня будет?" || message == "Я ему написал, пока не отвечает.")
                return "morozov-worried";

            // L3: one employee consistently insists Morozov exists and remembers his desk/history.
            if (message == "Я всё-таки не понимаю, где Морозов." ||
                message == "Он же сидит через два стола от меня." ||
                message == "Есть. Он в прошлом месяце мне смену закрывал." ||
                message == "Не нашёл.")
                return "morozov-remembers";

            // L3: the contradictory voice stays one person as well.
            if (message == "Там никто не сидит." || message == "Не было никакого Морозова.")
                return "morozov-denies";

            // L4 mail thread: finder speaks again when describing the message / failure to open it.
            if (message == "Нашёл письмо от Морозова." ||
                message == "Обычное. Он мне таблицу присылал." ||
                message == "Не могу открыть.")
                return "morozov-mail";

            // L4 responding participant asking about the mail / sender.
            if (message == "Какое ещё письмо?" || message == "А отправитель кто?" || message == "...")
                return "morozov-mail-reply";

            return null;
        }

        private static void PatchL2CellMessagePalette()
        {
            var levelId = PrototypeLevelRuntime.Current?.Id ?? string.Empty;
            if (!levelId.StartsWith("02_", StringComparison.OrdinalIgnoreCase)) return;

            var root = FindObjectsByType<RectTransform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .FirstOrDefault(r => r != null && r.gameObject.name == "Cell Message L2_CELL_SEEN");
            if (root == null) return;

            var image = root.GetComponent<Image>();
            if (image != null)
            {
                image.color = DimTurquoise;
                image.raycastTarget = true;
            }

            var text = root.GetComponentsInChildren<Text>(true).FirstOrDefault();
            if (text != null)
            {
                text.color = Cyan;
                text.fontStyle = FontStyle.Bold;
            }
        }

        private static bool IsRussian() =>
            string.Equals(ExcelHell.Application.ExcelHellApplication.CurrentLanguageCode, "ru", StringComparison.OrdinalIgnoreCase);

        private static uint StableHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261u;
                foreach (var c in value ?? string.Empty)
                {
                    hash ^= c;
                    hash *= 16777619u;
                }
                return hash == 0u ? 1u : hash;
            }
        }
    }
}
