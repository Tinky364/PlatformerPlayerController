using Godot;
using Game.Abstraction;

namespace Game.Service
{
    public class InputInvoker : Node, ISingleton
    {
        public static InputInvoker Singleton { get; private set; }
        public static bool LevelInputsLocked { get; private set; } = false;
        public static bool InterfaceInputsLocked { get; private set; } = false;

        public InputInvoker Init()
        {
            Singleton = this;
            Input.Singleton.Connect("joy_connection_changed", this, nameof(OnJoyConnectionChanged));
            return this;
        }
        
        private void OnJoyConnectionChanged(int deviceId, bool connected)
        {
            if (connected) GD.Print(Input.GetJoyName(deviceId));
            else GD.Print("Keyboard");
        }
        
        public static bool IsPressed(string action) =>
            !LevelInputsLocked && Input.IsActionJustPressed(action);
        
        public static bool IsPressing(string action) =>
            !LevelInputsLocked && Input.IsActionPressed(action);

        public static bool IsReleased(string action) =>
            !LevelInputsLocked && Input.IsActionJustReleased(action);

        public static float GetStrength(string action) =>
            LevelInputsLocked ? 0 : Input.GetActionStrength(action);

        public static float GetAxis(string negativeAction, string positiveAction) => 
            LevelInputsLocked ? 0 : Input.GetAxis(negativeAction, positiveAction);

        public void LockAllInputs(bool value)
        {
            LockLevelInputs(value);
            LockInterfaceInputs(value);
        }

        public void LockLevelInputs(bool value)
        {
            LevelInputsLocked = value;
        }

        public void LockInterfaceInputs(bool value)
        {
            InterfaceInputsLocked = value;
            App.Singleton.Root.GuiDisableInput = value;
        }
    }
}
