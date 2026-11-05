using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class Level03Bootstrapper
{
    public const string ScenePath = "Assets/Scenes/Scene03/Level03.unity";

    [MenuItem("Tools/Maps/Rebuild Level 03")]
    private static void RebuildFromMenu()
    {
        if (EditorUtility.DisplayDialog("Rebuild Level 03", "Replace Level03 with the GDD layout? Existing Level03 files will be backed up in Library/MapBackups.", "Rebuild", "Cancel"))
            BuildMap();
    }

    public static void BuildMap()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play mode before rebuilding Level03.");
        Scene existing = SceneManager.GetSceneByPath(ScenePath);
        bool wasOpen = existing.IsValid();
        if (wasOpen && existing.isDirty)
            throw new InvalidOperationException("Save Level03 before rebuilding it.");
        Backup();
        MapBootstrapper.EnsureFolder("Assets/Scenes/Scene03");
        if (wasOpen)
            EditorSceneManager.CloseScene(existing, true);
        var assets = new Level03Assets();
        assets.Build();
        Level03SceneBuilder.Build(assets);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceSynchronousImport);
        RegisterScene();
        Validate();
        if (wasOpen && !Application.isBatchMode)
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        Debug.Log("LEVEL03_BUILD_OK");
    }

    public static void BuildAndVerify()
    {
        BuildMap();
        Level03Verification.VerifyAndCapture();
    }

    public static void RegisterScene()
    {
        const string path = "ProjectSettings/EditorBuildSettings.asset";
        var settings = AssetDatabase.LoadAllAssetsAtPath(path);
        if (EditorBuildSettings.scenes.All(scene => scene.path != ScenePath))
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
        UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(settings, path, true);
        Debug.Log("LEVEL03_REGISTERED_IN_BUILD");
    }

    [MenuItem("Tools/Maps/Validate Level 03")]
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
            if (Count<MovingPlatform2D>(scene) != Level03Layout.Routes.Length ||
                Count<CollapsingPlatformObstacle>(scene) != Level03Layout.CollapsingDecks.Length ||
                Count<ShooterTotemObstacle>(scene) != 4 || Count<BarrelSpawner>(scene) != 1 ||
                Count<ContactDamage2D>(scene) != 2)
                throw new InvalidOperationException("Level03 obstacle instances are incomplete.");
            if (!Application.isPlaying)
            {
                Physics2D.SyncTransforms();
                var groundCollider = grid.transform.Find("Ground").GetComponent<CompositeCollider2D>();
                var barrel = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<RollingBarrelObstacle>()).Single().GetComponent<CircleCollider2D>();
                var overlaps = Physics2D.OverlapCircleAll(barrel.transform.TransformPoint(barrel.offset), barrel.radius * Mathf.Abs(barrel.transform.lossyScale.x));
                if (overlaps.Contains(groundCollider))
                    throw new InvalidOperationException("Initial barrel overlaps the terrain.");
            }
            Debug.Log("LEVEL03_VALIDATION_OK");
        }
        finally
        {
            if (opened)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static int Count<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>()).Count();
    }

    private static void Backup()
    {
        if (!File.Exists(ScenePath))
            return;
        string destination = "Library/MapBackups/Level03-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
        Directory.CreateDirectory(destination);
        foreach (string path in new[] { ScenePath, ScenePath + ".meta" }.Concat(Directory.Exists(Level03Assets.PrefabFolder) ? Directory.GetFiles(Level03Assets.PrefabFolder) : Array.Empty<string>()))
            if (File.Exists(path))
                File.Copy(path, Path.Combine(destination, Path.GetFileName(path)));
        Debug.Log("LEVEL03_BACKUP " + Path.GetFullPath(destination));
    }
}
