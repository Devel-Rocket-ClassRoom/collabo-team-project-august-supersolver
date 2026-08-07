using UnityEngine;

namespace PPS.Solver.Viewer
{
    /// <summary>
    /// 뷰어들이 나눠 쓰는 GL 선 그리기.
    /// 머티리얼은 하나만 만들어 돌려 쓴다.
    /// 두께는 주지 않는다 — 굵은 선이 필요하면
    /// 선분마다 사각형을 쳐야 해서 뷰어가 직접 한다.
    /// </summary>
    static class GLDraw
    {
        const int CircleSegments = 28;

        static Material _material;

        public static void SetPass()
        {
            if (_material == null)
            {
                // 에셋을 늘리지 않으려고 내장 셰이더를 쓴다.
                _material = new Material(Shader.Find("Hidden/Internal-Colored"))
                {
                    hideFlags = HideFlags.HideAndDontSave,
                };
                _material.SetFloat("_ZWrite", 0f);
                _material.SetFloat("_ZTest",
                    (float)UnityEngine.Rendering.CompareFunction.Always);
            }

            _material.SetPass(0);
        }

        public static void Vertex(Vector2 at) => GL.Vertex3(at.x, at.y, 0f);

        public static void Line(Vector2 a, Vector2 b)
        {
            Vertex(a);
            Vertex(b);
        }

        public static void Circle(Vector2 center, float radius)
        {
            Vector2 prev = center + new Vector2(radius, 0f);
            for (int i = 1; i <= CircleSegments; i++)
            {
                float angle = i * 2f * Mathf.PI / CircleSegments;
                Vector2 next = center +
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

                Line(prev, next);
                prev = next;
            }
        }
    }
}
