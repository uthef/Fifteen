using Godot;
using System;

namespace Fifteen.Nodes
{
    public class MenuButton : Button
    {
        [Export] public Models.MenuItemType ItemType;
        [Export] public string SwapText;

        public bool Toggled {get; private set;} = false;

        public void Toggle()
        {
            (Text, SwapText) = (SwapText, Text);
            Toggled = !Toggled;
        }
    }
}