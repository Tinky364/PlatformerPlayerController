using System.Collections.Generic;
using Godot;

namespace Game.Service.Debug
{
    public class DebugDraw : Control
    {
        private List<DebugVector> _vectors;
        
        public DebugDraw Init()
        {
            _vectors = new List<DebugVector>();
            return this;
        }

        public override void _Process(float delta)
        {
            base._Process(delta);
            if (!Visible) return;
            Update();
        }

        public override void _Draw()
        {
            base._Draw();
            foreach (DebugVector vector in _vectors)
            {
                vector.Draw(this);
            }
        }

        public void DrawTriangle(Vector2 pos, Vector2 dir, float size, Color color)
        {
            Vector2 a = pos + dir * size;
            Vector2 b = pos + dir.Rotated(2f * Mathf.Pi / 3f) * size;
            Vector2 c = pos + dir.Rotated(4f * Mathf.Pi / 3f) * size;
            Vector2[] points = {a, b, c};
            DrawPolygon(points, new[] {color});
        }

        public void AddVector(Node2D follow, string property, float scale, float width, Color color)
        {
            _vectors.Add(new DebugVector(follow, property, scale, width, color));
        }
    }
}
