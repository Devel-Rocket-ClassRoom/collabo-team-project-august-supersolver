using PPS.Core;
using UnityEngine;

namespace PPS.MapEditor
{
    public static class DeviceTransformGeometry
    {
        public static float RotationRadius(IDeviceData device, float handleRadius) =>
            Mathf.Max(device.DrawRadius + handleRadius * 3f, handleRadius * 6f);

        public static bool HitsRing(Vector2 pointer, Vector2 center, float radius, float tolerance) =>
            Vector2.Distance(pointer, center) > Mathf.Min(radius * 0.5f, tolerance)
            && Mathf.Abs(Vector2.Distance(pointer, center) - radius) <= tolerance;

        public static float Drag(DeviceEditKind kind, Vector2 center, Vector2 from,
            Vector2 to, float original)
        {
            if (kind == DeviceEditKind.Radius)
            {
                float radius = original + Vector2.Distance(to, center) - Vector2.Distance(from, center);
                return radius;
            }
            if ((to - center).sqrMagnitude < 0.000001f) return original;
            float angle = original + Mathf.DeltaAngle(Direction(from - center), Direction(to - center));
            return Mathf.Repeat(angle, 360f);
        }

        static float Direction(Vector2 direction) => Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }
}
