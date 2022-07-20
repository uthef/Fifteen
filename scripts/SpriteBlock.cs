using Godot;
using System;

namespace Fifteen.Scripts;

public class SpriteBlock : Sprite, IBlock
{
    public int ArrayPositionX { get; set; } = 0;
    public int ArrayPositionY { get; set; } = 0;
    public bool IsBeingAnimated { get; set; } = false;
    private MainScene _mainScene;

    private int _numberValue = 0;

    public int NumberValue
    {
        get => _numberValue;
        set => _numberValue = value;
    }
    public Vector2 Size
    {
        get => RegionRect.Size;
        set => RegionRect = new Rect2(Pos, value);
        
    }
    public Vector2 Pos
    {
        get => Position;
        set => Position = value;
    }

    public override void _Ready()
    {
        _mainScene = GetTree().Root.GetNode<MainScene>("Main Scene"); _mainScene = GetTree().Root.GetNode<MainScene>("Main Scene");
    }

    public override void _Process(float delta)
    {
        if (Input.IsMouseButtonPressed(1) && new Rect2(GlobalPosition, GetRect().Size).HasPoint(GetGlobalMousePosition()) && !IsBeingAnimated)
            _mainScene.TryToMove(this);
    }
}
