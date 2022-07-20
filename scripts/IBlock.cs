using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fifteen.Scripts
{
    public interface IBlock
    {
        public int NumberValue { get; set; }
        public int ArrayPositionX { get; set; }
        public int ArrayPositionY { get; set; }
        public bool IsBeingAnimated { get; set; }
        public Vector2 Size { get; set; }
        public Vector2 Pos { get; set; }
    }
}
