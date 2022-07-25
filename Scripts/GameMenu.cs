using Godot;
using System;
using Fifteen.Scripts.Storage;
using System.Threading.Tasks;

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
                
                if (!GlobalSettings.StoredPreferences.RootSection.GetBool("dark_theme", true)) {
                    var theme = GD.Load<Theme>("res://themes/dark.tres");
                    ColorThemes.AppTheme = new ColorThemes.Light();
                    theme.Set("Label/colors/font_color", ColorThemes.AppTheme.ForegroundColor);
                    StyleBoxFlat styleBox = (StyleBoxFlat) theme.Get("PanelContainer/styles/panel");
                    styleBox.BgColor = ColorThemes.AppTheme.PanelColor;
                    VisualServer.SetDefaultClearColor(ColorThemes.AppTheme.BackgroundColor);
                }
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
    }
}
