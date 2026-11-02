using System.Collections.Generic;
using UnityEngine;

public static class Level03Layout
{
    public static readonly RectInt[] Land = CreateLand();

    public static readonly RectInt[] Decks =
    {
        new RectInt(71, 2, 4, 1), new RectInt(77, 4, 3, 1),
        new RectInt(73, 6, 3, 1), new RectInt(80, 8, 3, 1),
        new RectInt(76, 10, 3, 1), new RectInt(82, 12, 3, 1),
        new RectInt(76, 14, 3, 1), new RectInt(77, 18, 3, 1),
        new RectInt(78, 22, 3, 1), new RectInt(77, 26, 3, 1),
        new RectInt(73, 28, 3, 1), new RectInt(68, 30, 3, 1),
        new RectInt(72, 32, 3, 1), new RectInt(20, 31, 3, 1),
        new RectInt(46, 30, 3, 1), new RectInt(64, 28, 3, 1),
        new RectInt(68, 35, 3, 1), new RectInt(62, 37, 3, 1),
        new RectInt(56, 39, 3, 1), new RectInt(50, 41, 3, 1),
        new RectInt(49, 43, 3, 1), new RectInt(18, 43, 3, 1),
        new RectInt(24, 45, 3, 1), new RectInt(30, 47, 3, 1),
        new RectInt(10, 57, 3, 1), new RectInt(16, 59, 3, 1),
        new RectInt(22, 61, 3, 1), new RectInt(28, 63, 3, 1),
        new RectInt(44, 57, 3, 1), new RectInt(50, 59, 3, 1),
        new RectInt(56, 60, 3, 1), new RectInt(62, 58, 3, 1),
        new RectInt(68, 60, 3, 1), new RectInt(74, 62, 3, 1)
    };

    public static readonly PlatformRoute[] Routes =
    {
        new PlatformRoute("LowerHallFerry", new Vector2(60, 2.2f), new Vector2(6, 0)),
        new PlatformRoute("CoastLift", new Vector2(-3, 24.2f), new Vector2(0, 5)),
        new PlatformRoute("WestLift", new Vector2(3, 30.2f), new Vector2(0, 10)),
        new PlatformRoute("CentralLift", new Vector2(26, 18.2f), new Vector2(0, 12)),
        new PlatformRoute("SecretLift", new Vector2(16, 42.2f), new Vector2(0, 12)),
        new PlatformRoute("GalleryLift", new Vector2(37, 46.2f), new Vector2(0, 9)),
        new PlatformRoute("BossWestLift", new Vector2(-3, 56.2f), new Vector2(0, 12)),
        new PlatformRoute("BossEastLift", new Vector2(95, 56.2f), new Vector2(0, 12))
    };

    public static readonly Vector2[] CollapsingDecks =
    {
        new Vector2(83.5f, 16.85f), new Vector2(85.5f, 20.85f),
        new Vector2(85.5f, 24.85f), new Vector2(37.5f, 33.85f)
    };

    private static RectInt[] CreateLand()
    {
        var land = new List<RectInt>
        {
            new RectInt(0, -4, 92, 4), new RectInt(46, 9, 24, 3),
            new RectInt(0, 27, 18, 3), new RectInt(9, 35, 8, 7),
            new RectInt(30, 16, 3, 16), new RectInt(33, 16, 9, 14),
            new RectInt(42, 16, 3, 16), new RectInt(52, 16, 5, 14),
            new RectInt(57, 16, 5, 10), new RectInt(76, 31, 16, 3),
            new RectInt(34, 43, 14, 3), new RectInt(0, 47, 13, 2),
            new RectInt(0, 53, 10, 3), new RectInt(40, 53, 52, 3),
            new RectInt(99, 61, 6, 2), new RectInt(0, 66, 90, 3),
            new RectInt(0, 80, 90, 2)
        };
        for (int x = 0; x < 45; x += 3)
            land.Add(new RectInt(x, 7, 3, 17 - x / 3));
        return land.ToArray();
    }

    public readonly struct PlatformRoute
    {
        public string Name { get; }
        public Vector2 Start { get; }
        public Vector2 Travel { get; }

        public PlatformRoute(string name, Vector2 start, Vector2 travel)
        {
            Name = name;
            Start = start;
            Travel = travel;
        }
    }
}
