using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Player;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class GameplaySceneInstaller
{
    public static string[] ScenePaths => AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" })
        .Select(AssetDatabase.GUIDToAssetPath)
        .Concat(EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path))
        .Where(path => File.Exists(path) && !path.Contains("/UI/") && !path.EndsWith("/MainMenu.unity") && !path.EndsWith("/GameOver.unity"))
        .Distinct().OrderBy(path => path).ToArray();

    [MenuItem("Tools/Gameplay/Install Shared Player In All Levels")]
    public static void InstallAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play mode before configuring gameplay.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save the open scenes before configuring gameplay.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        var paths = ScenePaths;
        string backup = "Library/GameplayBackups/Install-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
        foreach (string path in paths)
        {
            string target = Path.Combine(backup, path);
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            File.Copy(path, target);
        }
        if (Application.isBatchMode)
            EditorSceneManager.OpenScene(GameplayRigBuilder.ReferenceScene, OpenSceneMode.Single);
        GameplayRigBuilder.EnsurePrefab();
        try
        {
            foreach (string path in paths)
            {
                var scene = EditorSceneManager.OpenScene(path, Application.isBatchMode ? OpenSceneMode.Single : OpenSceneMode.Additive);
                ConfigureScene(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new IOException("Could not save " + path);
                if (!Application.isBatchMode)
                    EditorSceneManager.CloseScene(scene, true);
                Debug.Log("GAMEPLAY_INSTALLED " + path);
            }
            RegisterScenes(paths);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            if (!Application.isBatchMode)
                EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
        Debug.Log("GAMEPLAY_INSTALL_ALL_OK");
    }

    public static void ConfigureScene(Scene scene)
    {
        bool scaled = NormalizeMapScale(scene);
        if (scaled)
            TilemapCollisionBaker.RebuildScene(scene);
        GameplayRigBuilder.EnsurePrefab();
        var existing = GameplayRigBuilder.Components<GameplayRig>(scene).FirstOrDefault();
        if (existing != null)
        {
            if (scaled)
                AlignPlayerWithSpawn(scene, existing);
            GameplayHudInstaller.ConfigureScene(scene, existing);
            EditorSceneManager.MarkSceneDirty(scene);
            return;
        }
        var previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(scene);
        try
        {
            var players = GameplayRigBuilder.Components<PlayerMovement>(scene);
            var oldCamera = GameplayRigBuilder.Components<Camera>(scene).FirstOrDefault(camera => camera.CompareTag("MainCamera"));
            var oldFollow = GameplayRigBuilder.Components<CinemachineVirtualCameraBase>(scene);
            var spawn = GameplayRigBuilder.Components<Transform>(scene).FirstOrDefault(item => item.name == "PlayerSpawn");
            Vector3 position = spawn != null ? spawn.position : players.FirstOrDefault()?.transform.position ?? Vector3.zero;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameplayRigBuilder.PrefabPath);
            var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            root.name = "GameplayRig";
            root.transform.position = position;
            var rig = root.GetComponent<GameplayRig>();
            var health = rig.Player.GetComponent<PlayerHealth>();
            if (players.Length > 0)
            {
                var oldHealth = players[0].GetComponent<PlayerHealth>();
                if (oldHealth != null)
                {
                    health.healthBar = oldHealth.healthBar;
                    health.deathScreen = oldHealth.deathScreen;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(health);
                }
            }
            if (oldCamera != null)
            {
                rig.GameplayCamera.backgroundColor = oldCamera.backgroundColor;
                PrefabUtility.RecordPrefabInstancePropertyModifications(rig.GameplayCamera);
            }
            var replacements = new Dictionary<UnityEngine.Object, UnityEngine.Object>();
            foreach (var player in players)
                MapObjects(player.gameObject, rig.Player.gameObject, replacements);
            if (oldCamera != null)
                MapObjects(oldCamera.gameObject, rig.GameplayCamera.gameObject, replacements);
            foreach (var follow in oldFollow)
                MapObjects(follow.gameObject, rig.FollowCamera.gameObject, replacements);
            RemapReferences(scene, replacements);
            foreach (var player in players)
            {
                foreach (Transform child in player.transform.Cast<Transform>().ToArray())
                    child.SetParent(rig.Player.transform, true);
                UnityEngine.Object.DestroyImmediate(player.gameObject);
            }
            var obsoleteCameras = oldFollow.Select(camera => camera.gameObject)
                .Concat(oldCamera == null ? Array.Empty<GameObject>() : new[] { oldCamera.gameObject }).Distinct().ToArray();
            foreach (var camera in obsoleteCameras)
                if (camera != null)
                {
                    var target = camera == oldCamera?.gameObject ? rig.GameplayCamera.transform : rig.FollowCamera.transform;
                    foreach (Transform child in camera.transform.Cast<Transform>().ToArray())
                        child.SetParent(target, true);
                    UnityEngine.Object.DestroyImmediate(camera);
                }
            if (!GameplayRigBuilder.Components<Light2D>(scene).Any(light => light.lightType == Light2D.LightType.Global))
            {
                var light = new GameObject("Gameplay Global Light").AddComponent<Light2D>();
                light.lightType = Light2D.LightType.Global;
                light.intensity = 1;
            }
            if (spawn == null)
            {
                spawn = new GameObject("PlayerSpawn").transform;
                spawn.position = rig.Player.transform.position;
            }
            PlaceOnGround(rig);
            GameplayHudInstaller.ConfigureScene(scene, rig);
            EditorSceneManager.MarkSceneDirty(scene);
        }
        finally
        {
            if (previous.IsValid())
                SceneManager.SetActiveScene(previous);
        }
    }

    private static bool NormalizeMapScale(Scene scene)
    {
        var environment = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Environment");
        if (environment == null)
            return false;
        var grid = environment.GetComponentInChildren<Grid>();
        if (grid == null)
            return false;
        float current = grid.cellSize.x * grid.transform.lossyScale.x;
        if (Mathf.Approximately(current, MapBootstrapper.CellSize))
            return false;
        float factor = MapBootstrapper.CellSize / current;
        foreach (var root in scene.GetRootGameObjects().Where(root => root.name == "Environment" || root.name == "DynamicObstacles" || root.name == "LevelMarkers"))
        {
            root.transform.position *= factor;
            root.transform.localScale *= factor;
        }
        return true;
    }

    public static void AlignPlayerWithSpawn(Scene scene, GameplayRig rig)
    {
        var spawn = GameplayRigBuilder.Components<Transform>(scene).FirstOrDefault(item => item.name == "PlayerSpawn");
        if (spawn != null)
            rig.transform.position += spawn.position - rig.Player.transform.position;
        PlaceOnGround(rig);
        PrefabUtility.RecordPrefabInstancePropertyModifications(rig.transform);
    }

    private static void PlaceOnGround(GameplayRig rig)
    {
        Physics2D.SyncTransforms();
        var collider = rig.Player.GetComponent<Collider2D>();
        var origin = rig.Player.transform.position;
        float bottomOffset = origin.y - collider.bounds.min.y;
        float height = collider.bounds.size.y;
        var hits = new[] { 0f, height * 0.5f, height }
            .SelectMany(lift => Physics2D.RaycastAll(origin + Vector3.up * lift, Vector2.down, 8 + lift))
            .Where(hit => hit.distance > 0.0001f && hit.normal.y > 0.5f &&
                hit.point.y <= origin.y + bottomOffset + 0.05f &&
                !hit.collider.isTrigger && !hit.collider.transform.IsChildOf(rig.transform) &&
                (hit.collider.CompareTag("Ground") || hit.collider.CompareTag("Platform") || hit.collider.CompareTag("Box")))
            .OrderByDescending(hit => hit.point.y).ToArray();
        if (hits.Length == 0)
            throw new InvalidOperationException("No walkable ground below the player in " + rig.gameObject.scene.path);
        rig.transform.position += Vector3.up * (hits[0].point.y + bottomOffset + 0.05f - origin.y);
        Physics2D.SyncTransforms();
    }

    private static void MapObjects(GameObject oldObject, GameObject newObject, Dictionary<UnityEngine.Object, UnityEngine.Object> replacements)
    {
        replacements[oldObject] = newObject;
        foreach (var component in oldObject.GetComponents<Component>())
        {
            if (component == null)
                continue;
            var target = newObject.GetComponent(component.GetType());
            if (target == null && component is MonoBehaviour && UnityEditorInternal.ComponentUtility.CopyComponent(component))
            {
                UnityEditorInternal.ComponentUtility.PasteComponentAsNew(newObject);
                target = newObject.GetComponent(component.GetType());
            }
            if (target != null)
                replacements[component] = target;
        }
    }

    private static void RemapReferences(Scene scene, Dictionary<UnityEngine.Object, UnityEngine.Object> replacements)
    {
        foreach (var canvas in GameplayRigBuilder.Components<Canvas>(scene))
            if (canvas.worldCamera != null && replacements.TryGetValue(canvas.worldCamera, out var camera))
                canvas.worldCamera = (Camera)camera;
        foreach (var component in GameplayRigBuilder.Components<MonoBehaviour>(scene))
        {
            if (component == null || replacements.ContainsKey(component))
                continue;
            var serialized = new SerializedObject(component);
            var property = serialized.GetIterator();
            bool changed = false;
            while (property.NextVisible(true))
            {
                if (property.propertyPath == "m_Script")
                    continue;
                if (property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue == null)
                    continue;
                if (!replacements.TryGetValue(property.objectReferenceValue, out var target))
                    continue;
                property.objectReferenceValue = target;
                changed = true;
            }
            if (changed)
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
        }
    }

    private static void RegisterScenes(string[] paths)
    {
        var scenes = EditorBuildSettings.scenes.Where(scene => File.Exists(scene.path)).ToList();
        foreach (string path in paths)
            if (scenes.All(scene => scene.path != path))
                scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/EditorBuildSettings.asset"), "ProjectSettings/EditorBuildSettings.asset", true);
    }
}
