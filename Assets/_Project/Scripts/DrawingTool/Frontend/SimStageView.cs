using System.Collections.Generic;
using PPS.Core;
using PPS.Game;
using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 시뮬레이션 중 화면. 레벨 지오메트리는 정적이라 그대로
    /// 두고, 움직이거나 사라지는 것만 SimWorld 에서 읽어
    /// 얹는다 — 공·자유물체·핀·별·장치·파편.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SimStageView : MonoBehaviour
    {
        [SerializeField] GameSimDriver _driver;
        [SerializeField] DrawingSession _session;
        [SerializeField] LevelView _levelView;
        [SerializeField] StrokePreviewRenderer _strokes;
        [SerializeField] PivotMarkerView _pivots;

        /// 테마 그림이 없을 때 쓰는 파편 색. 닿으면 실패라
        /// 킬라인과 같은 붉은색을 쓴다.
        static readonly Color FragmentColor = new Color32(0x8A, 0x1C, 0x1C, 0xFF);

        /// 파편은 개수가 스텝마다 변한다. 레벨이 아니라
        /// 월드에서 나오므로 여기서 만들고 재사용한다.
        readonly List<SpriteRenderer> _fragments = new List<SpriteRenderer>();

        SimStyle _style;

        /// 획마다 바디 기준 로컬 점. 인덱스는 Solution.Strokes
        /// 와 같고, 따라갈 것이 없는 자리는 null 이다.
        readonly List<Vector2[]> _strokeLocal = new List<Vector2[]>();

        /// 핀이 얹힐 바디와 그 바디 기준 앵커.
        /// 인덱스는 Solution.Pivots 와 같다.
        readonly List<Rigidbody2D> _pivotHost = new List<Rigidbody2D>();
        readonly List<Vector2> _pivotLocal = new List<Vector2>();

        /// <summary>
        /// 첫 스텝 전에 불러야 한다. t0 의 바디는 회전이 없고
        /// 위치가 곧 그린 좌표의 원점이라, 그때 로컬 값을 떠
        /// 둬야 플레이를 눌러도 화면이 제자리에 있다.
        /// </summary>
        /// <summary>테마가 파편 그림을 준다. 없으면 코드 도형이다.</summary>
        public void SetStyle(SimStyle style) => _style = style;

        public void Begin()
        {
            _strokeLocal.Clear();
            _pivotHost.Clear();
            _pivotLocal.Clear();

            SimWorld world = _driver.World;
            if (world == null) return;

            IReadOnlyList<Rigidbody2D> bodies = world.StrokeBodies;
            BeginStrokes(bodies);
            BeginPivots(bodies);
        }

        /// <summary>재시도가 부른다. 그리기 때 자리로 돌린다.</summary>
        public void Reset()
        {
            _strokeLocal.Clear();
            _pivotHost.Clear();
            _pivotLocal.Clear();

            _levelView.ResetBall();
            _levelView.ShowAll();
            HideFragments();
            _strokes.Rebuild();
            _pivots.Refresh();
        }

        void LateUpdate()
        {
            // 재시도가 월드를 버리면 화면도 멈춘다. 되돌리는
            // 것은 Reset 이고 여기서 할 일은 없다.
            SimWorld world = _driver.World;
            if (world == null) return;

            _levelView.MoveBall(world.Ball.position);
            FollowStrokes(world.StrokeBodies);
            FollowPivots();
            FollowStars(world);
            FollowDevices(world);
            FollowFragments(world);
        }

        /// <summary>
        /// 먹은 별을 지운다. 판정은 코어가 이미 했다 —
        /// 여기서 또 재면 둘이 어긋난다.
        /// </summary>
        void FollowStars(SimWorld world)
        {
            IReadOnlyList<Vector2> stars = world.Level.Stars;

            for (int i = 0; i < stars.Count; i++)
                _levelView.SetStarVisible(i, !world.Judge.IsCollected(i));
        }

        /// <summary>
        /// 바디를 가질 장치인데 없으면 이미 터진 것이다.
        /// 바람처럼 몸이 없는 장치는 사라지지 않는다.
        /// </summary>
        void FollowDevices(SimWorld world)
        {
            IReadOnlyList<DeviceData> devices = world.Level.Devices;

            for (int i = 0; i < devices.Count; i++)
            {
                (DeviceData data, Rigidbody2D body) = world.GetDevice(i);

                _levelView.SetDeviceVisible(i, body != null || !DeviceFactory.MakesBody(data.Type));
            }
        }

        /// <summary>
        /// 파편은 닿으면 실패다. 안 보이면 왜 죽었는지 알 수 없다.
        /// 터질 때마다 늘어나므로 만든 것을 다시 쓴다.
        /// </summary>
        void FollowFragments(SimWorld world)
        {
            IReadOnlyList<Collider2D> hazards = world.Hazards;

            while (_fragments.Count < hazards.Count) _fragments.Add(CreateFragment());

            for (int i = 0; i < _fragments.Count; i++)
            {
                bool used = i < hazards.Count && hazards[i] != null;

                _fragments[i].gameObject.SetActive(used);
                if (!used) continue;

                Transform part = _fragments[i].transform;
                part.position = hazards[i].transform.position;
                part.localScale = Vector3.one * (FragBombDevice.FragmentRadius * 2f);
            }
        }

        void HideFragments()
        {
            for (int i = 0; i < _fragments.Count; i++)
                _fragments[i].gameObject.SetActive(false);
        }

        SpriteRenderer CreateFragment()
        {
            var part = new GameObject("Fragment");
            part.transform.SetParent(transform, false);

            var renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = _style == null ? ShapeSprites.Disc : _style.Sprites.Dot;
            renderer.color = _style == null ? FragmentColor : SimStyle.Plain;
            renderer.sortingOrder = RenderOrder.Fragment;

            return renderer;
        }

        void BeginStrokes(IReadOnlyList<Rigidbody2D> bodies)
        {
            IReadOnlyList<LineRenderer> lines = _strokes.Lines;

            for (int i = 0; i < bodies.Count; i++)
            {
                // 정적 고정선은 안 움직인다. 매 프레임 같은
                // 값을 다시 찍을 이유가 없다.
                Rigidbody2D body = DynamicAt(i, bodies);
                bool follows = body != null && i < lines.Count;

                _strokeLocal.Add(follows ? ToLocal(lines[i], body.position) : null);
            }
        }

        /// <summary>
        /// 핀은 물지 않은 채 제자리에 남으면 월드 고정처럼
        /// 읽힌다 — 단독 핀과 월드 고정을 가르는 표시가
        /// 통째로 거짓이 된다.
        /// </summary>
        void BeginPivots(IReadOnlyList<Rigidbody2D> bodies)
        {
            List<PivotJoint> pivots = _session.Solution.Pivots;

            for (int i = 0; i < pivots.Count; i++)
            {
                Rigidbody2D host = Host(pivots[i], bodies);

                _pivotHost.Add(host);
                _pivotLocal.Add(host == null ? Vector2.zero : pivots[i].Anchor - host.position);
            }
        }

        void FollowStrokes(IReadOnlyList<Rigidbody2D> bodies)
        {
            IReadOnlyList<LineRenderer> lines = _strokes.Lines;

            for (int i = 0; i < _strokeLocal.Count; i++)
            {
                Vector2[] local = _strokeLocal[i];
                if (local == null) continue;

                Transform body = bodies[i].transform;
                LineRenderer line = lines[i];

                for (int j = 0; j < local.Length; j++)
                    line.SetPosition(j, body.TransformPoint(local[j]));
            }
        }

        /// 마커는 원이라 각도가 안 보인다. 위치만 옮긴다.
        void FollowPivots()
        {
            IReadOnlyList<Transform> markers = _pivots.Markers;
            int count = Mathf.Min(_pivotHost.Count, markers.Count);

            for (int i = 0; i < count; i++)
            {
                Rigidbody2D host = _pivotHost[i];
                if (host == null) continue;

                markers[i].position = host.transform.TransformPoint(_pivotLocal[i]);
            }
        }

        /// <summary>
        /// 조인트를 매단 쪽에 마커도 얹는다. WorldBuilder 의
        /// PickHost 와 같은 규칙이라야 화면과 물리가 안 갈린다.
        /// 월드 고정 핀도 그대로 태운다 — 조인트가 앵커를
        /// 붙잡고 있어 같은 자리에 남고, 솔버가 밀리면 그
        /// 밀림이 눈에 보이는 편이 낫다.
        /// </summary>
        static Rigidbody2D Host(in PivotJoint pivot, IReadOnlyList<Rigidbody2D> bodies)
        {
            Rigidbody2D a = DynamicAt(pivot.StrokeA, bodies);
            return a != null ? a : DynamicAt(pivot.StrokeB, bodies);
        }

        /// <summary>
        /// 그 자리의 동적 바디. 미결합(-2)·월드(-1)·정적
        /// 고정선은 전부 null 이다.
        /// </summary>
        static Rigidbody2D DynamicAt(int index, IReadOnlyList<Rigidbody2D> bodies)
        {
            if (index < 0 || index >= bodies.Count) return null;

            Rigidbody2D body = bodies[index];
            return body != null && body.bodyType == RigidbodyType2D.Dynamic ? body : null;
        }

        /// <summary>
        /// t0 의 바디는 회전이 없고 위치가 곧 원점이라
        /// 로컬 점이 뺄셈 하나로 나온다.
        /// </summary>
        static Vector2[] ToLocal(LineRenderer line, Vector2 origin)
        {
            var local = new Vector2[line.positionCount];
            for (int i = 0; i < local.Length; i++)
                local[i] = (Vector2)line.GetPosition(i) - origin;

            return local;
        }
    }
}
