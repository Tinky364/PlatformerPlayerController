using Godot;

namespace UI
{
    public class Panel<T> : Control where T : Gui
    {
        [Export]
        private NodePath _guiPath;
        [Export]
        private NodePath _firstFocusControlPath;

        protected T Gui { get; private set; }
        protected Control FocusControl { get; private set; }

        public override void _Ready()
        {
            Gui = GetNode<T>(_guiPath);
            if (_firstFocusControlPath != null) FocusControl = GetNode<Button>(_firstFocusControlPath);
        }
    }
}
