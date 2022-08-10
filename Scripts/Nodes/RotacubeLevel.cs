using Godot;
using System;

public class RotacubeLevel : Spatial
{
    [Export] public Color Color;

    public override void _Ready()
    {
        var children = GetChildren();

        foreach (Node body in children)
        {
            if (body is KinematicBody) body.GetNode<MeshInstance>("MeshInstance").GetActiveMaterial(0).Set("albedo_color", Color.Lightened(.6f));
        }
    }
}
