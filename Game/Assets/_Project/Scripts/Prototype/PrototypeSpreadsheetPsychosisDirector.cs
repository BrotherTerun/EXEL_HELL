using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Release-safe pacing layer for Spreadsheet Psychosis v2.
    /// V2 remains responsible for authoring the visuals; this director decides which manifestations are actually
    /// allowed to stay on screen and moderates their displacement, opacity and lifetime so L4 stays playable.
    /// It never touches gameplay cells, tokens, selection or turns.
    /// </summary>
    [DefaultExecutionOrder(2280)]
    public sealed class PrototypeSpreadsheetPsychosisDirector : MonoBehaviour
    {
        private sealed class Tracked
        {
            public GameObject Root;
            public ManifestationType Type;
            public float StartedAt;
            public float Lifetime;
            public float MotionScale;
            public readonly Dictionary<RectTransform, Vector2> BasePositions = new();
        }

        private readonly Dictionary<int, Tracked> tracked = new();
        private RectTransform spreadsheet;
        private float lastAcceptedAt = -100f;
        private int gateCounter;
        private string levelId = string.Empty;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeSpreadsheetPsychosisDirector>() != null) return;
            var root = new GameObject("[PRESENTATION] Spreadsheet Psychosis Director");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeSpreadsheetPsychosisDirector>();
        }

        private void LateUpdate()
        {
            if (PrototypeAuthoringMode.Active) return;

            var currentLevel = PrototypeLevelRuntime.Current?.Id ?? string.Empty;
            if (!string.Equals(currentLevel, levelId, StringComparison.OrdinalIgnoreCase))
            {
                levelId = currentLevel;
                tracked.Clear();
                spreadsheet = null;
                lastAcceptedAt = -100f;
                gateCounter = 0;
            }

            BindSpreadsheet();
            if (spreadsheet == null) return;

            DiscoverNewManifestations();
            ModerateAcceptedManifestations();
            CleanupDead();
        }

        private void BindSpreadsheet()
        {
            if (spreadsheet != null) return;
            var prototype = FindFirstObjectByType<ExcelHellPrototype>();
            spreadsheet = prototype == null
                ? null
                : prototype.GetComponentsInChildren<RectTransform>(true)
                    .FirstOrDefault(r => r != null && r.gameObject.name == "Spreadsheet");
        }

        private void DiscoverNewManifestations()
        {
            // FalseKey lives inside a cell; every other v2 manifestation is a direct Spreadsheet overlay root.
            var roots = spreadsheet.GetComponentsInChildren<RectTransform>(true)
                .Where(r => r != null && r.gameObject.name.StartsWith("Psychosis v2 ", StringComparison.Ordinal))
                .Where(r => IsManifestationRoot(r.gameObject.name))
                .ToList();

            foreach (var rect in roots)
            {
                var go = rect.gameObject;
                var id = go.GetInstanceID();
                if (tracked.ContainsKey(id)) continue;

                var type = ParseType(go.name);
                var day = CurrentDay();
                if (!ShouldAccept(type, day))
                {
                    Debug.Log($"[PSYCHOSIS/DIRECTOR] Suppressed {type} day={day} (pacing/weight gate).");
                    Destroy(go);
                    continue;
                }

                var spec = GetSpec(type, day);
                var entry = new Tracked
                {
                    Root = go,
                    Type = type,
                    StartedAt = Time.unscaledTime,
                    Lifetime = spec.Lifetime,
                    MotionScale = spec.MotionScale
                };

                foreach (var child in go.GetComponentsInChildren<RectTransform>(true))
                    if (child != null)
                        entry.BasePositions[child] = child.anchoredPosition;

                ApplyOpacity(go, spec.OpacityScale, type == ManifestationType.FalseKey ? 1.08f : 1f);
                tracked[id] = entry;
                lastAcceptedAt = Time.unscaledTime;
                gateCounter++;

                Debug.Log($"[PSYCHOSIS/DIRECTOR] Accepted {type} day={day} lifetime={spec.Lifetime:0.0}s motion={spec.MotionScale:0.00}.");
            }
        }

        private bool ShouldAccept(ManifestationType type, int day)
        {
            // L3 is suspicion, not a broken worksheet. Keep only semantic/soft hallucinations.
            if (day <= 3 && type != ManifestationType.FalseKey && type != ManifestationType.GhostSelection)
                return false;

            var now = Time.unscaledTime;
            var minGap = day >= 4 ? 4.5f : 5.5f;
            if (type == ManifestationType.FalseKey) minGap -= 0.8f;
            if (now - lastAcceptedAt < minGap) return false;

            // Never stack several psychosis events over an already difficult puzzle.
            if (tracked.Values.Any(t => t?.Root != null)) return false;

            if (day < 4) return true;

            // L4 weighting: semantic/soft effects should dominate; physical tears are accents, not wallpaper.
            var probability = type switch
            {
                ManifestationType.FalseKey => 1.00f,
                ManifestationType.GhostSelection => 0.95f,
                ManifestationType.ColumnDrift => 0.62f,
                ManifestationType.RowDrift => 0.62f,
                ManifestationType.CellEscape => 0.48f,
                ManifestationType.GridTear => 0.38f,
                _ => 0.50f
            };

            var sample = StableSample(levelId, type, gateCounter);
            return sample <= probability;
        }

        private void ModerateAcceptedManifestations()
        {
            foreach (var entry in tracked.Values.ToList())
            {
                if (entry?.Root == null) continue;

                if (Time.unscaledTime - entry.StartedAt >= entry.Lifetime)
                {
                    Destroy(entry.Root);
                    continue;
                }

                // V2 computes its animation first. We then shrink only the animated delta around the initial pose.
                foreach (var pair in entry.BasePositions.ToList())
                {
                    var rect = pair.Key;
                    if (rect == null) continue;
                    var baseline = pair.Value;
                    rect.anchoredPosition = baseline + (rect.anchoredPosition - baseline) * entry.MotionScale;
                }
            }
        }

        private void CleanupDead()
        {
            var dead = tracked.Where(pair => pair.Value == null || pair.Value.Root == null)
                .Select(pair => pair.Key)
                .ToList();
            foreach (var id in dead) tracked.Remove(id);
        }

        private static DirectorSpec GetSpec(ManifestationType type, int day)
        {
            if (day <= 3)
            {
                return type switch
                {
                    ManifestationType.FalseKey => new DirectorSpec(3.0f, 0.75f, 0.95f),
                    ManifestationType.GhostSelection => new DirectorSpec(1.8f, 0.45f, 0.72f),
                    _ => new DirectorSpec(1.5f, 0.40f, 0.65f)
                };
            }

            return type switch
            {
                ManifestationType.FalseKey => new DirectorSpec(3.2f, 0.78f, 1.00f),
                ManifestationType.GhostSelection => new DirectorSpec(2.0f, 0.48f, 0.78f),
                ManifestationType.ColumnDrift => new DirectorSpec(2.1f, 0.52f, 0.72f),
                ManifestationType.RowDrift => new DirectorSpec(2.0f, 0.50f, 0.72f),
                ManifestationType.CellEscape => new DirectorSpec(1.9f, 0.46f, 0.68f),
                ManifestationType.GridTear => new DirectorSpec(0.85f, 0.38f, 0.62f),
                _ => new DirectorSpec(2.0f, 0.50f, 0.72f)
            };
        }

        private static void ApplyOpacity(GameObject root, float scale, float falseKeyBoost)
        {
            foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic == null) continue;
                var color = graphic.color;
                color.a = Mathf.Clamp01(color.a * scale * falseKeyBoost);
                graphic.color = color;
            }
        }

        private static bool IsManifestationRoot(string name)
        {
            return name == "Psychosis v2 False Key" ||
                   name == "Psychosis v2 Ghost Selection" ||
                   name == "Psychosis v2 Column Drift" ||
                   name == "Psychosis v2 Row Drift" ||
                   name == "Psychosis v2 Cell Escape" ||
                   name == "Psychosis v2 Grid Tear";
        }

        private static ManifestationType ParseType(string name)
        {
            if (name.EndsWith("False Key", StringComparison.Ordinal)) return ManifestationType.FalseKey;
            if (name.EndsWith("Ghost Selection", StringComparison.Ordinal)) return ManifestationType.GhostSelection;
            if (name.EndsWith("Column Drift", StringComparison.Ordinal)) return ManifestationType.ColumnDrift;
            if (name.EndsWith("Row Drift", StringComparison.Ordinal)) return ManifestationType.RowDrift;
            if (name.EndsWith("Cell Escape", StringComparison.Ordinal)) return ManifestationType.CellEscape;
            if (name.EndsWith("Grid Tear", StringComparison.Ordinal)) return ManifestationType.GridTear;
            return ManifestationType.Unknown;
        }

        private static int CurrentDay()
        {
            var id = PrototypeLevelRuntime.Current?.Id ?? string.Empty;
            if (id.StartsWith("04_", StringComparison.OrdinalIgnoreCase)) return 4;
            if (id.StartsWith("03_", StringComparison.OrdinalIgnoreCase)) return 3;
            if (id.StartsWith("02_", StringComparison.OrdinalIgnoreCase)) return 2;
            return 1;
        }

        private static float StableSample(string level, ManifestationType type, int counter)
        {
            unchecked
            {
                uint hash = 2166136261u;
                var text = (level ?? string.Empty) + ":" + type + ":" + counter;
                foreach (var c in text)
                {
                    hash ^= c;
                    hash *= 16777619u;
                }
                return (hash & 0xFFFFu) / 65535f;
            }
        }

        private readonly struct DirectorSpec
        {
            public readonly float Lifetime;
            public readonly float MotionScale;
            public readonly float OpacityScale;

            public DirectorSpec(float lifetime, float motionScale, float opacityScale)
            {
                Lifetime = lifetime;
                MotionScale = motionScale;
                OpacityScale = opacityScale;
            }
        }

        private enum ManifestationType
        {
            Unknown,
            FalseKey,
            GhostSelection,
            ColumnDrift,
            RowDrift,
            CellEscape,
            GridTear
        }
    }
}
