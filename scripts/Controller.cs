using System;
using Godot;

namespace Fifteen.scripts;

public class Controller : CanvasLayer
{
    private DateTime _startTimestamp = new DateTime(0);
    private DateTime _pauseTimestamp = new DateTime(0);
    private long _moves = 0;
    public const int MinGridWidth = 3;
    public const int MaxGridWidth = 7;
    public const int GridHeightMaxDiff = 2;
    
    private Label _timeLabel;
    private Label _dimensionLabel;
    private Label _movesLabel;
    private TextureButton _moveRightButton, _moveLeftButton;
    private AudioStreamPlayer _clickPlayer;

    private int _width, _height;

    public int GridWidth
    {
        get => _width;
        private set
        {
            _width = value;
            _dimensionLabel.Text = $"{_width} x {_height}";
        }
    }

    public int GridHeight
    {
        get => _height;
        private set
        {
            _height = value;
            _dimensionLabel.Text = $"{_width} x {_height}";
        }
    }

    public bool TimerActive { get; private set; }

    public long Moves
    {
        get => _moves;
        private set => _moves = value;
    }
    
    public override void _Ready()
    {
        _timeLabel = GetNode<Label>("VBox/Time");
        _movesLabel =  GetNode<Label>("VBox/Moves");
        _dimensionLabel = GetNode<Label>("HBox/DimensionLabel");
        _moveRightButton = GetNode<TextureButton>("HBox/MoveRightButton");
        _moveLeftButton = GetNode<TextureButton>("HBox/MoveLeftButton");
        _clickPlayer = GetNode<AudioStreamPlayer>("../ClickPlayer");
        
        GridWidth = 4;
        GridHeight = 4;
        SetProcess(false);
    }

    public override void _Process(float delta)
    {
        _timeLabel.Text = $"{DateTime.Now - _startTimestamp:hh':'mm':'ss'.'ff}";
    }

    public void AddMove()
    {
        _movesLabel.Text = $"{++Moves}";
    }

    private void ResetMoves()
    {
        _movesLabel.Text = $"{Moves = 0}";
    }

    private void ResetTimerLabel()
    {
        _timeLabel.Text = "00:00:00.00";
    }

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

    private void MoveLeftButtonPressed()
    {
        if (GridWidth > MinGridWidth || GridHeight > GridWidth)
        {
            if (GridHeight == GridWidth) GridHeight = --GridWidth + GridHeightMaxDiff;
            else GridHeight--;
            PauseTimer(true);
            GetTree().Root.GetNode<MainScene>("Main Scene").GenerateField(GridWidth, GridHeight);
            _clickPlayer.Play();
        }

        _moveRightButton.Disabled = false;
        if (_width == MinGridWidth && GridHeight == _width) _moveLeftButton.Disabled = true;
    }
    
    private void MoveRightButtonPressed()
    {
        if (GridWidth < MaxGridWidth || GridHeight < GridWidth + GridHeightMaxDiff)
        {
            if (GridHeight == GridWidth + GridHeightMaxDiff) GridHeight = ++GridWidth;
            else GridHeight++;
            PauseTimer(true);
            GetTree().Root.GetNode<MainScene>("Main Scene").GenerateField(GridWidth, GridHeight);
            _clickPlayer.Play();
        }

        _moveLeftButton.Disabled = false;
        if (_height == MaxGridWidth + GridHeightMaxDiff) _moveRightButton.Disabled = true;
    }
}