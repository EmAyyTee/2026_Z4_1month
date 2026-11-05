using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class Level02Assets
{
    private const string Source = "Assets/Treasure Hunters/Treasure Hunters/";
    public const string Folder = "Assets/Maps/Level02Art";
    public const string PrefabFolder = "Assets/Prefabs/Level02";
    public IslandArtBuilder Shared { get; } = new IslandArtBuilder();
    public Sprite Spikes { get; private set; }
    public Sprite Water { get; private set; }
    public Sprite Sky { get; private set; }
    public Sprite Chain { get; private set; }

    public void LoadExisting()
    {
        Shared.LoadExisting();
        Spikes = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/Spikes.png");
        Water = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/Water.png");
        Sky = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/Sky.png");
        Chain = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/Chain.png");
    }

    public void Build()
    {
        Shared.LoadExisting();
        MapBootstrapper.EnsureFolder(Folder);
        MapBootstrapper.EnsureFolder(PrefabFolder);
        Spikes = Import("Spikes", "Palm Tree Island/Sprites/Objects/Spikes/Spikes.png");
        Water = Import("Water", "Palm Tree Island/Sprites/Background/Additional Water.png");
        Sky = Import("Sky", "Palm Tree Island/Sprites/Background/Additional Sky.png");
        Chain = Import("Chain", "Pirate Ship/Sprites/Decorations/Chains/Small/01.png");
        BuildMovingPlatform();
        BuildSpikes();
        BuildWater();
    }

    private static Sprite Import(string name, string source)
    {
        string path = Folder + "/" + name + ".png";
        if (!File.Exists(path))
            File.Copy(Source + source, path);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
        settings.spritePivot = new Vector2(0.5f, 0);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private GameObject Root(string name, Sprite sprite)
    {
        var root = new GameObject(name, typeof(SpriteRenderer));
        var renderer = root.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = Shared.Material;
        renderer.sortingOrder = 5;
        return root;
    }

    private void BuildMovingPlatform()
    {
        var root = Root("MovingPlatform", Shared["BridgeDeck"]);
        root.tag = "Ground";
        root.GetComponent<SpriteRenderer>().color = new Color(0.75f, 1f, 1f);
        var body = root.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var collider = root.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(3f, 0.27f);
        collider.offset = new Vector2(0, 0.03f);
        string materialPath = Folder + "/MovingPlatform.physicsMaterial2D";
        var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(materialPath);
        if (material == null)
        {
            material = new PhysicsMaterial2D("MovingPlatform");
            AssetDatabase.CreateAsset(material, materialPath);
        }
        material.friction = 0;
        material.bounciness = 0;
        material.frictionCombine = PhysicsMaterialCombine2D.Minimum;
        EditorUtility.SetDirty(material);
        collider.sharedMaterial = material;
        root.AddComponent<MovingPlatform2D>();
        Save(root);
    }

    private void BuildSpikes()
    {
        var root = Root("SpikeStrip", Spikes);
        var renderer = root.GetComponent<SpriteRenderer>();
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = new Vector2(1, 1);
        var collider = root.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(1f, 0.4f);
        collider.offset = new Vector2(0f, 0.22f);
        root.AddComponent<ContactDamage2D>();
        Save(root);
    }

    private void BuildWater()
    {
        var root = Root("HazardWater", Water);
        var renderer = root.GetComponent<SpriteRenderer>();
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = new Vector2(1, 0.6f);
        renderer.color = new Color(0.6f, 1f, 1f, 0.9f);
        var collider = root.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(1f, 0.5f);
        collider.offset = new Vector2(0f, 0.25f);
        root.AddComponent<ContactDamage2D>();
        Save(root);
    }

    private static void Save(GameObject root)
    {
        try
        {
            PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/" + root.name + ".prefab");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
