using Godot;
using Manager;

namespace UI
{
    public class TestSceneGui : Gui
    {
        [Signal]
        private delegate void PausePanelRequested();
        
        public override void _Process(float delta)
        {
            if (InputManager.IsJustPressed("ui_end"))
            {
                EmitSignal(nameof(PausePanelRequested));
            }   
        }
    }
}
