using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fifteen.Scripts;
using Fifteen.Scripts.Storage;
using Godot;
using Vector2 = Godot.Vector2;


namespace Fifteen.Scripts;

public class MainScene : Node
{
	private PackedScene _cellScene;
	private PackedScene _blockScene;
	private PackedScene _spriteBlockScene;

	private Node2D _cellGroup, _blockGroup;
	private Tween _tween;
	private Controller _controller;
	private AudioStreamPlayer _impactPlayer;
	private AnimationPlayer _animationPlayer;
	private Sprite _refImage;

	private IBlock[,] _blocks = new IBlock[0, 0];
	private float _borderWidth;
	private float _cellSize;
	
	private const int MinGridWidth = 3;
	private const int MaxGridWidth = 7;
	private const int GridHeightMaxDiff = 2;
	
	private bool _gameActive;
	
	private int _width, _height;
	private int _imageCount = 5;

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

	public bool ImageMode = true;

	public override void _Ready()
	{
		_cellScene = GD.Load<PackedScene>("res://scenes/Cell.tscn");
		_blockScene = GD.Load<PackedScene>("res://scenes/Block.tscn");
		_spriteBlockScene = GD.Load<PackedScene>("res://scenes/SpriteBlock.tscn");
		
		_cellGroup = GetNode<Node2D>("InteractiveArea/CellGroup");
		_blockGroup = GetNode<Node2D>("InteractiveArea/BlockGroup");
		_tween = GetNode<Tween>("Tween");
		_controller = GetNode<Controller>("CanvasLayer");
		_controller.MoveButtonPressedEvent += MoveButtonPressed;
		_impactPlayer = GetNode<AudioStreamPlayer>("ImpactPlayer");
		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		_refImage = GetNode<Sprite>("InteractiveArea/ReferenceImage");
		
		Preferences.LoadData();
		GridWidth = Preferences.RootSection.GetInt32("f_width", 4, MaxGridWidth, MinGridWidth);
		GridHeight = Preferences.RootSection.GetInt32("f_height", GridWidth, GridWidth + GridHeightMaxDiff, GridWidth);

		if (GridWidth == MinGridWidth && GridHeight == MinGridWidth) 
			_controller.SetLeftButtonDisabled(true);
		else if (GridWidth == MaxGridWidth && GridHeight == MaxGridWidth + GridHeightMaxDiff)
			_controller.SetRightButtonDisabled(true);

		GenerateField(GridWidth, GridHeight);
	}

