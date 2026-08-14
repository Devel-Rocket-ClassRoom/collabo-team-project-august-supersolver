using System.Collections.Generic;
using PPS.Core;
using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>핀이 얼마나 채워졌는가.</summary>
    public enum PivotFill
    {
        /// 붙은 획이 아직 없다.
        Empty,

        /// 한쪽만 찼다. 나머지 칸이 나중 획을 기다린다.
        Waiting,

        /// 이을 것이 다 정해졌다.
        Bound,
    }

    /// <summary>
    /// 회전축을 화면에 보인다. 안 보이면 미결합 핀이
    /// 사라진 것과 같아 사고로 생긴 축을 눈치채지 못한
    /// 채 정답이 바뀐다. 크기는 dp 라 보이는 크기와
    /// 누를 수 있는 크기가 같다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PivotMarkerView : MonoBehaviour
    {
        /// 월드 고정을 두르는 바깥 원. 안쪽 점의 배수다.
        const float WorldRingScale = 1.8f;

        [SerializeField] DrawingSession _session;
        CanvasCameraFitter _fitter;

        // 핀은 대개 어두운 획 위에 놓여 실제 배경이 크림이
        // 아니다. 크림 대비만 보고 내리면 노랑이 갈색으로,
        // 초록이 목표 색과 같아 보인다.
        static readonly Color EmptyColor = new Color32(0x5C, 0x61, 0x6E, 0xFF);
        static readonly Color WaitingColor = new Color32(0xC0, 0x82, 0x00, 0xFF);
        static readonly Color BoundColor = new Color32(0x22, 0xA6, 0x5A, 0xFF);

        /// 핀 하나당 뿌리 하나. 크기는 뿌리가 진다.
        readonly List<Transform> _markers = new List<Transform>();

        /// 인덱스가 Solution.Pivots 와 같다.
        /// 시뮬 중에는 SimulationView 가 host 바디에 얹는다.
        public IReadOnlyList<Transform> Markers => _markers;
        private void Awake()
        {
            _fitter = CanvasCameraFitter.Instance;
        }
        void OnEnable()
        {
            _session.Changed += Refresh;
            Refresh();
        }

        void OnDisable() => _session.Changed -= Refresh;

        /// <summary>
        /// 그림을 기준으로 다시 세운다. 되돌리기로 핀
        /// 인덱스가 다시 매겨지므로 부분 갱신할 것이 없다.
        /// </summary>
        public void Show(Solution solution)
        {
            Clear();
            if (solution == null) return;

            List<PivotJoint> pivots = solution.Pivots;
            for (int i = 0; i < pivots.Count; i++)
                Add(pivots[i], i);
        }

        /// <summary>
        /// 월드 슬롯(-1)은 이미 정해진 값이라 채워진 것으로
        /// 본다 — 월드 고정은 획 하나로 완성이다.
        /// </summary>
        public static PivotFill FillOf(in PivotJoint pivot)
        {
            if (pivot.IsComplete) return PivotFill.Bound;

            return pivot.StrokeA >= 0 || pivot.StrokeB >= 0
                ? PivotFill.Waiting
                : PivotFill.Empty;
        }

        /// <summary>
        /// 화면→월드 계수는 세이프 에어리어와 게임 뷰
        /// 크기에 걸려 있고 둘 다 콜백이 없다. 뿌리 몇
        /// 개라 매 프레임 다시 재는 편이 싸다.
        /// </summary>
        void LateUpdate()
        {
            if (_markers.Count == 0 || !_fitter.IsReady) return;

            float diameter = _fitter.DpToWorld(TouchMetrics.TapThresholdDp) * 2f;
            for (int i = 0; i < _markers.Count; i++)
                _markers[i].localScale = Vector3.one * diameter;
        }

        /// <summary>재시도가 마커를 그리기 때 자리로 되돌릴 때도 쓴다.</summary>
        public void Refresh() => Show(_session.Solution);

        void Add(in PivotJoint pivot, int index)
        {
            PivotFill fill = FillOf(pivot);
            Color color = ColorOf(fill);

            var root = new GameObject($"Pivot_{index}").transform;
            root.SetParent(transform, false);
            root.position = pivot.Anchor;

            // 월드 고정은 테두리를 하나 더 둘러 단독 핀과
            // 가른다. 색은 이미 채움 상태를 쓰고 있어
            // 종류까지 색에 얹으면 둘이 섞인다.
            if (pivot.StrokeB == PivotJoint.WorldIndex)
                AddShape(root, "WorldRing", ShapeSprites.Ring, WorldRingScale, color);

            // 속을 채우는 것은 이을 것이 다 정해진 핀뿐이다.
            // 채움을 색에만 실으면 색약인 눈에 작동 여부가
            // 안 보인다 — 바깥 원과 축이 달라 안 겹친다.
            AddShape(root, "Dot",
                fill == PivotFill.Bound ? ShapeSprites.Disc : ShapeSprites.Ring,
                1f, color);

            _markers.Add(root);
        }

        static void AddShape(Transform root, string name, Sprite sprite, float scale, Color color)
        {
            var part = new GameObject(name);
            part.transform.SetParent(root, false);
            part.transform.localScale = Vector3.one * scale;

            var renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = RenderOrder.PivotMarker;
        }

        static Color ColorOf(PivotFill fill)
        {
            switch (fill)
            {
                case PivotFill.Bound: return BoundColor;
                case PivotFill.Waiting: return WaitingColor;
                default: return EmptyColor;
            }
        }

        void Clear()
        {
            for (int i = 0; i < _markers.Count; i++)
            {
                // 재생 중이 아니면 Destroy 는 미뤄지는 게
                // 아니라 거절된다. EditMode 테스트가 그렇다.
                if (Application.isPlaying) Destroy(_markers[i].gameObject);
                else DestroyImmediate(_markers[i].gameObject);
            }

            _markers.Clear();
        }
    }
}
