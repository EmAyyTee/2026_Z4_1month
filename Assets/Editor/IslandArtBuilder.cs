using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class IslandArtBuilder
{
    private const string Source = "Assets/Treasure Hunters/Treasure Hunters/";
    private const string ArtFolder = "Assets/Maps/IslandArt";
    private const string TileFolder = "Assets/Maps/Tiles/Island";
    private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    public RuleTile Ground { get; private set; }
    public RuleTile Platform { get; private set; }
    public Material Material { get; private set; }
    public Sprite this[string name] => sprites[name];

    public void LoadExisting()
    {
        Ground = AssetDatabase.LoadAssetAtPath<RuleTile>(TileFolder + "/IslandGround.asset");
        Platform = AssetDatabase.LoadAssetAtPath<RuleTile>(TileFolder + "/WoodPlatform.asset");
        Material = AssetDatabase.LoadAssetAtPath<Material>(ArtFolder + "/IslandUnlit.mat");
        if (Ground == null || Platform == null || Material == null)
            throw new InvalidOperationException("Build the shared island art before creating another level.");
        foreach (string path in Directory.GetFiles(ArtFolder, "*.png"))
            foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(path.Replace('\\', '/')).OfType<Sprite>())
                sprites[sprite.name] = sprite;
    }

    public void Build()
    {
        MapBootstrapper.EnsureFolder(ArtFolder);
        MapBootstrapper.EnsureFolder(TileFolder);
        var terrain = new List<SpriteRect>();
        for (int row = 0; row < 3; row++)
            for (int column = 0; column < 3; column++)
                terrain.Add(Slice($"Ground{column}{row}", column * 32, 128 - row * 32, 32, 32));
        terrain.Add(Slice("RockDetail", 208, 112, 32, 32));
        ImportSheet("Palm Tree Island/Sprites/Terrain/Terrain (32x32).png", "Terrain", terrain);
        var planks = new List<SpriteRect>();
        for (int i = 0; i < 4; i++)
            planks.Add(Slice("Plank" + i, 32 + i * 32, 32, 32, 32));
        planks.Add(Slice("BridgeDeck", 32, 52, 96, 12));
        ImportSheet("Pirate Ship/Sprites/Tilesets/Platforms (32x32).png", "Wood", planks, true);
        ImportSheet("Pirate Ship/Sprites/Tilesets/Terrain and Back Wall (32x32).png", "Support", new[] { Slice("Timber", 160, 288, 32, 32) });
        ImportSingle("Palm Tree Island/Sprites/Background/BG Image.png", "Horizon");
        ImportSingle("Palm Tree Island/Sprites/Background/Big Clouds.png", "CloudBank");
        ImportSingle("Palm Tree Island/Sprites/Background/Small Cloud 1.png", "Cloud1");
        ImportSingle("Palm Tree Island/Sprites/Background/Small Cloud 2.png", "Cloud2");
        ImportSingle("Palm Tree Island/Sprites/Background/Water Reflect Big 01.png", "Reflection");
        ImportSingle("Palm Tree Island/Sprites/Back Palm Trees/Back Palm Tree Regular 01.png", "Palm");
        ImportSingle("Palm Tree Island/Sprites/Back Palm Trees/Back Palm Tree Left 01.png", "PalmLeft");
        ImportSingle("Palm Tree Island/Sprites/Back Palm Trees/Back Palm Tree Right 01.png", "PalmRight");
        ImportSingle("Palm Tree Island/Sprites/Objects/Flag/Flag 01.png", "Flag");
        ImportSingle("Palm Tree Island/Sprites/Objects/Chest/Chest Close 01.png", "Chest");
        ImportSingle("Merchant Ship/Sprites/Barrel/Idle/1.png", "Barrel", true);
        ImportSingle("Shooter Traps/Sprites/Totems/Head 1/Idle 1/1.png", "Totem");
        ImportSingle("Shooter Traps/Sprites/Cannon/Cannon Ball Idle/1.png", "Projectile", true);

        Ground = Rule("IslandGround", this["Ground11"], Tile.ColliderType.Grid);
        for (int row = 0; row < 3; row++)
            for (int column = 0; column < 3; column++)
                AddRule(Ground, this[$"Ground{column}{row}"], new[] { Vector3Int.left, Vector3Int.right, Vector3Int.up, Vector3Int.down },
                    new[] { column == 0 ? 2 : 1, column == 2 ? 2 : 1, row == 0 ? 2 : 1, row == 2 ? 2 : 1 });

        Platform = Rule("WoodPlatform", this["Plank1"], Tile.ColliderType.Sprite);
        AddRule(Platform, this["Plank3"], new[] { Vector3Int.left, Vector3Int.right }, new[] { 2, 2 });
        AddRule(Platform, this["Plank0"], new[] { Vector3Int.left, Vector3Int.right }, new[] { 2, 1 });
        AddRule(Platform, this["Plank2"], new[] { Vector3Int.left, Vector3Int.right }, new[] { 1, 2 });
        AddRule(Platform, this["Plank1"], new[] { Vector3Int.left, Vector3Int.right }, new[] { 1, 1 });
        Ground.UpdateNeighborPositions();
        Platform.UpdateNeighborPositions();
        EditorUtility.SetDirty(Ground);
        EditorUtility.SetDirty(Platform);
        string materialPath = ArtFolder + "/IslandUnlit.mat";
        Material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (Material == null)
        {
            Material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            AssetDatabase.CreateAsset(Material, materialPath);
        }
        BuildPalette();
    }

    private void ImportSingle(string source, string name, bool centered = false)
    {
        string path = Copy(source, name);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var rect = Slice(name, 0, 0, texture.width, texture.height);
        rect.pivot = centered ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0f);
        Configure(path, new[] { rect }, false);
    }

    private void ImportSheet(string source, string name, IEnumerable<SpriteRect> rects, bool platform = false)
    {
        Configure(Copy(source, name), rects.ToArray(), platform);
    }

    private static string Copy(string source, string name)
    {
        string path = ArtFolder + "/" + name + ".png";
        if (!File.Exists(path))
        {
            File.Copy(Source + source, path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }
        return path;
    }

    private void Configure(string path, SpriteRect[] rects, bool platform)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 32f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var previous = provider.GetSpriteRects().ToDictionary(rect => rect.name, rect => rect.spriteID);
        foreach (var rect in rects)
            rect.spriteID = previous.TryGetValue(rect.name, out var id) ? id : GUID.Generate();
        provider.SetSpriteRects(rects);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
        if (platform)
        {
            var physics = provider.GetDataProvider<ISpritePhysicsOutlineDataProvider>();
            foreach (var rect in rects.Where(rect => rect.name.StartsWith("Plank")))
                physics.SetOutlines(rect.spriteID, new List<Vector2[]> { new[] { new Vector2(-16, 6), new Vector2(-16, 15), new Vector2(16, 15), new Vector2(16, 6) } });
        }
        provider.Apply();
        importer.SaveAndReimport();
        foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
            sprites[sprite.name] = sprite;
    }

    private static SpriteRect Slice(string name, int x, int y, int width, int height)
    {
        return new SpriteRect { name = name, rect = new Rect(x, y, width, height), alignment = SpriteAlignment.Custom, pivot = new Vector2(0.5f, 0.5f) };
    }

    private static RuleTile Rule(string name, Sprite sprite, Tile.ColliderType collider)
    {
        string path = TileFolder + "/" + name + ".asset";
        var tile = AssetDatabase.LoadAssetAtPath<RuleTile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<RuleTile>();
            AssetDatabase.CreateAsset(tile, path);
        }
        tile.m_DefaultSprite = sprite;
        tile.m_DefaultColliderType = collider;
        tile.m_TilingRules.Clear();
        return tile;
    }

    private static void AddRule(RuleTile tile, Sprite sprite, Vector3Int[] offsets, int[] neighbors)
    {
        tile.m_TilingRules.Add(new RuleTile.TilingRule { m_Sprites = new[] { sprite }, m_NeighborPositions = offsets.ToList(), m_Neighbors = neighbors.ToList(), m_ColliderType = tile.m_DefaultColliderType });
    }

    private void BuildPalette()
    {
        const string folder = "Assets/Maps/Palettes";
        const string path = folder + "/IslandTerrainPalette.prefab";
        MapBootstrapper.EnsureFolder(folder);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            GridPaletteUtility.CreateNewPalette(folder, "IslandTerrainPalette", GridLayout.CellLayout.Rectangle, GridPalette.CellSizing.Manual, Vector3.one, GridLayout.CellSwizzle.XYZ);
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var map = root.GetComponentInChildren<Tilemap>();
            map.ClearAllTiles();
            for (int x = 0; x < 5; x++)
                for (int y = 0; y < 3; y++)
                    map.SetTile(new Vector3Int(x, y), Ground);
            for (int x = 0; x < 5; x++)
                map.SetTile(new Vector3Int(x + 7, 2), Platform);
            map.GetComponent<TilemapRenderer>().sharedMaterial = Material;
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
