using Godot;
using System;

public class ClickPlayer : AudioStreamPlayer
{
    public override void _Ready()
    {
        Stream = GD.Load<AudioStream>("res://audio/click.wav");
        Bus = "HighPass";
    }
}
