using Godot;
using System;
using Fifteen.Models;
using Fifteen.Storage;
using Fifteen.Theming;

namespace Fifteen.Nodes
{
    public class SceneManager : Node
    {
        Tween _tween;
        float _duration = .3f;
        Tween.EaseType _easeType = Tween.EaseType.Out;
        Tween.TransitionType _transType = Tween.TransitionType.Cubic;

        [Export] Image[] _pictures;
        [Export] public Texture[] Confetti;
        public Texture[] Textures;

        public bool IsTweenActive 
        {
            get => _tween.IsActive();
        }

        Godot.Node2D _sceneToBeRemoved;
        public override void _EnterTree()
        {
            _tween = GetNode<Tween>("Tween");
            _tween.Connect("tween_all_completed", this, nameof(TweenAllCompleted));

            if (GlobalSettings.Preferences == null)
            {
                GlobalSettings.Preferences = new PreferenceManager();

                // Preloading resources
                GD.Load<PackedScene>("res://Scenes/FifteenPuzzle.tscn");
                GD.Load<PackedScene>("res://Scenes/MenuScene.tscn").Instance();
                Textures = new ImageTexture[_pictures.Length];
                for (int i = 0; i < _pictures.Length; i++)
                {
                    ImageTexture imageTexture = new ImageTexture();
                    imageTexture.CreateFromImage(_pictures[i], 4);
                    float a = GetViewport().Size.x - imageTexture.GetSize().x, 
                        b = a / imageTexture.GetSize().x * 100, 
                        c = imageTexture.GetSize().y / 100 * b;
                    imageTexture.SetSizeOverride(new Vector2(GetViewport().Size.x, imageTexture.GetSize().y + c));
                    Textures[i] = imageTexture;
                }
            }
        }


        public void UpdateTheme()
        {
            Theme theme = GD.Load<Theme>("res://Themes/Default.tres");
            DynamicFont dynamicFont = GD.Load<DynamicFont>("res://Themes/DynamicFonts/BlockFont.tres");
            ShaderMaterial material = GD.Load<ShaderMaterial>("res://Materials/IconColorInversion.tres");
            StyleBoxFlat panelStyleBox = theme.Get("PanelContainer/styles/panel") as StyleBoxFlat,
                menuButtonStyleBox = GD.Load<StyleBoxFlat>("res://Themes/MenuButton.tres");

            VisualServer.SetDefaultClearColor(GlobalSettings.CurrentTheme.BackgroundColor);
            dynamicFont.Set("outline_size", GlobalSettings.CurrentTheme.BlockFontOutline);
            theme.Set("Label/colors/font_color", GlobalSettings.CurrentTheme.ForegroundColor);
            theme.Set("Button/colors/font_color", GlobalSettings.CurrentTheme.ForegroundColor);
            theme.Set("Button/colors/font_color_hover", GlobalSettings.CurrentTheme.ForegroundColor);
            theme.Set("Button/colors/font_color_pressed", GlobalSettings.CurrentTheme.ForegroundColor);
            theme.Set("Button/colors/font_color_disabled", GlobalSettings.CurrentTheme.DisabledButtonColor);
            material.Set("shader_param/enabled", GlobalSettings.CurrentTheme.InvertIconColor);
            panelStyleBox.BgColor = GlobalSettings.CurrentTheme.PanelBackgroundColor;
            panelStyleBox.BorderColor = GlobalSettings.CurrentTheme.PanelStrokeColor;
            menuButtonStyleBox.Set("bg_color", GlobalSettings.CurrentTheme.ForegroundColor);
            menuButtonStyleBox.Set("border_color", GlobalSettings.CurrentTheme.PanelStrokeColor);
        }

        private void TweenAllCompleted()
        {
            _sceneToBeRemoved.QueueFree();
            _sceneToBeRemoved = null;
        }

        public void GoToScene(Node2D primaryScene, string secondaryScenePath, Vector2 movementDirection, string subScenePath = null, object[] data = null, MenuItem[] menuItems = null, bool limitedMode = false, string title = "Untitled")
        {
            var movement = movementDirection * GetViewport().Size;

            var primarySceneUI = primaryScene.GetNode<CanvasLayer>("CanvasLayer");
            var secondaryScene = GD.Load<PackedScene>(secondaryScenePath).Instance<Node2D>();
            var secondarySceneUI = secondaryScene.GetNode<CanvasLayer>("CanvasLayer");

            if (secondaryScene is IDataScene)
            {
                var dataScene = secondaryScene as IDataScene;
                dataScene.MenuItems = menuItems != null ? menuItems : new MenuItem[0];
                dataScene.Data = data != null ? data : new object[0];
                dataScene.Direction = -movementDirection;
                dataScene.LimitedMode = limitedMode;
                dataScene.Title = title;
            }

            if (subScenePath != null)
            {
                secondaryScene.AddChild(GD.Load<PackedScene>(subScenePath).Instance());
            }

            secondarySceneUI.Offset -= movement;
            secondaryScene.Position = secondarySceneUI.Offset;
            _sceneToBeRemoved = primaryScene;

            GetTree().Root.AddChild(secondaryScene);

            _tween.InterpolateProperty(primaryScene, "position", primaryScene.Position, primaryScene.Position + movement, _duration, _transType, _easeType);
            _tween.InterpolateProperty(primarySceneUI, "offset", primarySceneUI.Offset, primarySceneUI.Offset + movement, _duration, _transType, _easeType);
            _tween.InterpolateProperty(secondaryScene, "position", secondaryScene.Position, Vector2.Zero, _duration, _transType, _easeType);
            _tween.InterpolateProperty(secondarySceneUI, "offset", secondarySceneUI.Offset, Vector2.Zero, _duration, _transType, _easeType);
            _tween.Start();
        }
    }
} 