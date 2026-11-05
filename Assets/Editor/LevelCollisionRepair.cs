using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Player;

public static class LevelCollisionRepair
{
    [Serializable]
    private sealed class Report
    {
        public List<TilemapCollisionChecks.Result> layers = new List<TilemapCollisionChecks.Result>();
        public List<string> unrenderedSolids = new List<string>();
    }

    public static void AuditAll()
    {
        var report = new Report();
        foreach (string path in GameplaySceneInstaller.ScenePaths)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            report.layers.AddRange(TilemapCollisionChecks.Inspect(scene));
            foreach (var collider in GameplayRigBuilder.Components<Collider2D>(scene))
            {
                if (!collider.enabled || collider.isTrigger || !collider.gameObject.activeInHierarchy)
                    continue;
                if (collider.GetComponent<Renderer>() == null && collider.GetComponentInChildren<Renderer>() == null && collider.GetComponentInParent<Renderer>() == null)
                    report.unrenderedSolids.Add(path + " | " + collider.name + " | " + collider.GetType().Name);
            }
        }
        Directory.CreateDirectory("Library/CollisionVerification");
        File.WriteAllText("Library/CollisionVerification/before.json", JsonUtility.ToJson(report, true));
        foreach (var layer in report.layers)
            Debug.Log($"COLLISION_AUDIT {layer.scene} {layer.layer} missing={layer.missingSamples}/{layer.occupiedSamples} ghosts={layer.ghostSamples}/{layer.emptySamples}");
    }

    [MenuItem("Tools/Gameplay/Rebuild Level Collisions")]
    public static void RepairAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play mode before repairing colliders.");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save open scenes before repairing colliders.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        var before = new Report();
        var after = new Report();
        try
        {
            foreach (string path in GameplaySceneInstaller.ScenePaths)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                before.layers.AddRange(TilemapCollisionChecks.Inspect(scene));
                TilemapCollisionBaker.RebuildScene(scene);
                var results = TilemapCollisionChecks.Inspect(scene);
                after.layers.AddRange(results);
                foreach (var result in results)
                    if (result.missingSamples != 0 || result.ghostSamples != 0)
                        throw new InvalidOperationException($"Collider mismatch in {path}/{result.layer}: missing={result.missingSamples}, ghosts={result.ghostSamples}.");
                var rig = GameplayRigBuilder.Components<GameplayRig>(scene).Single();
                GameplaySceneInstaller.AlignPlayerWithSpawn(scene, rig);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new IOException("Could not save colliders in " + path);
                Debug.Log("LEVEL_COLLIDERS_REBUILT " + path);
            }
            Directory.CreateDirectory("Library/CollisionVerification");
            File.WriteAllText("Library/CollisionVerification/before.json", JsonUtility.ToJson(before, true));
            File.WriteAllText("Library/CollisionVerification/after.json", JsonUtility.ToJson(after, true));
            Debug.Log("ALL_LEVEL_COLLIDERS_REBUILT");
        }
        finally
        {
            if (!Application.isBatchMode)
                EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }
}
