using Godot;
using System;


namespace Fifteen.Scripts {
    public class TextureButtonTheme : TextureButton
    {
        [Export] string _assetName;
        public override void _EnterTree()
        {
            TextureNormal = GD.Load<Texture>($"res://sprites/ui/{_assetName}_{ColorThemes.AppTheme.AssetPostfix}/normal.png");
            TexturePressed = GD.Load<Texture>($"res://sprites/ui/{_assetName}_{ColorThemes.AppTheme.AssetPostfix}/pressed.png");
            TextureDisabled = GD.Load<Texture>($"res://sprites/ui/{_assetName}_{ColorThemes.AppTheme.AssetPostfix}/disabled.png");
        }
    }
}
