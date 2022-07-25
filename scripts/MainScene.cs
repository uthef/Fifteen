using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fifteen.Scripts.Special;
using Fifteen.Scripts.Storage;
using Godot;
using Godot.Collections;
using Vector2 = Godot.Vector2;

namespace Fifteen.Scripts {
	public class MainScene : Node
	{
		private PackedScene _blockScene;
		private PackedScene _spriteBlockScene;

		private Node2D _cellGroup, _blockGroup;
		private Tween _tween;
		private Controller _controller;
		private AudioStreamPlayer _impactPlayer;
		private AnimationPlayer _animationPlayer;
		private PictureReference _refImage;
		private RectButton  _switchImageButton, _refButton;
		private DynamicFont _blockFont;
		[Export] private Texture[] _pictures;

		private IBlock[,] _blocks = new IBlock[0, 0];
		public float BorderWidth {get; private set;}
		public Vector2 BorderMargin {get; private set;}
		public float CellSize {get; private set;}
		private const int MinGridWidth = 3;
		private const int MaxGridWidth = 7;
		private const int GridHeightMaxDiff = 2;
		private bool _gridActive;
		private int _width, _height;

		public int GridWidth
		{
			get => _width;
			private set
			{
				_width = value;
				_controller?.SetLabelText($"{GridWidth} x {GridHeight}");
			}
		}
		public int GridHeight
		{
			get => _height;
			private set
			{
				_height = value;
				_controller?.SetLabelText($"{GridWidth} x {GridHeight}");
			}
		}

		public bool ImageMode = false;

		public override void _EnterTree() 
		{
			// Loading resources
			_blockFont = GD.Load<DynamicFont>("res://themes/fonts/Block.tres");
			_blockFont.OutlineSize = ColorThemes.AppTheme.BlockNumberOutlineWidth;
		}

		public override void _Ready()
		{
			// Loading scenes
			_blockScene = GD.Load<PackedScene>("res://scenes/Block.tscn");
			_spriteBlockScene = GD.Load<PackedScene>("res://scenes/SpriteBlock.tscn");

			// Getting nodes
			_cellGroup = GetNode<Node2D>("InteractiveArea/CellGroup");
			_blockGroup = GetNode<Node2D>("InteractiveArea/BlockGroup");
			_tween = GetNode<Tween>("Tween");
			_controller = GetNode<Controller>("CanvasLayer");
			_switchImageButton = _controller.GetNode<RectButton>("OptionsMenu/VBoxContainer/SwitchImageMode");
			_refButton = _controller.GetNode<RectButton>("OptionsMenu/VBoxContainer/Reference");
			_impactPlayer = GetNode<AudioStreamPlayer>("ImpactPlayer");
			_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
			_refImage = GetNode<PictureReference>("InteractiveArea/Reference");

			// Adding events
			_controller.MoveButtonPressedEvent += MoveButtonPressed;
			_controller.OptionsPanelStateChanged += OptionsPanelStateChanged;
			_controller.OptionsItemSelectedEvent += OptionsItemSelected;
			_refImage.ClickEvent += ReferenceImageClick;

			GridWidth = GlobalSettings.StoredPreferences.RootSection.GetInt32("f_width", 4, MaxGridWidth, MinGridWidth);
			GridHeight = GlobalSettings.StoredPreferences.RootSection.GetInt32("f_height", GridWidth, GridWidth + GridHeightMaxDiff, GridWidth);
			ImageMode = GlobalSettings.StoredPreferences.RootSection.GetBool("picture_mode", false);

			if (GridWidth == MinGridWidth && GridHeight == MinGridWidth) 
				_controller.SetLeftButtonDisabled(true);
			else if (GridWidth == MaxGridWidth && GridHeight == MaxGridWidth + GridHeightMaxDiff)
				_controller.SetRightButtonDisabled(true);

			GenerateField(GridWidth, GridHeight, false, true);
		}

