using Godot;

namespace Game.Service.Debug
{
    public static class Log
    {
        public static bool Disabled { get; set; } = false;
        
        public static void Info(params object[] what)
        {
            if (Disabled) return;
            GD.Print(what);
        }

        public static void Warning(string message)
        {
            if (Disabled) return;
            GD.PushWarning(message);
        }
        
        public static void Error(string message)
        {
            if (Disabled) return;
            GD.PushError(message);
        }
    }
}
