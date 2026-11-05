using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class LevelCollisionVerification
{
    [Serializable]
    private sealed class Report
    {
        public List<Record> scenes = new List<Record>();
    }

    [Serializable]
    private sealed class Record
    {
        public string scene;
        public int filledSamples;
        public int emptySamples;
        public int surfaceRays;
        public int landings;
        public List<Vector2> failures = new List<Vector2>();
    }

    private sealed class Probe
    {
        public BoxCollider2D box;
        public Collider2D ground;
        public Vector2 surface;
    }

    public static void VerifyAll()
    {
        if (!Application.isBatchMode)
            throw new InvalidOperationException("Run the full collision verification in batch mode.");
        var report = new Report();
        foreach (string path in GameplaySceneInstaller.ScenePaths)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            GameplayVerification.Validate(scene);
            var record = Verify(scene);
            report.scenes.Add(record);
            Directory.CreateDirectory("Library/CollisionVerification");
            File.WriteAllText("Library/CollisionVerification/landings.json", JsonUtility.ToJson(report, true));
            Debug.Log($"COLLISION_SCENE_CHECKED {path} surfaces={record.surfaceRays} landings={record.landings} failures={record.failures.Count}");
        }
        var failed = report.scenes.FirstOrDefault(record => record.failures.Count != 0);
        if (failed != null)
            throw new InvalidOperationException("Invalid terrain surface or landing in " + failed.scene + ": " + failed.failures[0]);
        Debug.Log("ALL_LEVEL_COLLISIONS_VERIFIED " + report.scenes.Count);
    }

    private static Record Verify(Scene scene)
    {
        var layers = TilemapCollisionChecks.Inspect(scene);
        var record = new Record { scene = scene.path,
            filledSamples = layers.Sum(layer => layer.occupiedSamples), emptySamples = layers.Sum(layer => layer.emptySamples) };
        var actor = GameplayRigBuilder.Components<GameplayRig>(scene).Single().Player;
        var actorBody = actor.GetComponent<Rigidbody2D>();
        Vector2 size = actor.GetComponent<Collider2D>().bounds.size;
        var bodies = GameplayRigBuilder.Components<Rigidbody2D>(scene)
            .Where(body => body.bodyType == RigidbodyType2D.Dynamic)
            .Select(body => (body, body.simulated, body.bodyType)).ToArray();
        var previousMode = Physics2D.simulationMode;
        var probes = new List<Probe>();
        try
        {
            foreach (var entry in bodies)
            {
                if (entry.body.GetComponent<Moving_Platform.MovingPlatform>() != null || entry.body.GetComponent<MovingPlatform2D>() != null)
                    entry.body.bodyType = RigidbodyType2D.Kinematic;
                else
                    entry.body.simulated = false;
            }
            Physics2D.simulationMode = SimulationMode2D.Script;
            Physics2D.SyncTransforms();
            foreach (var source in GameplayRigBuilder.Components<TilemapCollider2D>(scene))
            {
                if (!source.isActiveAndEnabled || source.isTrigger ||
                    !(source.CompareTag("Ground") || source.CompareTag("Platform") || source.CompareTag("Box")))
                    continue;
                var composite = TilemapCollisionBaker.CompositeOf(source);
                Collider2D ground = composite != null ? composite : source;
                foreach (var point in Surfaces(source.GetComponent<Tilemap>()))
                {
                    if (ground.OverlapPoint(point + Vector2.up * 0.03f))
                        continue;
                    var hits = Physics2D.RaycastAll(point + Vector2.up * 0.05f, Vector2.down, 0.1f, ~0);
                    var hit = hits
                        .FirstOrDefault(candidate => candidate.collider == ground && candidate.normal.y > 0.5f);
                    record.surfaceRays++;
                    if (hit.collider == null || Mathf.Abs(hit.point.y - point.y) > 0.025f)
                    {
                        record.failures.Add(point);
                        if (record.failures.Count < 8)
                            Debug.LogWarning($"SURFACE_RAY_MISMATCH {source.name} at {point}: " +
                                string.Join("; ", hits.Select(candidate => $"{candidate.collider.name} at {candidate.point} normal {candidate.normal}")));
                        continue;
                    }
                    Vector2 center = point + Vector2.up * (size.y * 0.5f + 0.12f);
                    if (hit.normal.y < 0.98f || Physics2D.OverlapBoxAll(center, size * 0.98f, 0)
                        .Any(collider => !collider.isTrigger))
                        continue;
                    if (probes.Any(probe => Mathf.Abs(probe.surface.x - point.x) < size.x + 0.12f &&
                        Mathf.Abs(probe.surface.y - point.y) < size.y + 0.24f))
                        continue;
                    var item = new GameObject("Collision Verification Probe");
                    item.layer = actor.gameObject.layer;
                    item.transform.position = center;
                    var body = item.AddComponent<Rigidbody2D>();
                    body.gravityScale = actorBody.gravityScale;
                    body.linearDamping = actorBody.linearDamping;
                    body.constraints = RigidbodyConstraints2D.FreezeRotation;
                    body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                    var box = item.AddComponent<BoxCollider2D>();
                    box.size = size;
                    box.sharedMaterial = actor.GetComponent<Collider2D>().sharedMaterial;
                    probes.Add(new Probe { box = box, ground = ground, surface = point });
                }
            }
            Physics2D.SyncTransforms();
            for (int step = 0; step < 50; step++)
                scene.GetPhysicsScene2D().Simulate(0.02f);
            foreach (var probe in probes)
            {
                if (!probe.box.IsTouching(probe.ground) || Mathf.Abs(probe.box.bounds.min.y - probe.surface.y) > 0.035f)
                    record.failures.Add(probe.surface);
                else
                    record.landings++;
            }
        }
        finally
        {
            foreach (var probe in probes)
                UnityEngine.Object.DestroyImmediate(probe.box.gameObject);
            foreach (var entry in bodies)
                if (entry.body != null)
                {
                    entry.body.bodyType = entry.bodyType;
                    entry.body.simulated = entry.simulated;
                }
            Physics2D.simulationMode = previousMode;
            Physics2D.SyncTransforms();
        }
        return record;
    }

    private static IEnumerable<Vector2> Surfaces(Tilemap map)
    {
        var shape = new List<Vector2>();
        foreach (var cell in map.cellBounds.allPositionsWithin)
        {
            var type = map.GetColliderType(cell);
            if (type == Tile.ColliderType.None)
                continue;
            if (type == Tile.ColliderType.Grid)
            {
                yield return map.transform.TransformPoint(map.CellToLocalInterpolated((Vector3)cell + new Vector3(0.5f, 1)));
                continue;
            }
            var sprite = map.GetSprite(cell);
            if (sprite == null)
                continue;
            var origin = map.CellToLocalInterpolated((Vector3)cell + map.tileAnchor);
            var matrix = map.orientationMatrix * map.GetTransformMatrix(cell);
            for (int i = 0; i < sprite.GetPhysicsShapeCount(); i++)
            {
                sprite.GetPhysicsShape(i, shape);
                var polygon = shape.Select(point => (Vector2)map.transform.TransformPoint(origin + matrix.MultiplyPoint3x4(point))).ToArray();
                if (polygon.Length < 3)
                    continue;
                float x = (polygon.Min(point => point.x) + polygon.Max(point => point.x)) * 0.5f;
                float y = float.NegativeInfinity;
                for (int edge = 0; edge < polygon.Length; edge++)
                {
                    Vector2 a = polygon[edge], b = polygon[(edge + 1) % polygon.Length];
                    if (x < Mathf.Min(a.x, b.x) || x > Mathf.Max(a.x, b.x) || Mathf.Approximately(a.x, b.x))
                        continue;
                    y = Mathf.Max(y, Mathf.Lerp(a.y, b.y, (x - a.x) / (b.x - a.x)));
                }
                if (!float.IsNegativeInfinity(y))
                    yield return new Vector2(x, y);
            }
        }
    }
}