		#region Logic
		public void GenerateField(int width, int height, bool reverseAnimation = false, bool skipAnimation = false)
		{
			if (_blocks.Length != 0 && !skipAnimation)
			{
				_gridActive = false;
				_blocks = new IBlock[0, 0];
				_animationPlayer.CurrentAnimation = $"InteractiveAreaFadeOut{(reverseAnimation ? "" : "Reversed")}";
				_animationPlayer.Play();
				return;
			}

			if (_refImage.Modulate.a is 1f)
			{
				_animationPlayer.CurrentAnimation = "ReferenceFadeOut";
				_animationPlayer.Play();
			}

			var random = new Random();
			PackedScene scene = ImageMode ? _spriteBlockScene : _blockScene;

			_blocks = new IBlock[height, width];
			var plainArray = new int[_blocks.Length];
			List<int> numbers = new();

			CellSize = (GetViewport().Size.x - 48) / (width + (width < 5 && height > width ? 1 : 0));
			BorderWidth = 3f;
			BorderMargin = new Vector2(BorderWidth / 2, BorderWidth / 2);

			float hue = (float)random.NextDouble(), saturationStep = ColorThemes.AppTheme.MaxBlockNumberSaturation / _blocks.Length;
			int emptyCellRow = 0;

			foreach (Node child in _blockGroup.GetChildren()) child.QueueFree(); 
			
			_blockGroup.Position = _cellGroup.Position = new Vector2(GetViewport().Size.x  / 2 - CellSize * width / 2f - BorderWidth / 2, GetViewport().Size.y / 2 - CellSize * height / 2f - BorderWidth / 2);
			_cellGroup.Update();
			
			Vector2 textureStartVector = new Vector2();
			Texture texture = null;
			
			if (ImageMode)
			{
				texture = _pictures[random.Next(_pictures.Length)];
				textureStartVector = new Vector2(texture.GetWidth() / 2f - CellSize * width / 2, texture.GetHeight() / 2f - CellSize * height / 2);
				_refImage.Position = new Vector2(BorderWidth / 2, BorderWidth / 2) + _cellGroup.Position;
				_refImage.Texture = texture;
				_refImage.RegionRect = new Rect2(textureStartVector, new Vector2(CellSize * width, CellSize * height));
			}

			for (int i = 0; i < _blocks.Length; i++) numbers.Add(i);
			
			for (int i = 0, count = 0; i < height; i++)
			{
				for (int j = 0; j < width; j++)
				{
					IBlock blockInstance = scene.Instance<IBlock>();
					_blocks[i, j] = blockInstance;

					blockInstance.Column = j;
					blockInstance.Row = i;
					
					blockInstance.Pos = BorderMargin + new Vector2(CellSize * j, CellSize * i);

					var randIndex = random.Next(numbers.Count);
					
					if (numbers[randIndex] != 0)
					{
						blockInstance.Size = new Vector2(CellSize, CellSize);
						_blockGroup.AddChild(blockInstance as Node);
						plainArray[count++] = blockInstance.NumberValue = numbers[randIndex];
						
						if (blockInstance is SpriteBlock sprite)
						{
							sprite.Texture = texture;
							int n = height - (height - width), val = sprite.NumberValue - 1;
							int row = val / n;
							int col = val - n * row;
							sprite.GetNode<Label>("Label").Text = sprite.NumberValue.ToString();
							
							sprite.RegionRect =
								new Rect2(textureStartVector + new Vector2( col * CellSize, row * CellSize),
									new Vector2(CellSize, CellSize));
						}
						else if (blockInstance is Block block)
						{
							block.Color = Color.FromHsv(hue, blockInstance.NumberValue * saturationStep, ColorThemes.AppTheme.BlockBrightness);
							block.Number.RectSize = blockInstance.Size;
							_blockFont.Set("size", CellSize / 2f);
						}
					}
					else
					{
						blockInstance.NumberValue = _blocks.Length;
						emptyCellRow = i;
						plainArray[count++] = 0;
					}
					
					numbers.RemoveAt(randIndex);
				}
			}

			int inversions = 0;
			
			for (int i = 0; i < plainArray.Length; i++)
			{
				if (plainArray[i] == 0) continue;
				for (int j = 0; j < i; j++)
				{
					if (plainArray[j] > plainArray[i]) 
						inversions++;
				}
			}

			if (width % 2 == 0)
			{
				inversions += emptyCellRow + 1;
				if (height % 2 != 0) inversions++;
			}

			if (inversions % 2 != 0)
			{
				var row = emptyCellRow > 0 ? emptyCellRow - 1 : emptyCellRow + 1;
				var column = random.Next(_blocks.GetLength(1));
				var nextColumn = column > 0 ? column - 1 : column + 1;

				// Swap two neighbor blocks
				(_blocks[row, column], _blocks[row, nextColumn]) = (_blocks[row, nextColumn], _blocks[row, column]);
				(_blocks[row, column].Column, _blocks[row, nextColumn].Column) = 
					(_blocks[row, nextColumn].Column, _blocks[row, column].Column);
				(_blocks[row, column].Pos, _blocks[row, nextColumn].Pos) = 
					(_blocks[row, nextColumn].Pos, _blocks[row, column].Pos);
			}

			if (!skipAnimation)
			{
				_animationPlayer.CurrentAnimation = $"InteractiveAreaFadeIn{(reverseAnimation ? "" : "Reversed")}";
				_animationPlayer.Play();
			}
			else _gridActive = true;
		}
		public bool IsOrderCorrect()
		{
			for (int i = 0, count = 1; i < _blocks.GetLength(0); i++)
			{
				for (int j = 0; j < _blocks.GetLength(1); j++)
				{
					if (_blocks[i, j].NumberValue != count++) 
						return false;
				}
			}

			return true;
		}
		public bool TryToMove(IBlock block)
		{
			if (!_gridActive) return false;
			
			int x = block.Column, y = block.Row;

			if (y + 1 < _blocks.GetLength(0) && _blocks[y + 1, x].NumberValue == _blocks.Length)
			{
				(_blocks[y + 1, x], _blocks[y, x]) = (_blocks[y, x], _blocks[y + 1, x]);
				block.Row++;
			}
			else if (y - 1 >= 0 && _blocks[y - 1, x].NumberValue == _blocks.Length)
			{
				(_blocks[y - 1, x], _blocks[y, x]) = (_blocks[y, x], _blocks[y - 1, x]);
				block.Row--;
			}
			else if (x + 1 < _blocks.GetLength(1) && _blocks[y, x + 1].NumberValue == _blocks.Length)
			{
				(_blocks[y, x + 1], _blocks[y, x]) = (_blocks[y, x], _blocks[y, x + 1]);
				block.Column++;
			}
			else if (x - 1 >= 0 && _blocks[y, x - 1].NumberValue == _blocks.Length)
			{
				(_blocks[y, x - 1], _blocks[y, x]) = (_blocks[y, x], _blocks[y, x - 1]);
				block.Column--;
			}
			else return false;

			Vector2 movement = new Vector2(block.Column - x, block.Row - y);
			
			_controller.AddMove();
			if (!_controller.TimerActive)
			{
				Task.Run(() => { _controller.StartTimer(); });
			}
			block.IsBeingAnimated = true;

			var startVector = new Vector2(block.Size.x * x + BorderWidth / 2, block.Size.y * y + BorderWidth / 2);
			_tween.InterpolateProperty(block as Node, block is Block ? "rect_position" : "position", block.Pos,
				startVector + movement * block.Size, .5f, Tween.TransitionType.Cubic);
			_tween.Start();
			
			Task.Run(() =>
			{
				if (!IsOrderCorrect()) return;
				_tween.Start();
				_gridActive = false;
				_controller.PauseTimer();
			});

			return true;
		}
		#endregion
		#region Events
		private void TweenCompleted(object node, string path)
		{
			if (node is IBlock block)
			{
				block.IsBeingAnimated = false;
				_impactPlayer.Play();
			}
		}

