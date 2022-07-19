using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Vector2 = Godot.Vector2;

namespace Fifteen.scripts;

public class MainScene : Node
{
	private PackedScene _cellScene;
	private PackedScene _blockScene;

	private Node2D _cellGroup, _blockGroup;
	private Tween _blockAnimator;
	private Controller _controller;
	private Node2D _interactiveArea;
	private AudioStreamPlayer _impactPlayer;

	private Block[,] _blocks = new Block[0, 0];
	private float _borderWidth;
	private float _cellSize;

	private bool _gameActive = false;

	public override void _Ready()
	{
		_cellScene = GD.Load<PackedScene>("res://scenes/Cell.tscn");
		_blockScene = GD.Load<PackedScene>("res://scenes/Block.tscn");
		
		_cellGroup = GetNode<Node2D>("InteractiveArea/CellGroup");
		_blockGroup = GetNode<Node2D>("InteractiveArea/BlockGroup");
		_blockAnimator = GetNode<Tween>("BlockAnimator");
		_controller = GetNode<Controller>("CanvasLayer");
		_interactiveArea = GetNode<Node2D>("InteractiveArea");
		_impactPlayer = GetNode<AudioStreamPlayer>("ImpactPlayer");
		
		GenerateField(_controller.GridWidth, _controller.GridHeight);
	}

	public void GenerateField(int width, int height)
	{
		_gameActive = false;

		if (_blocks.Length != 0)
		{
			_blocks = new Block[0, 0];
			_blockAnimator.InterpolateProperty(_interactiveArea, "modulate", _interactiveArea.Modulate,
				new Color(_interactiveArea.Modulate) {a = 0f}, .2f);
			_blockAnimator.Start();
			return;
		}
		
		_blocks = new Block[height, width];
		var plainArray = new int[_blocks.Length];
		List<int> numbers = new();
		
		_cellSize = (GetViewport().Size.x - 48) / (width + (width < 5 && height > width ? 1 : 0));
		_borderWidth = 3f;
		var random = new Random();
		float hue = (float)random.NextDouble(), saturationStep = 1f / _blocks.Length;
		int emptyCellRow = 0;

		foreach (Node child in _cellGroup.GetChildren() + _blockGroup.GetChildren()) child.QueueFree(); 
		
		_blockGroup.Position = _cellGroup.Position = new Vector2(GetViewport().Size.x  / 2 - _cellSize * width / 2f - _borderWidth / 2, GetViewport().Size.y  / 2 - _cellSize * height / 2f - _borderWidth / 2);

		for (int i = 0; i < _blocks.Length; i++) numbers.Add(i);
		
		for (int i = 0, count = 0; i < _blocks.GetLength(0); i++)
		{
			for (int j = 0; j < _blocks.GetLength(1); j++)
			{
				var cellInstance =_cellScene.Instance<ReferenceRect>();
				var blockInstance =  _blockScene.Instance<Block>();
				_blocks[i, j] = blockInstance;
				
				blockInstance.ArrayPositionX = j;
				blockInstance.ArrayPositionY = i;

				cellInstance.RectSize = new Vector2(_cellSize, _cellSize);
				cellInstance.BorderWidth = _borderWidth;
				
				var margin = new Vector2(cellInstance.BorderWidth / 2, cellInstance.BorderWidth / 2);
				cellInstance.RectPosition = margin + new Vector2(_cellSize * j, _cellSize * i);
				blockInstance.RectPosition = cellInstance.RectPosition;

				_cellGroup.AddChild(cellInstance);
				
				var randIndex = random.Next(numbers.Count);

				if (numbers[randIndex] != 0)
				{
					blockInstance.RectSize = cellInstance.RectSize;
					_blockGroup.AddChild(blockInstance);
					plainArray[count++] = blockInstance.NumberValue = numbers[randIndex];
					blockInstance.Color = Color.FromHsv(hue, blockInstance.NumberValue * saturationStep, .4f);
					blockInstance.Number.RectSize = blockInstance.RectSize;
					blockInstance.Number.GetFont("custom_font").Set("size", _cellSize / 2f);
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
			(_blocks[row, column].RectPosition, _blocks[row, nextColumn].RectPosition) = (
				_blocks[row, nextColumn].RectPosition, _blocks[row, column].RectPosition);
		}

		_blockAnimator.InterpolateProperty(_interactiveArea, "modulate", _interactiveArea.Modulate,
			new Color(_interactiveArea.Modulate) { a = 1f }, .2f);
		_blockAnimator.Start();
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

	public bool TryToMove(Block block)
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
			var startVector = new Vector2(block.RectSize.x * x + _borderWidth / 2, block.RectSize.y * y + _borderWidth / 2);
			_blockAnimator.InterpolateProperty(block, "rect_position", block.RectPosition,
				startVector + movement * block.RectSize, .5f, Tween.TransitionType.Cubic);
			_blockAnimator.Start();
			
			Task.Run(() =>
			{
				if (!IsOrderCorrect()) return;
				_blockAnimator.Start();
				_gameActive = false;
				_controller.PauseTimer();
			});

			return true;
		}
		
		return false;
	}

	private void BlockAnimatorCompleted(object node, string path)
	{
		if (node is Block block)
		{
			block.IsBeingAnimated = false;
			_impactPlayer.Play();
		}
		else if (node is Node2D {Name: "InteractiveArea"} node2D)
		{
			if (node2D.Modulate.a == 0f)
			{
				GenerateField(_controller.GridWidth, _controller.GridHeight);
			}
			else
			{
				_gameActive = true;
			}
		}
	}

	public override void _Notification(int notification)
	{
		switch (notification)
		{
			case NotificationWmFocusOut:
				if (_controller.TimerActive) _controller.PauseTimer();
				break;
			case NotificationWmFocusIn:
				if (_controller.Moves > 0) _controller.StartTimer();
				break;
		}
	}
}
