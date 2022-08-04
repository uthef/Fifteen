using Godot;
using Godot.Collections;
using System;
using Fifteen.Models;
using Fifteen.Storage;
using Array = Godot.Collections.Array;
namespace Fifteen.Nodes
{
    public class FifteenPuzzle : Godot.Node2D
    {
        [Export] public Image[] Pictures;
        private Texture _texture;
        private ImageTexture[] _textures = new ImageTexture[0];
        FifteenModel _puzzle;
        DynamicFont _font;
        public int GridWidth {get; private set; } = 4;
        public int GridHeight {get; private set; } = 4;

        public const string TimeMask = "hh':'mm':'ss'.'ff";

        public const int MinGridWidth = 3, MaxGridWidth = 7, MaxGridHeightDifference = 2;
        public AudioStreamPlayer _impactPlayer, _successSoundPlayer;
        TimeSpan _pauseTime = new TimeSpan(0), _startTime = new TimeSpan(0);
        private int _moves = 0, _pictureIndex;
        public bool GameActive {get; private set; } = false;
        private bool _gameStarted = false, _pictureMode = false, _animated = false, _imageRestored = false, _hueRestored, _complteted;

        private PackedScene _blockScene;

        private Godot.Node2D _blocks, _cells, _interactiveArea;
        private Tween _tween;

        private AnimationPlayer _animator;

        private Sprite _imageReference;
        private AnimatedSprite _confettiSprite;

        private Block[] _sprites;

        float _cellSize, _hue, _saturationStep, _lineWidth = 3f;
        Vector2 _textureStart;

        public delegate void LabelValueChangeEventHandler(SceneLabel label, object value);
        public event LabelValueChangeEventHandler LabelValueChange;

        public override void _Ready()
        {
            GridWidth = GlobalSettings.Preferences.GetInt32("f_width", 4, MinGridWidth, MaxGridWidth);
            GridHeight = GlobalSettings.Preferences.GetInt32("f_height", GridWidth, GridWidth, GridWidth + MaxGridHeightDifference);
            _pictureMode = GlobalSettings.Preferences.GetBool("picture_mode", false);

            if (GetParent() is MainScene mainScene)
            {
                mainScene.OnMoveLeftButtonUp += MoveLeftButtonUp;
                mainScene.OnMoveRgihtButtonUp += MoveRightButtonUp;
                mainScene.OnMenuItemSelected += OnMenuItemSelected;
                mainScene.OnUIStateChange += OnUIStateChange;
                mainScene.OnOptionsButtonUp += OnOptionsButtonUp;
                LabelValueChange += mainScene.SetLabelValue;

                _textures = (ImageTexture[]) (mainScene as IDataScene).Data;

                if (GridWidth == MinGridWidth && GridHeight == MinGridWidth) mainScene.MoveLeftButton.Disabled = true;
                else if (GridWidth == MaxGridWidth && GridHeight == MaxGridWidth + MaxGridHeightDifference) mainScene.MoveRightButton.Disabled = true;
            }

     
            int[,] fifteen = GlobalSettings.Preferences.Get2DIntArray("fifteen", new int[0, 0]);
            string startTime = GlobalSettings.Preferences.GetString("start_time", null), pauseTime = GlobalSettings.Preferences.GetString("pause_time", null);
            int moves = GlobalSettings.Preferences.GetInt32("moves", 0, 0);
            int picIndex = GlobalSettings.Preferences.GetInt32("picture_index", -1, 0, _textures.Length);
            float hue = GlobalSettings.Preferences.GetFloat("hue", -1, 0);
            bool completed = GlobalSettings.Preferences.GetBool("f_completed", false);
   
            _cells = GetNode<Godot.Node2D>("InteractiveArea/Cells");
            _blockScene = GD.Load<PackedScene>("res://Scenes/Block.tscn");
            _font = GD.Load<DynamicFont>("res://Themes/DynamicFonts/BlockFont.tres");
            _blocks = GetNode<Godot.Node2D>("InteractiveArea/Blocks");
            _interactiveArea = GetNode<Godot.Node2D>("InteractiveArea");
            _tween = GetNode<Tween>("Tween");
            _impactPlayer = GetNode<AudioStreamPlayer>("ImpactPlayer");
            _animator = GetNode<AnimationPlayer>("Animator");
            _imageReference = GetNode<Sprite>("InteractiveArea/ImageReference");
            _successSoundPlayer = GetNode<AudioStreamPlayer>("SuccessSoundPlayer");
            _confettiSprite = GetNode<AnimatedSprite>("Confetti");

            if (completed) 
            {
                GlobalSettings.Preferences.RemoveKey("f_completed");
                GlobalSettings.Preferences.RemoveKey("fifteen");
            }
            else if (fifteen.Length == GridWidth * GridHeight && startTime != null && pauseTime != null && moves > 0)
            {
                _moves = moves;
                if (hue != -1) 
                {
                    _hue = hue;
                    _hueRestored = true;
                }
                if (picIndex != -1)
                {
                    _imageRestored = true;
                    _pictureIndex = picIndex;
                } 
                _startTime = TimeSpan.FromMilliseconds(ulong.Parse(pauseTime)) - TimeSpan.FromMilliseconds(ulong.Parse(startTime));
                UpdateValues();
                _startTime = TimeSpan.FromMilliseconds(ulong.Parse(startTime));
                _pauseTime = TimeSpan.FromMilliseconds(ulong.Parse(pauseTime));
                _puzzle = new FifteenModel(fifteen);
                GeneratePuzzle(true);
                return;
            }

            Reset();
            UpdateValues();
            GeneratePuzzle();
        }

