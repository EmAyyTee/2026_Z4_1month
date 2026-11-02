using System;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class TilemapLevelUtility
{
    public static Tilemap Layer(Transform parent, string name, int order, Material material)
    {
        var root = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        root.transform.SetParent(parent, false);
        var renderer = root.GetComponent<TilemapRenderer>();
        renderer.sortingOrder = order;
        renderer.sharedMaterial = material;
        return root.GetComponent<Tilemap>();
    }

    public static void AddCollision(Tilemap map)
    {
        var root = map.gameObject;
        root.tag = "Ground";
        root.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        var collider = root.AddComponent<TilemapCollider2D>();
        collider.ProcessTilemapChanges();
        var composite = root.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        composite.vertexDistance = 0.001f;
        collider.extrusionFactor = 0.0005f;
        collider.compositeOperation = Collider2D.CompositeOperation.Merge;
        composite.GenerateGeometry();
        if (composite.pathCount == 0)
            throw new InvalidOperationException($"No collision geometry generated for {map.name}.");
    }

    public static void Paint(Tilemap map, TileBase tile, RectInt rectangle)
    {
        foreach (var cell in rectangle.allPositionsWithin)
            map.SetTile(new Vector3Int(cell.x, cell.y), tile);
    }

    public static SpriteRenderer Sprite(Transform parent, string name, Sprite sprite, Material material, Vector2 position, int order)
    {
        var root = new GameObject(name, typeof(SpriteRenderer));
        root.transform.SetParent(parent, false);
        root.transform.localPosition = position;
        var renderer = root.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = material;
        renderer.sortingOrder = order;
        return renderer;
    }
}
