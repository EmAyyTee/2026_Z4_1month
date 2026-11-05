using UnityEditor;
using UnityEngine;

public static class IslandObstacleBuilder
{
    public static void Build(IslandArtBuilder art)
    {
        var projectile = Root("ObstacleProjectile", art, "Projectile");
        var projectileBody = projectile.AddComponent<Rigidbody2D>();
        projectileBody.gravityScale = 0;
        projectileBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var projectileCollider = projectile.AddComponent<CircleCollider2D>();
        projectileCollider.radius = 0.18f;
        projectileCollider.isTrigger = true;
        projectile.AddComponent<ObstacleProjectile>();
        var projectileAsset = Save(projectile);

        var barrel = Root("RollingBarrel", art, "Barrel");
        var barrelBody = barrel.AddComponent<Rigidbody2D>();
        barrelBody.gravityScale = 1;
        barrelBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        barrelBody.interpolation = RigidbodyInterpolation2D.Interpolate;
        var barrelCollider = barrel.AddComponent<CircleCollider2D>();
        barrelCollider.radius = 0.36f;
        barrel.AddComponent<RollingBarrelObstacle>();
        Save(barrel);

        var platform = Root("CollapsingPlatform", art, "BridgeDeck");
        platform.tag = "Ground";
        platform.GetComponent<SpriteRenderer>().color = new Color(1f, 0.82f, 0.62f);
        var platformBody = platform.AddComponent<Rigidbody2D>();
        platformBody.bodyType = RigidbodyType2D.Kinematic;
        platformBody.constraints = RigidbodyConstraints2D.FreezeRotation;
        platformBody.useFullKinematicContacts = true;
        platformBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var platformCollider = platform.AddComponent<BoxCollider2D>();
        platformCollider.size = new Vector2(3, 0.27f);
        platformCollider.offset = new Vector2(0, 0.03f);
        platform.AddComponent<CollapsingPlatformObstacle>();
        Save(platform);

        var totem = Root("ShooterTotem", art, "Totem");
        var totemCollider = totem.AddComponent<BoxCollider2D>();
        totemCollider.size = new Vector2(0.7f, 0.8f);
        totemCollider.offset = new Vector2(0, 0.45f);
        var firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(totem.transform, false);
        firePoint.transform.localPosition = new Vector3(-0.55f, 0.6f, 0);
        var shooter = totem.AddComponent<ShooterTotemObstacle>();
        var serialized = new SerializedObject(shooter);
        serialized.FindProperty("projectilePrefab").objectReferenceValue = projectileAsset.GetComponent<ObstacleProjectile>();
        serialized.FindProperty("firePoint").objectReferenceValue = firePoint.transform;
        serialized.FindProperty("detectionRadius").floatValue = 3.2f;
        serialized.FindProperty("projectileSpeed").floatValue = 2f;
        serialized.FindProperty("fireCooldown").floatValue = 1.8f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        Save(totem);
    }

    private static GameObject Root(string name, IslandArtBuilder art, string sprite)
    {
        var root = new GameObject(name);
        var renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = art[sprite];
        renderer.sortingOrder = 5;
        renderer.sharedMaterial = art.Material;
        return root;
    }

    private static GameObject Save(GameObject root)
    {
        try
        {
            return PrefabUtility.SaveAsPrefabAsset(root, MapBootstrapper.PrefabFolder + "/" + root.name + ".prefab");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
