using Godot;
using Fifteen.Models;
using Fifteen.Storage;
using Array = Godot.Collections.Array;

namespace Fifteen.Nodes
{
    public class MainScene : Godot.Node2D, IDataScene
    {
        public object[] Data {get; set;}
        [Export] public float MenuItemHeight = 100f;
        public MenuItem[] MenuItems {get; set; }
        private bool _menuState = false, _menuButtonSelected = false;
        public Vector2 Direction {get; set; }
        public bool LimitedMode {get; set; } = true;
        public string Title {get; set;}

        // Nodes
        private Tween _tween;
        private PanelContainer _menu;
        private TextureButton _optionsButton;
        private AudioStreamPlayer _fxPlayer;
        private Label _counter, _selectionBarValue, _timeLabel;

        public delegate void MenuItemSelected(MenuButton button);
        public delegate void IconUp(TextureButton moveLeftButton, TextureButton moveRightButton);

        public TextureButton MoveLeftButton, MoveRightButton;
        public event IconUp OnMoveLeftButtonUp, OnMoveRgihtButtonUp, OnOptionsButtonUp;
        public event MenuItemSelected OnMenuItemSelected;
        public delegate void UIStateChange(bool blocked);
        public event UIStateChange OnUIStateChange;
        float menuAnimationDuration = .25f;
        Tween.TransitionType menuAnimationType = Tween.TransitionType.Cubic;
        Tween.EaseType menuAnimationEaseType = Tween.EaseType.Out;
        // Resources
        private AudioStream _clickSample, _tapSample;

        public override void _EnterTree()
        {
            // Loading resources
            _clickSample = GD.Load<AudioStream>("res://Audio/Click.wav");
            _tapSample = GD.Load<AudioStream>("res://Audio/Tap.wav");

            // Getting nodes
            _tween = GetNode<Tween>("Tween");
            _menu = GetNode<PanelContainer>("CanvasLayer/Menu");
            _optionsButton = GetNode<TextureButton>("CanvasLayer/OptionsButton");
            _fxPlayer = GetNode<AudioStreamPlayer>("FxPlayer");
            _counter = GetNode<Label>("CanvasLayer/VBoxContainer/Counter");
            _timeLabel = GetNode<Label>("CanvasLayer/VBoxContainer/Time");
            _selectionBarValue = GetNode<Label>("CanvasLayer/SelectionBarValue");
            MoveLeftButton = GetNode<TextureButton>("CanvasLayer/MoveLeftButton");
            MoveRightButton = GetNode<TextureButton>("CanvasLayer/MoveRightButton");

            OnUIStateChange += _UIStateChanged;
        }

        public override void _Ready()
        {
            SetProcessInput(false);

            if (LimitedMode) 
            {
                foreach (Node node in GetTree().GetNodesInGroup("Controls"))
                    node.QueueFree();

                _timeLabel.Text = Title;
            }
            else InitializeMenu(_menu);
        }

        private void InitializeMenu(PanelContainer menu)
        {
            PackedScene scene = GD.Load<PackedScene>("res://Scenes/MenuButton.tscn");
            VBoxContainer container = menu.GetNode<VBoxContainer>("VBoxContainer");
            menu.RectSize = new Vector2(menu.RectSize.x, MenuItemHeight * MenuItems.Length);

            foreach (var item in MenuItems)
            {
                MenuButton button = scene.Instance<MenuButton>();
                button.Text = item.Text;
                button.SwapText = item.SwapText;
                button.ItemType = item.Type;
                button.Connect("button_down", this, nameof(MenuButtonClicked), new Array() {button, true} );
                button.Connect("button_up", this, nameof(MenuButtonRelease), new Array() {button});
                button.Connect("pressed", this, nameof(MenuButtonClicked), new Array() {button, false} );
                button.AddToGroup("MenuItems");

                container.AddChild(button);
            }
        }
        
        private void HideMenu()
        {  
            OnUIStateChange?.Invoke(false);
            _menuState = false;              
            _tween.InterpolateProperty(_menu, "modulate", _menu.Modulate, new Color(_menu.Modulate, 0f), menuAnimationDuration, menuAnimationType, menuAnimationEaseType);
            _tween.InterpolateProperty(_menu, "rect_position", _menu.RectPosition, new Vector2(_menu.RectPosition.x, 120), menuAnimationDuration, menuAnimationType, menuAnimationEaseType);
            _tween.Start();
        }

