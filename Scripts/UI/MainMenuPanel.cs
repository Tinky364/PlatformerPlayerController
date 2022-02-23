using Godot;

namespace UI
{
    public class MainMenuPanel : Panel<Gui>
    {
        public override void _Ready()
        {
            base._Ready();
            FocusControl.GrabFocus();
        }
    }
}
