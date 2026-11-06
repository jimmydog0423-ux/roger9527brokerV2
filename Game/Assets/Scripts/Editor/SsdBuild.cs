using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SSD.EditorTools
{
    /// <summary>
    /// 輸出 Windows 版遊戲到 H:\水蛇洞經紀人\Build\水蛇洞經紀人.exe（專案資料夾的上一層）。
    /// 編輯器選單：水蛇洞 / 輸出 Windows 遊戲
    /// 命令列：Unity.exe -batchmode -quit -projectPath Game -executeMethod SSD.EditorTools.SsdBuild.BuildWindows
    /// </summary>
    public static class SsdBuild
    {
        public const string ExeName = "水蛇洞經紀人.exe";

        public static string OutputDir
        {
            get
            {
                string project = Directory.GetParent(Application.dataPath).FullName;
                string parent = Directory.GetParent(project).FullName;
                return Path.Combine(parent, "Build");
            }
        }

        [MenuItem("水蛇洞/輸出 Windows 遊戲")]
        public static void BuildWindows()
        {
            if (!File.Exists(SsdProjectSetup.ScenePath)) SsdProjectSetup.CreateMainScene();
            SsdProjectSetup.ApplyPlayerSettings();
            Directory.CreateDirectory(OutputDir);
            string exe = Path.Combine(OutputDir, ExeName);
            BuildPlayerOptions opts = new BuildPlayerOptions
            {
                scenes = new[] { SsdProjectSetup.ScenePath },
                locationPathName = exe,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(opts);
            bool ok = report.summary.result == BuildResult.Succeeded;
            Debug.Log("[水蛇洞] 輸出" + (ok ? "完成：" + exe : "失敗：" + report.summary.result));
            if (Application.isBatchMode)
            {
                if (!ok) EditorApplication.Exit(1);
                return;
            }
            if (ok) EditorUtility.RevealInFinder(exe);
            else EditorUtility.DisplayDialog("水蛇洞", "輸出失敗，請看 Console 的錯誤訊息。", "好");
        }

        // Run with -buildTarget WebGL so Unity imports assets for the browser player.
        public static void BuildWeb()
        {
            if (!File.Exists(SsdProjectSetup.ScenePath)) SsdProjectSetup.CreateMainScene();
            SsdProjectSetup.ApplyPlayerSettings();
            string output = Path.Combine(Directory.GetParent(OutputDir).FullName, "WebBuild");

            var compression = PlayerSettings.WebGL.compressionFormat;
            bool fallback = PlayerSettings.WebGL.decompressionFallback;
            bool hashedNames = PlayerSettings.WebGL.nameFilesAsHashes;
            string template = PlayerSettings.WebGL.template;
            try
            {
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
                // GitHub Pages serves static files without custom Content-Encoding headers.
                PlayerSettings.WebGL.decompressionFallback = true;
                // Cached files from a previous deployment must not mix with the current build.
                PlayerSettings.WebGL.nameFilesAsHashes = true;
                PlayerSettings.WebGL.template = "PROJECT:SsdWeb";
                BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { SsdProjectSetup.ScenePath },
                    locationPathName = output,
                    target = BuildTarget.WebGL,
                    options = BuildOptions.None
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new System.InvalidOperationException("網頁版輸出失敗：" + report.summary.result);

                File.WriteAllText(Path.Combine(output, ".nojekyll"), "");
                const string fontLicense = "Cubic11-LICENSE.txt";
                File.Copy(Path.Combine(Application.dataPath, "Resources", "Fonts", fontLicense),
                    Path.Combine(output, fontLicense), overwrite: true);
                Debug.Log("[水蛇洞] 網頁版輸出完成：" + output);
            }
            finally
            {
                PlayerSettings.WebGL.compressionFormat = compression;
                PlayerSettings.WebGL.decompressionFallback = fallback;
                PlayerSettings.WebGL.nameFilesAsHashes = hashedNames;
                PlayerSettings.WebGL.template = template;
            }
        }
    }
}