        public override void _Process(float deltaTime)
        {
            if (_imageReference.Modulate.a > 0)
            {
                if (Input.IsMouseButtonPressed(1) && new Rect2(_imageReference.GlobalPosition, _imageReference.RegionRect.Size).HasPoint(GetGlobalMousePosition()))
                {
                    _tween.InterpolateProperty(_imageReference, "modulate", _imageReference.Modulate, new Color(_imageReference.Modulate, 0f), .25f);
                    _tween.Start();
                }
            }
            else 
            {
                foreach (var sprite in _sprites)
                {
                    if (Input.IsMouseButtonPressed(1) && !sprite.IsBeingInterpolated && new Rect2(sprite.GlobalPosition, sprite.RegionRect.Size).HasPoint(GetGlobalMousePosition()))
                    {
                        Vector2 direction = _puzzle.TryToMove(sprite.Row, sprite.Column);
                        if (direction == Vector2.Zero) continue;
                        
                        sprite.UpdateArrayPosition(direction);
                        _tween.InterpolateProperty(sprite, "position", sprite.Position, sprite.Position + direction * sprite.RegionRect.Size, .2f, Tween.TransitionType.Cubic, Tween.EaseType.Out);
                        _tween.Start();

                        LabelValueChange?.Invoke(SceneLabel.Counter, ++_moves);

                        if (!_gameStarted)
                        {
                            _gameStarted = true;
                            GlobalSettings.Preferences.Set2DIntArray("fifteen", _puzzle.AsArray());
                            if (_pauseTime.Ticks == 0) _startTime = TimeSpan.FromMilliseconds((long)OS.GetSystemTimeMsecs());
                            else ResumeTimer();
                        }
                        else 
                        {
                            Array array = GlobalSettings.Preferences.GetUnsafe<Array>("fifteen");
                            int oldColumn = sprite.Column - (int) direction.x, oldRow = sprite.Row - (int) direction.y;
                            ((array[oldRow] as Array)[oldColumn], (array[sprite.Row] as Array)[sprite.Column]) = ((array[sprite.Row] as Array)[sprite.Column], (array[oldRow] as Array)[oldColumn]);
                        }

                        break;
                    }
                }
            }

            if (_gameStarted) LabelValueChange?.Invoke(SceneLabel.Time, (TimeSpan.FromMilliseconds((long)OS.GetSystemTimeMsecs()) - _startTime).ToString(TimeMask));
        }


        private void Reset()
        {
            _pauseTime = new TimeSpan(0);
            _gameStarted = false;
            _moves = 0;
        }

