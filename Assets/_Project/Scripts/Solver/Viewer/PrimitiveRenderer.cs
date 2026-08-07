using System.Collections.Generic;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver.Viewer
{
    /// <summary>
    /// 시행이 놓은 프리미티브 배치를 그린다.
    /// 스텝 0 의 그림이다 — Free 는 시뮬이 시작되면
    /// 곧장 떨어지므로 그 자리에 남아 있지 않다.
    /// 그래서 Fixed 와 색을 나눈다.
    /// </summary>
    public sealed class PrimitiveRenderer : MonoBehaviour
    {
        /// 거의 검정. 제자리에 남는다.
        [SerializeField] Color _fixedColor = new Color32(0x23, 0x25, 0x2B, 0xFF);

        /// 진한 파랑. 시뮬이 시작되면 움직인다.
        [SerializeField] Color _freeColor = new Color32(0x1B, 0x4F, 0xA0, 0xFF);

        /// 어두운 벽돌. 시뮬이 돌지 않은 배치다.
        [SerializeField] Color _rejectedColor = new Color32(0x8A, 0x1C, 0x1C, 0xFF);

        readonly List<Stroke> _strokes = new List<Stroke>();

        bool _rejected;

        /// <summary>
        /// 배치를 그릴 폴리라인으로 펼친다.
        /// 거부된 배치도 그린다 — 어느 조각이 왜 걸렸는지는
        /// 사유 문자열보다 그림이 빠르다.
        /// </summary>
        public void Show(IReadOnlyList<Primitive> primitives, bool rejected)
        {
            _strokes.Clear();

            for (int i = 0; i < primitives.Count; i++)
                _strokes.Add(PrimitiveDecoder.ToStroke(primitives[i]));

            _rejected = rejected;
        }

        public void Clear() => _strokes.Clear();

        void OnRenderObject()
        {
            if (_strokes.Count == 0) return;

            GLDraw.SetPass();

            GL.PushMatrix();
            GL.Begin(GL.LINES);

            for (int i = 0; i < _strokes.Count; i++)
            {
                var stroke = _strokes[i];
                if (!stroke.IsValid) continue;

                GL.Color(_rejected ? _rejectedColor
                       : stroke.Tool == ToolType.FreeBody ? _freeColor
                       : _fixedColor);

                var points = stroke.Points;
                for (int p = 0; p + 1 < points.Count; p++)
                    GLDraw.Line(points[p], points[p + 1]);
            }

            GL.End();
            GL.PopMatrix();
        }
    }
}
