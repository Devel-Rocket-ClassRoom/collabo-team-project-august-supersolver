using System.Collections.Generic;
using PPS.Core;
using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 최소 렌더. 그리는 중인 획 하나와 확정된 획들을
    /// LineRenderer 로 그린다. 도구별 Material 과 핀 마커는
    /// 렌더링 작업 몫이라 여기 없다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StrokePreviewRenderer : MonoBehaviour
    {
        /// 시각 두께 = FreeBody 물리 두께.
        /// 어긋나면 화면의 선과 콜라이더가 따로 논다.
        const float Width = 0.12f;

        /// 씬 뷰 핀 표시 크기. 화면에 그리는 값이 아니라
        /// 배선 확인용이라 획 두께에 맞춰만 둔다.
        const float PivotGizmoRadius = 0.12f;

        /// 도구를 가르는 임시 색. Material 3종이 붙으면
        /// 사라진다 — 지금 기준은 "구분되는가"뿐이다.
        static readonly Color FixedLineTint = new Color(0.35f, 0.70f, 1f);
        static readonly Color FreeBodyTint = new Color(1f, 0.72f, 0.25f);

        /// URP Unlit 은 vertex color 를 안 읽어 startColor 로는
        /// 색이 안 바뀐다. 머티리얼 사본을 만들지 않으려고
        /// 프로퍼티 블록으로 _BaseColor 만 덮는다.
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] DrawInputBehaviour _input;
        [SerializeField] DrawingSession _session;
        [SerializeField] ToolSelection _tools;
        [SerializeField] Material _material;

        LineRenderer _preview;
        MaterialPropertyBlock _block;

        /// 확정 획의 선. 재구축 때 통째로 버린다.
        readonly List<LineRenderer> _lines = new List<LineRenderer>();

        void Awake()
        {
            _preview = CreateLine("Preview");

            // 그리는 중인 선은 항상 맨 위다. 확정 선보다
            // 먼저 만들어져서, 순서를 안 주면 밑에 깔린다.
            _preview.sortingOrder = short.MaxValue;
        }

        void OnEnable()
        {
            _session.Changed += Rebuild;
            Rebuild();
        }

        void OnDisable()
        {
            _session.Changed -= Rebuild;
        }

        void LateUpdate()
        {
            IReadOnlyList<Vector2> points = _input.PreviewPoints;
            bool visible = _input.IsDrawing && points.Count >= 2;

            _preview.enabled = visible;
            if (!visible) return;

            Tint(_preview, _tools.Current.ToToolType());

            _preview.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++)
                _preview.SetPosition(i, points[i]);
        }

        /// <summary>
        /// Solution 을 기준으로 다시 그린다. 덧붙이기만 하면
        /// 되돌리기·초기화로 사라진 획이 화면에 남는다.
        /// Destroy 는 렌더 전에 반영돼 겹쳐 보이는 프레임이 없다.
        /// </summary>
        void Rebuild()
        {
            for (int i = 0; i < _lines.Count; i++)
                Destroy(_lines[i].gameObject);
            _lines.Clear();

            List<Stroke> strokes = _session.Solution.Strokes;
            for (int i = 0; i < strokes.Count; i++)
            {
                List<Vector2> points = strokes[i].Points;

                LineRenderer line = CreateLine("Stroke");
                line.positionCount = points.Count;
                for (int j = 0; j < points.Count; j++)
                    line.SetPosition(j, points[j]);

                // 나중에 그린 획이 위로 온다. 안 주면 z 도
                // 머티리얼도 같아 그리는 순서가 정해지지 않는다.
                line.sortingOrder = i;

                Tint(line, strokes[i].Tool);
                _lines.Add(line);
            }
        }

        void Tint(LineRenderer line, ToolType tool)
        {
            if (_block == null) _block = new MaterialPropertyBlock();

            line.GetPropertyBlock(_block);
            _block.SetColor(BaseColor, tool == ToolType.FreeBody ? FreeBodyTint : FixedLineTint);
            line.SetPropertyBlock(_block);
        }

        /// <summary>
        /// 얼마나 채워졌는가. 월드 슬롯(-1)은 이미 정해진
        /// 값이라 채워진 것으로 본다 — 월드 고정은 획 하나로
        /// 완성이다.
        /// </summary>
        static Color PivotColor(in PivotJoint pivot)
        {
            bool waiting = pivot.StrokeA == PivotJoint.Unbound
                || pivot.StrokeB == PivotJoint.Unbound;

            if (!waiting) return new Color(0.30f, 0.90f, 0.45f);   // 초록 — 완성

            bool any = pivot.StrokeA >= 0 || pivot.StrokeB >= 0;
            return any
                ? new Color(1f, 0.85f, 0.25f)                      // 노랑 — 하나만
                : new Color(0.75f, 0.78f, 0.85f);                  // 회색 — 아직 없음
        }

        LineRenderer CreateLine(string name)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(transform, false);

            line.useWorldSpace = true;
            line.widthMultiplier = Width;
            line.material = _material;
            return line;
        }

        /// <summary>
        /// 핀 마커는 렌더링 작업 몫이라 아직 화면에 없다.
        /// 툴바→인식기→배치 배선이 실제로 도는지는 순수
        /// 테스트가 못 보므로 씬 뷰에서만 확인한다.
        /// </summary>
        void OnDrawGizmos()
        {
            if (_session == null) return;

            Solution solution = _session.Solution;
            List<PivotJoint> pivots = solution.Pivots;

            for (int i = 0; i < pivots.Count; i++)
            {
                PivotJoint pivot = pivots[i];

                Gizmos.color = PivotColor(pivot);
                Gizmos.DrawWireSphere(pivot.Anchor, PivotGizmoRadius);

                // 월드 고정은 테두리를 하나 더 둘러 단독 핀과 가른다.
                if (pivot.StrokeB == PivotJoint.WorldIndex)
                    Gizmos.DrawWireSphere(pivot.Anchor, PivotGizmoRadius * 1.8f);
            }
        }
    }
}
