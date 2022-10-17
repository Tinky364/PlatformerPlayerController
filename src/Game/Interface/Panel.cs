using Godot;

namespace Game.Interface
{
    public class Panel : Control
    {
        [Export]
        private NodePath _firstFocusControlPath;

        protected Control FocusControl { get; private set; }

        public Panel Init()
        {
            if (_firstFocusControlPath != null) FocusControl = GetNode<Button>(_firstFocusControlPath);
            return this;
        }
    }
}
