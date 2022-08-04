using Godot;
using System;

namespace Fifteen.Models
{

    public struct MenuItem
    {
        [Export] public MenuItemType Type;
        [Export] public string Text;
        public string SwapText;

        public MenuItem(MenuItemType type, string text, string swapText = null)
        {
            Type = type;
            Text = text;
            SwapText = swapText;
        }
    }
}
