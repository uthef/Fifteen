using Godot;

namespace Fifteen.Scripts;

public class Block : ColorRect
{
    private MainScene _mainScene;
    public Label Number { get; private set; }
    public int ArrayPositionX = 0, ArrayPositionY = 0;
    public bool IsBeingAnimated = false;
    
    private int _numberValue = 0;

    public int NumberValue
    {
        get => _numberValue;
        set
        {
            _numberValue = value;
            if (Number is null) return;
            Number.Text = value.ToString();
        }
    }

    public override void _Ready()
    {
        Number = GetNode<Label>("Number");
        _mainScene = GetTree().Root.GetNode<MainScene>("Main Scene");
    }

    public override void _Process(float delta)
    {
        if (Input.IsMouseButtonPressed(1) && GetGlobalRect().HasPoint(GetGlobalMousePosition()) && !IsBeingAnimated) 
            _mainScene.TryToMove(this);
    }
}