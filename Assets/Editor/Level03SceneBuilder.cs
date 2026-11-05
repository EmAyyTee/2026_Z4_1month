using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Level03SceneBuilder
{
    public static void Build(Level03Assets assets)
    {
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            assets.Shared.LoadExisting();
            var art = assets.Shared.Shared;
            var environment = ScaledRoot("Environment");
            var grid = new GameObject("Grid", typeof(Grid));
            grid.transform.SetParent(environment, false);
            var background = TilemapLevelUtility.Layer(grid.transform, "Background", -50, art.Material);
            var decorations = TilemapLevelUtility.Layer(grid.transform, "Decorations", -5, art.Material);
            var ground = TilemapLevelUtility.Layer(grid.transform, "Ground", 0, art.Material);
            var platforms = TilemapLevelUtility.Layer(grid.transform, "Platforms", 2, art.Material);
            var foreground = TilemapLevelUtility.Layer(grid.transform, "Foreground", 20, art.Material);
            foreach (var rectangle in Level03Layout.Land)
                TilemapLevelUtility.Paint(ground, art.Ground, rectangle);
            foreach (var rectangle in Level03Layout.Decks)
                TilemapLevelUtility.Paint(platforms, art.Platform, rectangle);
            ground.RefreshAllTiles();
            platforms.RefreshAllTiles();
            TilemapLevelUtility.AddCollision(ground);
            TilemapLevelUtility.AddCollision(platforms);
            Backdrop(background.transform, assets.Shared);
            Scenery(decorations.transform, foreground.transform, art);
            Obstacles(ScaledRoot("DynamicObstacles"), decorations.transform, assets.Shared);
            Markers();
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 2.8f;
            camera.transform.position = new Vector3(4, 0.8f, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(222, 195, 151, 255);
            GameplaySceneInstaller.ConfigureScene(scene);
            if (!EditorSceneManager.SaveScene(scene, Level03Bootstrapper.ScenePath))
                throw new System.IO.IOException("Could not save Level03.");
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

    private static SpriteRenderer Visual(Transform parent, IslandArtBuilder art, string sprite, Vector2 position, int order = -5)
    {
        return TilemapLevelUtility.Sprite(parent, sprite, art[sprite], art.Material, position, order);
    }

    private static void Backdrop(Transform parent, Level02Assets assets)
    {
        var sky = TilemapLevelUtility.Sprite(parent, "Sky", assets.Sky, assets.Shared.Material, new Vector2(50, -10), -100);
        sky.drawMode = SpriteDrawMode.Tiled;
        sky.size = new Vector2(130, 105);
        var sea = TilemapLevelUtility.Sprite(parent, "DistantSea", assets.Water, assets.Shared.Material, new Vector2(50, -10), -90);
        sea.drawMode = SpriteDrawMode.Tiled;
        sea.size = new Vector2(130, 7);
        for (int y = 5; y < 80; y += 14)
            for (int x = 5; x < 103; x += 21)
            {
                var cloud = Visual(parent, assets.Shared, x % 2 == 0 ? "Cloud1" : "Cloud2", new Vector2(x, y + x % 4), -80);
                cloud.transform.localScale = Vector3.one * 1.6f;
                cloud.color = new Color(1, 1, 1, 0.65f);
            }
    }

    private static void Scenery(Transform back, Transform front, IslandArtBuilder art)
    {
        var palms = new[]
        {
            new Vector2(3, 0), new Vector2(21, 0), new Vector2(48, 12),
            new Vector2(66, 12), new Vector2(8, 22), new Vector2(23, 17),
            new Vector2(39, 11), new Vector2(14, 30), new Vector2(11, 42),
            new Vector2(44, 32), new Vector2(60, 26), new Vector2(88, 34),
            new Vector2(44, 46), new Vector2(3, 49), new Vector2(5, 56),
            new Vector2(47, 56), new Vector2(87, 56), new Vector2(103, 63)
        };
        for (int i = 0; i < palms.Length; i++)
        {
            var palm = Visual(back, art, i % 2 == 0 ? "PalmLeft" : "PalmRight", palms[i]);
            palm.transform.localScale = Vector3.one * (1.1f + i % 3 * 0.2f);
        }
        foreach (var rectangle in Level03Layout.Land)
            if (rectangle.height >= 3)
                for (int x = rectangle.x + 2; x < rectangle.xMax - 1; x += 7)
                    Visual(front, art, "RockDetail", new Vector2(x + 0.5f, rectangle.y + 0.5f), 1);
        foreach (int x in new[] { 6, 26, 46, 66, 86 })
            Post(back, art, x, 69, 80);
        foreach (int x in new[] { 42, 86 })
            Post(back, art, x, 56, 66);
        Post(back, art, 87, 34, 53);
        Visual(back, art, "Flag", new Vector2(4, 0), 3).name = "StartFlag";
        Visual(back, art, "Flag", new Vector2(88, 69), 3).name = "PortalLocationFlag";
        foreach (var position in new[] { new Vector2(86, 34), new Vector2(3, 49), new Vector2(39, 46), new Vector2(103, 63) })
            Visual(back, art, "Chest", position, 3);
        Visual(front, art, "PalmLeft", new Vector2(-1, 0), 20).transform.localScale = Vector3.one * 1.5f;
    }

    private static void Post(Transform parent, IslandArtBuilder art, float x, float bottom, float top)
    {
        for (float y = bottom + 0.5f; y < top; y += 0.95f)
            Visual(parent, art, "Timber", new Vector2(x, y), -3).transform.localScale = new Vector3(0.35f, 1, 1);
    }

    private static void Obstacles(Transform root, Transform decorations, Level02Assets assets)
    {
        foreach (var route in Level03Layout.Routes)
        {
            var instance = Place(Level02Assets.PrefabFolder, "MovingPlatform", root, route.Start);
            instance.name = route.Name;
            var mover = instance.GetComponent<MovingPlatform2D>();
            var serialized = new SerializedObject(mover);
            serialized.FindProperty("localTravel").vector2Value = route.Travel;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(mover);
            if (route.Travel.y <= 0)
                continue;
            var chain = TilemapLevelUtility.Sprite(decorations, route.Name + "Chain", assets.Chain, assets.Shared.Material, route.Start, -4);
            chain.drawMode = SpriteDrawMode.Tiled;
            chain.size = new Vector2(1, route.Travel.y + 1);
        }
        foreach (var position in Level03Layout.CollapsingDecks)
            Place(MapBootstrapper.PrefabFolder, "CollapsingPlatform", root, position);
        foreach (var position in new[] { new Vector2(61, 12), new Vector2(50, 56), new Vector2(69, 56), new Vector2(86, 56) })
            Place(MapBootstrapper.PrefabFolder, "ShooterTotem", root, position);
        Place(Level03Assets.PrefabFolder, "BarrelRampTrap", root, new Vector2(4, 24.7f));
        SpikeStrip(root, new Vector2(37.5f, 30), 9);
        SpikeStrip(root, new Vector2(82, 56), 4);
    }

    private static void SpikeStrip(Transform parent, Vector2 position, float width)
    {
        var root = Place(Level02Assets.PrefabFolder, "SpikeStrip", parent, position);
        var renderer = root.GetComponent<SpriteRenderer>();
        renderer.size = new Vector2(width, renderer.size.y);
        var collider = root.GetComponent<BoxCollider2D>();
        collider.size = new Vector2(width, collider.size.y);
        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
    }

    private static GameObject Place(string folder, string name, Transform parent, Vector2 position)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(folder + "/" + name + ".prefab");
        if (prefab == null)
            throw new System.IO.FileNotFoundException("Missing prefab: " + folder + "/" + name);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = position;
        return instance;
    }

    private static void Markers()
    {
        var root = new GameObject("LevelMarkers").transform;
        Marker(root, "PlayerSpawn", new Vector2(12, 1.2f));
        Marker(root, "RangedTutorial", new Vector2(25, 1.2f));
        Marker(root, "BossEncounter", new Vector2(48, 70));
        Marker(root, "ExitPortal", new Vector2(88, 70));
        Marker(root, "LevelExit", new Vector2(88, 70));
        Marker(root, "SecretReward", new Vector2(103, 64));
        Marker(root, "BarricadeLower", new Vector2(76, 35));
        Marker(root, "BarricadeUpper", new Vector2(90, 57));
        var sections = new GameObject("GDDSections").transform;
        sections.SetParent(root, false);
        var centers = new[] { new Vector2(40, 3), new Vector2(82, 18), new Vector2(84, 35), new Vector2(20, 21), new Vector2(8, 31), new Vector2(37, 35), new Vector2(42, 47), new Vector2(58, 13), new Vector2(68, 57), new Vector2(45, 70) };
        for (int i = 0; i < centers.Length; i++)
            Marker(sections, "Section" + (i + 1).ToString("00"), centers[i]);
        var enemies = new GameObject("EnemySpawnMarkers").transform;
        enemies.SetParent(root, false);
        var positions = new[] { new Vector2(58, 1), new Vector2(50, 13), new Vector2(64, 13), new Vector2(11, 31), new Vector2(13, 43), new Vector2(40, 47), new Vector2(78, 35), new Vector2(54, 57), new Vector2(63, 57), new Vector2(76, 57) };
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
