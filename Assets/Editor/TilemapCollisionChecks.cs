using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class TilemapCollisionChecks
{
    [Serializable]
    public sealed class Result
    {
        public string scene;
        public string layer;
        public string tag;
        public bool simulated;
        public int occupiedSamples;
        public int missingSamples;
        public int emptySamples;
        public int ghostSamples;
        public Vector3 visualMin;
        public Vector3 visualMax;
        public Vector3 collisionMin;
        public Vector3 collisionMax;
        public List<Vector2> missingExamples = new List<Vector2>();
        public List<Vector2> ghostExamples = new List<Vector2>();
    }

    public static Result[] Inspect(Scene scene)
    {
        Physics2D.SyncTransforms();
        return GameplayRigBuilder.Components<TilemapCollider2D>(scene)
            .Where(collider => collider.enabled && collider.gameObject.activeInHierarchy)
            .Select(Inspect).ToArray();
    }

    private static Result Inspect(TilemapCollider2D source)
    {
        var map = source.GetComponent<Tilemap>();
        var composite = TilemapCollisionBaker.CompositeOf(source);
        Collider2D collider = composite != null ? composite : source;
        var contributors = source.gameObject.scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TilemapCollider2D>())
            .Where(other => other == source || composite != null && TilemapCollisionBaker.CompositeOf(other) == composite)
            .Select(other => other.GetComponent<Tilemap>()).ToArray();
        var result = new Result { scene = map.gameObject.scene.path, layer = map.name, tag = map.tag,
            simulated = collider.enabled && (collider.attachedRigidbody == null || collider.attachedRigidbody.simulated),
            visualMin = map.transform.TransformPoint(map.localBounds.min), visualMax = map.transform.TransformPoint(map.localBounds.max),
            collisionMin = collider.bounds.min, collisionMax = collider.bounds.max };
        var bounds = map.cellBounds;
        for (int y = bounds.yMin - 1; y <= bounds.yMax; y++)
            for (int x = bounds.xMin - 1; x <= bounds.xMax; x++)
            {
                var cell = new Vector3Int(x, y, 0);
                var type = map.GetColliderType(cell);
                if (type == Tile.ColliderType.None)
                {
                    foreach (var fraction in new[] { new Vector3(0.25f, 0.25f), new Vector3(0.75f, 0.25f), new Vector3(0.25f, 0.75f), new Vector3(0.75f, 0.75f) })
                    {
                        var point = (Vector2)map.transform.TransformPoint(map.CellToLocalInterpolated((Vector3)cell + fraction));
                        result.emptySamples++;
                        if (collider.OverlapPoint(point) && !contributors.Any(contributor => ExpectedAt(contributor, point)))
                        {
                            result.ghostSamples++;
                            if (result.ghostExamples.Count < 8)
                                result.ghostExamples.Add(point);
                        }
                    }
                    continue;
                }
                foreach (var point in SamplePoints(map, cell, type))
                {
                    result.occupiedSamples++;
                    if (result.simulated && collider.OverlapPoint(point))
                        continue;
                    result.missingSamples++;
                    if (result.missingExamples.Count < 8)
                        result.missingExamples.Add(point);
                }
            }
        return result;
    }

    private static bool ExpectedAt(Tilemap map, Vector2 point)
    {
        var cell = map.WorldToCell(point);
        var local = map.transform.InverseTransformPoint(point);
        var shape = new List<Vector2>();
        for (int y = cell.y - 3; y <= cell.y + 3; y++)
            for (int x = cell.x - 3; x <= cell.x + 3; x++)
            {
                var candidate = new Vector3Int(x, y, cell.z);
                var type = map.GetColliderType(candidate);
                if (type == Tile.ColliderType.None)
                    continue;
                if (type == Tile.ColliderType.Grid)
                {
                    if (candidate == cell)
                        return true;
                    continue;
                }
                var sprite = map.GetSprite(candidate);
                if (sprite == null)
                    continue;
                var origin = map.CellToLocalInterpolated((Vector3)candidate + map.tileAnchor);
                var matrix = map.orientationMatrix * map.GetTransformMatrix(candidate);
                Vector2 spritePoint = matrix.inverse.MultiplyPoint3x4(local - origin);
                for (int i = 0; i < sprite.GetPhysicsShapeCount(); i++)
                {
                    sprite.GetPhysicsShape(i, shape);
                    if (Contains(shape, spritePoint))
                        return true;
                }
            }
        return false;
    }

    private static IEnumerable<Vector2> SamplePoints(Tilemap map, Vector3Int cell, Tile.ColliderType type)
    {
        if (type == Tile.ColliderType.Grid)
        {
            foreach (var fraction in new[] { new Vector3(0.25f, 0.25f), new Vector3(0.75f, 0.25f), new Vector3(0.25f, 0.75f), new Vector3(0.75f, 0.75f) })
                yield return map.transform.TransformPoint(map.CellToLocalInterpolated((Vector3)cell + fraction));
            yield break;
        }
        var sprite = map.GetSprite(cell);
        if (sprite == null)
            yield break;
        var points = new List<Vector2>();
        var origin = map.CellToLocalInterpolated((Vector3)cell + map.tileAnchor);
        var matrix = map.orientationMatrix * map.GetTransformMatrix(cell);
        for (int shape = 0; shape < sprite.GetPhysicsShapeCount(); shape++)
        {
            sprite.GetPhysicsShape(shape, points);
            if (points.Count < 3)
                continue;
            Vector2 center = points.Aggregate(Vector2.zero, (sum, point) => sum + point) / points.Count;
            if (!Contains(points, center))
            {
                var start = points[0];
                center = (start + points[1] + points[2]) / 3;
                if (!Contains(points, center))
                    continue;
            }
            yield return map.transform.TransformPoint(origin + matrix.MultiplyPoint3x4(center));
        }
    }

    private static bool Contains(List<Vector2> polygon, Vector2 point)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            var a = polygon[i];
            var b = polygon[j];
            if ((a.y > point.y) != (b.y > point.y) && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                inside = !inside;
        }
        return inside;
    }
}
