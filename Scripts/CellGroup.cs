using Godot;
using System;

namespace Fifteen.Scripts {
    public class CellGroup : Node2D
    {
        private MainScene _mainScene;
        public override void _Ready() 
        {
            _mainScene = GetTree().Root.GetNode<MainScene>("Main Scene");
        }

        public override void _Draw() 
        {
            for (int i = 0; i < _mainScene.GridHeight; i++) 
            {
                for (int j = 0; j < _mainScene.GridWidth; j++) 
                {
                    DrawRect(
                        new Rect2(_mainScene.BorderMargin + new Vector2(_mainScene.CellSize * j, _mainScene.CellSize * i), 
                        new Vector2(_mainScene.CellSize, _mainScene.CellSize)), 
                        new Color(1, 1, 1), 
                        false, 
                        _mainScene.BorderWidth
                    );
                }   
            }
        }
    } 
}