		private void OptionsPanelStateChanged(bool opened) 
		{
			_refButton.Enabled = ImageMode;

			if (opened)
			{
				_gridActive = false;
				_switchImageButton.Text = ImageMode ? _switchImageButton.ToggleText : _switchImageButton.DefaultText;
				_refButton.Text = _refImage.Modulate.a is 1f ? _refButton.ToggleText : _refButton.DefaultText;
			}
			else _gridActive = !(_refImage.Modulate.a > 0); 
		}

		private void ReferenceImageClick()
		{
			_animationPlayer.CurrentAnimation = "ReferenceFadeOut";
			_animationPlayer.Play();
		}

		private void OptionsItemSelected(OptionItems item)
		{
			switch (item)
			{
				case OptionItems.Reset:
					_controller.PauseTimer(true);
					GenerateField(GridWidth, GridHeight);
					break;
				case OptionItems.SwitchImageMode:
					_controller.PauseTimer(true);
					GlobalSettings.StoredPreferences.RootSection.SetBool("picture_mode", ImageMode = !ImageMode);
					GlobalSettings.StoredPreferences.Save();
					_refImage.Modulate = new Color(_refImage.Modulate) { a = 0f };
					GenerateField(GridWidth, GridHeight);
					break;
				case OptionItems.Reference:
					if (_refImage.Modulate.a > 0)
					{
						_animationPlayer.CurrentAnimation = "ReferenceFadeOut";
						_animationPlayer.Play();
					}
					else
					{
						_animationPlayer.CurrentAnimation = "ReferenceFadeIn";
						_animationPlayer.Play();
					}
					break;
			}
		}

