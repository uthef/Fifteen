using Godot;

public static class ColorThemes {
    public static IColorTheme AppTheme;

    public enum Values
    {
        Dark, Light
    }

    public static IColorTheme GetTheme(ColorThemes.Values theme)
    {
        switch (theme)
        {
            case Values.Light:
                return new Light();
            default:
                return new Dark();
        }
    }

    private class Light : IColorTheme
    {
        public ColorThemes.Values Value {get;} = ColorThemes.Values.Light;
        public string AssetPostfix {get;} = "light";
        public Color BackgroundColor {get;} = Color.FromHsv(0, 0, .9f);
        public Color ForegroundColor {get;} = new Color(0, 0, 0);
        public Color PanelColor {get;} = new Color(.87f, .87f, .87f);
        public float BlockBrightness {get;} = .9f; 
        public Color BlockNumberOutlineColor {get;} = new Color(1, 1, 1);
        public int BlockNumberOutlineWidth {get;} = 0;
        public float MaxBlockNumberSaturation {get;} = .7f;

    }

   private class Dark : IColorTheme
    {
        public ColorThemes.Values Value {get;} = ColorThemes.Values.Dark;
        public string AssetPostfix {get;} = "dark";
        public Color BackgroundColor {get;} = Color.FromHsv(0, 0, .1f);
        public Color ForegroundColor {get;} = new Color(1, 1, 1);
        public Color PanelColor {get;} = new Color(.12f, .12f, .12f);
        public float BlockBrightness {get;} = .4f;
        public Color BlockNumberOutlineColor {get;} = new Color(0, 0, 0);
        public int BlockNumberOutlineWidth {get;} = 3;
        public float MaxBlockNumberSaturation {get;} = 1f;
    }
}