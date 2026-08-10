using System.Collections.Generic;
using PPS.Core;
using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 최소 렌더. 그리는 중인 획 하나와 확정된 획들을
    /// LineRenderer 로 그린다. 도구별 머티리얼과 핀 마커는
    /// 렌더링 작업 몫이라 여기 없다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StrokePreviewRenderer : MonoBehaviour
    {
        /// 시각 두께 = FreeBody 물리 두께.
        /// 어긋나면 화면의 선과 콜라이더가 따로 논다.
        const float Width = 0.12f;

        [SerializeField] DrawInputBehaviour _input;
        [SerializeField] Material _material;

        LineRenderer _preview;

        void Awake()
        {
            _preview = CreateLine("Preview");
        }

        void OnEnable()
        {
            _input.StrokeConfirmed += AddConfirmed;
        }

        void OnDisable()
        {
            _input.StrokeConfirmed -= AddConfirmed;
        }

        void LateUpdate()
        {
            IReadOnlyList<Vector2> points = _input.PreviewPoints;
            bool visible = _input.IsDrawing && points.Count >= 2;

            _preview.enabled = visible;
            if (!visible) return;

            _preview.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++)
                _preview.SetPosition(i, points[i]);
        }

        void AddConfirmed(Stroke stroke)
        {
            LineRenderer line = CreateLine("Stroke");
            line.positionCount = stroke.Points.Count;
            for (int i = 0; i < stroke.Points.Count; i++)
                line.SetPosition(i, stroke.Points[i]);
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