		private void MoveButtonPressed(bool whatButton)
		{
			if (whatButton)
			{
				if (GridWidth < MaxGridWidth || GridHeight < GridWidth + GridHeightMaxDiff)
				{
					if (GridHeight == GridWidth + GridHeightMaxDiff) GridHeight = ++GridWidth;
					else GridHeight++;
				}
				GenerateField(GridWidth, GridHeight);

				_controller.SetLeftButtonDisabled(false);
				if (_height == MaxGridWidth + GridHeightMaxDiff) _controller.SetRightButtonDisabled(true);
			}
			else
			{
				if (GridWidth > MinGridWidth || GridHeight > GridWidth)
				{
					if (GridHeight == GridWidth) GridHeight = --GridWidth + GridHeightMaxDiff;
					else GridHeight--;
				}
				GenerateField(GridWidth, GridHeight, true);
				_controller.SetRightButtonDisabled(false);
				if (_width == MinGridWidth && GridHeight == _width) _controller.SetLeftButtonDisabled(true);
			}
			
			_controller.PauseTimer(true);
			
			GlobalSettings.StoredPreferences.RootSection.SetFloat("f_width", GridWidth);
			GlobalSettings.StoredPreferences.RootSection.SetFloat("f_height", GridHeight);
			GlobalSettings.StoredPreferences.Save();
		}
		private void AnimationFinished(string name)
		{
			switch (name)
			{
				case "InteractiveAreaFadeOutReversed":
					GenerateField(GridWidth, GridHeight);
					break;
				case "InteractiveAreaFadeOut":
					GenerateField(GridWidth, GridHeight, true);
					break;
				case "ReferenceFadeOut":
					_gridActive = true;
					break;
				case "InteractiveAreaFadeInReversed":
				case "InteractiveAreaFadeIn":
					_gridActive = _refImage.Modulate.a is not 1f;
					break;
				case "ReferenceFadeIn":
					_gridActive = false;
					break;
			}
		}
		#endregion
	}
}