        private void ShowMenu()
        {
            OnUIStateChange?.Invoke(true);
            _menuState = true;
            _menu.Visible = true;
            _tween.InterpolateProperty(_menu, "modulate", _menu.Modulate, new Color(_menu.Modulate, 1f), menuAnimationDuration, menuAnimationType, menuAnimationEaseType);
            _tween.InterpolateProperty(_menu, "rect_position", _menu.RectPosition, new Vector2(_menu.RectPosition.x, 120 + 30), menuAnimationDuration, menuAnimationType, menuAnimationEaseType);
            _tween.Start();
        }
        
        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouse && !mouse.IsPressed() && mouse.ButtonIndex == 1 && !_menu.GetGlobalRect().HasPoint(mouse.Position) && !_optionsButton.GetGlobalRect().HasPoint(mouse.Position))
            {
                HideMenu();
                _optionsButton.Pressed = false;
                SetProcessInput(false);
            }

            
            if (@event is InputEventKey key && key.AsText() == "T" && key.Pressed)
            {
                GlobalSettings.CurrentTheme = Theming.ColorThemes.GetTheme(
                    GlobalSettings.CurrentTheme is Theming.ColorThemes.Dark ? Theming.PredefinedTheme.Light : Theming.PredefinedTheme.Dark);
                GetNode<SceneManager>("/root/SceneManager").UpdateTheme();
            }
        }

        private void PlaySample(AudioStream sample)
        {
            _fxPlayer.Stream = sample;
            _fxPlayer.Play();
        }

        private void _UIStateChanged(bool blocked)
        {
            var buttons = GetTree().GetNodesInGroup("Buttons");

            foreach (TextureButton button in buttons)
            {
                button.ButtonMask = System.Convert.ToInt32(!blocked);
            }
        }

        #region ButtonEvents

        private void MenuButtonRelease(MenuButton button)
        {
            var rect = button.GetNode<Panel>("ColorRect");
            _tween.InterpolateProperty(rect, "modulate", rect.Modulate, new Color(rect.Modulate, 0f), .25f);
            _tween.Start();
        }
        private void MenuButtonClicked(MenuButton button, bool isDown)
        {
            if (_menu.Modulate.a < .5f || _menuButtonSelected) return;

            if (isDown)
            {
                var rect = button.GetNode<Panel>("ColorRect");
                _tween.InterpolateProperty(rect, "modulate", rect.Modulate, new Color(rect.Modulate, .3f), .25f);
                _tween.Start();
            }
            else
            {
                PlaySample(_tapSample);
                _menuButtonSelected = true;
                HideMenu();
                _optionsButton.Pressed = false;
                OnMenuItemSelected?.Invoke(button);
            }
        }


        private void OptionsButtonUp()
        {
            PlaySample(_clickSample);

            if (_menuState)
            {
                SetProcessInput(false);
                HideMenu();
            }
            else 
            {
                _menuButtonSelected = false;
                SetProcessInput(true);
                ShowMenu();
                OnOptionsButtonUp?.Invoke(null, null);
            }
        }

        private void MoveLeftButtonUp()
        {
            PlaySample(_clickSample);
            OnMoveLeftButtonUp?.Invoke(MoveLeftButton, MoveRightButton);
        }

        private void MoveRightButtonUp()
        {
            PlaySample(_clickSample);
            OnMoveRgihtButtonUp?.Invoke(MoveLeftButton, MoveRightButton);
        }

        private void BackButtonUp()
        {
            var sceneManager = GetNode<SceneManager>("/root/SceneManager");
            if (sceneManager.IsTweenActive) return;
            
            PlaySample(_clickSample);
            OnUIStateChange?.Invoke(true);
            sceneManager.GoToScene(this, "res://Scenes/MenuScene.tscn", Direction);
        }
        #endregion

        public void SetLabelValue(SceneLabel label, object value)
        {
            switch (label)
            {
                case SceneLabel.Time:
                    _timeLabel.Text = (string) value;
                    break;
                case SceneLabel.Counter:
                    _counter.Text = value.ToString();
                    break;
                case SceneLabel.SelectionBarValue:
                    _selectionBarValue.Text = value.ToString();
                    break;
            }
        }
    }
}