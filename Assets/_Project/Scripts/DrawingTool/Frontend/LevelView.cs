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

        /// 영역을 두르는 점 한 칸의 목표 길이. 변마다
        /// 칸 수를 반올림해 실제 길이는 조금씩 다르다.
        const float DashPeriod = 0.5f;

        /// 한 칸에서 점이 차지하는 몫. 나머지가 빈칸이다.
        const float DashRatio = 0.55f;

        const float DashWidth = 0.06f;

        /// 죽는 선의 두께. 테마 그림이 없을 때만 쓴다.
        const float KillLineWidth = 0.08f;

        /// 테마 그림이 없을 때 쓰는 장치 본체 지름.
        /// 그림이 있으면 몸집은 SimStyle 이 안다.
        const float DeviceSize = 0.6f;

        /// 링이라 안쪽은 안 가리지만 띠가 지나는 자리의
        /// 지형은 가린다. 밝은 배경 위에서는 이보다 옅으면
        /// 띠 자체가 안 보인다.
        const float DeviceRangeAlpha = 0.6f;

        /// 그릴 수 있는 곳을 두르는 점선. 뒤에 깔린 배경
        /// 아트가 무엇일지 몰라 밝은 그림에도 남게 어둡되,
        /// 지형과 같은 색이면 지형처럼 읽혀 한 단계 옅다.
        static readonly Color PlayAreaColor = new Color32(0x4A, 0x51, 0x60, 0xE0);

        /// 죽는 선. 끊기지 않는 실선이라 색을 못 가려도
        /// 그릴 수 있는 곳의 점선과 갈린다.
        static readonly Color KillLineColor = new Color32(0xD8, 0x2C, 0x2C, 0xF0);

        // 테마 그림이 없을 때만 쓰는 색. 맵 에디터 값
        // 그대로라 그림이 빠져도 저작자가 보던 화면에 가깝다.
        static readonly Color TerrainColor = new Color32(0x23, 0x25, 0x2B, 0xFF);
        static readonly Color BallColor = new Color32(0xC0, 0x14, 0x3C, 0xFF);
        static readonly Color GoalColor = new Color32(0x0E, 0x7A, 0x3C, 0xFF);
        static readonly Color StarColor = new Color32(0xE8, 0x9A, 0x1C, 0xFF);

        // 맵 에디터가 안 그리는 것이라 여기서 정한다.
        static readonly Color DeviceColor = new Color32(0x0B, 0x6E, 0x6E, 0xFF);

        /// 지어 둔 것 전부. 레벨이 바뀌면 통째로 버린다.
        readonly List<GameObject> _parts = new List<GameObject>();

        /// 시뮬 중 움직이는 것은 공 하나다. 나머지 지오메트리는
        /// 정적이라 다시 세울 일이 없다.
        Transform _ball;

        /// 시뮬 중 사라지는 것들. 인덱스는 LevelData 와 같다.
        /// 장치는 몸과 범위가 따로라 둘을 나란히 든다.
        readonly List<Transform> _stars = new List<Transform>();
        readonly List<Transform> _deviceBodies = new List<Transform>();
        readonly List<Transform> _deviceRanges = new List<Transform>();

        /// 재시도가 공을 되돌릴 출발점의 출처.
        LevelData _level;

        /// 그림의 출처. 테마가 물려 주기 전에는 코드 도형을
        /// 쓴다 — 씬을 단독으로 열어도 판은 보여야 한다.
        SimStyle _style;

        /// 테마가 없으면 null 이다.
        SimStyle.Shapes Art => _style == null ? null : _style.Sprites;

        /// 지형은 코드가 만든 사각형이라 테마가 색만 준다.
        Color TerrainInk => _style == null ? TerrainColor : _style.Terrain;

        /// <summary>
        /// 테마가 정해지면 그림도 정해진다. 레벨이 이미
        /// 서 있으면 다시 세운다 — 테마는 나중에 올 수 있다.
        /// </summary>
        public void SetStyle(SimStyle style)
        {
            _style = style;
            if (_level != null) SetLevel(_level);
        }

        /// <summary>
        /// 레벨이 정해지면 화면도 정해진다. 영역은
        /// 카메라가 맞추는 것과 같은 함수에서 나온다 —
        /// 갈라지면 못 그리는 자리가 그려 보인다.
        /// </summary>
        public void SetLevel(LevelData level)
        {
            Clear();

            _level = level;
            if (level == null) return;

            Rect area = LevelDataArea.Calculate(level);
            AddPlayAreaOutline(area);
            AddKillLine(area, level.KillY);

            for (int i = 0; i < level.Terrain.Count; i++)
                AddSegment(level.Terrain[i], i);

            for (int i = 0; i < level.Devices.Count; i++)
                AddDevice(level.Devices[i], i);

            // 셋의 크기는 LevelData 의 const 다 — 파일에 남은
            // BallRadius·GoalRadius 는 무시된다. 그림은 테마가
            // 주므로 게임·에디터와 같은 것이 나온다.
            AddThemed("Goal", level.GoalPosition, LevelData.GoalRadius * 2f,
                Art?.Goal, GoalColor, RenderOrder.Goal);

            for (int i = 0; i < level.Stars.Count; i++)
                _stars.Add(AddThemed($"Star_{i}", level.Stars[i],
                    LevelData.StarCaptureRadius * 2f,
                    Art?.Star, StarColor, RenderOrder.Star));

            _ball = AddThemed("Ball", level.BallStart, LevelData.BallRadius * 2f,
                Art?.Ball, BallColor, RenderOrder.Ball);
        }

        /// <summary>
        /// 시뮬 중 공을 옮긴다. 물리 오브젝트에는 렌더러가
        /// 없어 화면 쪽 공이 바디를 따라가야 한다.
        /// 각도까지 받아야 구르는 것이 구르는 것으로 보인다.
        /// </summary>
        /// <param name="degrees">바디의 회전. Rigidbody2D 와 같은 도 단위다.</param>
        public void MoveBall(Vector2 position, float degrees)
        {
            if (_ball == null) return;

            _ball.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, degrees));
        }

        /// <summary>
        /// 죽은 공을 지운다. 남겨 두면 킬라인 아래에 멈춰
        /// 선 공이 아직 살아 있는 것처럼 읽힌다.
        /// </summary>
        public void SetBallVisible(bool visible)
        {
            if (_ball != null) _ball.gameObject.SetActive(visible);
        }

        /// <summary>재시도가 부른다. 공만 출발점으로 되돌린다.</summary>
        public void ResetBall()
        {
            if (_level != null) MoveBall(_level.BallStart, 0f);
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
                TerrainInk, RenderOrder.Terrain);
        }

        void AddDevice(in DeviceData device, int index)
        {
            _deviceRanges.Add(AddDot($"DeviceRange_{index}", device.Position,
                device.Radius * 2f,
                ShapeSprites.Ring, Fade(DeviceColor, DeviceRangeAlpha),
                RenderOrder.Device));

            // 몸집과 방향은 SimStyle 이 안다 — 게임과 같은
            // 크기로 그려야 저작자가 본 것이 그대로 온다.
            Sprite art = _style == null ? null : _style.SpriteOf(device.Type);
            float diameter = art == null ? DeviceSize : SimStyle.RadiusOf(device) * 2f;

            Transform body = AddThemed($"Device_{index}", device.Position, diameter,
                art, DeviceColor, RenderOrder.Device + 1);

            body.rotation = Quaternion.Euler(0f, 0f, SimStyle.AngleOf(device));
            _deviceBodies.Add(body);
        }

        /// <summary>먹은 별을 지운다. 남아 있으면 먹었는지 알 수 없다.</summary>
        public void SetStarVisible(int index, bool visible)
        {
            if (index >= 0 && index < _stars.Count)
                _stars[index].gameObject.SetActive(visible);
        }

        /// <summary>
        /// 터진 장치를 지운다. 몸과 범위가 같이 사라진다 —
        /// 없는 폭탄의 범위는 거짓이다.
        /// </summary>
        public void SetDeviceVisible(int index, bool visible)
        {
            if (index < 0 || index >= _deviceBodies.Count) return;

            _deviceBodies[index].gameObject.SetActive(visible);
            _deviceRanges[index].gameObject.SetActive(visible);
        }

        /// <summary>재시도가 부른다. 시뮬 중 지운 것을 되살린다.</summary>
        public void ShowAll()
        {
            SetBallVisible(true);

            for (int i = 0; i < _stars.Count; i++) SetStarVisible(i, true);
            for (int i = 0; i < _deviceBodies.Count; i++) SetDeviceVisible(i, true);
        }

        /// <summary>
        /// 그릴 수 있는 곳을 점선으로 두른다. 채운 판은
        /// 뒤에 깔린 배경을 통째로 가린다.
        /// </summary>
        void AddPlayAreaOutline(Rect area)
        {
            var root = new GameObject("PlayArea");
            root.transform.SetParent(transform, false);
            root.transform.position = area.center;
            _parts.Add(root);

            float halfWidth = area.width * 0.5f;
            float halfHeight = area.height * 0.5f;

            AddDashes(root.transform, new Vector2(0f, halfHeight), area.width, true);
            AddDashes(root.transform, new Vector2(0f, -halfHeight), area.width, true);
            AddDashes(root.transform, new Vector2(-halfWidth, 0f), area.height, false);
            AddDashes(root.transform, new Vector2(halfWidth, 0f), area.height, false);
        }

        /// <param name="center">뿌리 기준 변의 한가운데.</param>
        /// <param name="horizontal">가로 변이면 점도 눕는다.</param>
        void AddDashes(Transform root, Vector2 center, float length, bool horizontal)
        {
            // 칸 수를 반올림해 변 길이에 맞춘다. 목표 간격을
            // 그대로 쓰면 마지막 점이 모서리 밖으로 나간다.
            int count = Mathf.Max(1, Mathf.RoundToInt(length / DashPeriod));
            float step = length / count;

            Vector2 direction = horizontal ? Vector2.right : Vector2.up;
            Vector2 size = horizontal
                ? new Vector2(step * DashRatio, DashWidth)
                : new Vector2(DashWidth, step * DashRatio);

            for (int i = 0; i < count; i++)
            {
                // 칸 한가운데에 놓는다. 칸 끝에 놓으면
                // 양 모서리에서 점이 반만 남는다.
                float offset = step * (i + 0.5f) - length * 0.5f;

                var part = new GameObject("Dash");
                part.transform.SetParent(root, false);
                part.transform.localPosition = center + direction * offset;
                part.transform.localScale = new Vector3(size.x, size.y, 1f);

                var renderer = part.AddComponent<SpriteRenderer>();
                renderer.sprite = ShapeSprites.Quad;
                renderer.color = PlayAreaColor;
                renderer.sortingOrder = RenderOrder.PlayArea;
            }
        }

        /// <summary>
        /// 죽는 선을 긋는다. 자리는 그릴 수 있는 곳의 아래
        /// 변이 아니라 KillY 다 — 판정이 보는 값과 갈라지면
        /// 선 위에서 안 죽는다.
        /// </summary>
        void AddKillLine(Rect area, float killY)
        {
            var part = new GameObject("KillLine");
            part.transform.SetParent(transform, false);
            _parts.Add(part);

            var renderer = part.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = RenderOrder.PlayArea;

            Sprite art = _style == null ? null : _style.KillLine;
            if (art == null)
            {
                renderer.sprite = ShapeSprites.Quad;
                renderer.color = KillLineColor;
                part.transform.position = new Vector3(area.center.x, killY, 0f);
                part.transform.localScale =
                    new Vector3(area.width, KillLineWidth, 1f);
                return;
            }

            // 늘리면 그림이 찌그러진다. 가로로만 반복해
            // 채우고 세로는 원본 크기 그대로다.
            renderer.sprite = art;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = new Vector2(area.width, art.bounds.size.y);

            // 죽는 선은 그림의 아랫변이다. 위로 세워야
            // 불길이 선에서 피어오르는 것으로 읽힌다.
            // bounds 는 피벗 기준이라 어디 잡아도 맞는다.
            part.transform.position =
                new Vector3(area.center.x, killY - art.bounds.min.y, 0f);
        }

        void AddQuad(string name, Vector2 center, Vector2 size, float angle,
            Color color, int order)
        {
            Transform part = Add(name, ShapeSprites.Quad, color, order);

            part.SetPositionAndRotation(center, Quaternion.Euler(0f, 0f, angle));
            part.localScale = new Vector3(size.x, size.y, 1f);
        }

        /// <summary>
        /// 테마 그림이 있으면 덧칠 없이 그대로 쓰고, 없으면
        /// 코드 도형에 색을 입힌다. 그림에 색을 곱하면
        /// 아트가 의도한 색이 안 나온다.
        /// </summary>
        Transform AddThemed(string name, Vector2 center, float diameter, Sprite art,
            Color fallback, int order) =>
            AddDot(name, center, diameter,
                art == null ? ShapeSprites.Disc : art,
                art == null ? fallback : SimStyle.Plain,
                order);

        Transform AddDot(string name, Vector2 center, float diameter, Sprite sprite,
            Color color, int order)
        {
            Transform part = Add(name, sprite, color, order);

            part.position = center;
            part.localScale = Vector3.one * diameter;
            return part;
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
            _stars.Clear();
            _deviceBodies.Clear();
            _deviceRanges.Clear();
            _ball = null;
        }

        static Color Fade(Color color, float alpha) =>
            new Color(color.r, color.g, color.b, alpha);
    }
}
