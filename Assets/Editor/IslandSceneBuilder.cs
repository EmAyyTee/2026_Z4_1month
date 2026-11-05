using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class IslandSceneBuilder
{
    public static readonly RectInt[] Land =
    {
        new RectInt(0, -4, 12, 8), new RectInt(12, -4, 8, 9),
        new RectInt(20, -4, 6, 8), new RectInt(31, -4, 8, 10),
        new RectInt(39, -4, 6, 12), new RectInt(45, -4, 5, 10),
        new RectInt(57, -4, 7, 12), new RectInt(64, -4, 8, 14),
        new RectInt(72, -4, 6, 15), new RectInt(78, -4, 10, 13),
        new RectInt(88, -4, 8, 14)
    };

    public static readonly RectInt[] Decks =
    {
        new RectInt(8, 5, 5, 1), new RectInt(17, 7, 5, 1),
        new RectInt(26, 3, 5, 1), new RectInt(29, 4, 3, 1),
        new RectInt(34, 9, 5, 1), new RectInt(48, 7, 4, 1),
        new RectInt(55, 6, 3, 1), new RectInt(61, 9, 4, 1),
        new RectInt(80, 11, 5, 1)
    };

    public static void Build(IslandArtBuilder art)
    {
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            var environment = new GameObject("Environment");
            environment.transform.localScale = Vector3.one * MapBootstrapper.CellSize;
            var grid = new GameObject("Grid", typeof(Grid));
            grid.transform.SetParent(environment.transform, false);
            var background = Layer(grid.transform, "Background", -50, art);
            var decorations = Layer(grid.transform, "Decorations", -5, art);
            var ground = Layer(grid.transform, "Ground", 0, art);
            var platforms = Layer(grid.transform, "Platforms", 2, art);
            var foreground = Layer(grid.transform, "Foreground", 20, art);
            foreach (var rect in Land)
                Paint(ground, art.Ground, rect);
            foreach (var rect in Decks)
                Paint(platforms, art.Platform, rect);
            ground.RefreshAllTiles();
            platforms.RefreshAllTiles();
            AddCollision(ground);
            AddCollision(platforms);
            BuildBackdrop(background.transform, art);
            BuildScenery(decorations.transform, foreground.transform, art);
            BuildObstacles(art);
            CreateMarkers();
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 2.8f;
            camera.transform.position = new Vector3(4.5f, 2f, -10f);
            camera.backgroundColor = new Color32(222, 195, 151, 255);
            camera.clearFlags = CameraClearFlags.SolidColor;
            GameplaySceneInstaller.ConfigureScene(scene);
            EditorSceneManager.SaveScene(scene, MapBootstrapper.ScenePath);
        }
        finally
        {
            if (Application.isBatchMode)
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            else
            {
                if (previous.IsValid())
                    SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    private static Tilemap Layer(Transform parent, string name, int order, IslandArtBuilder art)
    {
        return TilemapLevelUtility.Layer(parent, name, order, art.Material);
    }

    private static void AddCollision(Tilemap map)
    {
        TilemapLevelUtility.AddCollision(map);
    }

    private static void Paint(Tilemap map, TileBase tile, RectInt rectangle)
    {
        TilemapLevelUtility.Paint(map, tile, rectangle);
    }

    private static void BuildBackdrop(Transform root, IslandArtBuilder art)
    {
        var horizon = Sprite(root, art, "Horizon", new Vector2(48, -8), -100);
        horizon.transform.localScale = new Vector3(10, 8, 1);
        for (int i = 0; i < 8; i++)
        {
            var cloud = Sprite(root, art, i % 2 == 0 ? "Cloud1" : "Cloud2", new Vector2(i * 14 - 2, 12 + i % 3), -80);
            cloud.transform.localScale = Vector3.one * (1.2f + (i % 2) * 0.3f);
        }
        for (int i = 0; i < 9; i++)
        {
            var palm = Sprite(root, art, i % 2 == 0 ? "PalmLeft" : "PalmRight", new Vector2(4 + i * 11, 1.8f), -60);
            palm.color = new Color(0.78f, 0.82f, 0.85f, 1f);
            palm.transform.localScale = Vector3.one * 2.5f;
            Sprite(root, art, "Reflection", new Vector2(i * 12 + 2, -1 + i % 2), -70);
        }
    }

    private static void BuildScenery(Transform back, Transform front, IslandArtBuilder art)
    {
        var palms = new[] { new Vector2(3, 4), new Vector2(7, 4), new Vector2(15, 5), new Vector2(24, 4), new Vector2(34, 6), new Vector2(41, 8), new Vector2(59, 8), new Vector2(69, 10), new Vector2(75, 11), new Vector2(82, 9), new Vector2(92, 10) };
        for (int i = 0; i < palms.Length; i++)
        {
            var palm = Sprite(back, art, i % 3 == 0 ? "PalmLeft" : i % 3 == 1 ? "PalmRight" : "Palm", palms[i], -5);
            palm.transform.localScale = Vector3.one * (1.3f + i % 3 * 0.25f);
        }
        foreach (var rect in Decks.Where(rect => rect.x != 29 && rect.x != 55))
        {
            for (int x = rect.x; x < rect.xMax; x += Math.Max(2, rect.width - 1))
            {
                float floor = Surface(x);
                for (float y = floor + 0.5f; y < rect.y + 0.7f; y += 0.85f)
                {
                    var post = Sprite(back, art, "Timber", new Vector2(x + 0.5f, y), -3);
                    post.transform.localScale = new Vector3(0.2f, 0.9f, 1f);
                }
            }
        }
        for (int x = 2; x < 95; x += 5)
        {
            float height = Surface(x);
            if (height <= 0)
                continue;
            Sprite(front, art, "RockDetail", new Vector2(x + 0.5f, height - 1.7f - x % 3), 1);
        }
        Sprite(back, art, "Flag", new Vector2(2, 4), 3).name = "StartFlag";
        Sprite(back, art, "Flag", new Vector2(93, 10), 3).name = "ExitFlag";
        Sprite(back, art, "Chest", new Vector2(74, 11), 3).name = "TreasureLandmark";
        foreach (var position in new[] { new Vector2(0, 3), new Vector2(96, 9) })
        {
            var palm = Sprite(front, art, "Palm", position, 20);
            palm.transform.localScale = Vector3.one * 2f;
            palm.color = new Color(0.65f, 0.72f, 0.7f, 1);
        }
    }

    public static float Surface(int x)
    {
        return Land.Where(rect => x >= rect.xMin && x < rect.xMax).Select(rect => (float)rect.yMax).DefaultIfEmpty(-3).Max();
    }

    private static void BuildObstacles(IslandArtBuilder art)
    {
        var root = new GameObject("DynamicObstacles");
        root.transform.localScale = Vector3.one * MapBootstrapper.CellSize;
        Place("CollapsingPlatform", root.transform, new Vector2(51.5f, 6f));
        Place("CollapsingPlatform", root.transform, new Vector2(54.5f, 6f));
        Place("ShooterTotem", root.transform, new Vector2(43, 8));
        Place("ShooterTotem", root.transform, new Vector2(86, 9));
        var ramp = Sprite(root.transform, art, "BridgeDeck", new Vector2(21, 8.8f), 2);
        ramp.name = "BarrelRamp";
        ramp.transform.localScale = new Vector3(1.6f, 1, 1);
        ramp.transform.localRotation = Quaternion.Euler(0, 0, -12);
        ramp.gameObject.tag = "Ground";
        var rampCollider = ramp.gameObject.AddComponent<BoxCollider2D>();
        rampCollider.size = new Vector2(3, 0.25f);
        var emitter = new GameObject("BarrelSpawn");
        emitter.transform.SetParent(root.transform, false);
        emitter.transform.localPosition = new Vector3(19.5f, 9.7f);
        var spawner = emitter.AddComponent<BarrelSpawner>();
        var serialized = new SerializedObject(spawner);
        serialized.FindProperty("prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<RollingBarrelObstacle>(MapBootstrapper.PrefabFolder + "/RollingBarrel.prefab");
        serialized.FindProperty("activationDistance").floatValue = 4f;
        serialized.FindProperty("spawnInterval").floatValue = 4f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        Place("RollingBarrel", root.transform, new Vector2(20.5f, 9.5f));
    }

    private static void Place(string name, Transform parent, Vector2 position)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MapBootstrapper.PrefabFolder + "/" + name + ".prefab");
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = position;
    }

    public static SpriteRenderer Sprite(Transform parent, IslandArtBuilder art, string name, Vector2 position, int order)
    {
        return TilemapLevelUtility.Sprite(parent, name, art[name], art.Material, position, order);
    }

    private static void CreateMarkers()
    {
        var root = new GameObject("LevelMarkers");
        Marker(root.transform, "PlayerSpawn", new Vector2(3, 5));
        Marker(root.transform, "LevelExit", new Vector2(93, 11));
    }

    private static void Marker(Transform parent, string name, Vector2 cellPosition)
    {
        var marker = new GameObject(name);
        marker.transform.SetParent(parent, false);
        marker.transform.localPosition = cellPosition * MapBootstrapper.CellSize;
    }
}
