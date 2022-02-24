using Godot;

namespace Manager
{
    public class InputManager : Singleton<InputManager>
    {
        public static bool ActionInputsLocked { get; private set; } = false;
        public static bool GuiInputsLocked { get; private set; } = false;

        public override void _EnterTree() { SetSingleton(); }

        public override void _Ready()
        {
            Input.Singleton.Connect("joy_connection_changed", this, nameof(OnJoyConnectionChanged));
        }

        private void OnJoyConnectionChanged(int deviceId, bool connected)
        {
            if (connected) GD.Print(Input.GetJoyName(deviceId));
            else GD.Print("Keyboard");
        }

        public static bool IsJustPressed(string action) =>
            !ActionInputsLocked && Input.IsActionJustPressed(action);
        
        public static bool IsPressed(string action) =>
            !ActionInputsLocked && Input.IsActionPressed(action);

        public static float GetStrength(string action)
        {
            if (ActionInputsLocked) return 0;
            return Input.GetActionStrength(action);
        }

        public void LockAllInputs(bool value)
        {
            LockActionInputs(value);
            LockGuiInputs(value);
        }

        public void LockActionInputs(bool value)
        {
            ActionInputsLocked = value;
        }

        public void LockGuiInputs(bool value)
        {
            GuiInputsLocked = value;
            GetTree().Root.GuiDisableInput = value;
        }
    }
}