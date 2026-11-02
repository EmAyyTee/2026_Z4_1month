using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class Level02Bootstrapper
{
    public const string ScenePath = "Assets/Scenes/Scene02/Level02.unity";

    [MenuItem("Tools/Maps/Rebuild Level 02")]
    private static void RebuildFromMenu()
    {
        if (EditorUtility.DisplayDialog("Rebuild Level 02", "Replace Level02 with the GDD layout? Existing Level02 files will be backed up in Library/MapBackups.", "Rebuild", "Cancel"))
            BuildMap();
    }

    public static void BuildMap()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play mode before rebuilding Level02.");
        Scene existing = SceneManager.GetSceneByPath(ScenePath);
        bool wasOpen = existing.IsValid();
        if (wasOpen && existing.isDirty)
            throw new InvalidOperationException("Save Level02 before rebuilding it.");
        Backup();
        MapBootstrapper.EnsureFolder("Assets/Scenes/Scene02");
        if (wasOpen)
            EditorSceneManager.CloseScene(existing, true);
        var assets = new Level02Assets();
        assets.Build();
        Level02SceneBuilder.Build(assets);
        AssetDatabase.SaveAssets();
        RegisterScene();
        Validate();
        if (wasOpen && !Application.isBatchMode)
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        Debug.Log("LEVEL02_BUILD_OK");
    }

    public static void BuildAndVerify()
    {
        BuildMap();
        Level02Verification.VerifyAndCapture();
    }

    public static void RegisterScene()
    {
        const string path = "ProjectSettings/EditorBuildSettings.asset";
        var settings = AssetDatabase.LoadAllAssetsAtPath(path);
        if (EditorBuildSettings.scenes.All(scene => scene.path != ScenePath))
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
        UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(settings, path, true);
        Debug.Log("LEVEL02_REGISTERED_IN_BUILD");
    }

    [MenuItem("Tools/Maps/Validate Level 02")]
    public static void Validate()
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool opened = !scene.IsValid();
        if (opened)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            var grid = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Grid>()).Single();
            foreach (string name in new[] { "Ground", "Platforms", "Background", "Decorations", "Foreground" })
            {
                var layer = grid.transform.Find(name);
                if (layer == null || layer.GetComponent<Tilemap>() == null)
                    throw new InvalidOperationException("Missing tilemap: " + name);
                if (name != "Ground" && name != "Platforms")
                    continue;
                if (layer.GetComponent<Rigidbody2D>()?.bodyType != RigidbodyType2D.Static ||
                    layer.GetComponent<TilemapCollider2D>()?.compositeOperation != Collider2D.CompositeOperation.Merge ||
                    layer.GetComponent<CompositeCollider2D>() == null ||
                    layer.GetComponent<CompositeCollider2D>()?.pathCount == 0 || !layer.CompareTag("Ground"))
                    throw new InvalidOperationException("Invalid collision: " + name);
            }
            if (scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MovingPlatform2D>()).Count() != Level02Layout.Routes.Length)
                throw new InvalidOperationException("Moving platforms are missing.");
            Debug.Log("LEVEL02_VALIDATION_OK");
        }
        finally
        {
            if (opened)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void Backup()
    {
        if (!File.Exists(ScenePath))
            return;
        string destination = "Library/MapBackups/Level02-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
        Directory.CreateDirectory(destination);
        foreach (string path in new[] { ScenePath, ScenePath + ".meta" }.Concat(Directory.Exists(Level02Assets.PrefabFolder) ? Directory.GetFiles(Level02Assets.PrefabFolder) : Array.Empty<string>()))
            if (File.Exists(path))
                File.Copy(path, Path.Combine(destination, Path.GetFileName(path)));
        Debug.Log("LEVEL02_BACKUP " + Path.GetFullPath(destination));
    }
}
