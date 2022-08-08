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

        public Godot.Object Plugin;

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

            
            if (OS.GetName() == "Android")
            {
                Plugin = Engine.GetSingleton("FifteenPlugin");
                Plugin.Connect("onConfigChanged", this, nameof(OnConfigurationChanged));
            }

            AudioServer.SetBusMute(0, Convert.ToBoolean(GlobalSettings.Preferences.GetInt32("mute_sounds", 0)));
            GlobalSettings.CurrentTheme = ColorThemes.GetTheme((PredefinedTheme) GlobalSettings.Preferences.GetInt32("theme", 0), Plugin);
            
            UpdateTheme();
        }

        private void OnConfigurationChanged(string nightMode)
        {
            if ((PredefinedTheme) GlobalSettings.Preferences.GetInt32("theme", 1) == PredefinedTheme.Auto && nightMode == "false")
            {
                GlobalSettings.CurrentTheme = new ColorThemes.Light();
                UpdateTheme();
            }
        }

        private void SelectorItemChanged(Selector node)
        {
            switch (node.PreferenceKey)
            {
                case "theme":
                    GlobalSettings.CurrentTheme = ColorThemes.GetTheme((PredefinedTheme) node.Position);
                    UpdateTheme();
                    break;
                case "mute_sounds":
                    if (node.Position == 0) AudioServer.SetBusMute(0, false);
                    else AudioServer.SetBusMute(0, true);
                    break;
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

            GlobalSettings.BlockFont = dynamicFont;
        }

        private void TweenAllCompleted()
        {
            _sceneToBeRemoved.QueueFree();
            _sceneToBeRemoved = null;
        }

        public void GoToScene(Node2D primaryScene, string secondaryScenePath, Vector2 movementDirection, string subScenePath = null, object[] data = null, MenuItem[] menuItems = null, bool limitedMode = false, string title = "")
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
                var instance = GD.Load<PackedScene>(subScenePath).Instance();
                if (instance is Container) 
                {
                    secondarySceneUI.AddChild(instance);
                    secondarySceneUI.MoveChild(instance, 0);
                }
                else secondaryScene.AddChild(instance);
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