using System;
using Fifteen.Scripts.Special;
using Fifteen.Scripts.Storage;
using Godot;

namespace Fifteen.Scripts {
    public class Controller : CanvasLayer
    {
        private DateTime _startTimestamp = new DateTime(0);
        private DateTime _pauseTimestamp = new DateTime(0);
        private long _moves = 0;
        private bool _menuBlocked;
        public static bool MenuVisible { get; private set; } = false;

        private Label _timeLabel;
        private Label _label;
        private Label _counterLabel;
        private TextureButton _moveLeftButton, _moveRightButton;
        private AudioStreamPlayer _clickPlayer;
        private PanelContainer _options;
        private TextureButton _optionsButton;
        private AnimationPlayer _uiAnimator;

        public delegate void MoveButtonPressedEventHandler(bool whatButton);
        public delegate void OptionsItemSelectedEventHandler(OptionItems item);
        public delegate void OptionsPanelStateChangedEventHandler(bool opened);
        public event OptionsItemSelectedEventHandler OptionsItemSelectedEvent;
        public event MoveButtonPressedEventHandler MoveButtonPressedEvent;
        public event OptionsPanelStateChangedEventHandler OptionsPanelStateChanged;

        public bool TimerActive { get; private set; }

        public long Moves
        {
            get => _moves;
            private set => _moves = value;
        }
        
        public override void _Ready()
        {
            _timeLabel = GetNode<Label>("TopBox/VBox/Time");
            _counterLabel =  GetNode<Label>("TopBox/VBox/Counter");
            _label = GetNode<Label>("HBox/Label");
            _moveRightButton = GetNode<TextureButton>("HBox/MoveRightButton");
            _moveLeftButton = GetNode<TextureButton>("HBox/MoveLeftButton");
            _clickPlayer = GetTree().Root.GetNode<AudioStreamPlayer>("/root/ClickPlayer");
            _options = GetNode<PanelContainer>("OptionsMenu");
            _optionsButton = GetNode<TextureButton>("TopBox/OptionsButton");
            _uiAnimator = GetNode<AnimationPlayer>("../UIAnimationPlayer");

            var imageTexture = new ImageTexture();
            imageTexture.CreateFromImage(GlobalSettings.FrameBuffer);
            GetNode<Sprite>("../Slide").Texture = imageTexture;

            _uiAnimator.CurrentAnimation = "FadeOut";
            _uiAnimator.PlaybackSpeed = 3;
            _uiAnimator.Play();

            SetProcess(false);
        }

        public override void _Process(float delta)
        {
            _timeLabel.Text = $"{DateTime.Now - _startTimestamp:hh':'mm':'ss'.'ff}";
        }

        public void AddMove() => _counterLabel.Text = $"{++Moves}";
        private void ResetMoves() => _counterLabel.Text = $"{Moves = 0}";
        private void ResetTimerLabel() => _timeLabel.Text = "00:00:00.00";
        public void SetLabelText(string value) => _label.Text = value;

        public void StartTimer()
        {
            if (!TimerActive)
            {
                if (_pauseTimestamp.Ticks > 0) _startTimestamp += DateTime.Now - _pauseTimestamp;
                else _startTimestamp = DateTime.Now;
                SetProcess(TimerActive = true);
            }
        }

        public void PauseTimer(bool stop = false)
        {
            if (stop)
            {
                ResetMoves();
                ResetTimerLabel();
                _pauseTimestamp = new DateTime(0);
            }
            else _pauseTimestamp = DateTime.Now;
            SetProcess(TimerActive = false);
        }

        public void PlayClickSound() => _clickPlayer.Play();
        
        public void SetLeftButtonDisabled(bool value) => _moveLeftButton.Disabled = value;
        public void SetRightButtonDisabled(bool value) => _moveRightButton.Disabled = value;
        private void MoveLeftButtonPressed() 
        { 
            MoveButtonPressedEvent?.Invoke(false);
            PlayClickSound();
        }
        private void MoveRightButtonPressed()
        { 
            MoveButtonPressedEvent?.Invoke(true);
            PlayClickSound();
        }

        private void OptionsButtonPressed(bool auto = false)
        {
            if (_options.Modulate.a > 0)
            {
                if (!auto) PlayClickSound();
                _optionsButton.Pressed = false;
                _uiAnimator.CurrentAnimation = "MenuFadeOut";
                _uiAnimator.Play();
            }
            else if (_options.Modulate.a <= 1)
            {
                if (!auto) PlayClickSound();
                OptionsPanelStateChanged?.Invoke(true);
                _options.Visible = true;
                _uiAnimator.CurrentAnimation = "MenuFadeIn";
                _uiAnimator.Play();
                if (TimerActive) PauseTimer();
            }
        }

        public override void _Input(InputEvent @event)
        {
            if (_options.Modulate.a > 0 && !_uiAnimator.IsPlaying() && @event.IsPressed() && @event is InputEventMouse mouse 
                && !_options.GetGlobalRect().HasPoint(mouse.Position)) OptionsButtonPressed(true);
        }

        private void UIAnimationFinished(string name)
        {
            switch (name)
            {
                case "MenuFadeIn":
                    MenuVisible = true;
                    break;
                case "MenuFadeOut":
                    OptionsPanelStateChanged?.Invoke(false);
                    _options.Visible = false;
                    MenuVisible = _menuBlocked = false;
                    break;
                case "FadeOut":
                    _uiAnimator.PlaybackSpeed *= 2;
                    GetNode<Sprite>("../Slide").QueueFree();
                    GlobalSettings.FrameBuffer.Dispose();
                    break;
                default:
                    if (name.EndsWith("Pressed"))
                    {
                        _uiAnimator.CurrentAnimation = "MenuFadeOut";
                        _uiAnimator.Play();
                    }
                    break;
            }
        }

        private void ResetButtonInput(InputEvent @event, string nodeName) => OptionsItemSelected(@event, nodeName);
        private void SwitchImageModeButtonInput(InputEvent @event, string nodeName) => OptionsItemSelected(@event, nodeName);
        private void ReferenceButtonInput(InputEvent @event, string nodeName) => OptionsItemSelected(@event, nodeName);
        
        private void OptionsItemSelected(InputEvent @event, string nodeName)
        {
            if (_options.Modulate.a is 1f && @event is InputEventMouseButton {Pressed: true, ButtonIndex: 1} && !_menuBlocked)
            {
                if (!Enum.TryParse(nodeName, out OptionItems item)) return;
                
                var button = _options.GetNode<RectButton>($"VBoxContainer/{nodeName}");

                if (!button.Enabled) return;
                _menuBlocked = true;
                OptionsItemSelectedEvent?.Invoke(item);
                _optionsButton.Pressed = false;
                _uiAnimator.CurrentAnimation = nodeName + "ButtonPressed";
                _uiAnimator.Play();
            }
        }

        private void BackButtonPressed() 
		{
            PlayClickSound();
            GlobalSettings.FrameBuffer = GetViewport().GetTexture().GetData();
            GetTree().Root.CallDeferred("add_child", GD.Load<PackedScene>("res://scenes/GameMenu.tscn").Instance<Node>());
            GetTree().Root.GetNode("Main Scene").QueueFree();
		}

        public override void _Notification(int notification)
        {
            switch (notification)
            {
                case NotificationWmFocusOut:
                    if (TimerActive) PauseTimer();
                    break;
                case NotificationWmGoBackRequest:
                    BackButtonPressed();
                    break;
            }
        }
    }
}