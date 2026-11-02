using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class IslandMapVerification
{
    private const string PendingKey = "IslandMapVerification.Pending";
    private static double testStart;

    public static void VerifyAndCapture()
    {
        if (!Application.isBatchMode)
            throw new InvalidOperationException("Use batch mode for the automated play-mode verification.");
        EditorSceneManager.OpenScene(MapBootstrapper.ScenePath, OpenSceneMode.Single);
        SessionState.SetBool(PendingKey, true);
        ResumeVerification();
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    private static void ResumeVerification()
    {
        if (!SessionState.GetBool(PendingKey, false))
            return;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode)
            return;
        testStart = EditorApplication.timeSinceStartup;
        EditorApplication.update += CompleteVerification;
    }

    private static void CompleteVerification()
    {
        if (EditorApplication.timeSinceStartup - testStart < 0.5)
            return;
        EditorApplication.update -= CompleteVerification;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        SessionState.SetBool(PendingKey, false);
        try
        {
            Run();
            CapturePreviews();
            VerifyFallingBodies();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    public static void Run()
    {
        Scene scene = SceneManager.GetSceneByPath(MapBootstrapper.ScenePath);
        bool opened = !scene.IsValid();
        if (opened)
            scene = EditorSceneManager.OpenScene(MapBootstrapper.ScenePath, OpenSceneMode.Additive);
        try
        {
            var grid = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Grid>()).Single();
            var ground = grid.transform.Find("Ground").GetComponent<Tilemap>();
            var platforms = grid.transform.Find("Platforms").GetComponent<Tilemap>();
            Physics2D.SyncTransforms();
            Check(ground.GetSprite(new Vector3Int(5, 3)).name == "Ground10", "Ground surface must use the grass edge.");
            Check(ground.GetSprite(new Vector3Int(5, 0)).name == "Ground11", "Ground interior must use rock fill.");
            Check(ground.GetSprite(new Vector3Int(0, 3)).name == "Ground00", "Ground left corner must be capped.");
            Check(platforms.GetSprite(new Vector3Int(8, 5)).name == "Plank0", "Platform must have a left end.");
            Check(platforms.GetSprite(new Vector3Int(10, 5)).name == "Plank1", "Platform must have a middle.");
            Check(platforms.GetSprite(new Vector3Int(12, 5)).name == "Plank2", "Platform must have a right end.");
            var groundCollider = ground.GetComponent<CompositeCollider2D>();
            var platformCollider = platforms.GetComponent<CompositeCollider2D>();
            Check(groundCollider.pathCount > 0 && platformCollider.pathCount > 0, "Saved map must contain collision geometry.");
            Check(groundCollider.OverlapPoint(new Vector2(5.5f, 3.5f) * MapBootstrapper.CellSize), "Ground collision must be solid.");
            Check(!groundCollider.OverlapPoint(new Vector2(27.5f, 2f) * MapBootstrapper.CellSize), "Ravine must remain open.");
            Check(platformCollider.OverlapPoint(new Vector2(27.5f, 3.85f) * MapBootstrapper.CellSize), "Wood deck collision must match its surface.");
            Check(!platformCollider.OverlapPoint(new Vector2(27.5f, 3.3f) * MapBootstrapper.CellSize), "Space below a deck must not be solid.");
            Check(ground.GetSprite(new Vector3Int(5, 3)).rect.size == new Vector2(32, 32), "Terrain sprites must be sliced to 32 pixels.");
            Check(AssetDatabase.LoadAssetAtPath<GridPalette>("Assets/Maps/Palettes/IslandTerrainPalette.prefab") != null, "Palette must be recognized by Unity.");
            Check(scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CollapsingPlatformObstacle>()).Count() == 2, "Bridge must contain two independent collapsing sections.");
            Debug.Log("ISLAND_PHYSICS_AND_ART_TESTS_OK");
        }
        finally
        {
            if (opened)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void VerifyFallingBodies()
    {
        var scene = SceneManager.GetSceneByPath(MapBootstrapper.ScenePath);
        var grid = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Grid>()).Single();
        var ground = grid.transform.Find("Ground").GetComponent<CompositeCollider2D>();
        var platforms = grid.transform.Find("Platforms").GetComponent<CompositeCollider2D>();
        var positions = new[] { new Vector2(5.5f, 7f), new Vector2(27.5f, 6f), new Vector2(27.5f, 2f) };
        var probes = new Rigidbody2D[positions.Length];
        var previousMode = Physics2D.simulationMode;
        try
        {
            Physics2D.simulationMode = SimulationMode2D.Script;
            for (int i = 0; i < probes.Length; i++)
            {
                var probe = new GameObject("MapCollisionProbe");
                SceneManager.MoveGameObjectToScene(probe, scene);
                probe.transform.position = positions[i] * MapBootstrapper.CellSize;
                probes[i] = probe.AddComponent<Rigidbody2D>();
                probes[i].collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                probe.AddComponent<CircleCollider2D>().radius = 0.1f;
            }
            Physics2D.SyncTransforms();
            for (int step = 0; step < 90; step++)
                scene.GetPhysicsScene2D().Simulate(0.02f);
            Check(probes[0].IsTouching(ground), "A falling body must land on the ground.");
            Check(probes[1].IsTouching(platforms), "A falling body must land on the thin wooden deck.");
            Check(probes[2].position.y < -1f, "A body below the bridge must fall through the ravine.");
            Debug.Log("ISLAND_FALL_TESTS_OK");
        }
        finally
        {
            foreach (var probe in probes)
                if (probe != null)
                    UnityEngine.Object.DestroyImmediate(probe.gameObject);
            Physics2D.simulationMode = previousMode;
        }
    }

    public static void CapturePreviews()
    {
        Scene scene = SceneManager.GetSceneByPath(MapBootstrapper.ScenePath);
        bool opened = !scene.IsValid();
        if (opened)
            scene = EditorSceneManager.OpenScene(MapBootstrapper.ScenePath, OpenSceneMode.Additive);
        try
        {
            var camera = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>()).Single();
            camera.scene = scene;
            Directory.CreateDirectory("Library/MapPreviews");
            Capture(camera, "Level01-overview", new Vector2(48, 5), 14, 3200, 900);
            Capture(camera, "Level01-coast", new Vector2(16, 6), 9, 1600, 900);
            Capture(camera, "Level01-bridge", new Vector2(48, 7), 9, 1600, 900);
            Capture(camera, "Level01-cliffs", new Vector2(80, 9), 9, 1600, 900);
            Debug.Log("ISLAND_PREVIEWS_OK " + Path.GetFullPath("Library/MapPreviews"));
        }
        finally
        {
            if (opened)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    public static void Capture(Camera camera, string name, Vector2 center, float halfHeight, int width, int height)
    {
        camera.transform.position = new Vector3(center.x * MapBootstrapper.CellSize, center.y * MapBootstrapper.CellSize, -10);
        camera.orthographicSize = halfHeight * MapBootstrapper.CellSize;
        camera.aspect = (float)width / height;
        var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        try
        {
            target.Create();
            var request = new RenderPipeline.StandardRequest { destination = target };
            if (RenderPipeline.SupportsRenderRequest(camera, request))
                RenderPipeline.SubmitRenderRequest(camera, request);
            else
            {
                camera.targetTexture = target;
                camera.Render();
            }
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            File.WriteAllBytes("Library/MapPreviews/" + name + ".png", texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previous;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
