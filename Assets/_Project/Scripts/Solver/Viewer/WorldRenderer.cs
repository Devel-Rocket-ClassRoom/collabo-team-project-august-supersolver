using PPS.Core;
using UnityEngine;

namespace PPS.Solver.Viewer
{
    /// <summary>
    /// 살아 있는 월드를 그린다 — 지금 스텝의 바디들.
    /// 콜라이더를 그대로 그린다. 원래 그은 선으로
    /// 되돌리지 않는 것은 실제로 부딪히는 모양을
    /// 봐야 하기 때문이다.
    /// 월드를 소유하지 않는다. 짓고 밟고 버리는 것은
    /// 넘겨준 쪽 몫이다.
    /// </summary>
    public sealed class WorldRenderer : MonoBehaviour
    {
        /// 크림슨. 실제로 굴러가는 공이다.
        [SerializeField] Color _ballColor = new Color32(0xC0, 0x14, 0x3C, 0xFF);

        /// 거의 검정. 정적 바디.
        [SerializeField] Color _staticColor = new Color32(0x23, 0x25, 0x2B, 0xFF);

        /// 진한 파랑. 움직이는 바디.
        [SerializeField] Color _dynamicColor = new Color32(0x1B, 0x4F, 0xA0, 0xFF);

        /// 진한 주황. 닿으면 실패.
        [SerializeField] Color _hazardColor = new Color32(0xD4, 0x4A, 0x00, 0xFF);

        SimWorld _world;

        public void Show(SimWorld world) => _world = world;

        public void Clear() => _world = null;

        void OnRenderObject()
        {
            if (_world == null) return;

            GLDraw.SetPass();

            GL.PushMatrix();
            GL.Begin(GL.LINES);

            var bodies = _world.Bodies;
            for (int i = 0; i < bodies.Count; i++)
            {
                var body = bodies[i];
                if (body == null) continue;   // 파괴된 파편

                GL.Color(ReferenceEquals(body, _world.Ball) ? _ballColor
                       : IsHazard(body) ? _hazardColor
                       : body.bodyType == RigidbodyType2D.Static ? _staticColor
                       : _dynamicColor);

                DrawBody(body);
            }

            GL.End();
            GL.PopMatrix();
        }

        static void DrawBody(Rigidbody2D body)
        {
            var edge = body.GetComponent<EdgeCollider2D>();
            if (edge != null)
            {
                var points = edge.points;
                var at = body.transform;

                for (int i = 0; i + 1 < points.Length; i++)
                    GLDraw.Line(at.TransformPoint(points[i]),
                                at.TransformPoint(points[i + 1]));
                return;
            }

            var polygon = body.GetComponent<PolygonCollider2D>();
            if (polygon != null)
            {
                var at = body.transform;

                for (int p = 0; p < polygon.pathCount; p++)
                {
                    var path = polygon.GetPath(p);
                    for (int i = 0; i < path.Length; i++)
                        GLDraw.Line(at.TransformPoint(path[i]),
                                    at.TransformPoint(path[(i + 1) % path.Length]));
                }
                return;
            }

            var circle = body.GetComponent<CircleCollider2D>();
            if (circle != null) GLDraw.Circle(body.position, circle.radius);
        }

        /// <summary>목록이 짧아 선형 검색으로 충분하다.</summary>
        bool IsHazard(Rigidbody2D body)
        {
            var hazards = _world.Hazards;
            for (int i = 0; i < hazards.Count; i++)
            {
                var hazard = hazards[i];
                if (hazard != null && ReferenceEquals(hazard.attachedRigidbody, body))
                    return true;
            }
            return false;
        }
    }
}
