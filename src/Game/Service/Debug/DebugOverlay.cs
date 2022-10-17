using Godot;
using Game.Abstraction;

namespace Game.Service.Debug
{
    public class DebugOverlay : CanvasLayer, ISingleton
    {
        public static DebugOverlay Instance { get; private set; }
        public DebugDraw Draw { get; private set; }

        public DebugOverlay Init()
        {
            Instance = this;
            Draw = GetNode<DebugDraw>("DebugDraw").Init();
            if (!InputMap.HasAction("toggle_debug"))
            {
                InputMap.AddAction("toggle_debug");
                InputEventKey inputEventKey = new InputEventKey();
                inputEventKey.Scancode = (uint)KeyList.Backslash;
                InputMap.ActionAddEvent("toggle_debug", inputEventKey);
            }
            return this;
        }

        public override void _Input(InputEvent @event)
        {
            base._Input(@event);
            if (@event.IsActionPressed("toggle_debug"))
            {
                foreach (Control control in GetChildren())
                {
                    control.Visible = !control.Visible;
                }
            }
        }
    }
}
