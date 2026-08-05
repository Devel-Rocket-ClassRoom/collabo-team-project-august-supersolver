using UnityEngine;

namespace PPS.Solver
{
    /// <summary>
    /// 프리미티브의 모양. Shape 별로 다른 규칙
    /// (각도 주기·점 생성)은 전부 이 축에 붙인다.
    /// </summary>
    public enum PrimitiveShape
    {
        /// 직선
        Line = 0,

        /// 그릇(U자)
        Bowl = 1,

        /// 정삼각형
        Triangle = 2,
    }

    /// <summary>
    /// Shape 하나가 가진 규칙 묶음.
    /// 점 수·대표 꼭짓점·잉크량은 UnitPoints 에서
    /// 파생시켜 같은 사실을 두 번 적지 않는다.
    /// </summary>
    public sealed class ShapeRule
    {
        /// 각도가 겹치기 시작하는 주기(라디안).
        public readonly float AnglePeriod;

        /// 외접원 반지름 1, 각도 0 일 때의 폴리라인.
        /// 닫힌 모양은 첫 점을 끝에 한 번 더 넣는다.
        public readonly Vector2[] UnitPoints;

        /// PivotSpot.Middle 이 걸리는 지점.
        public readonly Vector2 MiddleAnchor;

        /// Size(R) 가 뜻하는 치수의 배율.
        /// 무엇의 치수인지는 Shape 마다 다르다.
        public readonly float DimensionPerRadius;

        /// 잉크 소모량 ÷ R. 외접원이 아니라
        /// 실제 폴리라인 길이라 Shape 마다 크게 다르다.
        public readonly float InkPerRadius;

        /// 후보 각도를 몇 개로 쪼갤지.
        /// 탐색 비용과 직결돼 튜닝 대상이므로 가변이다.
        public int AngleDivisions;

        public ShapeRule(float anglePeriod, Vector2[] unitPoints, Vector2 middleAnchor,
            float dimensionPerRadius, int angleDivisions)
        {
            AnglePeriod = anglePeriod;
            UnitPoints = unitPoints;
            MiddleAnchor = middleAnchor;
            DimensionPerRadius = dimensionPerRadius;
            AngleDivisions = angleDivisions;
            InkPerRadius = PolylineLength(unitPoints);
        }

        /// PivotSpot.Start 가 걸리는 대표 꼭짓점.
        public Vector2 StartAnchor => UnitPoints[0];

        public int PointCount => UnitPoints.Length;

        static float PolylineLength(Vector2[] points)
        {
            float sum = 0f;
            for (int i = 1; i < points.Length; i++)
                sum += Vector2.Distance(points[i - 1], points[i]);
            return sum;
        }
    }

    public static class PrimitiveShapeExtensions
    {
        /// 그릇 호를 이루는 점 수. 8~12 사이.
        const int BowlPoints = 9;

        /// <summary>
        /// Shape 규칙표. enum 값 순서대로 놓는다.
        /// Shape 를 늘릴 때 고치는 곳은 여기 하나다.
        /// </summary>
        static readonly ShapeRule[] Table =
        {
            // 직선 — 뒤집어도 같은 선이라 주기가 180°.
            // 0 은 끝점, 0.5 는 무게중심, 길이 = 2R.
            new ShapeRule(Mathf.PI,
                new[] { new Vector2(-1f, 0f), new Vector2(1f, 0f) },
                Vector2.zero, 2f, 8),

            // 그릇 — 아래 반원. 0 은 한쪽 팔 끝,
            // 0.5 는 바닥 중앙, 치수는 팔 끝 간 거리.
            // 무게중심은 U자 빈 공간에 놓여 어색하다.
            new ShapeRule(2f * Mathf.PI,
                BowlArc(),
                new Vector2(0f, -1f), 2f, 16),

            // 정삼각형 — 세 변이 같아 120° 마다 겹친다.
            // 0 은 한 꼭짓점, 0.5 는 무게중심,
            // 외접원 R 인 정삼각형의 한 변은 √3·R.
            new ShapeRule(2f * Mathf.PI / 3f,
                TriangleLoop(),
                Vector2.zero, Mathf.Sqrt(3f), 6),
        };

        public static ShapeRule Rule(this PrimitiveShape shape) => Table[(int)shape];

        /// <summary>
        /// 각도가 한 바퀴 도는 주기(라디안).
        /// 이 값을 넘는 각은 같은 모양이 된다
        /// </summary>
        public static float AnglePeriod(this PrimitiveShape shape) => shape.Rule().AnglePeriod;

        /// 표가 enum 을 다 덮는지. 테스트가 본다.
        public static int RuleCount => Table.Length;

        static Vector2[] BowlArc()
        {
            var points = new Vector2[BowlPoints];
            for (int i = 0; i < BowlPoints; i++)
            {
                float t = Mathf.PI + Mathf.PI * i / (BowlPoints - 1);
                points[i] = new Vector2(Mathf.Cos(t), Mathf.Sin(t));
            }
            return points;
        }

        static Vector2[] TriangleLoop()
        {
            var points = new Vector2[4];
            for (int i = 0; i < 3; i++)
            {
                float t = 2f * Mathf.PI * i / 3f;
                points[i] = new Vector2(Mathf.Cos(t), Mathf.Sin(t));
            }
            points[3] = points[0];
            return points;
        }
    }
}
