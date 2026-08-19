using UnityEngine;

using DeviceType = PPS.Core.DeviceType;

namespace PPS.Core
{
    /// <summary>
    /// 시뮬레이션에 나오는 것을 어떤 모양·색·크기로 그리는가.
    /// 게임과 편집이 같은 것을 보게 하려고 에셋 하나로 뽑아 둔다.
    /// </summary>
    [CreateAssetMenu(fileName = "SimStyle", menuName = "PPS/Sim Style")]
    public sealed class SimStyle : ScriptableObject
    {
        /// <summary>
        /// 아틀라스에서 골라 넣는 모양들.
        /// 색과 이름이 겹쳐 따로 묶는다.
        /// </summary>
        [System.Serializable]
        public sealed class Shapes
        {
            /// 파편. 작고 여럿이라 그림 하나로 돌려 쓴다.
            public Sprite Dot;

            /// 굴러가는 공. 시작 지점 표시도 같은 그림이다.
            public Sprite Ball;

            public Sprite Goal;
            public Sprite Star;
            public Sprite Bomb;
            public Sprite FragBomb;
            public Sprite Spike;
            public Sprite Wind;
        }

        public Shapes Sprites = new Shapes();

        /// 공이 킬라인 아래로 떨어졌을 때 터뜨릴 것.
        /// 에셋이라 사본을 만들어 재생한다.
        public ParticleSystem KillEffect;

        /// 지형 선분. 코드가 만든 흰 사각형이라 색이 있어야 한다.
        public Color Terrain = new Color32(0x23, 0x25, 0x2B, 0xFF);

        /// <summary>
        /// 덧칠하지 않는다. 아틀라스 그림의 제 색이 나온다.
        /// 오브젝트는 그림으로 구분한다 — 색은 편집 중에만 쓴다.
        /// </summary>
        public static readonly Color Plain = Color.white;

        public Sprite SpriteOf(DeviceType type)
        {
            switch (type)
            {
                case DeviceType.FragBomb: return Sprites.FragBomb;
                case DeviceType.Spike: return Sprites.Spike;
                case DeviceType.Wind: return Sprites.Wind;
                default: return Sprites.Bomb;
            }
        }

        /// <summary>
        /// 화면에 그릴 반지름. 콜라이더 크기 그대로다 —
        /// 그림에 여백이 있으면 아틀라스 쪽에서 잘라야 한다.
        /// 보이는 것과 닿는 것이 어긋나면 레벨을 못 만든다.
        /// </summary>
        public static float RadiusOf(in DeviceData device)
        {
            switch (device.Type)
            {
                case DeviceType.Spike:
                    return Mathf.Max(device.Radius, SpikeDevice.MinRadius);

                // 바람은 화살표라 몸 크기가 없다. 구역 안에
                // 들어갈 만한 크기로 그린다.
                case DeviceType.Wind:
                    return Mathf.Max(device.Radius * 0.5f, 0.3f);

                default:
                    return BombDevice.BodyRadius;
            }
        }

        /// 방향이 없는 장치는 돌리지 않는다.
        public static float AngleOf(in DeviceData device) =>
            device.Type == DeviceType.Wind ? device.Angle : 0f;
    }
}
