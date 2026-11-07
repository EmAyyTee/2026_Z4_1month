using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class TilemapCollisionBaker
{
    private sealed class Settings
    {
        public readonly Vector2 offset;
        public readonly PhysicsMaterial2D material;
        public readonly bool trigger;
        public readonly bool effector;
        public readonly bool enabled;
        public readonly LayerMask include;
        public readonly LayerMask exclude;
        public readonly LayerMask send;
        public readonly LayerMask receive;
        public readonly LayerMask capture;
        public readonly LayerMask callbacks;
        public readonly int priority;

        public Settings(Collider2D collider)
        {
            offset = collider.offset;
            material = collider.sharedMaterial;
            trigger = collider.isTrigger;
            effector = collider.usedByEffector;
            enabled = collider.enabled;
            include = collider.includeLayers;
            exclude = collider.excludeLayers;
            send = collider.forceSendLayers;
            receive = collider.forceReceiveLayers;
            capture = collider.contactCaptureLayers;
            callbacks = collider.callbackLayers;
            priority = collider.layerOverridePriority;
        }

        public void Apply(Collider2D collider)
        {
            collider.offset = offset;
            collider.sharedMaterial = material;
            collider.isTrigger = trigger;
            collider.usedByEffector = effector;
            collider.enabled = enabled;
            collider.includeLayers = include;
            collider.excludeLayers = exclude;
            collider.forceSendLayers = send;
            collider.forceReceiveLayers = receive;
            collider.contactCaptureLayers = capture;
            collider.callbackLayers = callbacks;
            collider.layerOverridePriority = priority;
        }
    }

    public static CompositeCollider2D CompositeOf(TilemapCollider2D collider)
    {
        return collider.compositeOperation == Collider2D.CompositeOperation.None || collider.attachedRigidbody == null
            ? null : collider.attachedRigidbody.GetComponent<CompositeCollider2D>();
    }

    public static void RebuildScene(Scene scene)
    {
        var sources = GameplayRigBuilder.Components<TilemapCollider2D>(scene);
        foreach (var source in sources)
        {
            var body = source.attachedRigidbody;
            if (!source.isActiveAndEnabled || source.isTrigger || body == null ||
                !(source.CompareTag("Ground") || source.CompareTag("Platform")))
                continue;
            body.simulated = true;
            if (body.GetComponent<Moving_Platform.MovingPlatform>() != null || body.GetComponent<MovingPlatform2D>() != null)
                body.bodyType = RigidbodyType2D.Kinematic;
            if (PrefabUtility.IsPartOfPrefabInstance(body))
                PrefabUtility.RecordPrefabInstancePropertyModifications(body);
        }
        var entries = sources.Select(source => new
        {
            old = source,
            map = source.GetComponent<Tilemap>(),
            settings = new Settings(source),
            operation = source.compositeOperation,
            extrusion = source.extrusionFactor
        }).ToArray();
        var composites = GameplayRigBuilder.Components<CompositeCollider2D>(scene).Select(composite => new
        {
            old = composite,
            owner = composite.gameObject,
            settings = new Settings(composite),
            geometry = sources.Any(source => CompositeOf(source) == composite)
                ? CompositeCollider2D.GeometryType.Polygons : composite.geometryType,
            generation = composite.generationType,
            vertexDistance = composite.vertexDistance,
            edgeRadius = composite.edgeRadius
        }).ToArray();
        var oldColliders = new HashSet<UnityEngine.Object>(entries.Select(entry => (UnityEngine.Object)entry.old)
            .Concat(composites.Select(entry => (UnityEngine.Object)entry.old)));
        var links = new List<(MonoBehaviour owner, string path, UnityEngine.Object target)>();
        foreach (var script in GameplayRigBuilder.Components<MonoBehaviour>(scene))
        {
            if (script == null)
                continue;
            var serialized = new SerializedObject(script);
            var property = serialized.GetIterator();
            while (property.NextVisible(true))
                if (property.propertyType == SerializedPropertyType.ObjectReference &&
                    !ReferenceEquals(property.objectReferenceValue, null) && oldColliders.Contains(property.objectReferenceValue))
                    links.Add((script, property.propertyPath, property.objectReferenceValue));
        }
        var replacements = new Dictionary<UnityEngine.Object, UnityEngine.Object>();
        foreach (var entry in entries)
            UnityEngine.Object.DestroyImmediate(entry.old);
        foreach (var entry in composites)
            UnityEngine.Object.DestroyImmediate(entry.old);
        Physics2D.SyncTransforms();
        var created = new List<TilemapCollider2D>();
        foreach (var entry in entries)
        {
            entry.map.RefreshAllTiles();
            var collider = entry.map.gameObject.AddComponent<TilemapCollider2D>();
            entry.settings.Apply(collider);
            collider.extrusionFactor = entry.extrusion;
            collider.ProcessTilemapChanges();
            created.Add(collider);
            replacements[entry.old] = collider;
        }
        foreach (var entry in composites)
        {
            var composite = entry.owner.AddComponent<CompositeCollider2D>();
            entry.settings.Apply(composite);
            composite.geometryType = entry.geometry;
            composite.generationType = entry.generation;
            composite.vertexDistance = entry.vertexDistance;
            composite.edgeRadius = entry.edgeRadius;
            replacements[entry.old] = composite;
        }
        for (int i = 0; i < entries.Length; i++)
        {
            created[i].compositeOperation = entries[i].operation;
            created[i].ProcessTilemapChanges();
        }
        Physics2D.SyncTransforms();
        foreach (var composite in composites.Select(entry => entry.owner.GetComponent<CompositeCollider2D>()))
            composite.GenerateGeometry();
        foreach (var group in links.GroupBy(link => link.owner))
        {
            var serialized = new SerializedObject(group.Key);
            foreach (var link in group)
                serialized.FindProperty(link.path).objectReferenceValue = replacements[link.target];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (PrefabUtility.IsPartOfPrefabInstance(group.Key))
                PrefabUtility.RecordPrefabInstancePropertyModifications(group.Key);
        }
    }
}
