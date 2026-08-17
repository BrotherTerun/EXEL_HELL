using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Small visual guide for diegetic onboarding. The actual tutorial lives in protagonist/chat lines;
    /// this class only points at the UI/object currently being discussed.
    /// </summary>
    [DefaultExecutionOrder(2270)]
    public sealed class PrototypeTutorialVisualGuide : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo TurnField = typeof(ExcelHellPrototype).GetField("turn", Flags);
        private static readonly FieldInfo CellsField = typeof(ExcelHellPrototype).GetField("cells", Flags);
        private static readonly FieldInfo ViewsField = typeof(ExcelHellPrototype).GetField("views", Flags);

        private static readonly Color Cyan = new(0.01f, 0.92f, 0.92f, 1f);
        private static readonly Color Danger = new(0.98f, 0.10f, 0.13f, 1f);

        private ExcelHellPrototype prototype;
        private CellModel[,] cells;
        private ExcelHellCellView[,] views;
        private Canvas canvas;
        private readonly Dictionary<GameObject, Outline> outlines = new();
        private string levelId;
        private float levelBoundAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeTutorialVisualGuide>() != null) return;
            var root = new GameObject("[PRESENTATION] Contextual Tutorial Highlights");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeTutorialVisualGuide>();
        }

        private void LateUpdate()
        {
            if (PrototypeAuthoringMode.Active) return;

            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != prototype) Bind(current);
            ClearEnabled();
            if (prototype == null || canvas == null) return;

            DisableLegacyTutorialPanel();

            var id = PrototypeLevelRuntime.Current?.Id ?? string.Empty;
            if (id.StartsWith("01_", StringComparison.OrdinalIgnoreCase))
                ApplyL1Guide();
            else if (id.StartsWith("02_", StringComparison.OrdinalIgnoreCase))
                ApplyL2Guide();
        }

        private void Bind(ExcelHellPrototype owner)
        {
            ClearAll();
            prototype = owner;
            cells = null;
            views = null;
            canvas = null;
            levelId = PrototypeLevelRuntime.Current?.Id ?? string.Empty;
            levelBoundAt = Time.unscaledTime;
            if (prototype == null) return;
            cells = CellsField?.GetValue(prototype) as CellModel[,];
            views = ViewsField?.GetValue(prototype) as ExcelHellCellView[,];
            canvas = prototype.GetComponentsInChildren<Canvas>(true).FirstOrDefault();
        }

        private void ApplyL1Guide()
        {
            var turn = ReadTurn();

            // First protagonist line explicitly tells the player the assignment is in chat.
            if (turn == 0 && Time.unscaledTime - levelBoundAt < 7.0f)
            {
                Highlight(FindRect("Chat Reserved")?.gameObject, Cyan, 4f);
                return;
            }

            if (turn <= 1)
            {
                foreach (var cell in FormulaCells(FormulaKind.Sort))
                    Highlight(View(cell)?.gameObject, Cyan, 3f);
                return;
            }

            if (turn <= 3)
            {
                foreach (var cell in FormulaCells(FormulaKind.Sum))
                    Highlight(View(cell)?.gameObject, Cyan, 3f);
                return;
            }

            if (turn <= 5)
            {
                foreach (var goal in ReportTargets())
                    Highlight(View(goal)?.gameObject, Cyan, 3f);
            }
        }

        private void ApplyL2Guide()
        {
            if (cells == null) return;
            var corrupted = cells.Cast<CellModel>().FirstOrDefault(cell => cell != null && cell.State == CellState.Corrupted);
            if (corrupted == null) return;

            Highlight(View(corrupted)?.gameObject, Danger, 4f);
            Highlight(FindRect("Delete Reserved")?.gameObject, Danger, 4f);
        }

        private IEnumerable<CellModel> FormulaCells(FormulaKind kind)
        {
            if (cells == null) yield break;
            foreach (var cell in cells)
                if (cell != null && cell.State == CellState.Normal && cell.Formula == kind)
                    yield return cell;
        }

        private IEnumerable<CellModel> ReportTargets()
        {
            var layout = PrototypeLevelRuntime.Current?.GoalLayout;
            if (layout == null || cells == null) yield break;
            foreach (var placement in layout)
            {
                if (placement.Row < 0 || placement.Column < 0 || placement.Row >= cells.GetLength(0) || placement.Column >= cells.GetLength(1)) continue;
                yield return cells[placement.Row, placement.Column];
            }
        }

        private ExcelHellCellView View(CellModel cell) =>
            cell == null || views == null ? null : views[cell.Row, cell.Column];

        private int ReadTurn() => TurnField?.GetValue(prototype) is int value ? value : 0;

        private RectTransform FindRect(string name)
        {
            if (canvas == null) return null;
            return canvas.GetComponentsInChildren<RectTransform>(true)
                .FirstOrDefault(rect => rect != null && rect.gameObject.name == name);
        }

        private void Highlight(GameObject target, Color color, float thickness)
        {
            if (target == null) return;
            if (!outlines.TryGetValue(target, out var outline) || outline == null)
            {
                outline = target.GetComponent<Outline>() ?? target.AddComponent<Outline>();
                outlines[target] = outline;
            }

            var pulse = 0.64f + 0.36f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5.6f));
            var c = color;
            c.a = pulse;
            outline.effectColor = c;
            outline.effectDistance = new Vector2(thickness, -thickness);
            outline.useGraphicAlpha = false;
            outline.enabled = true;
        }

        private void DisableLegacyTutorialPanel()
        {
            var legacy = FindFirstObjectByType<PrototypeContextualTutorial>();
            if (legacy != null && legacy.enabled)
                legacy.enabled = false;
        }

        private void ClearEnabled()
        {
            foreach (var outline in outlines.Values)
                if (outline != null) outline.enabled = false;
        }

        private void ClearAll()
        {
            foreach (var outline in outlines.Values)
                if (outline != null) outline.enabled = false;
            outlines.Clear();
        }
    }
}
