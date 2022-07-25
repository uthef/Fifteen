using Godot;
using System;
using Fifteen.Scripts.Storage;

namespace Fifteen.Scripts 
{
    public class GameMenu : Node
    {
        Sprite _slide;
        public override void _EnterTree()
        {
            _slide = GetNode<Sprite>("Slide");

            if (GlobalSettings.FrameBuffer is not null) 
            {
                var animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
                animationPlayer.CurrentAnimation = "FadeIn";
                animationPlayer.Play();
                var imageTexture = new ImageTexture();
                imageTexture.CreateFromImage(GlobalSettings.FrameBuffer);
                _slide.Texture = imageTexture;
            }

            if (GlobalSettings.StoredPreferences is null) {
                GlobalSettings.StoredPreferences = new Preferences(out Error error);
                SwitchTheme(GlobalSettings.StoredPreferences.RootSection.GetTheme("theme", ColorThemes.Values.Dark));
            }
        }

        private void FifteenButtonPressed() 
        {
            GlobalSettings.FrameBuffer = GetViewport().GetTexture().GetData();
            var instance = GD.Load<PackedScene>("res://scenes/Main Scene.tscn").Instance<MainScene>();
            GetTree().Root.AddChild(instance);
            this.QueueFree();
        }

        private void SprucesButtonPressed()
        {
            GlobalSettings.FrameBuffer = GetViewport().GetTexture().GetData();
            GetTree().Root.AddChild(GD.Load<PackedScene>("res://scenes/Spruces Scene.tscn").Instance<Node>());
            this.QueueFree();
        }

        private void AnimationFinished(string name) 
        {
            _slide.QueueFree();
            GlobalSettings.FrameBuffer.Dispose();
        }

        private void SwitchTheme(ColorThemes.Values colorTheme) 
        {
            var theme = GD.Load<Theme>("res://themes/dark.tres");
            ColorThemes.AppTheme = ColorThemes.GetTheme(colorTheme);
            theme.Set("Label/colors/font_color", ColorThemes.AppTheme.ForegroundColor);
            StyleBoxFlat styleBox = (StyleBoxFlat) theme.Get("PanelContainer/styles/panel");
            styleBox.BgColor = ColorThemes.AppTheme.PanelColor;
            VisualServer.SetDefaultClearColor(ColorThemes.AppTheme.BackgroundColor);
            GlobalSettings.StoredPreferences.RootSection.SetTheme("theme", ColorThemes.AppTheme.Value);
            GlobalSettings.StoredPreferences.Save();
        }

        private void OptionsButtonPressed()
        {
            if (ColorThemes.AppTheme.Value is ColorThemes.Values.Dark) SwitchTheme(ColorThemes.Values.Light);    
            else SwitchTheme(ColorThemes.Values.Dark);   
        }
    }
}