        private void UpdateValues()
        {
            LabelValueChange?.Invoke(SceneLabel.Counter, _moves);
            LabelValueChange?.Invoke(SceneLabel.SelectionBarValue, $"{GridWidth} x {GridHeight}");
            LabelValueChange?.Invoke(SceneLabel.Time, _startTime.ToString(TimeMask));
        }

        private void GeneratePuzzle(bool predefined = false)
        {    
            SetProcess(false);
            _complteted = false;

            if (_imageReference.Modulate.a > 0) _imageReference.Modulate = new Color(_imageReference.Modulate, 0);

            if (!predefined)
            {
                _gameStarted = false;
                _startTime = new TimeSpan(0);
                _puzzle = new FifteenModel(GridWidth, GridHeight); 
                GlobalSettings.Preferences.RemoveKey("f_completed");
                Reset();
                UpdateValues();
            }

            _cellSize = (GetViewport().Size.x - 48) / (GridWidth + (GridWidth < 5 && GridHeight > GridWidth ? 1 : 0));
            _confettiSprite.Scale = new Vector2(_cellSize * GridWidth / 1024, _cellSize * GridWidth / 1024);
            _confettiSprite.Position = new Vector2(GetViewport().Size.x / 2, GetViewport().Size.y / 2);

            _font.Size = (int) _cellSize / 2;

            if (_pictureMode)
            {
                GlobalSettings.Preferences.RemoveKey("hue");
                if (_imageRestored) _imageRestored = false;
                else _pictureIndex = new Random().Next(_textures.Length);

                _texture = _textures[_pictureIndex];
                GetNode<Sprite>("InteractiveArea/ImageReference").Texture = _texture;
                _textureStart =  new Vector2(_texture.GetWidth() / 2f - _cellSize * GridWidth / 2, _texture.GetHeight() / 2f - _cellSize * GridHeight / 2);
                GetNode<Sprite>("InteractiveArea/ImageReference").RegionRect = new Rect2(_textureStart, new Vector2(_cellSize * GridWidth, _cellSize * GridHeight));
            }
            else 
            {
                GlobalSettings.Preferences.RemoveKey("picture_index");
                if (_hueRestored) _hueRestored = false;
                else _hue = (float) new Random().NextDouble();
            
                _saturationStep = GlobalSettings.CurrentTheme.MaxSaturation / _puzzle.Length;

                ImageTexture imageTexture = new ImageTexture();
                Image image = new Image();
                image.Create((int) _cellSize, (int) _cellSize, false, Image.Format.Rgb8);
                imageTexture.CreateFromImage(image);
                _texture = imageTexture;
                _textureStart = new Vector2();
            }

            if (!predefined) SaveDataAsync();            

            DrawPuzzle();
        }

        private void ResumeTimer()
        {
            _startTime = TimeSpan.FromMilliseconds((long)OS.GetSystemTimeMsecs()) - (_pauseTime - _startTime);
            _pauseTime = new TimeSpan(0);
        }

        private void DrawPuzzle()
        {
            _sprites = new Block[_puzzle.Length - 1];
            foreach (Block child in _blocks.GetChildren()) child.QueueFree();

            _interactiveArea.Position = new Vector2(GetViewport().Size.x / 2 - _cellSize * GridWidth / 2, GetViewport().Size.y / 2 - _cellSize * GridHeight / 2);
            for (int i = 0, count = 0; i < GridHeight; i++)
            {
                for (int j = 0; j < GridWidth; j++)
                {
                    var number = _puzzle.GetNumber(i, j);

                    if (number != _puzzle.Length)
                    {
                        var instance = _blockScene.Instance<Block>();
                        instance.Texture = _texture;
                        var label = instance.GetNode<Label>("Label");
                        instance.Position = new Vector2(_cellSize * j, _cellSize * i);
                        if (_pictureMode)
                        {
                            int n = GridHeight - (GridHeight - GridWidth), val = number - 1;
							int row = val / n;
							int col = val - n * row;
                            instance.Material = null;
                            instance.RegionRect = new Rect2(_textureStart + new Vector2(col * _cellSize, row * _cellSize), new Vector2(_cellSize, _cellSize));
                            label.Visible = false;
                        }
                        else 
                        {
                            label.Visible = true;
                            label.Text = number.ToString();
                            instance.Material = (ShaderMaterial) instance.Material.Duplicate();
                            instance.Material.Set("shader_param/first_color", Color.FromHsv(_hue, _saturationStep * number, 
                                GlobalSettings.CurrentTheme.BlockColorBrightness.Min));
                            instance.Material.Set("shader_param/second_color", Color.FromHsv(_hue, _saturationStep * number, 
                                GlobalSettings.CurrentTheme.BlockColorBrightness.Max));    
                            instance.RegionRect = new Rect2(new Vector2(0, 0), new Vector2(_cellSize, _cellSize));
                        }
                        
                        instance.UpdateArrayPosition(new Vector2(j, i), false);
                        _blocks.AddChild(instance);

                        _sprites[count++] = instance;
                    }
                }
            }

            _cells.Update();
            SetProcess(true);
        }

