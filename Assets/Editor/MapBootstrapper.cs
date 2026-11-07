using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class MapBootstrapper
{
    public const string ScenePath = "Assets/Scenes/Scene01/Level01.unity";
    public const string PrefabFolder = "Assets/Prefabs/Obstacles";
    public const float CellSize = 1f;

    [MenuItem("Tools/Maps/Rebuild Level 01")]
    private static void RebuildFromMenu()
    {
        if (EditorUtility.DisplayDialog("Rebuild Level 01", "Replace Level01 with the island layout? A backup of the scene and obstacle prefabs will be saved in Library/MapBackups.", "Rebuild", "Cancel"))
            BuildMap();
    }

    public static void BuildMap()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play mode before rebuilding the map.");
        Scene openLevel = SceneManager.GetSceneByPath(ScenePath);
        bool wasOpen = openLevel.IsValid();
        if (wasOpen && openLevel.isDirty)
            throw new InvalidOperationException("Save Level01 before rebuilding it.");
        Backup();
        EnsureFolder("Assets/Scenes/Scene01");
        EnsureFolder(PrefabFolder);
        EnsureGroundTag();
        if (wasOpen)
            EditorSceneManager.CloseScene(openLevel, true);
        var art = new IslandArtBuilder();
        art.Build();
        IslandObstacleBuilder.Build(art);
        IslandSceneBuilder.Build(art);
        if (EditorBuildSettings.scenes.All(scene => scene.path != ScenePath))
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
        AssetDatabase.SaveAssets();
        Validate();
        if (wasOpen && !Application.isBatchMode)
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        Debug.Log("ISLAND_BUILD_OK");
    }

    public static void BuildAndVerify()
    {
        BuildMap();
        IslandMapVerification.VerifyAndCapture();
    }

    [MenuItem("Tools/Maps/Validate Level 01")]
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
                    throw new InvalidOperationException($"Missing tilemap: {name}");
                if (name != "Ground" && name != "Platforms")
                    continue;
                if (layer.GetComponent<Rigidbody2D>()?.bodyType != RigidbodyType2D.Static ||
                    layer.GetComponent<CompositeCollider2D>() == null ||
                    layer.GetComponent<TilemapCollider2D>()?.compositeOperation != Collider2D.CompositeOperation.Merge ||
                    !layer.CompareTag("Ground"))
                    throw new InvalidOperationException($"Invalid ground collision: {name}");
            }
            foreach (string name in new[] { "RollingBarrel", "CollapsingPlatform", "ShooterTotem", "ObstacleProjectile" })
                if (AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/{name}.prefab") == null)
                    throw new InvalidOperationException($"Missing obstacle prefab: {name}");
            Debug.Log("ISLAND_VALIDATION_OK");
        }
        finally
        {
            if (opened)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    public static void CapturePreview()
    {
        IslandMapVerification.CapturePreviews();
    }

    public static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        foreach (string part in parts.Skip(1))
        {
            if (!AssetDatabase.IsValidFolder(current + "/" + part))
                AssetDatabase.CreateFolder(current, part);
            current += "/" + part;
        }
    }

    private static void EnsureGroundTag()
    {
        var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
        var serialized = new SerializedObject(settings);
        var tags = serialized.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == "Ground")
                return;
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = "Ground";
        serialized.ApplyModifiedPropertiesWithoutUndo();
        UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new[] { settings }, "ProjectSettings/TagManager.asset", true);
    }

    private static void Backup()
    {
        string destination = "Library/MapBackups/" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
        Directory.CreateDirectory(destination);
        foreach (string path in new[] { ScenePath, ScenePath + ".meta" }.Concat(Directory.Exists(PrefabFolder) ? Directory.GetFiles(PrefabFolder) : Array.Empty<string>()))
            if (File.Exists(path))
                File.Copy(path, Path.Combine(destination, Path.GetFileName(path)));
        Debug.Log("MAP_BACKUP " + Path.GetFullPath(destination));
    }
}
