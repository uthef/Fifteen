using Godot;
public interface IColorTheme
{
    public ColorThemes.Values Value {get;}
    public string AssetPostfix {get;}
    public Color BackgroundColor {get;}
    public Color ForegroundColor {get;} 
    public Color PanelColor {get;}
    public float BlockBrightness {get;}
    public Color BlockNumberOutlineColor {get;}
    public int BlockNumberOutlineWidth {get;}
    public float MaxBlockNumberSaturation {get;}
}