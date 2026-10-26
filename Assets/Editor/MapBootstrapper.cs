using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class MapBootstrapper
{
    private const string SceneFolder = "Assets/Scenes/Scene01";
    private const string ScenePath = SceneFolder + "/Level01.unity";
    private const string TileFolder = "Assets/Maps/Tiles";
    private const string PaletteFolder = "Assets/Maps/Palettes";
    private const string PrefabFolder = "Assets/Prefabs/Obstacles";
    private const string PalettePath = PaletteFolder + "/IslandPalette.prefab";
    private const string TerrainPath = "Assets/Treasure Hunters/Treasure Hunters/Palm Tree Island/Sprites/Terrain/Terrain (32x32).png";
    private const string BackWallPath = "Assets/Treasure Hunters/Treasure Hunters/Pirate Ship/Sprites/Tilesets/Terrain and Back Wall (32x32).png";
    private const string PlatformPath = "Assets/Treasure Hunters/Treasure Hunters/Pirate Ship/Sprites/Tilesets/Platforms (32x32).png";
    private const string BarrelPath = "Assets/Treasure Hunters/Treasure Hunters/Merchant Ship/Sprites/Barrel/Idle/1.png";
    private const string TotemPath = "Assets/Treasure Hunters/Treasure Hunters/Shooter Traps/Sprites/Totems/Head 1/Idle 1/1.png";
    private const string ProjectilePath = "Assets/Treasure Hunters/Treasure Hunters/Shooter Traps/Sprites/Cannon/Cannon Ball Idle/1.png";

    [InitializeOnLoadMethod]
    private static void ScheduleAutomaticBuild()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            return;

        EditorApplication.delayCall += BuildWhenReady;
    }

    private static void BuildWhenReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += BuildWhenReady;
            return;
        }

        try
        {
            BuildMap();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    [MenuItem("Tools/Maps/Build Level 01")]
    public static void BuildMap()
    {
        EnsureFolder(SceneFolder);
        EnsureFolder(TileFolder);
        EnsureFolder(PaletteFolder);
        EnsureFolder(PrefabFolder);
        Tile groundTile = CreateTile("Ground", LoadSprite(TerrainPath, 7), Tile.ColliderType.Grid);
        Tile platformTile = CreateTile("Platform", LoadSprite(PlatformPath, 0), Tile.ColliderType.Grid);
        Tile backgroundTile = CreateTile("Background", LoadSprite(BackWallPath, 19), Tile.ColliderType.None);
        Tile decorationTile = CreateTile("Decoration", LoadSprite(TerrainPath, 10), Tile.ColliderType.None);
        Tile foregroundTile = CreateTile("Foreground", LoadSprite(TerrainPath, 1), Tile.ColliderType.None);

        CreatePalette(new[] { groundTile, platformTile, backgroundTile, decorationTile, foregroundTile });
        ObstacleProjectile projectilePrefab = CreateProjectilePrefab();
        CreateRollingBarrelPrefab();
        CreateCollapsingPlatformPrefab();
        CreateShooterTotemPrefab(projectilePrefab);
        CreateScene(groundTile, platformTile, backgroundTile, decorationTile, foregroundTile);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Validate();
        Debug.Log("MAP_BUILD_OK");
    }

    public static void Validate()
    {
        Scene previousScene = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        try
        {
            Grid grid = GetGrid(scene);

            if (grid == null)
                throw new InvalidOperationException("Grid is missing.");

            string[] requiredLayers = { "Ground", "Platforms", "Background", "Decorations", "Foreground" };

            foreach (string layerName in requiredLayers)
            {
                Transform layer = grid.transform.Find(layerName);

                if (layer == null || layer.GetComponent<Tilemap>() == null)
                    throw new InvalidOperationException($"Tilemap layer {layerName} is missing.");
            }

            ValidateCollisionLayer(grid.transform.Find("Ground"));
            ValidateCollisionLayer(grid.transform.Find("Platforms"));

            string[] prefabs =
            {
                PrefabFolder + "/RollingBarrel.prefab",
                PrefabFolder + "/CollapsingPlatform.prefab",
                PrefabFolder + "/ShooterTotem.prefab",
                PrefabFolder + "/ObstacleProjectile.prefab"
            };

            foreach (string prefabPath in prefabs)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
                    throw new InvalidOperationException($"Prefab {prefabPath} is missing.");
            }

            if (!scene.IsValid())
                throw new InvalidOperationException("Level scene is invalid.");
        }
        finally
        {
            if (previousScene.IsValid())
                SceneManager.SetActiveScene(previousScene);

            EditorSceneManager.CloseScene(scene, true);
        }

        Debug.Log("MAP_VALIDATION_OK");
    }

    public static void CapturePreview()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Camera camera = Camera.main;

        if (camera == null)
            throw new InvalidOperationException("Main camera is missing.");

        camera.transform.position = new Vector3(32f, 8f, -10f);
        camera.orthographicSize = 11f;

        RenderTexture renderTexture = new RenderTexture(1600, 900, 24);
        Texture2D texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        camera.targetTexture = renderTexture;
        camera.Render();
        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
        texture.Apply();

        string outputPath = GetArgument("-mapPreviewPath") ?? Path.GetFullPath("MapPreview.png");
        File.WriteAllBytes(outputPath, texture.EncodeToPNG());

        camera.targetTexture = null;
        RenderTexture.active = null;
        UnityEngine.Object.DestroyImmediate(renderTexture);
        UnityEngine.Object.DestroyImmediate(texture);
        Debug.Log($"MAP_PREVIEW_OK {outputPath}");
    }

    private static void CreateScene(TileBase groundTile, TileBase platformTile, TileBase backgroundTile, TileBase decorationTile, TileBase foregroundTile)
    {
        Scene previousScene = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        CreateCamera();

        GameObject environment = new GameObject("Environment");
        GameObject gridObject = new GameObject("Grid", typeof(Grid));
        gridObject.transform.SetParent(environment.transform);

        Tilemap background = CreateTilemap(gridObject.transform, "Background", -20, false);
        Tilemap decorations = CreateTilemap(gridObject.transform, "Decorations", -5, false);
        Tilemap ground = CreateTilemap(gridObject.transform, "Ground", 0, true);
        Tilemap platforms = CreateTilemap(gridObject.transform, "Platforms", 2, true);
        Tilemap foreground = CreateTilemap(gridObject.transform, "Foreground", 20, false);

        PaintBackground(background, backgroundTile);
        PaintLevelGeometry(ground, groundTile, platforms, platformTile);
        PaintDecorations(decorations, decorationTile, foreground, foregroundTile);
        CreateMarkers();
        PlaceObstacles();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();

        if (previousScene.IsValid())
            SceneManager.SetActiveScene(previousScene);

        EditorSceneManager.CloseScene(scene, true);
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7f;
        camera.backgroundColor = new Color(0.72f, 0.86f, 0.93f);
        cameraObject.transform.position = new Vector3(10f, 7f, -10f);
    }

    private static Tilemap CreateTilemap(Transform parent, string name, int sortingOrder, bool collisionEnabled)
    {
        GameObject layerObject = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        layerObject.transform.SetParent(parent);
        TilemapRenderer renderer = layerObject.GetComponent<TilemapRenderer>();
        renderer.sortingOrder = sortingOrder;

        if (collisionEnabled)
        {
            Rigidbody2D rigidbody = layerObject.AddComponent<Rigidbody2D>();
            rigidbody.bodyType = RigidbodyType2D.Static;
            CompositeCollider2D composite = layerObject.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            TilemapCollider2D tilemapCollider = layerObject.AddComponent<TilemapCollider2D>();
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
        }

        return layerObject.GetComponent<Tilemap>();
    }

    private static void PaintLevelGeometry(Tilemap ground, TileBase groundTile, Tilemap platforms, TileBase platformTile)
    {
        PaintRectangle(ground, groundTile, 0, 0, 68, 2);
        PaintRectangle(ground, groundTile, 0, 2, 2, 14);
        PaintRectangle(ground, groundTile, 24, 2, 4, 7);
        PaintRectangle(ground, groundTile, 30, 2, 4, 14);
        PaintRectangle(ground, groundTile, 39, 2, 3, 9);
        PaintRectangle(ground, groundTile, 55, 2, 3, 5);
        PaintRectangle(ground, groundTile, 66, 2, 2, 13);

        PaintPlatform(platforms, platformTile, 4, 4, 4);
        PaintPlatform(platforms, platformTile, 8, 7, 4);
        PaintPlatform(platforms, platformTile, 12, 10, 5);
        PaintPlatform(platforms, platformTile, 15, 14, 12);
        PaintPlatform(platforms, platformTile, 22, 6, 3);
        PaintPlatform(platforms, platformTile, 28, 11, 3);
        PaintPlatform(platforms, platformTile, 34, 5, 5);
        PaintPlatform(platforms, platformTile, 36, 9, 4);
        PaintPlatform(platforms, platformTile, 42, 4, 5);
        PaintPlatform(platforms, platformTile, 45, 7, 4);
        PaintPlatform(platforms, platformTile, 49, 10, 6);
        PaintPlatform(platforms, platformTile, 50, 14, 5);
        PaintPlatform(platforms, platformTile, 58, 6, 4);
        PaintPlatform(platforms, platformTile, 61, 9, 3);
        PaintPlatform(platforms, platformTile, 63, 12, 3);
    }

    private static void PaintBackground(Tilemap background, TileBase tile)
    {
        PaintRectangle(background, tile, 2, 2, 22, 12);
        PaintRectangle(background, tile, 28, 2, 11, 13);
        PaintRectangle(background, tile, 42, 2, 24, 12);
    }

    private static void PaintDecorations(Tilemap decorations, TileBase decorationTile, Tilemap foreground, TileBase foregroundTile)
    {
        foreach (Vector3Int position in new[]
                 {
                     new Vector3Int(3, 2), new Vector3Int(14, 11), new Vector3Int(19, 15),
                     new Vector3Int(35, 2), new Vector3Int(50, 11), new Vector3Int(64, 13)
                 })
            decorations.SetTile(position, decorationTile);

        foreach (Vector3Int position in new[] { new Vector3Int(8, 2), new Vector3Int(41, 2), new Vector3Int(59, 2) })
            foreground.SetTile(position, foregroundTile);
    }

    private static void PaintPlatform(Tilemap tilemap, TileBase tile, int x, int y, int width)
    {
        PaintRectangle(tilemap, tile, x, y, width, 1);
    }

    private static void PaintRectangle(Tilemap tilemap, TileBase tile, int x, int y, int width, int height)
    {
        for (int column = x; column < x + width; column++)
        {
            for (int row = y; row < y + height; row++)
                tilemap.SetTile(new Vector3Int(column, row), tile);
        }
    }

    private static void CreateMarkers()
    {
        GameObject markers = new GameObject("LevelMarkers");
        GameObject spawn = new GameObject("PlayerSpawn");
        spawn.transform.SetParent(markers.transform);
        spawn.transform.position = new Vector3(3f, 3f, 0f);
        GameObject exit = new GameObject("LevelExit");
        exit.transform.SetParent(markers.transform);
        exit.transform.position = new Vector3(64f, 14f, 0f);
    }

    private static void PlaceObstacles()
    {
        GameObject obstacleRoot = new GameObject("DynamicObstacles");
        PlacePrefab(PrefabFolder + "/RollingBarrel.prefab", obstacleRoot.transform, new Vector3(18f, 16f, 0f));
        PlacePrefab(PrefabFolder + "/CollapsingPlatform.prefab", obstacleRoot.transform, new Vector3(46f, 8f, 0f));
        PlacePrefab(PrefabFolder + "/ShooterTotem.prefab", obstacleRoot.transform, new Vector3(52f, 15f, 0f));
    }

    private static void PlacePrefab(string path, Transform parent, Vector3 position)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

        if (instance == null)
            throw new InvalidOperationException($"Could not instantiate {path}.");

        instance.transform.SetParent(parent);
        instance.transform.position = position;
    }

    private static ObstacleProjectile CreateProjectilePrefab()
    {
        GameObject root = new GameObject("ObstacleProjectile");
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSprite(ProjectilePath, 0);
        Rigidbody2D body = root.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        root.AddComponent<ObstacleProjectile>();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/ObstacleProjectile.prefab");
        UnityEngine.Object.DestroyImmediate(root);
        return prefab.GetComponent<ObstacleProjectile>();
    }

    private static void CreateRollingBarrelPrefab()
    {
        GameObject root = new GameObject("RollingBarrel");
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSprite(BarrelPath, 0);
        Rigidbody2D body = root.AddComponent<Rigidbody2D>();
        body.gravityScale = 1.5f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
        collider.radius = 0.45f;
        root.AddComponent<RollingBarrelObstacle>();
        PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/RollingBarrel.prefab");
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void CreateCollapsingPlatformPrefab()
    {
        GameObject root = new GameObject("CollapsingPlatform");
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSprite(PlatformPath, 0);
        Rigidbody2D body = root.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(2.8f, 0.35f);
        root.AddComponent<CollapsingPlatformObstacle>();
        PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/CollapsingPlatform.prefab");
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void CreateShooterTotemPrefab(ObstacleProjectile projectilePrefab)
    {
        GameObject root = new GameObject("ShooterTotem");
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSprite(TotemPath, 0);
        root.AddComponent<BoxCollider2D>();
        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(root.transform);
        firePoint.transform.localPosition = new Vector3(0.7f, 0.15f, 0f);
        ShooterTotemObstacle shooter = root.AddComponent<ShooterTotemObstacle>();
        SerializedObject serializedShooter = new SerializedObject(shooter);
        serializedShooter.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
        serializedShooter.FindProperty("firePoint").objectReferenceValue = firePoint.transform;
        serializedShooter.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/ShooterTotem.prefab");
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static Tile CreateTile(string name, Sprite sprite, Tile.ColliderType colliderType)
    {
        string path = $"{TileFolder}/{name}.asset";
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);

        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, path);
        }

        tile.sprite = sprite;
        tile.colliderType = colliderType;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static void CreatePalette(IReadOnlyList<TileBase> tiles)
    {
        GameObject root = new GameObject("IslandPalette", typeof(Grid));
        GameObject tilemapObject = new GameObject("Tiles", typeof(Tilemap), typeof(TilemapRenderer));
        tilemapObject.transform.SetParent(root.transform);
        Tilemap tilemap = tilemapObject.GetComponent<Tilemap>();

        for (int index = 0; index < tiles.Count; index++)
            tilemap.SetTile(new Vector3Int(index, 0), tiles[index]);

        PrefabUtility.SaveAsPrefabAsset(root, PalettePath);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static Sprite LoadSprite(string path, int index)
    {
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(sprite => sprite.name).ToArray();

        if (sprites.Length == 0)
            throw new InvalidOperationException($"No sprites found at {path}.");

        return sprites[Mathf.Clamp(index, 0, sprites.Length - 1)];
    }

    private static void ValidateCollisionLayer(Transform layer)
    {
        if (layer == null || layer.GetComponent<TilemapCollider2D>() == null ||
            layer.GetComponent<CompositeCollider2D>() == null ||
            layer.GetComponent<Rigidbody2D>()?.bodyType != RigidbodyType2D.Static)
            throw new InvalidOperationException($"Collision setup is invalid on {layer?.name ?? "missing layer"}.");
    }

    private static Grid GetGrid(Scene scene)
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Grid>(true))
            .FirstOrDefault();
    }

    private static void AddSceneToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();

        if (scenes.All(scene => scene.path != ScenePath))
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];

        for (int index = 1; index < parts.Length; index++)
        {
            string next = current + "/" + parts[index];

            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[index]);

            current = next;
        }
    }

    private static string GetArgument(string name)
    {
        string[] arguments = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(arguments, name);
        return index >= 0 && index + 1 < arguments.Length ? arguments[index + 1] : null;
    }
}
