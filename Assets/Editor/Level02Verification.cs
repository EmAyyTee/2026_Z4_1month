using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class Level02Verification
{
    private const string PendingKey = "Level02Verification.Pending";
    private static double startedAt;
    private static float rideStartedAt;
    private static MovingPlatform2D ferry;
    private static MovingPlatform2D lift;
    private static Rigidbody2D ferryRider;
    private static Rigidbody2D liftRider;
    private static Vector2 initialFerryPosition;
    private static Vector2 initialLiftPosition;

    public static void VerifyAndCapture()
    {
        if (!Application.isBatchMode)
            throw new InvalidOperationException("Run automated verification in batch mode.");
        EditorSceneManager.OpenScene(Level02Bootstrapper.ScenePath, OpenSceneMode.Single);
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
            ferry = movers.Single(item => item.name == "EastFerry");
            lift = movers.Single(item => item.name == "WestLift");
            initialFerryPosition = ferry.transform.position;
            initialLiftPosition = lift.transform.position;
            ferryRider = Probe(initialFerryPosition + Vector2.up * 0.17f);
            liftRider = Probe(initialLiftPosition + Vector2.up * 0.17f);
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
            Check(ferry.transform.position.x > initialFerryPosition.x + 0.2f, "Horizontal platform must move.");
            Check(ferryRider.position.x > initialFerryPosition.x + 0.15f, "Platform must carry a standing body horizontally.");
            Check(Mathf.Abs(ferryRider.position.x - ferry.transform.position.x) < 0.15f, "Rider must remain on the ferry.");
            Check(lift.transform.position.y > initialLiftPosition.y + 0.2f, "Lift must move upward.");
            Check(liftRider.position.y > initialLiftPosition.y + 0.2f, "Lift must carry a standing body upward.");
            Check(liftRider.IsTouching(lift.GetComponent<Collider2D>()), "Rider must remain supported by the lift.");
            Debug.Log("LEVEL02_MOVING_PLATFORM_TESTS_OK");
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

    private static Scene LevelScene => SceneManager.GetSceneByPath(Level02Bootstrapper.ScenePath);

    private static Tilemap Map(string name)
    {
        var grid = LevelScene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Grid>()).Single();
        return grid.transform.Find(name).GetComponent<Tilemap>();
    }

    private static void VerifyGeometry()
    {
        Level02Bootstrapper.Validate();
        Physics2D.SyncTransforms();
        var ground = Map("Ground");
        var platforms = Map("Platforms");
        var groundCollider = ground.GetComponent<CompositeCollider2D>();
        var platformCollider = platforms.GetComponent<CompositeCollider2D>();
        Check(ground.GetSprite(new Vector3Int(5, -1)).name == "Ground10", "Ground must have a grass surface.");
        Check(ground.GetSprite(new Vector3Int(5, -3)).name == "Ground11", "Ground must have solid rock fill.");
        Check(platforms.GetSprite(new Vector3Int(6, 23)).name == "Plank0", "Deck must have a left cap.");
        Check(platforms.GetSprite(new Vector3Int(8, 23)).name == "Plank2", "Deck must have a right cap.");
        Check(groundCollider.OverlapPoint(new Vector2(5.5f, -0.5f) * MapBootstrapper.CellSize), "Ground collision is missing.");
        Check(!groundCollider.OverlapPoint(new Vector2(20, 5) * MapBootstrapper.CellSize), "Lower courtyard must remain open.");
        Check(platformCollider.OverlapPoint(new Vector2(7.5f, 23.85f) * MapBootstrapper.CellSize), "Deck surface collision is missing.");
        Check(!platformCollider.OverlapPoint(new Vector2(7.5f, 23.3f) * MapBootstrapper.CellSize), "Space below deck must remain open.");
        foreach (var route in Level02Layout.Routes)
            for (int step = 0; step <= 30; step++)
            {
                var center = (route.Start + route.Travel * (step / 30f) + new Vector2(0, 0.03f)) * MapBootstrapper.CellSize;
                var overlaps = Physics2D.OverlapBoxAll(center, new Vector2(2.98f, 0.26f) * MapBootstrapper.CellSize, 0);
                Check(!overlaps.Contains(groundCollider) && !overlaps.Contains(platformCollider), route.Name + " travels through solid terrain.");
            }
        var water = LevelScene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ContactDamage2D>()).Single(item => item.name == "HazardWater");
        Check(water.GetComponent<Collider2D>().isTrigger && water.GetComponent<Collider2D>().OverlapPoint(new Vector2(20, 19.3f) * MapBootstrapper.CellSize), "Water must have a matching damage trigger.");
        Check(LevelScene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CollapsingPlatformObstacle>()).Count() == 2, "Water crossing requires two collapsing segments.");
        Debug.Log("LEVEL02_GEOMETRY_AND_ROUTE_TESTS_OK");
    }

    public static void CapturePreviews()
    {
        var scene = LevelScene;
        bool opened = !scene.IsValid();
        if (opened)
            scene = EditorSceneManager.OpenScene(Level02Bootstrapper.ScenePath, OpenSceneMode.Additive);
        try
        {
            var camera = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>()).Single();
            camera.scene = scene;
            Directory.CreateDirectory("Library/MapPreviews");
            IslandMapVerification.Capture(camera, "Level02-overview", new Vector2(59, 27), 36, 2600, 1500);
            IslandMapVerification.Capture(camera, "Level02-courtyard", new Vector2(29, 6), 12, 2000, 1000);
            IslandMapVerification.Capture(camera, "Level02-water", new Vector2(25, 23), 10, 2200, 900);
            IslandMapVerification.Capture(camera, "Level02-upper", new Vector2(84, 43), 15, 2200, 1200);
            Debug.Log("LEVEL02_PREVIEWS_OK");
        }
        finally
        {
            if (opened)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static Rigidbody2D Probe(Vector2 position)
    {
        var root = new GameObject("Level02PhysicsProbe");
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
        var bodies = new[] { Probe(new Vector2(10, 3) * MapBootstrapper.CellSize), Probe(new Vector2(7.5f, 26) * MapBootstrapper.CellSize), Probe(new Vector2(80, 3) * MapBootstrapper.CellSize) };
        var previous = Physics2D.simulationMode;
        try
        {
            Physics2D.simulationMode = SimulationMode2D.Script;
            Physics2D.SyncTransforms();
            for (int step = 0; step < 90; step++)
                LevelScene.GetPhysicsScene2D().Simulate(0.02f);
            Check(bodies[0].IsTouching(Map("Ground").GetComponent<CompositeCollider2D>()), "Falling body must land on the ground.");
            Check(bodies[1].IsTouching(Map("Platforms").GetComponent<CompositeCollider2D>()), "Falling body must land on a deck.");
            Check(bodies[2].position.y < -1, "A body must fall through the open ravine.");
            Debug.Log("LEVEL02_FALL_TESTS_OK");
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
