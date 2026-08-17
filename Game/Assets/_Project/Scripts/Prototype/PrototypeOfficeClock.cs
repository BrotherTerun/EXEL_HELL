using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    [DefaultExecutionOrder(1960)]
    public sealed class PrototypeOfficeClock : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private const float ClockBaseVolume = 0.45f;
        private static readonly FieldInfo TurnField = typeof(ExcelHellPrototype).GetField("turn", Flags);

        private ExcelHellPrototype prototype;
        private Canvas canvas;
        private RectTransform clockSlot;
        private Text clockText;

        private AudioSource clockAudio;
        private AudioClip clock01;
        private AudioClip clock02;
        private AudioClip clock03;
        private AudioClip activeClockClip;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeOfficeClock>() != null) return;
            var root = new GameObject("[PRESENTATION] Office Clock");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeOfficeClock>();
        }

        private void Awake()
        {
            clockAudio = gameObject.AddComponent<AudioSource>();
            clockAudio.loop = true;
            clockAudio.playOnAwake = false;
            clockAudio.spatialBlend = 0f;
            clockAudio.volume = ClockBaseVolume;

            clock01 = LoadClockClip("clock_01");
            clock02 = LoadClockClip("clock_02");
            clock03 = LoadClockClip("clock_03");
        }

        private void Update()
        {
            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != prototype) Bind(current);
            if (prototype == null) return;
            if (clockText == null) TryBuild();
            RefreshClock();
        }

        private void Bind(ExcelHellPrototype owner)
        {
            prototype = owner;
            canvas = null;
            clockSlot = null;
            if (clockText != null) Destroy(clockText.gameObject);
            clockText = null;

            if (prototype == null)
            {
                StopClockAudio();
                return;
            }

            var canvases = prototype.GetComponentsInChildren<Canvas>(true);
            canvas = canvases.Length > 0 ? canvases[0] : null;
        }

        private void TryBuild()
        {
            if (canvas == null) return;
            foreach (var rect in canvas.GetComponentsInChildren<RectTransform>(true))
            {
                if (rect.gameObject.name != "Office Clock Display") continue;
                clockSlot = rect;
                break;
            }
            if (clockSlot == null) return;

            var go = new GameObject("Office Clock Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(clockSlot, false);
            var rectTransform = go.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            clockText = go.GetComponent<Text>();
            clockText.font = PrototypeVisualTheme.MonoFont;
            clockText.fontSize = 34;
            clockText.fontStyle = FontStyle.Bold;
            clockText.alignment = TextAnchor.MiddleCenter;
            clockText.color = new Color(0.90f, 0.16f, 0.12f, 1f);
            clockText.raycastTarget = false;

            Debug.Log("[UI-CLOCK] Office wall clock presentation bound.");
        }

        private void RefreshClock()
        {
            if (prototype == null) return;
            if (TurnField?.GetValue(prototype) is not int turn) return;

            var maxTurns = Mathf.Max(1, PrototypeLevelRuntime.Current?.MaxTurns ?? 1);
            var clampedTurn = Mathf.Clamp(turn, 0, maxTurns);
            var minutes = Mathf.RoundToInt(540f * clampedTurn / maxTurns);
            var total = 9 * 60 + minutes;

            if (clockText != null)
                clockText.text = $"{total / 60:00}:{total % 60:00}";

            RefreshClockAudio(total);
        }

        private void RefreshClockAudio(int totalMinutes)
        {
            var desired = totalMinutes < 13 * 60
                ? clock01
                : totalMinutes < 16 * 60
                    ? clock02
                    : clock03;

            if (desired == activeClockClip) return;

            activeClockClip = desired;
            if (clockAudio == null) return;

            clockAudio.Stop();
            clockAudio.clip = desired;
            if (desired != null)
            {
                clockAudio.Play();
                Debug.Log($"[AUDIO/CLOCK] switched to {desired.name} at {totalMinutes / 60:00}:{totalMinutes % 60:00}.");
            }
        }

        private void StopClockAudio()
        {
            activeClockClip = null;
            if (clockAudio == null) return;
            clockAudio.Stop();
            clockAudio.clip = null;
        }

        private static AudioClip LoadClockClip(string name)
        {
            string[] paths =
            {
                $"SFX/EXCEL_HELL_Audio_Kit_v1/{name}",
                $"SFX/Clock/{name}",
                $"SFX/{name}",
                name
            };

            foreach (var path in paths)
            {
                var clip = Resources.Load<AudioClip>(path);
                if (clip != null) return clip;
            }

            Debug.LogWarning($"[AUDIO/CLOCK] missing {name}; put it under a Resources folder (preferably Resources/SFX). ");
            return null;
        }
    }
}
