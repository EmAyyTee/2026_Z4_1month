using UnityEngine;

public static class Level02Layout
{
    public static readonly RectInt[] Land =
    {
        new RectInt(0, -4, 70, 4), new RectInt(70, -4, 4, 13),
        new RectInt(0, 9, 50, 3), new RectInt(6, 17, 38, 2), new RectInt(44, 17, 2, 5),
        new RectInt(60, 17, 6, 3), new RectInt(64, 27, 48, 3),
        new RectInt(68, 38, 36, 2), new RectInt(26, 37, 6, 2), new RectInt(42, 37, 6, 2),
        new RectInt(53, 44, 10, 3), new RectInt(102, 52, 10, 3),
        new RectInt(88, -4, 4, 5), new RectInt(92, -4, 4, 7), new RectInt(96, -4, 4, 9),
        new RectInt(100, -4, 4, 11), new RectInt(104, -4, 8, 14)
    };

    public static readonly RectInt[] Decks =
    {
        new RectInt(47, 2, 3, 1), new RectInt(51, 4, 3, 1), new RectInt(53, 6, 3, 1),
        new RectInt(48, 10, 5, 1), new RectInt(21, 13, 4, 1), new RectInt(30, 13, 4, 1),
        new RectInt(6, 23, 3, 1), new RectInt(18, 23, 3, 1),
        new RectInt(24, 25, 3, 1), new RectInt(30, 24, 3, 1),
        new RectInt(42, 24, 3, 1), new RectInt(48, 23, 3, 1), new RectInt(54, 22, 3, 1),
        new RectInt(6, 32, 3, 1), new RectInt(13, 32, 3, 1), new RectInt(20, 32, 10, 1),
        new RectInt(42, 33, 8, 1), new RectInt(47, 40, 3, 1),
        new RectInt(66, 48, 3, 1), new RectInt(72, 49, 7, 1), new RectInt(83, 49, 2, 1), new RectInt(89, 49, 5, 1),
        new RectInt(77, 5, 3, 1), new RectInt(83, 7, 3, 1), new RectInt(89, 10, 3, 1),
        new RectInt(93, 12, 3, 1), new RectInt(94, 14, 3, 1), new RectInt(90, 16, 3, 1), new RectInt(87, 18, 3, 1),
        new RectInt(73, 32, 3, 1), new RectInt(77, 34, 3, 1), new RectInt(81, 35, 3, 1),
        new RectInt(105, 41, 3, 1), new RectInt(108, 43, 3, 1),
        new RectInt(113, 50, 3, 1), new RectInt(115, 46, 3, 1), new RectInt(113, 42, 3, 1),
        new RectInt(115, 38, 3, 1), new RectInt(113, 34, 3, 1), new RectInt(115, 30, 3, 1),
        new RectInt(113, 26, 3, 1), new RectInt(115, 22, 3, 1), new RectInt(113, 18, 3, 1), new RectInt(111, 14, 3, 1)
    };

    public readonly struct PlatformRoute
    {
        public readonly string Name;
        public readonly Vector2 Start;
        public readonly Vector2 Travel;

        public PlatformRoute(string name, float x, float y, float travelX, float travelY)
        {
            Name = name;
            Start = new Vector2(x, y);
            Travel = new Vector2(travelX, travelY);
        }
    }

    public static readonly PlatformRoute[] Routes =
    {
        new PlatformRoute("LowerCrossing", 56, 8.5f, 10, 0),
        new PlatformRoute("LowerLift", 60, 1, 0, 7),
        new PlatformRoute("WestLift", 3, 12.2f, 0, 10),
        new PlatformRoute("WestUpperLift", 3, 24.5f, 0, 8),
        new PlatformRoute("CentreLift", 35, 33, 0, 6),
        new PlatformRoute("AlcoveLift", 50.5f, 41.2f, 0, 6),
        new PlatformRoute("EastFerry", 73, 21, 12, 0),
        new PlatformRoute("MiddleEastLift", 62, 22, 0, 7),
        new PlatformRoute("UpperEastLift", 108, 30.2f, 0, 9),
        new PlatformRoute("InnerLift", 65, 40.2f, 0, 6),
        new PlatformRoute("SummitLift", 99, 50, 0, 4)
    };
}
