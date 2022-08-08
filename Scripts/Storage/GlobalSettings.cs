using Fifteen.Theming;
using Fifteen.Nodes;
using Godot;

namespace Fifteen.Storage
{
    public static class GlobalSettings
    {
        public static PreferenceManager Preferences;
        public static IColorTheme CurrentTheme = 
            ColorThemes.GetTheme(PredefinedTheme.Dark);

        public static DynamicFont BlockFont;
    }
}