using Godot;
using Fifteen.Storage;
using Godot.Collections;
using Fifteen.Models;

namespace Fifteen.Nodes
{
    public class Rotacube : ViewportContainer
    {
        Spatial _cube;
        KinematicBody _cubeBody;
        RigidBody _ball;
        Vector2 _mouseStartPosition, _rotation;
        public static Rotacube Singleton;
        bool _rotating;
        AudioStreamPlayer _player;

        private int _level = 0, _maxLevel = 14;

        private Array _levels;
        private const int MinLevel = 0;
        private const string LevelMask = "Level {0}", CoinMask = "{0}/{1}";

        private int _coins = 0, _maxCoins = 0;
        private Spatial _currentLevel;

        [Signal] public delegate void CoinCollected();
        public delegate void LabelValueChangeEventHandler(SceneLabel label, object value);
        public event LabelValueChangeEventHandler LabelValueChange;

        public Rotacube()
        {
            Singleton = this;
        }

        public override void _Ready()
        {
            _cube = GetNode<Spatial>("Viewport/Cube");
            _cubeBody = _cube.GetNode<KinematicBody>("KinematicBody");
            _ball = GetNode<RigidBody>("Viewport/Ball");
            _player = GetNode<AudioStreamPlayer>("Viewport/CoinPlayer");
            
            RectSize = GetNode<Viewport>("Viewport").Size = GetViewport().Size;

            if (GetParent().GetParent() is MainScene mainScene)
            {
                mainScene.OnUIStateChange += OnUIStateChange;
                LabelValueChange += mainScene.SetLabelValue;
                mainScene.OnMoveLeftButtonUp += MoveLeftButtonUp;
                mainScene.OnMoveRgihtButtonUp += MoveRightButtonUp;

                _levels = GlobalSettings.Preferences.GetArray("rotacube_levels", new Array());
                _level = GlobalSettings.Preferences.GetInt32("rotacube_level", 0);

                LoadLevel();

                LabelValueChange.Invoke(SceneLabel.SelectionBarValue, string.Format(LevelMask, _level + 1));
                LabelValueChange.Invoke(SceneLabel.Counter, string.Format(CoinMask, 0, _maxCoins = GetTree().GetNodesInGroup("Coins").Count));
                if (_level == MinLevel) mainScene.MoveLeftButton.Disabled = true;
                if (_level == _maxLevel) mainScene.MoveRightButton.Disabled = true;
            }
        }

        private void LoadLevel()
        {
            if (_currentLevel != null) _cube.RemoveChild(_currentLevel);
            var scene = GD.Load<PackedScene>($"res://Scenes/RotacubeLevels/Level{_level}.tscn");
            _currentLevel = scene.Instance<Spatial>();
            var children = _currentLevel.GetChildren();
            foreach (Node child in children)
            {
                if (child.IsInGroup("Blocks"))
                {
                    SpatialMaterial material = new SpatialMaterial();
                    //material.Set("albedo_color", Color.FromHsv((float) GD.RandRange(0, 361), .5f, .5f));
                    child.GetNode<MeshInstance>("MeshInstance").MaterialOverride = material;
                }
            }

            _cube.AddChild(_currentLevel);
        }

        private void OnUIStateChange(bool blocked)
        {
            SetPhysicsProcess(!blocked);
        }

        private void MoveLeftButtonUp(TextureButton button1, TextureButton button2)
        {
            if (_level > MinLevel) _level--;
            if (_level == MinLevel) button1.Disabled = true;
            button2.Disabled = false;
            LabelValueChange.Invoke(SceneLabel.SelectionBarValue, string.Format(LevelMask, _level + 1));
        }

        private void MoveRightButtonUp(TextureButton button1, TextureButton button2)
        {
            if (_level < _maxLevel) _level++;
            if (_level == _maxLevel) button2.Disabled = true;
            button1.Disabled = false;
            LabelValueChange.Invoke(SceneLabel.SelectionBarValue, string.Format(LevelMask, _level + 1));
        }

        public override void _PhysicsProcess(float delta)
        {
            if (Input.IsActionJustPressed("click"))
            {
                _rotating = true;
                _mouseStartPosition = GetViewport().GetMousePosition();
            }

            if (Input.IsActionJustReleased("click")) _rotating = false;

            if (_rotating)
            {
                _rotation = GetViewport().GetMousePosition();
                _cube.RotateX((_rotation.y - _mouseStartPosition.y) * .3f * delta);
                _cube.RotateY((_rotation.x - _mouseStartPosition.x) * .3f * delta);
                _mouseStartPosition = _rotation;
            }
        }

        private void On_CoinCollected()
        {
            LabelValueChange.Invoke(SceneLabel.Counter, string.Format(CoinMask, ++_coins, _maxCoins));
            if (GetTree().GetNodesInGroup("Coins").Count == 0) OnLevelCompleted();
            _player.Play();
        }

        private void OnLevelCompleted()
        {
            GD.Print("Win!");
            if (_level >= _levels.Count) 
            {
                _levels.Add(0);
                GlobalSettings.Preferences.SaveData();
            }
        }
    }
}