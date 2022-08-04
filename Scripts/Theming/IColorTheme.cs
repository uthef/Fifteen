using Godot;

namespace Fifteen.Theming
{
    public interface IColorTheme
    {
        Color BackgroundColor {get; set;}
        Color ForegroundColor {get; set;}
        Color PanelBackgroundColor {get; set;}
        Color PanelStrokeColor {get; set;}
        Color DisabledButtonColor {get; set;}
        bool InvertIconColor {get; set;}
        float BlockFontOutline {get; set;}

        FloatRange BlockColorBrightness {get; set;}
        float MaxSaturation {get; set;}
    }
    public struct FloatRange
    {
        public float Min, Max;

        public FloatRange(float float1, float float2)
        {
            Min = float1;
            Max = float2;
        }
    }
}