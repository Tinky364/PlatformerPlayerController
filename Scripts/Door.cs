using Godot;
using System;
using NavTool;

public class Door : NavBody2D
{
    private Area2D _triggerArea;
    
    public override void _Ready()
    {
        base._Ready();
        _triggerArea = GetNode<Area2D>("../TriggerArea");
        _triggerArea.Connect("body_entered", this, nameof(OnTriggerred));
    }

    private void OnTriggerred(Node node)
    {
        if (!(node is Player)) return;
        GD.Print("a");
        MoveLerp(LerpingMode.Y, GlobalPosition - new Vector2(0, 32f), 2f);
    }
    
    public override void _PhysicsProcess(float delta)
    {
        base._PhysicsProcess(delta);
        Velocity = MoveAndSlideInArea(Velocity, delta, Vector2.Up);
    }
}
