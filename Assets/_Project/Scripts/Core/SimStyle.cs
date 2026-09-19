using System.Collections.Generic;
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
        }

        public Shapes Sprites = new Shapes();

        /// <summary>장치 한 종류의 모양.</summary>
        [System.Serializable]
        public sealed class DeviceVisual
        {
            [Tooltip("이 이미지 설정을 적용할 장치 종류입니다.")]
            public DeviceType Type;
            [Tooltip("게임과 맵에디터에서 표시할 장치 본체 이미지입니다. 크기는 장치 데이터에 맞춰 자동 조절됩니다.")]
            public Sprite Sprite;
            [Tooltip("맵에디터에서 선택한 장치의 효과 범위를 채우는 이미지입니다. 폭탄·바람의 실제 반경에 맞춰 표시됩니다.")]
            public Sprite RangeFill;
            [Tooltip("맵에디터의 효과 범위 테두리와 반경 조절 도구에 쓰는 이미지입니다. 장치 반경에 맞춰 크기가 조절됩니다.")]
            public Sprite RangeOutline;
            [Tooltip("맵에디터에서 장치의 진행 방향을 표시하는 이미지입니다. 오른쪽을 향하는 원본을 사용하면 장치 각도에 맞춰 회전합니다. 바람처럼 방향이 있는 장치에 사용합니다.")]
            public Sprite DirectionArrow;
            [Tooltip("폭발 순간 표시한 뒤 자동으로 사라지는 이미지입니다. 일반 폭탄은 폭발 반경, 파편 폭탄은 본체 크기에 맞춰 표시됩니다. 비워 두면 폭발 이미지를 표시하지 않습니다.")]
            public Sprite Explosion;
        }

        /// <summary>
        /// 장치 종류별 모양. 새 장치는 여기 항목 하나로 끝난다 —
        /// 크기는 데이터가 든다(IDeviceData.DrawRadius).
        /// </summary>
        public List<DeviceVisual> Devices = new List<DeviceVisual>();

        /// 공이 킬라인 아래로 떨어졌을 때 터뜨릴 것.
        /// 에셋이라 사본을 만들어 재생한다.
        public ParticleSystem KillEffect;

        /// 킬라인을 따라 까는 그림. 가로로 반복해 판 너비를
        /// 채운다 — 길쭉해도 되므로 1wu 인 Shapes 와 따로 둔다.
        public Sprite KillLine;

        public Sprite TerrainSprite;

        /// 지형 스프라이트에 입히는 색.
        public Color Terrain = new Color32(0x23, 0x25, 0x2B, 0xFF);

        /// <summary>
        /// 덧칠하지 않는다. 아틀라스 그림의 제 색이 나온다.
        /// 오브젝트는 그림으로 구분한다 — 색은 편집 중에만 쓴다.
        /// </summary>
        public static readonly Color Plain = Color.white;

        /// <summary>
        /// 안 꽂힌 종류는 null 이다. 아직 그리지 않은 장치까지
        /// 막으면 아트를 기다리느라 작업이 선다.
        /// </summary>
        public Sprite SpriteOf(DeviceType type) => VisualOf(type)?.Sprite;

        public DeviceVisual VisualOf(DeviceType type)
        {
            for (int i = 0; i < Devices.Count; i++)
                if (Devices[i] != null && Devices[i].Type == type) return Devices[i];

            return null;
        }

        /// 방향이 없는 장치는 돌리지 않는다.
        public static float AngleOf(IDeviceData device) =>
            device is IHasFacing facing ? facing.FacingDegrees : 0f;
    }
}