	#region Logic
	public void GenerateField(int width, int height, bool reverseAnimation = false)
	{
		if (_blocks.Length != 0)
		{
			_gameActive = false;
			_blocks = new IBlock[0, 0];
			_animationPlayer.CurrentAnimation = $"InteractiveAreaFadeOut{(reverseAnimation ? "" : "Reversed")}";
			_animationPlayer.Play();
			return;
		}

		var random = new Random();
		PackedScene scene = ImageMode ? _spriteBlockScene : _blockScene;

		_blocks = new IBlock[height, width];
		var plainArray = new int[_blocks.Length];
		List<int> numbers = new();

		_cellSize = (GetViewport().Size.x - 48) / (width + (width < 5 && height > width ? 1 : 0));
		_borderWidth = 3f;
		float hue = (float)random.NextDouble(), saturationStep = 1f / _blocks.Length;
		int emptyCellRow = 0;

		foreach (Node child in _cellGroup.GetChildren() + _blockGroup.GetChildren()) child.QueueFree(); 
		
		_blockGroup.Position = _cellGroup.Position = new Vector2(GetViewport().Size.x  / 2 - _cellSize * width / 2f - _borderWidth / 2, GetViewport().Size.y  / 2 - _cellSize * height / 2f - _borderWidth / 2);
		Texture texture = null;
		Vector2 textureStartVector = new Vector2();
			
		if (ImageMode)
		{
			texture = GD.Load<Texture>($"res://sprites/images/{random.Next(_imageCount)}.jpg");
			textureStartVector = new Vector2(texture.GetWidth() / 2f - _cellSize * width / 2, 0);
			_refImage.Position = new Vector2(_borderWidth, _borderWidth ) + _cellGroup.Position;
			_refImage.Texture = texture;
			_refImage.RegionRect = new Rect2(textureStartVector, new Vector2(_cellSize * _blocks.GetLength(1), _cellSize * _blocks.GetLength(0)));
		}

		for (int i = 0; i < _blocks.Length; i++) numbers.Add(i);
		
		for (int i = 0, count = 0; i < _blocks.GetLength(0); i++)
		{
			for (int j = 0; j < _blocks.GetLength(1); j++)
			{
				var cellInstance =_cellScene.Instance<ReferenceRect>();
				IBlock blockInstance = scene.Instance<IBlock>();
				_blocks[i, j] = blockInstance;
				
				blockInstance.ArrayPositionX = j;
				blockInstance.ArrayPositionY = i;

				cellInstance.RectSize = new Vector2(_cellSize, _cellSize);
				cellInstance.BorderWidth = _borderWidth;
				
				var margin = new Vector2(cellInstance.BorderWidth / 2, cellInstance.BorderWidth / 2);
				cellInstance.RectPosition = margin + new Vector2(_cellSize * j, _cellSize * i);
				blockInstance.Pos = cellInstance.RectPosition;

				_cellGroup.AddChild(cellInstance);

				var randIndex = random.Next(numbers.Count);
				
				if (numbers[randIndex] != 0)
				{
					blockInstance.Size = cellInstance.RectSize;
					_blockGroup.AddChild(blockInstance as Node);
					plainArray[count++] = blockInstance.NumberValue = numbers[randIndex];
					
					if (blockInstance is SpriteBlock sprite)
					{
						sprite.Texture = texture;
						int h = _blocks.GetLength(0), w = _blocks.GetLength(1), diff = h - (h - w);
						int row = (sprite.NumberValue - 1) / diff;
						int col = sprite.NumberValue - 1 - diff * row;
						sprite.GetNode<Label>("Label").Text = sprite.NumberValue.ToString();
						
						sprite.RegionRect =
							new Rect2(new Vector2( textureStartVector.x + col * _cellSize, row * _cellSize),
								new Vector2(_cellSize, _cellSize));
					}
					else if (blockInstance is Block block)
                    {
						block.Color = Color.FromHsv(hue, blockInstance.NumberValue * saturationStep, .4f);
						block.Number.RectSize = blockInstance.Size;
						block.Number.GetFont("custom_font").Set("size", _cellSize / 2f);
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
			(_blocks[row, column].ArrayPositionX, _blocks[row, nextColumn].ArrayPositionX) = (
				_blocks[row, nextColumn].ArrayPositionX, _blocks[row, column].ArrayPositionX);
			(_blocks[row, column].Pos, _blocks[row, nextColumn].Pos) = (
				_blocks[row, nextColumn].Pos, _blocks[row, column].Pos);
		}

		_animationPlayer.CurrentAnimation = $"InteractiveAreaFadeIn{(reverseAnimation ? "" : "Reversed")}";
		_animationPlayer.Play();
	}
	public bool IsOrderCorrect()
	{
		for (int i = 0; i < _blocks.GetLength(0); i++)
		{
			for (int j = 0; j < _blocks.GetLength(1); j++)
			{
				if (j > 0)
				{
					if (_blocks[i, j].NumberValue - _blocks[i, j - 1].NumberValue != 1) return false;
				}
				else if (i > 0)
				{
					if (_blocks[i, j].NumberValue - _blocks[i - 1, _blocks.GetLength(1) - 1].NumberValue != 1) return false;
				}
			}
		}

		return true;
	}
	public bool TryToMove(IBlock block)
	{
		if (!_gameActive) return false;
		
		int x = block.ArrayPositionX, y = block.ArrayPositionY;
		Vector2? movement = null;

		if (y + 1 < _blocks.GetLength(0))
		{
			if (_blocks[y + 1, x].NumberValue == _blocks.Length)
			{
				(_blocks[y + 1, x], _blocks[y, x]) = (_blocks[y, x], _blocks[y + 1, x]);
				block.ArrayPositionY++;
				movement = Vector2.Down;
			}
		}

		if (y - 1 >= 0)
		{
			if (_blocks[y - 1, x].NumberValue == _blocks.Length)
			{
				(_blocks[y - 1, x], _blocks[y, x]) = (_blocks[y, x], _blocks[y - 1, x]);
				block.ArrayPositionY--;
				movement = Vector2.Up;
			}
		}

		if (x - 1 >= 0)
		{
			if (_blocks[y, x - 1].NumberValue == _blocks.Length)
			{
				(_blocks[y, x - 1], _blocks[y, x]) = (_blocks[y, x], _blocks[y, x - 1]);
				block.ArrayPositionX--;
				movement = Vector2.Left;
			}
		}
		
		if (x + 1 < _blocks.GetLength(1))
		{
			if (_blocks[y, x + 1].NumberValue == _blocks.Length)
			{
				(_blocks[y, x + 1], _blocks[y, x]) = (_blocks[y, x], _blocks[y, x + 1]);
				block.ArrayPositionX++;
				movement = Vector2.Right;
			}
		}

		if (movement is not null)
		{
			_controller.AddMove();
			if (!_controller.TimerActive)
			{
				Task.Run(() => { _controller.StartTimer(); });
			}
			block.IsBeingAnimated = true;

			var startVector = new Vector2(block.Size.x * x + _borderWidth / 2, block.Size.y * y + _borderWidth / 2);
			_tween.InterpolateProperty(block as Node, block is Block ? "rect_position" : "position", block.Pos,
				startVector + movement * block.Size, .5f, Tween.TransitionType.Cubic);
			_tween.Start();
			
			Task.Run(() =>
			{
				if (!IsOrderCorrect()) return;
				_tween.Start();
				_gameActive = false;
				_controller.PauseTimer();
			});

			return true;
		}
		
		return false;
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
		Preferences.RootSection.SetFloat("f_width", GridWidth);
		Preferences.RootSection.SetFloat("f_height", GridHeight);
		Preferences.SaveData();
        
		_controller.PlayClickSound();
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
			case "InteractiveAreaFadeInReversed":
			case "InteractiveAreaFadeIn":
				_gameActive = true;
				break;
		}
	}
	#endregion
}