        private void DrawCells()
        {
            if (_pictureMode)
            {
                for (int i = 0; i < GridHeight; i++)
                {
                    for (int j = 0; j < GridWidth; j++)
                    {
                        _cells.DrawRect(new Rect2(new Vector2(j * _cellSize, i * _cellSize), new Vector2(_cellSize, _cellSize)), new Color(1f, 1f, 1f), false, _lineWidth, false);
                    }
                }
            }
            else
            {
                _cells.DrawRect(new Rect2(new Vector2(0, 0), new Vector2(_cellSize * GridWidth, _cellSize * GridHeight)), GlobalSettings.CurrentTheme.ForegroundColor, false, _lineWidth, false);
            }
        }

        public override void _Notification(int what)
        {
            switch (what)
            {
                case NotificationWmFocusOut:
                    if (_gameStarted)
                    {
                        _gameStarted = false;
                        _pauseTime = TimeSpan.FromMilliseconds((long)OS.GetSystemTimeMsecs());
                    }
                    break;
            }
        }

        private void TweenCompleted(object node, string nodePath)
        {
            if (node is Block block)
            {
                block.IsBeingInterpolated = false;

                if (_gameStarted)
                {
                    _pauseTime = TimeSpan.FromMilliseconds(OS.GetSystemTimeMsecs());
                    if (_puzzle.IsOrderCorrect()) 
                    {
                        GlobalSettings.Preferences.SetBool("f_completed", true);
                        SetProcess(false);
                        _gameStarted = false;
                    }
                    
                    SaveDataAsync();
                }

                _impactPlayer.PitchScale = (float) GD.RandRange(0.9, 1.1);
                _impactPlayer.Play();
            }
        }

        private void TweenAllCompleted()
        {
            if (GlobalSettings.Preferences.GetBool("f_completed", false) && !_complteted)
            {
                _complteted = true;
                if (_pictureMode)
                {
                    _tween.InterpolateProperty(_imageReference, "modulate", _imageReference.Modulate, new Color(_imageReference.Modulate, 1f), .25f);
                    _tween.Start();
                }
                _successSoundPlayer.Play();
                _confettiSprite.Visible = true;
                _confettiSprite.Frame = 0;
                _confettiSprite.Play();
            }
        }

        private async void SaveDataAsync()
        {
            await System.Threading.Tasks.Task.Run(() => {
                if (_pictureMode) GlobalSettings.Preferences.SetFloat("picture_index", _pictureIndex);
                else GlobalSettings.Preferences.SetFloat("hue", _hue);
                
                GlobalSettings.Preferences.SetFloat("f_width", GridWidth);
                GlobalSettings.Preferences.SetFloat("f_height", GridHeight);
                GlobalSettings.Preferences.SetBool("picture_mode", _pictureMode);
                GlobalSettings.Preferences.SetString("start_time", _startTime.TotalMilliseconds.ToString());
                GlobalSettings.Preferences.SetString("pause_time", _pauseTime.TotalMilliseconds.ToString());
                GlobalSettings.Preferences.SetFloat("moves", _moves);
                GlobalSettings.Preferences.SaveData();
            });
        }

        private void ConfettiAnimationFinished()
        {
             _confettiSprite.Visible = false;
             _confettiSprite.Stop();
        }

