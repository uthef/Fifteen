using Godot;
using System;

public class PolygonButton : TextureRect
{
    [Signal]
    delegate void PolygonButtonPressed(string name);

    Tween _tween;
    Polygon2D _poly;

    bool hovered = false;

    public override void _Ready()
    {
        _tween = GetParent().GetNode<Tween>("../Tween");
        _poly = GetNode<Polygon2D>("Area2D/Polygon");
    }

    private void OnArea2DInput(Node viewport, InputEvent inputEvent, int shape_idx)
    {
        if (inputEvent is InputEventMouseButton mouse)
        {
            if (mouse.IsPressed())
            {
                hovered = true;
                _tween.InterpolateProperty(_poly, "modulate", _poly.Modulate, new Color(_poly.Modulate, .4f), .15f);
            }
            else
            {
                _tween.InterpolateProperty(_poly, "modulate", _poly.Modulate, new Color(_poly.Modulate, 0f), .15f);
                if (hovered) EmitSignal("PolygonButtonPressed", Name);
            }

            _tween.Start();
        }
    }

    private void MouseExited()
    {
        hovered = false;
        _tween.InterpolateProperty(_poly, "modulate", _poly.Modulate, new Color(_poly.Modulate, 0f), .15f);
        _tween.Start();
    }
}
