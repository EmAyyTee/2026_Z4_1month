using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class Level03Verification
{
    private const string PendingKey = "Level03Verification.Pending";
    private static double startedAt;
    private static float rideStartedAt;
    private static MovingPlatform2D ferry;
    private static MovingPlatform2D lift;
    private static Rigidbody2D ferryRider;
    private static Rigidbody2D liftRider;
    private static Rigidbody2D barrel;
    private static Vector2 ferryStart;
    private static Vector2 liftStart;
    private static Vector2 barrelStart;
    private static Scene LevelScene => SceneManager.GetSceneByPath(Level03Bootstrapper.ScenePath);

    public static void VerifyAndCapture()
    {
        if (!Application.isBatchMode)
            throw new InvalidOperationException("Run automated verification in batch mode.");
        EditorSceneManager.OpenScene(Level03Bootstrapper.ScenePath, OpenSceneMode.Single);
        SessionState.SetBool(PendingKey, true);
        Resume();
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    private static void Resume()
    {
        if (!SessionState.GetBool(PendingKey, false))
            return;
        EditorApplication.playModeStateChanged -= OnPlayMode;
        EditorApplication.playModeStateChanged += OnPlayMode;
    }

    private static void OnPlayMode(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode)
            return;
        startedAt = EditorApplication.timeSinceStartup;
        EditorApplication.update += BeginChecks;
    }

    private static void BeginChecks()
    {
        if (EditorApplication.timeSinceStartup - startedAt < 0.5)
            return;
        EditorApplication.update -= BeginChecks;
        try
        {
            VerifyGeometry();
            CapturePreviews();
            var movers = UnityEngine.Object.FindObjectsByType<MovingPlatform2D>(FindObjectsSortMode.None);
            ferry = movers.Single(item => item.name == "LowerHallFerry");
            lift = movers.Single(item => item.name == "WestLift");
            ferryStart = ferry.transform.position;
            liftStart = lift.transform.position;
            ferryRider = Probe(ferryStart + Vector2.up * 0.17f);
            liftRider = Probe(liftStart + Vector2.up * 0.17f);
            barrel = UnityEngine.Object.FindAnyObjectByType<RollingBarrelObstacle>().GetComponent<Rigidbody2D>();
            barrelStart = barrel.position;
            rideStartedAt = Time.time;
            EditorApplication.update += FinishRideChecks;
        }
        catch (Exception exception)
        {
            Finish(exception);
        }
    }

    private static void FinishRideChecks()
    {
        if (Time.time - rideStartedAt < 1.8f)
        {
            if (EditorApplication.timeSinceStartup - startedAt > 30)
                Finish(new TimeoutException("Play-mode simulation did not advance."));
            return;
        }
        EditorApplication.update -= FinishRideChecks;
        try
        {
            Check(ferry.transform.position.x > ferryStart.x + 0.2f, "Horizontal platform must move.");
            Check(ferryRider.position.x > ferryStart.x + 0.15f, "Ferry must carry a standing body.");
            Check(Mathf.Abs(ferryRider.position.x - ferry.transform.position.x) < 0.15f, "Rider must remain on the ferry.");
            Check(lift.transform.position.y > liftStart.y + 0.2f, "Lift must move upward.");
            Check(liftRider.position.y > liftStart.y + 0.2f, "Lift must carry a standing body upward.");
            Check(liftRider.IsTouching(lift.GetComponent<Collider2D>()), "Rider must remain supported by the lift.");
            Check(barrel != null && barrel.position.x > barrelStart.x + 0.1f, "Barrel must roll downhill.");
            Debug.Log("LEVEL03_MOVING_PLATFORM_AND_BARREL_TESTS_OK");
            UnityEngine.Object.DestroyImmediate(ferryRider.gameObject);
            UnityEngine.Object.DestroyImmediate(liftRider.gameObject);
            VerifyFallingBodies();
            Finish(null);
        }
        catch (Exception exception)
        {
            Finish(exception);
        }
    }

    private static void Finish(Exception exception)
    {
        EditorApplication.update -= BeginChecks;
        EditorApplication.update -= FinishRideChecks;
        EditorApplication.playModeStateChanged -= OnPlayMode;
        SessionState.SetBool(PendingKey, false);
        if (exception != null)
            Debug.LogException(exception);
        EditorApplication.Exit(exception == null ? 0 : 1);
    }

    private static Tilemap Map(string name)
    {
        var grid = LevelScene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Grid>()).Single();
        return grid.transform.Find(name).GetComponent<Tilemap>();
    }

    private static void VerifyGeometry()
    {
        Level03Bootstrapper.Validate();
        Physics2D.SyncTransforms();
        var ground = Map("Ground");
        var platforms = Map("Platforms");
        var groundCollider = ground.GetComponent<CompositeCollider2D>();
        var platformCollider = platforms.GetComponent<CompositeCollider2D>();
        Check(ground.GetSprite(new Vector3Int(5, -1)).name == "Ground10", "Ground must have a grass surface.");
        Check(ground.GetSprite(new Vector3Int(5, -3)).name == "Ground11", "Ground must have solid rock fill.");
        Check(platforms.GetSprite(new Vector3Int(71, 2)).name == "Plank0", "Deck must have a left cap.");
        Check(platforms.GetSprite(new Vector3Int(74, 2)).name == "Plank2", "Deck must have a right cap.");
        Check(groundCollider.OverlapPoint(new Vector2(5.5f, -0.5f) * MapBootstrapper.CellSize), "Ground collision is missing.");
        Check(!groundCollider.OverlapPoint(new Vector2(10, 3) * MapBootstrapper.CellSize), "Starter corridor must remain open.");
        Check(!groundCollider.OverlapPoint(new Vector2(55, 14) * MapBootstrapper.CellSize), "Middle corridor must remain open.");
        Check(platformCollider.OverlapPoint(new Vector2(72.5f, 2.85f) * MapBootstrapper.CellSize), "Deck surface collision is missing.");
        Check(!platformCollider.OverlapPoint(new Vector2(72.5f, 2.3f) * MapBootstrapper.CellSize), "Space below deck must remain open.");
        for (int x = 1; x < 90; x++)
        {
            Check(groundCollider.OverlapPoint(new Vector2(x, 68.5f) * MapBootstrapper.CellSize), "Boss arena requires a continuous floor.");
            Check(!groundCollider.OverlapPoint(new Vector2(x, 73) * MapBootstrapper.CellSize), "Boss arena must remain open.");
        }
        foreach (var route in Level03Layout.Routes)
            for (int step = 0; step <= 30; step++)
            {
                var center = (route.Start + route.Travel * (step / 30f) + new Vector2(0, 0.03f)) * MapBootstrapper.CellSize;
                var overlaps = Physics2D.OverlapBoxAll(center, new Vector2(2.98f, 0.26f) * MapBootstrapper.CellSize, 0);
                Check(!overlaps.Contains(groundCollider) && !overlaps.Contains(platformCollider), route.Name + " travels through solid terrain.");
            }
        var hazards = LevelScene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ContactDamage2D>()).ToArray();
        Check(hazards.All(item => item.GetComponent<Collider2D>().isTrigger), "Spikes require damage triggers.");
        Check(hazards.Any(item => item.GetComponent<Collider2D>().OverlapPoint(new Vector2(37.5f, 30.3f) * MapBootstrapper.CellSize)), "Spike pit damage area is missing.");
        Debug.Log("LEVEL03_GEOMETRY_AND_ROUTE_TESTS_OK");
    }

    public static void CapturePreviews()
    {
        var scene = LevelScene;
        bool opened = !scene.IsValid();
        if (opened)
            scene = EditorSceneManager.OpenScene(Level03Bootstrapper.ScenePath, OpenSceneMode.Additive);
        try
        {
            var camera = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>()).Single();
            camera.scene = scene;
            Directory.CreateDirectory("Library/MapPreviews");
            IslandMapVerification.Capture(camera, "Level03-overview", new Vector2(50, 39), 47, 2400, 1900);
            IslandMapVerification.Capture(camera, "Level03-lower", new Vector2(47, 12), 20, 2600, 1000);
            IslandMapVerification.Capture(camera, "Level03-central", new Vector2(40, 35), 18, 2000, 1200);
            IslandMapVerification.Capture(camera, "Level03-gallery", new Vector2(72, 58), 11, 2400, 1000);
            IslandMapVerification.Capture(camera, "Level03-arena", new Vector2(46, 73), 13, 2400, 600);
            Debug.Log("LEVEL03_PREVIEWS_OK");
        }
        finally
        {
            if (opened)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static Rigidbody2D Probe(Vector2 position)
    {
        var root = new GameObject("Level03PhysicsProbe");
        SceneManager.MoveGameObjectToScene(root, LevelScene);
        root.transform.position = position;
        var body = root.AddComponent<Rigidbody2D>();
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        root.AddComponent<CircleCollider2D>().radius = 0.1f;
        return body;
    }

    private static void VerifyFallingBodies()
    {
        var bodies = new[] { Probe(new Vector2(10, 3) * MapBootstrapper.CellSize), Probe(new Vector2(72.5f, 5) * MapBootstrapper.CellSize), Probe(new Vector2(97, 3) * MapBootstrapper.CellSize) };
        var previous = Physics2D.simulationMode;
        try
        {
            Physics2D.simulationMode = SimulationMode2D.Script;
            Physics2D.SyncTransforms();
            for (int step = 0; step < 90; step++)
                LevelScene.GetPhysicsScene2D().Simulate(0.02f);
            Check(bodies[0].IsTouching(Map("Ground").GetComponent<CompositeCollider2D>()), "Falling body must land on the ground.");
            Check(bodies[1].IsTouching(Map("Platforms").GetComponent<CompositeCollider2D>()), "Falling body must land on a deck.");
            Check(bodies[2].position.y < -1, "A body must fall outside the map boundary.");
            Debug.Log("LEVEL03_FALL_TESTS_OK");
        }
        finally
        {
            Physics2D.simulationMode = previous;
            foreach (var body in bodies)
                UnityEngine.Object.DestroyImmediate(body.gameObject);
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
