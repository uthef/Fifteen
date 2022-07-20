using System;
using System.Net.Mime;
using Godot;
using Fifteen.Scripts.Storage;

namespace Fifteen.Scripts;

public class Controller : CanvasLayer
{
    private DateTime _startTimestamp = new DateTime(0);
    private DateTime _pauseTimestamp = new DateTime(0);
    private long _moves = 0;

    private Label _timeLabel;
    private Label _label;
    private Label _counterLabel;
    private TextureButton _moveRightButton, _moveLeftButton;
    private AudioStreamPlayer _clickPlayer;

    public delegate void MoveButtonPressedEventHandler(bool whatButton);
    public event MoveButtonPressedEventHandler MoveButtonPressedEvent;

    public bool TimerActive { get; private set; }

    public long Moves
    {
        get => _moves;
        private set => _moves = value;
    }
    
    public override void _Ready()
    {
        _timeLabel = GetNode<Label>("VBox/Time");
        _counterLabel =  GetNode<Label>("VBox/Counter");
        _label = GetNode<Label>("HBox/Label");
        _moveRightButton = GetNode<TextureButton>("HBox/MoveRightButton");
        _moveLeftButton = GetNode<TextureButton>("HBox/MoveLeftButton");
        _clickPlayer = GetNode<AudioStreamPlayer>("../ClickPlayer");
        
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
    private void MoveLeftButtonPressed() => MoveButtonPressedEvent?.Invoke(false);
    private void MoveRightButtonPressed() => MoveButtonPressedEvent?.Invoke(true);
    
    public override void _Notification(int notification)
    {
        switch (notification)
        {
            case NotificationWmFocusOut:
                if (TimerActive) PauseTimer();
                break;
            case NotificationWmFocusIn:
                if (Moves > 0) StartTimer();
                break;
        }
    }
}