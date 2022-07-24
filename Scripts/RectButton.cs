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
                var label = GetNode<Label>("Label");
                label.Modulate = _enabled ? new Color(label.Modulate) {a = 1} : new Color(label.Modulate) {a = .4f};
            }
        }

        public override void _EnterTree()
        {
            Color = ColorThemes.AppTheme.ForegroundColor;
        }

        public override void _Ready()
        {
            Enabled = Enabled;
            Text = DefaultText;
        }
    }
}