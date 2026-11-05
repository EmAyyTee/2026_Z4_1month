using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Game;
using Player;
using UI;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameplayVerification
{
    private const string Pending = "GameplayVerification.Pending";
    private const string Index = "GameplayVerification.Index";
    private const string Results = "GameplayVerification.Results";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static GameplayRig rig;
    private static Rigidbody2D body;
    private static int phase;
    private static float phaseTime;
    private static double startedAt;
    private static Vector2 movementStart;
    private static Vector2 cameraStart;
    private static float jumpStart;
    private static float direction;
    private static Record record;
    private static GameObject testCoin;
    private static int expectedCoins;
    private static int expectedScore;

    [Serializable]
    private sealed class Report { public List<Record> scenes = new List<Record>(); }

    [Serializable]
    private sealed class Record
    {
        public string scene;
        public float speed;
        public float jumpForce;
        public float movedDistance;
        public float jumpRise;
        public float jumpClearance;
        public float dashSpeed;
        public float cameraMovement;
        public float healthAfterDamage;
        public float healthAfterRegeneration;
        public int coinsAfterPickup;
        public float healthBarAfterDeath;
    }

    public static void VerifyWithCollisions()
    {
        LevelCollisionVerification.VerifyAll();
        VerifyAll();
    }

    public static void VerifyAll()
    {
        if (!Application.isBatchMode)
            throw new InvalidOperationException("Run the automated play-mode verification in batch mode.");
        SessionState.SetBool(Pending, true);
        SessionState.SetInt(Index, 0);
        SessionState.SetString(Results, JsonUtility.ToJson(new Report()));
        Resume();
        OpenNext();
    }

    [InitializeOnLoadMethod]
    private static void Resume()
    {
        if (!SessionState.GetBool(Pending, false))
            return;
        EditorApplication.playModeStateChanged -= OnState;
        EditorApplication.playModeStateChanged += OnState;
    }

    private static void OpenNext()
    {
        try
        {
            int index = SessionState.GetInt(Index, 0);
            var paths = GameplaySceneInstaller.ScenePaths;
            if (index >= paths.Length)
            {
                Directory.CreateDirectory("Library/GameplayVerification");
                var report = JsonUtility.FromJson<Report>(SessionState.GetString(Results, JsonUtility.ToJson(new Report())));
                File.WriteAllText("Library/GameplayVerification/report.json", JsonUtility.ToJson(report, true));
                Debug.Log("GAMEPLAY_ALL_LEVELS_VERIFIED " + report.scenes.Count);
                Finish(null);
                return;
            }
            var scene = EditorSceneManager.OpenScene(paths[index], OpenSceneMode.Single);
            Validate(scene);
            EditorApplication.EnterPlaymode();
        }
        catch (Exception exception)
        {
            Finish(exception);
        }
    }

    private static void OnState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += OpenNext;
            return;
        }
        if (state != PlayModeStateChange.EnteredPlayMode)
            return;
        startedAt = EditorApplication.timeSinceStartup;
        phase = 0;
        phaseTime = Time.time;
        rig = UnityEngine.Object.FindAnyObjectByType<GameplayRig>();
        body = rig.Player.GetComponent<Rigidbody2D>();
        record = new Record { scene = rig.gameObject.scene.path, speed = rig.Player.speed, jumpForce = rig.Player.jumpForce };
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        try
        {
            Check(EditorApplication.timeSinceStartup - startedAt < 30, "Play-mode test timed out: " + record.scene);
            if (phase == 0)
            {
                if (Time.time - phaseTime < 0.8f)
                    return;
                Check(!rig.Player.GetComponent<PlayerHealth>().IsDead, "Player died at spawn.");
                Check(rig.Player.isGrounded || rig.Player.onSomething, "Player must settle on walkable terrain.");
                direction = FreeDirection();
                rig.Player.enabled = false;
                Set("_horizontal", direction);
                movementStart = body.position;
                cameraStart = rig.GameplayCamera.transform.position;
                phaseTime = Time.time;
                phase = 1;
            }
            if (phase == 1)
            {
                Call("HandleMovement");
                if (Time.time - phaseTime < 0.4f)
                    return;
                record.movedDistance = (body.position.x - movementStart.x) * direction;
                Check(record.movedDistance > 0.6f, "Player must move using the shared controller.");
                Set("_horizontal", 0f);
                jumpStart = body.position.y;
                var jumpBounds = rig.Player.GetComponent<Collider2D>().bounds;
                record.jumpClearance = Physics2D.BoxCastAll(jumpBounds.center, jumpBounds.size * 0.98f, 0, Vector2.up, 2)
                    .Where(hit => !hit.collider.isTrigger && !hit.collider.transform.IsChildOf(rig.transform) && hit.normal.y < -0.5f)
                    .Select(hit => hit.distance).DefaultIfEmpty(2).Min();
                Set("_jumpPressed", true);
                Call("HandleJump");
                Check(body.linearVelocity.y > 5, "First jump must apply upward velocity.");
                phaseTime = Time.time;
                phase = 2;
                return;
            }
            if (phase == 2)
            {
                Call("HandleMovement");
                if (Time.time - phaseTime < 0.18f)
                    return;
                record.jumpRise = body.position.y - jumpStart;
                float expectedRise = Mathf.Min(0.3f, Mathf.Max(0.02f, record.jumpClearance * 0.7f));
                if (record.jumpRise <= expectedRise)
                    GameplayHudCapture.Capture(rig, "Failure-" + Path.GetFileNameWithoutExtension(record.scene));
                Check(record.jumpRise > expectedRise, $"Player must rise above the ground in {record.scene}: start={jumpStart}, position={body.position}, velocity={body.linearVelocity}, clearance={record.jumpClearance}.");
                Set("_jumpPressed", true);
                Call("HandleJump");
                Check(body.linearVelocity.y > 5, "Double jump must apply upward velocity.");
                Check((int)Get("_jumpsRemaining") == 0, "Both jumps must be consumed.");
                Set("_jumpPressed", true);
                Call("HandleJump");
                Check((int)Get("_jumpsRemaining") == 0, "A third jump must remain unavailable.");
                Set("_lastRightPressTime", -10f);
                Call("HandleDash", direction, -10f);
                Call("HandleDash", direction, Time.time);
                Call("HandleMovement");
                record.dashSpeed = Mathf.Abs(body.linearVelocity.x);
                Check(record.dashSpeed > rig.Player.speed * 3.5f, "Double tap must activate dash.");
                phaseTime = Time.time;
                phase = 3;
                return;
            }
            if (phase == 3)
            {
                Call("HandleMovement");
                if (Time.time - phaseTime < 0.06f)
                    return;
                Set("_isDashing", false);
                Set("_horizontal", 0f);
                Call("HandleMovement");
                phaseTime = Time.time;
                phase = 4;
                return;
            }
            if (phase == 5)
            {
                if (Time.time - phaseTime < 0.12f)
                    return;
                Check(testCoin == null, "The real coin prefab must be picked up by the player.");
                Check(rig.Hud.Session.Coins >= expectedCoins && rig.Hud.Session.Score >= expectedScore,
                    "Coin pickup must update the shared game session.");
                Check(rig.Hud.CoinsText.text == rig.Hud.Session.Coins.ToString(), "HUD must display the updated coin count.");
                record.coinsAfterPickup = rig.Hud.Session.Coins;
                GameplayHudCapture.Capture(rig, Path.GetFileNameWithoutExtension(record.scene));
                phase = 6;
                return;
            }
            if (phase == 6)
            {
                if (Time.time - phaseTime < 0.7f)
                    return;
                var health = rig.Player.GetComponent<PlayerHealth>();
                record.healthAfterRegeneration = health.CurrentHealth;
                Check(record.healthAfterRegeneration > record.healthAfterDamage, "Health regeneration must update the player.");
                Check(Mathf.Approximately(rig.Hud.HealthBar.value, health.CurrentHealth), "Health regeneration must update the HUD.");
                rig.Hud.gameObject.SetActive(false);
                rig.Hud.Session.AddCoins(2);
                rig.Hud.gameObject.SetActive(true);
                Check(rig.Hud.CoinsText.text == rig.Hud.Session.Coins.ToString(), "Re-enabled HUD must refresh its coin count.");
                Check(Mathf.Approximately(rig.Hud.HealthBar.value, health.CurrentHealth), "Re-enabled HUD must refresh its health bar.");
                health.KillImmediately();
                record.healthBarAfterDeath = rig.Hud.HealthBar.value;
                Check(health.IsDead && Mathf.Approximately(record.healthBarAfterDeath, 0), "Player death must empty the health bar.");
                if (health.deathScreen != null)
                    Check(health.deathScreen.activeInHierarchy, "The existing death screen must remain visible after death.");
                Debug.Log("HUD_DAMAGE_REGENERATION_COINS_AND_DEATH_OK " + record.scene);
                CompleteScene();
                return;
            }
            if (phase != 4 || Time.time - phaseTime < 0.4f)
                return;
            record.cameraMovement = Vector2.Distance(cameraStart, rig.GameplayCamera.transform.position);
            Check(record.cameraMovement > 0.05f, "Camera must follow the moving player.");
            var viewport = rig.GameplayCamera.WorldToViewportPoint(rig.Player.transform.position);
            Check(viewport.z > 0 && viewport.x > 0 && viewport.x < 1 && viewport.y > 0 && viewport.y < 1, "Player must remain in the camera view.");
            Check(ReferenceEquals(rig.GameplayCamera.GetComponent<CinemachineBrain>().ActiveVirtualCamera, rig.FollowCamera), "Shared Cinemachine camera must be active.");
            Directory.CreateDirectory("Library/MapPreviews");
            rig.GameplayCamera.scene = rig.gameObject.scene;
            IslandMapVerification.Capture(rig.GameplayCamera, "Gameplay-" + Path.GetFileNameWithoutExtension(record.scene), rig.Player.transform.position, 5, 1280, 720);
            BeginHudChecks();
        }
        catch (Exception exception)
        {
            Finish(exception);
        }
    }

    private static void BeginHudChecks()
    {
        var health = rig.Player.GetComponent<PlayerHealth>();
        Check(rig.Hud.Session == GameSession.Instance, "HUD and pickups must use the same game session.");
        Check(Mathf.Approximately(rig.Hud.HealthBar.value, health.CurrentHealth), "HUD must show the initial player health.");
        Check(health.CurrentHealth > 25, "Player must survive the controlled damage test.");
        int expectedHealth = health.CurrentHealth - 25;
        health.TakeDamage(25, null);
        record.healthAfterDamage = health.CurrentHealth;
        Check(health.CurrentHealth == expectedHealth && Mathf.Approximately(rig.Hud.HealthBar.value, expectedHealth),
            "Taking damage must immediately update the linked health bar.");
        health.regenDelay = 0.4f;
        health.regenInterval = 0.05f;
        health.regenAmount = 5;
        expectedCoins = rig.Hud.Session.Coins + 1;
        expectedScore = rig.Hud.Session.Score + 100;
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Prefabs/GoldCoin.prefab");
        Check(asset != null, "The existing gold coin prefab must be available.");
        testCoin = UnityEngine.Object.Instantiate(asset, health.GetComponent<Collider2D>().bounds.center, Quaternion.identity);
        testCoin.name = "HUD Verification Coin";
        phaseTime = Time.time;
        phase = 5;
    }

    private static void CompleteScene()
    {
        var report = JsonUtility.FromJson<Report>(SessionState.GetString(Results, JsonUtility.ToJson(new Report())));
        report.scenes.Add(record);
        SessionState.SetString(Results, JsonUtility.ToJson(report));
        Debug.Log("GAMEPLAY_SCENE_VERIFIED " + record.scene);
        SessionState.SetInt(Index, SessionState.GetInt(Index, 0) + 1);
        EditorApplication.update -= Tick;
        EditorApplication.ExitPlaymode();
    }

    public static void Validate(Scene scene)
    {
        foreach (var layer in TilemapCollisionChecks.Inspect(scene))
            Check(layer.missingSamples == 0 && layer.ghostSamples == 0,
                "Terrain collision differs from tiles in " + scene.path + "/" + layer.layer);
        var rigs = GameplayRigBuilder.Components<GameplayRig>(scene);
        Check(rigs.Length == 1, "Expected one gameplay rig in " + scene.path);
        var current = rigs[0];
        var spawnBounds = current.Player.GetComponent<Collider2D>().bounds;
        Check(!Physics2D.OverlapBoxAll(spawnBounds.center, spawnBounds.size * 0.98f, 0).Any(collider =>
            !collider.isTrigger && !collider.transform.IsChildOf(current.transform) &&
            (collider.CompareTag("Ground") || collider.CompareTag("Platform") || collider.CompareTag("Box"))),
            "Player must start outside solid terrain in " + scene.path);
        Check(GameplayRigBuilder.Components<PlayerMovement>(scene).Length == 1, "Expected one player.");
        Check(GameplayRigBuilder.Components<Camera>(scene).Count(camera => camera.enabled) == 1, "Expected one gameplay camera.");
        Check(GameplayRigBuilder.Components<AudioListener>(scene).Count(listener => listener.enabled) == 1, "Expected one audio listener.");
        Check(current.FollowCamera.Follow == current.Player.transform, "Follow target is missing.");
        Check(current.Player.CompareTag("Player"), "Player tag is missing.");
        Check(Mathf.Approximately(current.Player.speed, 3) && Mathf.Approximately(current.Player.jumpForce, 8), "Controller settings must match Krystian's reference scene.");
        Check(current.Player.maxJumps == 2 && Mathf.Approximately(current.FollowCamera.Lens.OrthographicSize, 5), "Jump count and camera settings must be consistent.");
        Check(current.Hud != null && current.Hud.PlayerHealth == current.Player.GetComponent<PlayerHealth>(), "HUD must be bound to the shared player.");
        Check(current.Hud.HealthBar != null && current.Hud.PlayerHealth.healthBar == current.Hud.HealthBar, "HUD health slider must be assigned to PlayerHealth.");
        Check(current.Hud.Session != null && current.Hud.CoinsText != null, "HUD coin counter must be connected to a game session.");
        Check(GameplayRigBuilder.Components<HUDManager>(scene).Count(hud => hud.isActiveAndEnabled) == 1, "Expected one visible player HUD.");
        Check(GameplayRigBuilder.Components<UnityEngine.UI.Slider>(scene).Count(slider => slider.isActiveAndEnabled &&
            slider.name.StartsWith("PlayerHealthBar", StringComparison.Ordinal)) == 1, "Expected one active player health bar.");
        Check(GameplayRigBuilder.Components<GameSession>(scene).Count(session => session.gameObject.activeInHierarchy) == 1, "Expected one active game session.");
        foreach (var item in GameplayRigBuilder.Components<Transform>(scene))
            Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject) == 0, "Missing script in " + scene.path + ": " + item.name);
    }

    private static float FreeDirection()
    {
        var bounds = rig.Player.GetComponent<Collider2D>().bounds;
        float Available(float value) => Physics2D.BoxCastAll(bounds.center, bounds.size * 0.9f, 0, Vector2.right * value, 2)
            .Where(hit => !hit.collider.isTrigger && !hit.collider.transform.IsChildOf(rig.transform))
            .Select(hit => hit.distance).DefaultIfEmpty(2).Min();
        return Available(1) >= Available(-1) ? 1 : -1;
    }

    private static void Set(string name, object value) => typeof(PlayerMovement).GetField(name, Private).SetValue(rig.Player, value);
    private static object Get(string name) => typeof(PlayerMovement).GetField(name, Private).GetValue(rig.Player);

    private static void Call(string name, params object[] arguments)
    {
        typeof(PlayerMovement).GetMethod(name, Private).Invoke(rig.Player, arguments);
    }

    private static void Finish(Exception exception)
    {
        SessionState.SetBool(Pending, false);
        EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= OnState;
        if (exception != null)
            Debug.LogException(exception);
        EditorApplication.Exit(exception == null ? 0 : 1);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
