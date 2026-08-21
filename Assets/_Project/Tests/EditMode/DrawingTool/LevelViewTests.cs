using NUnit.Framework;
using PPS.Core;
using UnityEngine;

namespace PPS.DrawingTool.Tests
{
    /// <summary>
    /// 화면에 선 것이 레벨 데이터와 맞는지 잰다.
    /// 색과 모양은 눈으로 볼 몫이고, 여기서 지키는 것은
    /// 개수·크기·자리다 — 눈으로는 못 잡는 것들이다.
    /// </summary>
    public class LevelViewTests
    {
        GameObject _go;
        LevelView _view;

        [SetUp]
        public void 화면을_만든다()
        {
            _go = new GameObject("LevelView");
            _view = _go.AddComponent<LevelView>();
        }

        [TearDown]
        public void 화면을_치운다() => Object.DestroyImmediate(_go);

        /// 지형 2 · 장치 1 · 별 1 인 판.
        static LevelData MakeLevel() => new LevelData
        {
            BallStart = new Vector2(-2f, 1f),
            GoalPosition = new Vector2(3f, -1f),
            KillY = -5f,
            Terrain =
            {
                new StaticSegment(new Vector2(-3f, 0f), new Vector2(1f, 0f)),
                new StaticSegment(new Vector2(1f, 0f), new Vector2(4f, -2f)),
            },
            Stars = { new Vector2(0f, 2f) },
            Devices = { new DeviceData { Position = new Vector2(2f, 2f), Radius = 1.5f } },
        };

        [Test]
        public void 레벨을_물리면_구성요소가_전부_선다()
        {
            _view.SetLevel(MakeLevel());

            // 영역 점선 · 킬라인 · 지형 2 · 장치 2 · 목표 · 별 · 공
            Assert.AreEqual(9, _go.transform.childCount);

            Assert.AreEqual(2, CountStartingWith("Terrain_"), "지형 선분 수가 다르다");
            Assert.AreEqual(1, CountStartingWith("Star_"), "별 수가 다르다");
            Assert.AreEqual(1, CountStartingWith("DeviceRange_"), "장치 반경이 없다");

            Assert.IsNotNull(_go.transform.Find("Ball"));
            Assert.IsNotNull(_go.transform.Find("Goal"));
        }

        [Test]
        public void 다시_물려도_늘어나지_않는다()
        {
            _view.SetLevel(MakeLevel());
            int first = _go.transform.childCount;

            _view.SetLevel(MakeLevel());

            Assert.AreEqual(first, _go.transform.childCount,
                "레벨을 갈아 끼웠는데 이전 것이 화면에 남았다");
        }

        /// <summary>
        /// 점선이 곧 "여기는 그릴 수 있다"는 표시다.
        /// 카메라가 맞추는 영역과 갈라지면 못 그리는
        /// 자리가 그려 보인다.
        /// </summary>
        [Test]
        public void 영역_점선이_LevelDataArea_와_일치한다()
        {
            LevelData level = MakeLevel();
            _view.SetLevel(level);

            Rect area = LevelDataArea.Calculate(level);
            Transform outline = _go.transform.Find("PlayArea");

            Bounds bounds = DashBounds(outline);

            // 점은 경계선을 타고 앉아 두께의 절반씩 밖으로
            // 나온다. 두께를 걷어내면 영역과 같아야 한다.
            Vector3 dash = outline.GetChild(0).localScale;
            float thickness = Mathf.Min(dash.x, dash.y);

            Assert.AreEqual(area.center.x, bounds.center.x, 1e-4f);
            Assert.AreEqual(area.center.y, bounds.center.y, 1e-4f);
            Assert.AreEqual(area.width, bounds.size.x - thickness, 1e-4f);
            Assert.AreEqual(area.height, bounds.size.y - thickness, 1e-4f);
        }

        /// 점 전부를 담는 상자. 점마다 자리와 크기가 달라
        /// 뿌리 하나로는 테두리 크기를 알 수 없다.
        static Bounds DashBounds(Transform outline)
        {
            var bounds = new Bounds(outline.GetChild(0).position, Vector3.zero);

            foreach (Transform dash in outline)
            {
                bounds.Encapsulate(dash.position + dash.localScale * 0.5f);
                bounds.Encapsulate(dash.position - dash.localScale * 0.5f);
            }

            return bounds;
        }

        /// <summary>
        /// 죽는 선은 그릴 수 있는 곳의 아래 변이 아니다.
        /// 판정이 보는 KillY 와 갈라지면 그린 선 위에서
        /// 안 죽는다.
        /// </summary>
        [Test]
        public void 킬라인이_KillY_높이에_선다()
        {
            LevelData level = MakeLevel();
            Rect area = LevelDataArea.Calculate(level);

            // 둘이 같은 픽스처면 자리를 어디서 뽑았는지
            // 구분이 안 돼 검사가 헛돈다.
            Assert.Less(level.KillY, area.yMin, "픽스처의 킬라인이 아래 변에 붙어 있다");

            _view.SetLevel(level);
            Transform line = _go.transform.Find("KillLine");

            Assert.IsNotNull(line, "킬라인이 없다");
            Assert.AreEqual(level.KillY, line.position.y, 1e-4f);
            Assert.AreEqual(area.center.x, line.position.x, 1e-4f);
            Assert.AreEqual(area.width, line.localScale.x, 1e-4f,
                "킬라인이 판 너비를 안 채운다");
        }

        /// <summary>
        /// 그림을 깔면 윗변이 죽는 선이다. 가운데를 맞추면
        /// 띠의 위 절반이 아직 살아 있는 자리를 덮는다.
        /// </summary>
        [Test]
        public void 킬라인_그림은_윗변이_KillY_에_붙는다()
        {
            var style = ScriptableObject.CreateInstance<SimStyle>();

            // 가운데 피벗 1wu. 아트가 오기 전의 대역이다.
            style.KillLine = ShapeSprites.Quad;

            LevelData level = MakeLevel();
            _view.SetStyle(style);
            _view.SetLevel(level);

            var line = _go.transform.Find("KillLine").GetComponent<SpriteRenderer>();
            float top = line.transform.position.y + line.size.y * 0.5f;

            Assert.AreEqual(SpriteDrawMode.Tiled, line.drawMode, "그림을 늘려 깔았다");
            Assert.AreEqual(level.KillY, top, 1e-4f, "그림 윗변이 죽는 선에 안 붙었다");
            Assert.AreEqual(LevelDataArea.Calculate(level).width, line.size.x, 1e-4f,
                "킬라인이 판 너비를 안 채운다");

            Object.DestroyImmediate(style);
        }

        /// <summary>
        /// 맵 에디터가 원 하나를 24개 선분으로 굽는다.
        /// 선분을 길이 그대로 그리면 이음매마다 틈이
        /// 남아 원이 톱니로 보인다.
        /// </summary>
        [Test]
        public void 지형_선분이_두께만큼_길어_이음매를_덮는다()
        {
            LevelData level = MakeLevel();
            _view.SetLevel(level);

            StaticSegment segment = level.Terrain[0];
            Transform part = _go.transform.Find("Terrain_0");

            float length = (segment.B - segment.A).magnitude;
            float width = part.localScale.y;

            Assert.AreEqual(length + width, part.localScale.x, 1e-5f,
                "선분이 두께만큼 길어지지 않았다");
        }

        int CountStartingWith(string prefix)
        {
            int found = 0;
            foreach (Transform child in _go.transform)
                if (child.name.StartsWith(prefix)) found++;

            return found;
        }
    }
}
