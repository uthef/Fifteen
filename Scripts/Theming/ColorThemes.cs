using Godot;

namespace Fifteen.Theming
{
    public class ColorThemes
    {
        public class Dark : IColorTheme
        {
            public Color BackgroundColor {get; set;} = Color.Color8(20, 20, 20);
            public Color ForegroundColor {get; set;} = new Color(1f, 1f, 1f);
            public Color PanelBackgroundColor {get; set;} = Color.Color8(30, 30, 30);
            public Color PanelStrokeColor {get; set;} = Color.Color8(40, 40, 40);
            public Color DisabledButtonColor {get; set;} = new Color(1f, 1f, 1f, .5f);
            public bool InvertIconColor {get; set;} = false;
            public FloatRange BlockColorBrightness {get; set;} = new FloatRange(.4f, .6f);
            public float MaxSaturation {get; set;} = .9f;

            public float BlockFontOutline {get; set;} = 2;

        }

        public class Light : IColorTheme
        {
            public Color BackgroundColor {get; set;} = Color.Color8(215, 215, 215);
            public Color ForegroundColor {get; set;} = new Color(0f, 0f, 0f);
            public Color PanelBackgroundColor {get; set;} = Color.Color8(225, 225, 225);
            public Color PanelStrokeColor {get; set;} = Color.Color8(215, 215, 215);
            public Color DisabledButtonColor {get; set;} = new Color(0f, 0f, 0f, .5f);
            public bool InvertIconColor {get; set;} = true;
            public FloatRange BlockColorBrightness {get; set;} = new FloatRange(.8f, 1f);
            public float MaxSaturation {get; set;} = .6f;
            public float BlockFontOutline {get; set;} = 0f;
        }

        public static IColorTheme GetTheme(PredefinedTheme theme, Object themePlugin = null)
        {
            switch (theme)
            {
                case PredefinedTheme.Light:
                    return new Light();
                case PredefinedTheme.Dark:
                    return new Dark();
                default:
                    if (themePlugin != null)
                    {
                        string nightMode = (string) themePlugin.Call("isNightModeEnabled");
                        if (nightMode == "false") return new Light();
                        else return new Dark();
                    }
                    else return new Dark();
            }
        }
    }
}