        private void AnimationFinished(string animation)
        {
            switch (animation)
            {
                case "FadeOutReversed":
                    GeneratePuzzle();
                    Reset();
                    _animated = false;
                    _animator.Play("FadeInReversed");
                    break;
                case "FadeOut":
                    GeneratePuzzle();
                    Reset();
                    _animated = false;
                    _animator.Play("FadeIn");
                    break;
            }
        }

        #region UI
        private void MoveLeftButtonUp(TextureButton moveLeftButton, TextureButton moveRightButton)
        {
            if (GridWidth > MinGridWidth || GridHeight > GridWidth)
            {
                GridHeight = GridHeight > GridWidth ? GridHeight - 1 : --GridWidth + MaxGridHeightDifference;
                LabelValueChange?.Invoke(SceneLabel.SelectionBarValue, $"{GridWidth} x {GridHeight}");

                if (!_animated)
                {
                    _animator.Play("FadeOut");
                    _animated = true;
                }
                else 
                {
                    _animator.Play("FadeIn");  
                    GeneratePuzzle();
                    _animated = false;
                }
    
                if (GridWidth == MinGridWidth && GridHeight == MinGridWidth) moveLeftButton.Disabled = true;
                else if (moveRightButton.Disabled) moveRightButton.Disabled = false;
            }
        }

        private void MoveRightButtonUp(TextureButton moveLeftButton, TextureButton moveRightButton)
        {
            if (GridHeight < GridWidth + MaxGridHeightDifference || GridWidth < MaxGridWidth)
            {
                GridHeight = GridHeight < GridWidth + MaxGridHeightDifference ? GridHeight + 1 : ++GridWidth;
                LabelValueChange?.Invoke(SceneLabel.SelectionBarValue, $"{GridWidth} x {GridHeight}");
                
                
                if (!_animated) 
                {
                    _animator.Play("FadeOutReversed");  
                    _animated = true;
                }
                else 
                {
                    _animator.Play("FadeInReversed");  
                    GeneratePuzzle();
                    _animated = false;
                }

                if (GridWidth == MaxGridWidth && GridHeight == MaxGridWidth + MaxGridHeightDifference) moveRightButton.Disabled = true;
                else if (moveLeftButton.Disabled) moveLeftButton.Disabled = false;
            }
        }

        private void OnOptionsButtonUp(TextureButton button1, TextureButton button2)
        {
            Godot.Collections.Array items = GetTree().GetNodesInGroup("MenuItems");

            foreach (MenuButton item in items)
            {
                if (item.ItemType is MenuItemType.SwitchMode && item.Toggled != _pictureMode) item.Toggle();
                else if (item.ItemType is MenuItemType.ShowHideReference) 
                {
                    item.Disabled = !_pictureMode;
                    if (_imageReference.Modulate.a > 0 != item.Toggled) item.Toggle();
                }
            }
        }

        private void OnMenuItemSelected(MenuButton button)
        {
            switch (button.ItemType)
            {
                case MenuItemType.Reset:
                    _animator.Play("FadeOut");
                    break;
                case MenuItemType.SwitchMode:
                    _pictureMode = !_pictureMode;
                    if (_imageReference.Modulate.a > 0) 
                    {
                         _tween.InterpolateProperty(_imageReference, "modulate", _imageReference.Modulate, new Color(_imageReference.Modulate, 0f), .25f);
                        _tween.Start();
                    }
                    _animator.Play("FadeOut");
                    break;
                case MenuItemType.ShowHideReference:
                        _tween.InterpolateProperty(_imageReference, "modulate", _imageReference.Modulate, new Color(_imageReference.Modulate, _imageReference.Modulate.a > 0f ? 0f : 1f), .25f);
                        _tween.Start();
                    break;
            }
        }

        private void OnUIStateChange(bool blocked)
        {
            if (!GlobalSettings.Preferences.GetBool("f_completed", false))
                SetProcess(!blocked);
            if (_gameStarted && blocked)
            {
                _gameStarted = false;
                _pauseTime = TimeSpan.FromMilliseconds((long)OS.GetSystemTimeMsecs());
            }
        }
    }
    #endregion
}