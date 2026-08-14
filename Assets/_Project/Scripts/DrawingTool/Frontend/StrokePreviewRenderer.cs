using System.Collections.Generic;
using PPS.Core;
using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 그리는 중인 획 하나와 확정된 획들을 LineRenderer
    /// 로 그린다. 핀은 PivotMarkerView 몫이고, 도구별
    /// Material 은 아직 임시 색이다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StrokePreviewRenderer : MonoBehaviour
    {
        /// 시각 두께 = FreeBody 물리 두께.
        /// 어긋나면 화면의 선과 콜라이더가 따로 논다.
        const float Width = 0.12f;

        /// 도구를 가르는 임시 색. 따뜻한 색은 별·핀이
        /// 이미 쓰고 있어 파랑·보라만 남았다 — 색약에
        /// 약한 짝이라 Material 3종이 형태를 더해야 한다.
        static readonly Color FixedLineTint = new Color32(0x1B, 0x4F, 0xA0, 0xFF);
        static readonly Color FreeBodyTint = new Color32(0x7A, 0x2E, 0x9E, 0xFF);

        /// URP Unlit 은 vertex color 를 안 읽어 startColor 로는
        /// 색이 안 바뀐다. 머티리얼 사본을 만들지 않으려고
        /// 프로퍼티 블록으로 _BaseColor 만 덮는다.
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] PointerReader _input;
        [SerializeField] DrawingSession _session;
        [SerializeField] ToolSelection _tools;
        [SerializeField] Material _material;

        LineRenderer _preview;
        MaterialPropertyBlock _block;

        /// 확정 획의 선. 재구축 때 통째로 버린다.
        readonly List<LineRenderer> _lines = new List<LineRenderer>();

        /// 인덱스가 Solution.Strokes 와 같다.
        /// 시뮬 중에는 SimulationView 가 바디에 얹는다.
        public IReadOnlyList<LineRenderer> Lines => _lines;

        void Awake()
        {
            _preview = CreateLine("Preview");

            // 그리는 중인 선은 항상 맨 위다. 확정 선보다
            // 먼저 만들어져서, 순서를 안 주면 밑에 깔린다.
            _preview.sortingOrder = RenderOrder.Preview;
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
        /// 재시도도 이 길로 획을 그리기 때 자리로 되돌린다.
        /// </summary>
        public void Rebuild()
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
                line.sortingOrder = RenderOrder.Stroke + i;

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

        LineRenderer CreateLine(string name)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(transform, false);

            line.useWorldSpace = true;
            line.widthMultiplier = Width;
            line.material = _material;
            return line;
        }
    }
}
