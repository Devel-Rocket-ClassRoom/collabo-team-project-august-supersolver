using PPS.Core;
using UnityEngine;

// UnityEngine 에도 같은 이름이 있다(SystemInfo.deviceType).
using DeviceType = PPS.Core.DeviceType;

namespace PPS.MapEditor
{
    /// <summary>
    /// 편집 중에만 보이는 것의 색.
    /// 오브젝트의 모양·크기는 SimStyle 이 든다 —
    /// 편집 화면과 게임이 같은 것을 보아야 한다.
    /// </summary>
    [CreateAssetMenu(fileName = "MapEditStyle", menuName = "PPS/Map Edit Style")]
    public sealed class MapEditStyle : ScriptableObject
    {
        public SimStyle Sim;

        [Header("편집 가시성 — 편집 중에만 보이는 것들")]
        public Color Selected = Color.white;

        public Color Vertex = new Color32(0x3D, 0x8B, 0xFF, 0xFF);
        public Color Bounds = new Color32(0x9A, 0x9A, 0x9A, 0x99);
        public Color Scale = new Color32(0xFF, 0x6B, 0x2C, 0xFF);

        /// 고른 장치가 미치는 범위. 겹쳐 보이게 반투명이다.
        public Color Reach = new Color32(0xFF, 0x6B, 0x2C, 0x30);

        /// 삽입 모드에서 고른 도형의 색. 누르기 전에 알린다.
        public Color Insert = new Color32(0x3D, 0x8B, 0xFF, 0xFF);

        /// <summary>
        /// 미치는 범위를 따로 그릴 것인가.
        /// 가시는 몸이 곧 범위라 겹쳐 그리면 뜻이 없다.
        /// </summary>
        public static bool HasReach(in DeviceData device) =>
            device.Type != DeviceType.Spike && device.Radius > 0f;
    }
}
