using Godot;
using System;
using Fifteen.Scripts;

public class Reference : Sprite
{
    public delegate void ClickEventHandler();
    public event ClickEventHandler ClickEvent;
    
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: 1, Pressed: false } && new Rect2(Position, GetRect().Size).HasPoint(GetGlobalMousePosition()) && Modulate.a == 1 && !Controller.MenuVisible) ClickEvent?.Invoke();
    }
}
