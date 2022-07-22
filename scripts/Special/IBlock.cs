using Godot;
namespace Fifteen.Scripts.Special
{
    public interface IBlock
    {
        public int NumberValue { get; set; }
        public int Column { get; set; }
        public int Row { get; set; }
        public bool IsBeingAnimated { get; set; }
        public Vector2 Size { get; set; }
        public Vector2 Pos { get; set; }
    }
}
