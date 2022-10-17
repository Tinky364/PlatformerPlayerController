using Godot;

namespace Game.Service
{
    public static class Mathff
    {
        public static float DeltaAngle(float current, float target)
        {
            float delta = Repeat(target - current, 360f);
            if (delta > 180f) delta -= 360f;
            return delta;
        }
    
        public static float Repeat(float t, float length)
        {
            return Mathf.Clamp(t - Mathf.Floor(t / length) * length, 0.0f, length);
        }

        public static float DirectionToOrientation(Vector3 direction)
        {
            if (!direction.IsNormalized()) direction = direction.Normalized();
            return Mathf.Rad2Deg(Mathf.Atan2(-direction.x, -direction.z));
        }

        public static Vector3 OrientationToDirection(float orientation)
        {
            float radians = Mathf.Deg2Rad(orientation);
            return new Vector3(Mathf.Sin(radians), 0, Mathf.Cos(radians));
        }
    }
}
