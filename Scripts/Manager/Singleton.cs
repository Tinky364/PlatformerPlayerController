using Godot;

public class Singleton<T> : Node where T : Singleton<T>
{
    public static T S { get; private set; }

    protected void SetSingleton()
    {
        if (S != null && S != this)
        {
            GD.Print($"Multiple instances of singleton class named {typeof(T)}!");
            QueueFree();
        }
        else S = (T)this;
    }
}
