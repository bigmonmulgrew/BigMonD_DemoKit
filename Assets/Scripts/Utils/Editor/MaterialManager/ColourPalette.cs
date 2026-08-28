using UnityEngine;

public readonly struct ColourPalette
{
    public readonly ColourIds Id;
    public readonly Color Colour;

    ColourPalette(ColourIds id, Color color) { Id = id;            Colour = color; }
    ColourPalette(int id, Color color)       { Id = (ColourIds)id; Colour = color; }
    static ColourPalette()
    {
        int enumCount = System.Enum.GetValues(typeof(ColourIds)).Length;

        if (ByIndex.Length != enumCount)
        {
            Debug.LogError("ColourPalette mismatch: Enum has {enumCount} values, but ByIndex has {ByIndex.Length} entries.");
        }

        for (int i = 0; i < ByIndex.Length; i++)
        {
            if ((int)ByIndex[i].Id != i)
            {
                Debug.LogError($"ColourPalette ID mismatch at index {i}: expected {(ColourIds)i}, got {ByIndex[i].Id}");
            }
        }
    }

    public static readonly ColourPalette[] ByIndex =
    {
        new (ColourIds.White,       Color.white),
        new (ColourIds.Black,       Color.black),
        new (ColourIds.Grey,        Color.grey),
        new (ColourIds.Red,         Color.red),
        new (ColourIds.Green,       Color.green),
        new (ColourIds.Blue,        Color.blue),
        new (ColourIds.Cyan,        Color.cyan),
        new (ColourIds.Magenta,     Color.magenta),
        new (ColourIds.Yellow,      Color.yellow),
        new (ColourIds.Gold,        new Color(1.0f, 0.843f, 0.0f)),
        new (ColourIds.Silver,      new Color(0.753f, 0.753f, 0.753f)),
        new (ColourIds.Bronze,      new Color(0.804f, 0.498f, 0.196f)),
        new (ColourIds.Orange,      new Color(1.0f, 0.647f, 0.0f)),
        new (ColourIds.Purple,      new Color(0.502f, 0.0f, 0.502f)),
        new (ColourIds.Teal,        new Color(0.0f, 0.502f, 0.502f)),
        new (ColourIds.Pink,        new Color(1.0f, 0.753f, 0.796f)),
        new (ColourIds.Brown,       new Color(0.647f, 0.165f, 0.165f)),
        new (ColourIds.LightRed,    new Color(1.0f, 0.5f, 0.5f)),
        new (ColourIds.LightGreen,  new Color(0.5f, 1.0f, 0.5f)),
        new (ColourIds.LightBlue,   new Color(0.5f, 0.5f, 1.0f)),
        new (ColourIds.LightCyan,   new Color(0.5f, 1.0f, 1.0f)),
        new (ColourIds.LightMagenta,new Color(1.0f, 0.5f, 1.0f)),
        new (ColourIds.LightYellow, new Color(1.0f, 1.0f, 0.5f)),
        new (ColourIds.DarkRed,     new Color(0.5f, 0.0f, 0.0f)),
        new (ColourIds.DarkGreen,   new Color(0.0f, 0.5f, 0.0f)),
        new (ColourIds.DarkBlue,    new Color(0.0f, 0.0f, 0.5f)),
        new (ColourIds.DarkCyan,    new Color(0.0f, 0.5f, 0.5f)),
        new (ColourIds.DarkMagenta, new Color(0.5f, 0.0f, 0.5f)),
        new (ColourIds.DarkYellow,  new Color(0.5f, 0.5f, 0.0f)),
        new (ColourIds.LightOrange, new Color(1.0f, 0.8f, 0.6f)),
        new (ColourIds.LightPurple, new Color(0.8f, 0.6f, 0.8f)),
        new (ColourIds.LightTeal,   new Color(0.6f, 0.8f, 0.8f)),
        new (ColourIds.LightPink,   new Color(1.0f, 0.8f, 0.9f)),
        new (ColourIds.LightBrown,  new Color(0.8f, 0.6f, 0.4f)),
        new (ColourIds.DarkOrange,  new Color(0.8f, 0.4f, 0.0f)),
        new (ColourIds.DarkPurple,  new Color(0.4f, 0.0f, 0.4f)),
        new (ColourIds.DarkTeal,    new Color(0.0f, 0.4f, 0.4f)),
        new (ColourIds.DarkPink,    new Color(0.8f, 0.4f, 0.5f)),
        new (ColourIds.DarkBrown,   new Color(0.4f, 0.2f, 0.2f))

    };

    public enum ColourIds
    {
        White,
        Black,
        Grey,
        Red,
        Green,
        Blue,
        Cyan,
        Magenta,
        Yellow,
        Gold,
        Silver,
        Bronze,
        Orange,
        Purple,
        Teal,
        Pink,
        Brown,
        LightRed,
        LightGreen,
        LightBlue,
        LightCyan,
        LightMagenta,
        LightYellow,
        DarkRed,
        DarkGreen,
        DarkBlue,
        DarkCyan,
        DarkMagenta,
        DarkYellow,
        LightOrange,
        LightPurple,
        LightTeal,
        LightPink,
        LightBrown,
        DarkOrange,
        DarkPurple,
        DarkTeal,
        DarkPink,
        DarkBrown
    }

    public static ColourPalette Get(int i)       => ByIndex[i];
    public static ColourPalette Get(ColourIds c) => ByIndex[(int)c];
    public static Color GetColour(int i)         => ByIndex[i].Colour;
    public static Color GetColour(ColourIds c)   => ByIndex[(int)c].Colour;

    public static Color White => GetColour(ColourIds.White);
    public static Color Black => GetColour(ColourIds.Black);
    public static Color Grey  => GetColour(ColourIds.Grey);
    public static Color Red   => GetColour(ColourIds.Red);
    public static Color Green => GetColour(ColourIds.Green);
    public static Color Blue  => GetColour(ColourIds.Blue);
    public static Color Cyan  => GetColour(ColourIds.Cyan);
    public static Color Magenta => GetColour(ColourIds.Magenta);
    public static Color Yellow => GetColour(ColourIds.Yellow);
    public static Color Gold  => GetColour(ColourIds.Gold);
    public static Color Silver => GetColour(ColourIds.Silver);
    public static Color Bronze => GetColour(ColourIds.Bronze);
    public static Color Orange => GetColour(ColourIds.Orange);
    public static Color Purple => GetColour(ColourIds.Purple);
    public static Color Teal => GetColour(ColourIds.Teal);
    public static Color Pink => GetColour(ColourIds.Pink);
    public static Color Brown => GetColour(ColourIds.Brown);
    public static Color LightRed   => GetColour(ColourIds.LightRed);
    public static Color LightGreen => GetColour(ColourIds.LightGreen);
    public static Color LightBlue  => GetColour(ColourIds.LightBlue);
    public static Color LightCyan  => GetColour(ColourIds.LightCyan);
    public static Color LightMagenta => GetColour(ColourIds.LightMagenta);
    public static Color LightYellow => GetColour(ColourIds.LightYellow);
    public static Color DarkRed   => GetColour(ColourIds.DarkRed);
    public static Color DarkGreen => GetColour(ColourIds.DarkGreen);
    public static Color DarkBlue  => GetColour(ColourIds.DarkBlue);
    public static Color DarkCyan  => GetColour(ColourIds.DarkCyan);
    public static Color DarkMagenta => GetColour(ColourIds.DarkMagenta);
    public static Color DarkYellow => GetColour(ColourIds.DarkYellow);
    public static Color LightOrange => GetColour(ColourIds.LightOrange);
    public static Color LightPurple => GetColour(ColourIds.LightPurple);
    public static Color LightTeal   => GetColour(ColourIds.LightTeal);
    public static Color LightPink   => GetColour(ColourIds.LightPink);
    public static Color LightBrown  => GetColour(ColourIds.LightBrown);
    public static Color DarkOrange  => GetColour(ColourIds.DarkOrange);
    public static Color DarkPurple  => GetColour(ColourIds.DarkPurple);
    public static Color DarkTeal    => GetColour(ColourIds.DarkTeal);
    public static Color DarkPink    => GetColour(ColourIds.DarkPink);
    public static Color DarkBrown   => GetColour(ColourIds.DarkBrown);

}