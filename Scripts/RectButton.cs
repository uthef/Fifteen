using Godot;

namespace Fifteen.Scripts
{
    public class RectButton : ColorRect
    {
        [Export] private bool _enabled = true;
        private string _text;
        [Export] public string DefaultText = "Item";
        [Export] public string ToggleText = "Item";
        
        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                GetNode<Label>("Label").Text = _text;
            }
        }
        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                GetNode<Label>("Label").Modulate = _enabled ? new Color(1f, 1f, 1f)  : new Color(.3f, .3f, .3f);
            }
        }

        public override void _Ready()
        {
            Enabled = Enabled;
            Text = DefaultText;
        }
    }
}