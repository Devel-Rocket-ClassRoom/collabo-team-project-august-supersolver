using System.Collections.Generic;
using PPS.Core;
using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 레벨을 화면에 세운다 — 그릴 수 있는 영역과
    /// 지형·장치·목표·별·공. LevelData 만 보므로 월드를
    /// 짓지 않는다. 시뮬 중 위치 갱신은 상태 머신 몫이다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelView : MonoBehaviour
    {
        /// 지형 시각 두께. 획과 같은 값이다 — 둘 다
        /// 두께 없는 선분이라 굵기로 갈리면 오해가 된다.
        const float TerrainWidth = 0.12f;

        /// 킬라인이 뻗는 길이. 영역 폭의 배수다 —
        /// 끝이 보이면 넘어가도 되는 구간처럼 읽힌다.
        const float KillLineSpan = 4f;

        const float KillLineWidth = 0.06f;

        /// 장치 본체 지름. DeviceData 에 몸집이 없어 앱이
        /// 정한다 — Radius 는 영향 범위지 크기가 아니다.
        /// 계약에 몸집이 생기면 이 상수는 사라진다.
        const float DeviceSize = 0.6f;

        /// 링이라 안쪽은 안 가리지만 띠가 지나는 자리의
        /// 지형은 가린다. 밝은 판 위에서는 이보다 옅으면
        /// 띠 자체가 안 보인다.
        const float DeviceRangeAlpha = 0.6f;

        /// 그릴 수 있는 곳. 맵 에디터의 캔버스 배경과
        /// 같은 색이라 저작자가 보던 판이 그대로 온다.
        static readonly Color PlayAreaColor = new Color32(0xFF, 0xD4, 0x99, 0xFF);

        // 넷은 맵 에디터 값 그대로다. 인스펙터로 열어 두면
        // 값의 주인이 씬이 되어, 저작자 화면과 어긋나도
        // 코드만 보고는 알 수 없다.
        static readonly Color TerrainColor = new Color32(0x23, 0x25, 0x2B, 0xFF);
        static readonly Color BallColor = new Color32(0xC0, 0x14, 0x3C, 0xFF);
        static readonly Color GoalColor = new Color32(0x0E, 0x7A, 0x3C, 0xFF);
        static readonly Color StarColor = new Color32(0xE8, 0x9A, 0x1C, 0xFF);

        // 둘은 맵 에디터가 안 그리는 것이라 여기서 정한다.
        static readonly Color KillLineColor = new Color32(0x8A, 0x1C, 0x1C, 0xFF);
        static readonly Color DeviceColor = new Color32(0x0B, 0x6E, 0x6E, 0xFF);

        /// 지어 둔 것 전부. 레벨이 바뀌면 통째로 버린다.
        readonly List<GameObject> _parts = new List<GameObject>();

        /// <summary>
        /// 레벨이 정해지면 화면도 정해진다. 영역은
        /// 카메라가 맞추는 것과 같은 함수에서 나온다 —
        /// 갈라지면 못 그리는 자리가 그려 보인다.
        /// </summary>
        public void SetLevel(LevelData level)
        {
            Clear();
            if (level == null) return;

            Rect area = LevelDataArea.Calculate(level);
            AddQuad("PlayArea", area.center, area.size, 0f,
                PlayAreaColor, RenderOrder.PlayArea);

            AddQuad("KillLine",
                new Vector2(area.center.x, level.KillY),
                new Vector2(area.width * KillLineSpan, KillLineWidth),
                0f, KillLineColor, RenderOrder.KillLine);

            for (int i = 0; i < level.Terrain.Count; i++)
                AddSegment(level.Terrain[i], i);

            for (int i = 0; i < level.Devices.Count; i++)
                AddDevice(level.Devices[i], i);

            // 셋의 크기는 LevelData 의 const 다 — 파일에 남은
            // BallRadius·GoalRadius 는 무시된다. 모양·색은 맵
            // 에디터와 맞춰 저작자와 같은 화면을 준다.
            AddDot("Goal", level.GoalPosition, LevelData.GoalRadius * 2f,
                ShapeSprites.Disc, GoalColor, RenderOrder.Goal);

            for (int i = 0; i < level.Stars.Count; i++)
                AddDot($"Star_{i}", level.Stars[i], LevelData.StarCaptureRadius * 2f,
                    ShapeSprites.Disc, StarColor, RenderOrder.Star);

            AddDot("Ball", level.BallStart, LevelData.BallRadius * 2f,
                ShapeSprites.Disc, BallColor, RenderOrder.Ball);
        }

        void AddSegment(in StaticSegment segment, int index)
        {
            Vector2 ab = segment.B - segment.A;

            // 길이에 두께를 더해 끝을 사각으로 넓힌다.
            // 원을 구운 24각형은 이음매마다 틈이 남는다.
            AddQuad($"Terrain_{index}",
                (segment.A + segment.B) * 0.5f,
                new Vector2(ab.magnitude + TerrainWidth, TerrainWidth),
                Mathf.Atan2(ab.y, ab.x) * Mathf.Rad2Deg,
                TerrainColor, RenderOrder.Terrain);
        }

        void AddDevice(in DeviceData device, int index)
        {
            AddDot($"DeviceRange_{index}", device.Position, device.Radius * 2f,
                ShapeSprites.Ring, Fade(DeviceColor, DeviceRangeAlpha),
                RenderOrder.Device);

            AddDot($"Device_{index}", device.Position, DeviceSize,
                ShapeSprites.Disc, DeviceColor, RenderOrder.Device + 1);
        }

        void AddQuad(string name, Vector2 center, Vector2 size, float angle,
            Color color, int order)
        {
            Transform part = Add(name, ShapeSprites.Quad, color, order);

            part.SetPositionAndRotation(center, Quaternion.Euler(0f, 0f, angle));
            part.localScale = new Vector3(size.x, size.y, 1f);
        }

        void AddDot(string name, Vector2 center, float diameter, Sprite sprite,
            Color color, int order)
        {
            Transform part = Add(name, sprite, color, order);

            part.position = center;
            part.localScale = Vector3.one * diameter;
        }

        /// 스프라이트가 1wu 라 스케일이 곧 월드 크기다.
        Transform Add(string name, Sprite sprite, Color color, int order)
        {
            var part = new GameObject(name);
            part.transform.SetParent(transform, false);

            var renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;

            _parts.Add(part);
            return part.transform;
        }

        /// <summary>
        /// 통째로 버린다. 그릴 것이 열 몇 개라 부분
        /// 갱신은 이득이 없고 경로만 둘로 갈린다.
        /// </summary>
        void Clear()
        {
            for (int i = 0; i < _parts.Count; i++)
            {
                // 재생 중이 아니면 Destroy 는 미뤄지는 게
                // 아니라 거절된다. EditMode 테스트가 그렇다.
                if (Application.isPlaying) Destroy(_parts[i]);
                else DestroyImmediate(_parts[i]);
            }

            _parts.Clear();
        }

        static Color Fade(Color color, float alpha) =>
            new Color(color.r, color.g, color.b, alpha);
    }
}
