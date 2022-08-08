using Godot;
using System;
using Fifteen.Storage;
using Fifteen.Nodes;
using Fifteen.Theming;

public class Selector : HBoxContainer
{
    [Export] string[] _values;
    [Export] public  string PreferenceKey {get; private set;}

    [Signal] delegate void SelectorItemChanged(Selector node);
    int _position = 0;

    public int Position 
    {
        get => _position;
        set
        {
            if (value < 0) _position = _values.Length - 1;
            else if (value >= _values.Length) _position = 0;
            else _position = value;
        }
    }
    public override void _Ready()
    {
        Position = GlobalSettings.Preferences.GetInt32(PreferenceKey, 0);
        UpdateLabel();

        var sceneManager = GetNode<SceneManager>("/root/SceneManager");
        Connect(nameof(SelectorItemChanged), sceneManager, "SelectorItemChanged");
    }

    public void UpdateLabel()
    {
        if (_values.Length > 0) GetNode<Label>("Label").Text = _values[Position];
        GlobalSettings.Preferences.SetInt32(PreferenceKey, Position);
        GlobalSettings.Preferences.SaveData();
    }

    private void MoveLeftPressed()
    {
        Position--;
        GetNode<AudioStreamPlayer>("/root/MainScene/FxPlayer").Play();
        UpdateLabel();
        EmitSignal(nameof(SelectorItemChanged), this);
    }

    private void MoveRightPressed()
    {
        GetNode<AudioStreamPlayer>("/root/MainScene/FxPlayer").Play();
        Position++;
        UpdateLabel();
        EmitSignal(nameof(SelectorItemChanged), this);
    }
}
