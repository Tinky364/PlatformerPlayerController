using Godot;

namespace Game.Service.Debug
{
    public class DebugVector
    {
        private readonly Node2D _follow;
        private readonly string _property;
        private readonly float _scale;
        private readonly float _width;
        private readonly Color _color;

        public DebugVector(Node2D follow, string property, float scale, float width, Color color)
        {
            _follow = follow;
            _property = property;
            _scale = scale;
            _width = width;
            _color = color;
        }
        
        public void Draw(DebugDraw debugDraw)
        {
            Vector2 start = _follow.GlobalPosition;
            Vector2 end = _follow.GlobalPosition + (Vector2)_follow.Get(_property) * _scale;
            if (start.DistanceSquaredTo(end) <= 0.1f) return;
            debugDraw.DrawLine(start, end, _color, _width);
            debugDraw.DrawTriangle(end, start.DirectionTo(end), _width * 2, _color);
        }
    }
}
