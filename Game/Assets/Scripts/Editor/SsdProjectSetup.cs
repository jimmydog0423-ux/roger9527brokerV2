using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SSD.EditorTools
{
    /// <summary>
    /// 第一次打開專案時自動建立主場景、加入 Build Settings，並設定視窗大小。
    /// 遊戲畫面全部由 GameRoot 在執行時用程式組出來，所以場景本身是空的。
    /// </summary>
    [InitializeOnLoad]
    public static class SsdProjectSetup
    {
        public const string ScenePath = "Assets/Scenes/Main.unity";

        static SsdProjectSetup()
        {
            EditorApplication.delayCall += RunOnce;
        }

        static void RunOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (SessionState.GetBool("ssd_setup_checked", false)) return;
            SessionState.SetBool("ssd_setup_checked", true);
            if (File.Exists(ScenePath)) return;
            CreateMainScene();
        }

        [MenuItem("水蛇洞/建立或重建主場景")]
        public static void CreateMainScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            ApplyPlayerSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[水蛇洞] 已建立主場景：" + ScenePath + "。按 Play 就能開始。");
        }

        public static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "SSD Studio";
            PlayerSettings.productName = "水蛇洞經紀人";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SplashScreen.show = false;
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        [MenuItem("水蛇洞/開啟主場景")]
        public static void OpenMainScene()
        {
            if (!File.Exists(ScenePath)) CreateMainScene();
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("水蛇洞/刪除存檔")]
        public static void DeleteSave()
        {
            string p = Path.Combine(Application.persistentDataPath, "save1.json");
            if (File.Exists(p)) File.Delete(p);
            Debug.Log("[水蛇洞] 已刪除存檔：" + p);
        }
    }

    /// <summary>
    /// 俐方體11號用「無反鋸齒點陣」方式匯入，放大整數倍時每個像素都是方的。
    /// </summary>
    public class SsdPixelFontImport : AssetPostprocessor
    {
        void OnPreprocessAsset()
        {
            TrueTypeFontImporter importer = assetImporter as TrueTypeFontImporter;
            if (importer == null) return;
            if (!assetPath.Contains("Cubic11")) return;
            importer.fontSize = 12;
            importer.fontTextureCase = FontTextureCase.Dynamic;
            importer.fontRenderingMode = FontRenderingMode.HintedRaster;
        }
    }
}
