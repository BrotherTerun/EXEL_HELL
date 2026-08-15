using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ExcelHell.EditorTools
{
    public static class BuildPlaytest
    {
        [MenuItem("EXEL HELL/Build/Windows Realtime Playtest")]
        public static void BuildWindowsPlaytest()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? ".";
            var outputDir = Path.Combine(projectRoot, "Builds", "EXEL_HELL_Realtime");
            Directory.CreateDirectory(outputDir);
            var outputPath = Path.Combine(outputDir, "EXEL_HELL_Realtime.exe");

            var previousName = PlayerSettings.productName;
            PlayerSettings.productName = "EXEL HELL - Realtime Playtest";
            try
            {
                var options = new BuildPlayerOptions
                {
                    scenes = new[] { "Assets/Scenes/SampleScene.unity" },
                    locationPathName = outputPath,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None
                };

                var report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result == BuildResult.Succeeded)
                    Debug.Log($"EXEL HELL playtest build ready: {outputPath}");
                else
                    Debug.LogError($"EXEL HELL playtest build failed: {report.summary.result}");
            }
            finally
            {
                PlayerSettings.productName = previousName;
            }
        }
    }
}
