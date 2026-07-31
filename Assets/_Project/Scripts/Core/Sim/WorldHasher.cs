using System.Runtime.InteropServices;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 월드 상태의 지문. 결정론 테스트·디싱크 디버깅·실기기 표본 검증이 공유한다.
    ///
    /// **비트 단위로 비교한다.** 부동소수점을 반올림해서 비교하면 미세한 갈라짐이
    /// 오차로 흡수돼 버리고, 그 갈라짐은 수백 스텝 뒤에 눈에 보이는 차이로 자란다.
    /// 결정론은 "거의 같다"가 아니라 "같다"여야 한다.
    /// </summary>
    public static class WorldHasher
    {
        const ulong FnvOffsetBasis = 14695981039346656037UL;
        const ulong FnvPrime = 1099511628211UL;

        /// <summary>바디를 등록 순서대로 훑어 position/rotation/velocity 를 섞는다.</summary>
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
                    // 파괴된 바디도 자리를 차지한다 — 개수가 어긋나는 것 자체가 디싱크 신호다.
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
        /// float 의 비트 패턴을 할당 없이 읽기 위한 공용체.
        /// BitConverter.GetBytes 는 스텝마다 배열을 할당해 솔버 처리량을 갉아먹는다.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        struct FloatBits
        {
            [FieldOffset(0)] public float F;
            [FieldOffset(0)] public int I;
        }
    }
}
