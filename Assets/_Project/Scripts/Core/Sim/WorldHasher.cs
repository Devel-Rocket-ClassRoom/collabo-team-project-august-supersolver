using System.Runtime.InteropServices;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 월드 상태의 지문.
    /// 비트 단위로 비교한다 — 반올림하면
    /// 미세한 갈라짐이 오차로 흡수된다.
    /// </summary>
    public static class WorldHasher
    {
        const ulong FnvOffsetBasis = 14695981039346656037UL;
        const ulong FnvPrime = 1099511628211UL;

        /// <summary>바디를 등록 순서대로 훑는다.</summary>
        public static ulong Hash(SimWorld world)
        {
            ulong h = FnvOffsetBasis;
            var bodies = world.Bodies;

            MixInt(ref h, bodies.Count);

            for (int i = 0; i < bodies.Count; i++)
            {
                var b = bodies[i];
                if (b == null)
                {
                    // 파괴된 바디도 자리를 차지한다.
                    MixInt(ref h, int.MinValue);
                    continue;
                }

                Vector2 p = b.position;
                Vector2 v = b.linearVelocity;

                MixFloat(ref h, p.x);
                MixFloat(ref h, p.y);
                MixFloat(ref h, b.rotation);
                MixFloat(ref h, v.x);
                MixFloat(ref h, v.y);
                MixFloat(ref h, b.angularVelocity);
                MixInt(ref h, b.IsSleeping() ? 1 : 0);
            }

            return h;
        }

        static void MixFloat(ref ulong h, float value)
        {
            var bits = new FloatBits { F = value };
            MixInt(ref h, bits.I);
        }

        static void MixInt(ref ulong h, int value)
        {
            unchecked
            {
                for (int shift = 0; shift < 32; shift += 8)
                {
                    h ^= (byte)(value >> shift);
                    h *= FnvPrime;
                }
            }
        }

        /// <summary>
        /// 할당 없이 float 비트를 읽는 공용체.
        /// BitConverter 는 스텝마다 배열을 만든다.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        struct FloatBits
        {
            [FieldOffset(0)] public float F;
            [FieldOffset(0)] public int I;
        }
    }
}
