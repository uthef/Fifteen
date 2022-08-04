using Godot;

namespace Fifteen.Models
{
    public interface IDataScene
    {
        object[] Data {get; set;}
        MenuItem[] MenuItems {get; set; }
        Vector2 Direction {get; set; }
        bool LimitedMode {get; set;}
    }
}