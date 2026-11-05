using UnityEditor;
using UnityEngine;

public sealed class Level03Assets
{
    public const string PrefabFolder = "Assets/Prefabs/Level03";
    public Level02Assets Shared { get; } = new Level02Assets();

    public void Build()
    {
        Shared.LoadExisting();
        MapBootstrapper.EnsureFolder(PrefabFolder);
        var root = new GameObject("BarrelRampTrap");
        try
        {
            root.tag = "Ground";
            root.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            var ramp = TilemapLevelUtility.Sprite(root.transform, "Ramp", Shared.Shared["BridgeDeck"], Shared.Shared.Material, Vector2.zero, 5);
            ramp.tag = "Ground";
            ramp.transform.localScale = new Vector3(1.6f, 1, 1);
            ramp.transform.localRotation = Quaternion.Euler(0, 0, -12);
            ramp.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(3, 0.25f);
            var barrel = AssetDatabase.LoadAssetAtPath<GameObject>(MapBootstrapper.PrefabFolder + "/RollingBarrel.prefab");
            var spawner = new GameObject("BarrelSpawner").AddComponent<BarrelSpawner>();
            spawner.transform.SetParent(root.transform, false);
            spawner.transform.localPosition = new Vector2(-1.5f, 1);
            var serialized = new SerializedObject(spawner);
            serialized.FindProperty("prefab").objectReferenceValue = barrel.GetComponent<RollingBarrelObstacle>();
            serialized.FindProperty("activationDistance").floatValue = 4;
            serialized.FindProperty("spawnInterval").floatValue = 4;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var initialBarrel = (GameObject)PrefabUtility.InstantiatePrefab(barrel);
            initialBarrel.transform.SetParent(root.transform, false);
            initialBarrel.transform.localPosition = new Vector2(-1, 0.9f);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/BarrelRampTrap.prefab");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
