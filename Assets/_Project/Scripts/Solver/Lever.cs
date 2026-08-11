using System.Collections.Generic;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver
{
    /// <summary>
    /// 공을 띄우는 지렛대. 판 하나와 추 하나, 그리고 월드 축이다.
    /// 판 위의 자리는 전부 길이 대비 비율로 잡는다 — 같은 비율이면
    /// 크기만 다른 같은 지렛대라, 프리셋을 찾을 때 축이 겹치지 않는다.
    /// 판은 늘 수평에서 시작하고, 좌우는 Facing 으로만 갈린다.
    /// </summary>
    public readonly struct Lever : IPrimitive
    {
        /// 추 상자의 폭. 판 길이 대비다.
        public const float WeightWidthRatio = 0.2f;

        /// <summary>
        /// 추 상자의 중심이 오는 자리. 상자 반폭과 같아 상자가 판 끝에 맞는다.
        /// 추를 더 안쪽으로 들이면 팔이 짧아지기만 해서 이득이 없다.
        /// </summary>
        public const float WeightAt = WeightWidthRatio * 0.5f;

        /// 공이 얹히는 자리. 월드 좌표다.
        public readonly Vector2 BallSeat;

        /// 판 전체 길이.
        public readonly float Length;

        /// 축이 놓이는 자리. 길이 대비 비율이다.
        public readonly float FulcrumAt;

        /// 공이 얹히는 자리. 길이 대비 비율이다.
        public readonly float BallAt;

        /// 추에 채운 줄 수. 이것이 추의 무게다.
        public readonly int WeightRows;

        /// 추 상자 바닥이 판에서 얼마나 위에 있는지. 낙차다.
        public readonly float Drop;

        /// 판이 뻗는 쪽.
        public readonly bool FacingRight;

        public Lever(
            Vector2 ballSeat, float length, float fulcrumAt, float ballAt,
            int weightRows, float drop, bool facingRight = true)
        {
            BallSeat = ballSeat;
            Length = length;
            FulcrumAt = fulcrumAt;
            BallAt = ballAt;
            WeightRows = Mathf.Max(1, weightRows);
            Drop = drop;
            FacingRight = facingRight;
        }

        /// <summary>
        /// 말이 되는 배치인가.
        /// 추와 공이 축을 사이에 두고 갈라져야 지렛대가 된다.
        /// </summary>
        public bool IsValid => FulcrumAt > WeightAt && BallAt > FulcrumAt && BallAt <= 1f;

        /// 판이 뻗는 방향. 늘 수평이다.
        public Vector2 Along => FacingRight ? Vector2.right : Vector2.left;

        /// 판의 0 지점. 추가 놓이는 쪽 끝이다.
        public Vector2 Origin => BallSeat - Vector2.up * LevelData.BallRadius
                                 - Along * (BallAt * Length);

        public Vector2 PlankEnd => Origin + Along * Length;

        public Vector2 Fulcrum => Origin + Along * (FulcrumAt * Length);

        /// 추가 떨어져 닿을 판 위의 자리.
        public Vector2 WeightFoot => Origin + Along * (WeightAt * Length);

        public float WeightWidth => WeightWidthRatio * Length;

        /// <summary>
        /// 상자 높이. 줄 수가 무게를 정하므로 높이는 그 줄이
        /// 겹치지 않게 들어갈 최소치다 — 더 키우면 자리만 먹는다.
        /// </summary>
        public float WeightHeight => (WeightRows - 1) * WeightBlock.MinRowGap;

        public WeightBlock Weight => new WeightBlock(
            WeightFoot + Vector2.up * (Drop + WeightHeight * 0.5f),
            new Vector2(WeightWidth, WeightHeight),
            WeightRows);

        /// <summary>
        /// 이 지렛대를 놓으려면 추 자리 위로 필요한 세로 공간.
        /// 배치할 때 지형 여유와 견주는 값이다.
        /// </summary>
        public float RequiredHeadroom => Drop + WeightHeight;

        /// 판과 추를 그리는 데 드는 잉크. 후보를 볼 순서를 정한다.
        public float Ink => Length + Weight.Length;

        /// <summary>
        /// 판이 축을 중심으로 angle 만큼 돈 자리.
        /// 양수가 추 쪽이 내려가는 방향이다 — 지렛대가 실제로 도는 쪽이다.
        /// 놓을 자리가 되는지 보려면 시작 자리만이 아니라
        /// 지나갈 자리를 전부 봐야 한다.
        /// </summary>
        public void Swept(float angle, out Vector2 from, out Vector2 to)
        {
            // 왼쪽을 보면 같은 회전이 반대로 보인다.
            float turn = FacingRight ? angle : -angle;

            Vector2 pivot = Fulcrum;
            from = pivot + Turn(Origin - pivot, turn);
            to = pivot + Turn(PlankEnd - pivot, turn);
        }

        static Vector2 Turn(Vector2 vector, float radians)
        {
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);

            return new Vector2(
                vector.x * cos - vector.y * sin,
                vector.x * sin + vector.y * cos);
        }

        /// 같은 지렛대를 반대쪽으로 뒤집은 것.
        public Lever Mirrored => new Lever(
            BallSeat, Length, FulcrumAt, BallAt, WeightRows, Drop, !FacingRight);

        /// 이 지렛대를 다른 자리에 그대로 옮긴 것.
        public Lever At(Vector2 ballSeat) => new Lever(
            ballSeat, Length, FulcrumAt, BallAt, WeightRows, Drop, FacingRight);

        /// <summary>
        /// 판·추·축을 솔루션에 붙인다.
        /// 축이 스트로크 인덱스를 참조해서, 인덱스를 아는 여기서 넣어야 한다.
        /// 추는 판보다 뒤에 온다 — 등록 순서가 물리 결과를 정한다.
        /// </summary>
        public void AppendTo(Solution solution)
        {
            int plank = solution.Strokes.Count;

            solution.Strokes.Add(new Stroke(
                ToolType.FreeBody,
                new List<Vector2> { Origin, PlankEnd }));

            solution.Strokes.Add(Weight.ToStroke());

            solution.Pivots.Add(new PivotJoint(plank, PivotJoint.WorldIndex, Fulcrum));
        }

        public override string ToString()
            => $"L{Length:F2} 축{FulcrumAt:F2} 공{BallAt:F2} {WeightRows}줄 낙차{Drop:F2}";
    }
}
