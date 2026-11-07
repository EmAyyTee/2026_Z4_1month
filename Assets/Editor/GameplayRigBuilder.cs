using System;
using System.Linq;
using Player;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameplayRigBuilder
{
    public const string PrefabPath = "Assets/Prefabs/Gameplay/GameplayRig.prefab";
    public const string ReferenceScene = "Assets/Scenes/Level_0.unity";
    public const string PlayerPrefab = "Assets/Prefab/Player.prefab";

    public static void EnsurePrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            GameplayHudInstaller.EnsurePrefabHud();
            return;
        }
        MapBootstrapper.EnsureFolder("Assets/Prefabs/Gameplay");
        Scene previous = SceneManager.GetActiveScene();
        Scene reference = SceneManager.GetSceneByPath(ReferenceScene);
        bool opened = !reference.IsValid();
        if (opened)
            reference = EditorSceneManager.OpenScene(ReferenceScene, OpenSceneMode.Additive);
        Scene temporary = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(temporary);
            var root = new GameObject("GameplayRig");
            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab), temporary);
            player.transform.SetParent(root.transform, false);
            player.transform.localPosition = Vector3.zero;
            var movement = player.GetComponent<PlayerMovement>();
            var referenceMovement = Components<PlayerMovement>(reference).Single();
            movement.speed = referenceMovement.speed;
            movement.jumpForce = referenceMovement.jumpForce;
            movement.maxJumps = referenceMovement.maxJumps;
            movement.dashMultiplier = referenceMovement.dashMultiplier;
            movement.dashDuration = referenceMovement.dashDuration;
            movement.dashCooldown = referenceMovement.dashCooldown;
            movement.doubleTapTime = referenceMovement.doubleTapTime;
            movement.isKnockedBack = false;
            movement.isGrounded = false;
            movement.onSomething = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(movement);
            var sourceCamera = Components<Camera>(reference).Single(camera => camera.CompareTag("MainCamera"));
            var main = UnityEngine.Object.Instantiate(sourceCamera.gameObject, root.transform).GetComponent<Camera>();
            main.name = "Main Camera";
            main.transform.localPosition = new Vector3(0, 0, -10);
            var sourceFollow = Components<CinemachineCamera>(reference).Single();
            var follow = UnityEngine.Object.Instantiate(sourceFollow.gameObject, root.transform).GetComponent<CinemachineCamera>();
            follow.name = "Follow Camera";
            follow.transform.localPosition = new Vector3(0, 0, -10);
            follow.Follow = player.transform;
            var rig = root.AddComponent<GameplayRig>();
            var serialized = new SerializedObject(rig);
            serialized.FindProperty("player").objectReferenceValue = movement;
            serialized.FindProperty("gameplayCamera").objectReferenceValue = main;
            serialized.FindProperty("followCamera").objectReferenceValue = follow;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (PrefabUtility.SaveAsPrefabAsset(root, PrefabPath) == null)
                throw new InvalidOperationException("Cannot create the shared gameplay rig.");
        }
        finally
        {
            if (previous.IsValid())
                SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(temporary, true);
            if (opened)
                EditorSceneManager.CloseScene(reference, true);
        }
        AssetDatabase.SaveAssets();
        GameplayHudInstaller.EnsurePrefabHud();
    }

    public static T[] Components<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
    }
}
