using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class Level02SceneBuilder
{
    public static void Build(Level02Assets assets)
    {
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            assets.LoadExisting();
            var environment = ScaledRoot("Environment");
            var grid = new GameObject("Grid", typeof(Grid));
            grid.transform.SetParent(environment, false);
            var background = TilemapLevelUtility.Layer(grid.transform, "Background", -50, assets.Shared.Material);
            var decorations = TilemapLevelUtility.Layer(grid.transform, "Decorations", -5, assets.Shared.Material);
            var ground = TilemapLevelUtility.Layer(grid.transform, "Ground", 0, assets.Shared.Material);
            var platforms = TilemapLevelUtility.Layer(grid.transform, "Platforms", 2, assets.Shared.Material);
            var foreground = TilemapLevelUtility.Layer(grid.transform, "Foreground", 20, assets.Shared.Material);
            foreach (var rectangle in Level02Layout.Land)
                TilemapLevelUtility.Paint(ground, assets.Shared.Ground, rectangle);
            foreach (var rectangle in Level02Layout.Decks)
                TilemapLevelUtility.Paint(platforms, assets.Shared.Platform, rectangle);
            ground.RefreshAllTiles();
            platforms.RefreshAllTiles();
            TilemapLevelUtility.AddCollision(ground);
            TilemapLevelUtility.AddCollision(platforms);
            Backdrop(background.transform, assets);
            Scenery(decorations.transform, foreground.transform, assets);
            Obstacles(ScaledRoot("DynamicObstacles"), decorations.transform, assets);
            Markers();
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 2.8f;
            camera.transform.position = new Vector3(4f, 0.8f, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(222, 195, 151, 255);
            GameplaySceneInstaller.ConfigureScene(scene);
            EditorSceneManager.SaveScene(scene, Level02Bootstrapper.ScenePath);
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

    private static Transform ScaledRoot(string name)
    {
        var root = new GameObject(name).transform;
        root.localScale = Vector3.one * MapBootstrapper.CellSize;
        return root;
    }

    private static SpriteRenderer Visual(Transform parent, Level02Assets assets, string sprite, Vector2 position, int order = -5)
    {
        return TilemapLevelUtility.Sprite(parent, sprite, assets.Shared[sprite], assets.Shared.Material, position, order);
    }

    private static void Backdrop(Transform parent, Level02Assets assets)
    {
        var sky = TilemapLevelUtility.Sprite(parent, "Sky", assets.Sky, assets.Shared.Material, new Vector2(58, -8), -100);
        sky.drawMode = SpriteDrawMode.Tiled;
        sky.size = new Vector2(140, 80);
        var sea = TilemapLevelUtility.Sprite(parent, "DistantSea", assets.Water, assets.Shared.Material, new Vector2(58, -8), -90);
        sea.drawMode = SpriteDrawMode.Tiled;
        sea.size = new Vector2(140, 6);
        for (int y = 8; y <= 60; y += 13)
            for (int x = 4; x < 120; x += 23)
            {
                var cloud = Visual(parent, assets, (x + y) % 2 == 0 ? "Cloud1" : "Cloud2", new Vector2(x, y + x % 5), -80);
                cloud.transform.localScale = Vector3.one * 1.7f;
                cloud.color = new Color(1f, 1f, 1f, 0.7f);
            }
        for (int x = 8; x < 116; x += 15)
        {
            var palm = Visual(parent, assets, "PalmLeft", new Vector2(x, -2), -60);
            palm.transform.localScale = Vector3.one * 2.5f;
            palm.color = new Color(0.65f, 0.75f, 0.8f, 0.6f);
        }
    }

    private static void Scenery(Transform back, Transform front, Level02Assets assets)
    {
        var palms = new[] { new Vector2(4, 0), new Vector2(18, 0), new Vector2(41, 0), new Vector2(46, 12), new Vector2(8, 12), new Vector2(64, 20), new Vector2(69, 30), new Vector2(98, 30), new Vector2(87, 40), new Vector2(57, 47), new Vector2(108, 55), new Vector2(106, 10) };
        for (int i = 0; i < palms.Length; i++)
        {
            var palm = Visual(back, assets, i % 2 == 0 ? "Palm" : "PalmRight", palms[i]);
            palm.transform.localScale = Vector3.one * (1.2f + i % 3 * 0.2f);
        }
        foreach (var rectangle in Level02Layout.Land)
            for (int x = rectangle.x + 2; x < rectangle.xMax - 1; x += 7)
                Visual(front, assets, "RockDetail", new Vector2(x + 0.5f, rectangle.y + 0.5f), 1);
        foreach (int x in new[] { 8, 22, 38, 47 })
            Post(back, assets, x, 0, 9);
        foreach (int x in new[] { 68, 102 })
            Post(back, assets, x, 0, 27);
        foreach (int x in new[] { 71, 100 })
            Post(back, assets, x, 30, 38);
        Visual(back, assets, "Flag", new Vector2(2, 0), 3).name = "StartFlag";
        Visual(back, assets, "Flag", new Vector2(110, 10), 3).name = "ExitFlag";
        Visual(back, assets, "Chest", new Vector2(28, 39), 3);
        Visual(back, assets, "Chest", new Vector2(58, 47), 3);
        Visual(back, assets, "Chest", new Vector2(108, 55), 3);
        var edgePalm = Visual(front, assets, "PalmLeft", new Vector2(-1, 0), 20);
        edgePalm.transform.localScale = Vector3.one * 2;
    }

    private static void Post(Transform parent, Level02Assets assets, float x, float bottom, float top)
    {
        for (float y = bottom + 0.5f; y < top; y += 0.95f)
        {
            var post = Visual(parent, assets, "Timber", new Vector2(x, y), -3);
            post.transform.localScale = new Vector3(0.35f, 1, 1);
        }
    }

    private static void Obstacles(Transform root, Transform decorations, Level02Assets assets)
    {
        foreach (var route in Level02Layout.Routes)
        {
            var instance = Place(Level02Assets.PrefabFolder, "MovingPlatform", root, route.Start);
            instance.name = route.Name;
            var serialized = new SerializedObject(instance.GetComponent<MovingPlatform2D>());
            serialized.FindProperty("localTravel").vector2Value = route.Travel;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.GetComponent<MovingPlatform2D>());
            if (route.Travel.y > 0)
            {
                var chain = TilemapLevelUtility.Sprite(decorations, route.Name + "Chain", assets.Chain, assets.Shared.Material, route.Start, -4);
                chain.drawMode = SpriteDrawMode.Tiled;
                chain.size = new Vector2(1, route.Travel.y + 1);
            }
        }
        Place(MapBootstrapper.PrefabFolder, "CollapsingPlatform", root, new Vector2(13.5f, 24.85f));
        Place(MapBootstrapper.PrefabFolder, "CollapsingPlatform", root, new Vector2(37.5f, 25.85f));
        Place(MapBootstrapper.PrefabFolder, "ShooterTotem", root, new Vector2(39, 12));
        Place(MapBootstrapper.PrefabFolder, "ShooterTotem", root, new Vector2(95, 30));
        Strip(root, "SpikeStrip", new Vector2(27.5f, 12), 5, false);
        Strip(root, "SpikeStrip", new Vector2(76, 38), 12, true);
        Strip(root, "SpikeStrip", new Vector2(98, 38), 6, true);
        Strip(root, "SpikeStrip", new Vector2(91.5f, 40), 5, false);
        Strip(root, "HazardWater", new Vector2(25, 19), 38, false);
        for (int x = 9; x < 44; x += 8)
            Visual(root, assets, "Reflection", new Vector2(x, 19.56f), 6);
    }

    private static void Strip(Transform parent, string prefab, Vector2 position, float width, bool upsideDown)
    {
        var root = Place(Level02Assets.PrefabFolder, prefab, parent, position);
        var renderer = root.GetComponent<SpriteRenderer>();
        renderer.size = new Vector2(width, renderer.size.y);
        var collider = root.GetComponent<BoxCollider2D>();
        collider.size = new Vector2(width, collider.size.y);
        if (upsideDown)
            root.transform.localRotation = Quaternion.Euler(0, 0, 180);
        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
    }

    private static GameObject Place(string folder, string name, Transform parent, Vector2 position)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(folder + "/" + name + ".prefab");
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = position;
        return instance;
    }

    private static void Markers()
    {
        var root = new GameObject("LevelMarkers").transform;
        Marker(root, "PlayerSpawn", new Vector2(4, 1.2f));
        Marker(root, "MeleeTutorial", new Vector2(9, 1));
        Marker(root, "LevelExit", new Vector2(110, 11));
        Marker(root, "BossEncounter", new Vector2(107, 11));
        Marker(root, "UpperLookout", new Vector2(108, 56));
        var enemies = new GameObject("EnemySpawnMarkers").transform;
        enemies.SetParent(root, false);
        var positions = new[] { new Vector2(24, 1), new Vector2(32, 1), new Vector2(42, 1), new Vector2(18, 13), new Vector2(46, 13), new Vector2(25, 34), new Vector2(45, 40), new Vector2(58, 48), new Vector2(78, 31), new Vector2(100, 31), new Vector2(84, 41) };
        for (int i = 0; i < positions.Length; i++)
            Marker(enemies, "Enemy" + (i + 1).ToString("00"), positions[i]);
    }

    private static void Marker(Transform parent, string name, Vector2 position)
    {
        var marker = new GameObject(name).transform;
        marker.SetParent(parent, false);
        marker.localPosition = position * MapBootstrapper.CellSize;
    }
}
