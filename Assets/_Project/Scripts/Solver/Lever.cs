using System.Collections.Generic;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver
{
    /// <summary>
    /// 공을 띄우는 지렛대. 판 하나와 추 하나, 그리고 월드 축이다.
    /// 공은 판의 한쪽 끝에 얹히고, 반대쪽 끝 위에서 추가 떨어진다.
    /// 축이 월드에 박혀 있어 받침이 미끄러질 여지가 없다 —
    /// 실측값이 배치마다 흔들리면 표를 쓸 수 없다.
    /// </summary>
    public readonly struct Lever
    {
        /// 회전 중심. 월드 좌표.
        public readonly Vector2 Fulcrum;

        /// 축에서 공이 얹히는 끝까지.
        public readonly float BallArm;

        /// 축에서 추가 떨어지는 끝까지.
        public readonly float WeightArm;

        /// <summary>
        /// 판의 기울기(라디안). 공 쪽이 이 방향이다 —
        /// 0 이면 공이 오른쪽, π 면 왼쪽이다.
        /// </summary>
        public readonly float Angle;

        /// 추가 차지하는 자리. 무게와 무관하게 고정이다.
        public readonly Vector2 WeightSize;

        /// 추에 채운 줄 수. 이것이 추의 무게다.
        public readonly int WeightRows;

        /// 추를 판 끝에서 얼마나 위에 놓을지. 낙차이자 타이밍이다.
        public readonly float Drop;

        public Lever(
            Vector2 fulcrum, float ballArm, float weightArm,
            float angle, Vector2 weightSize, int weightRows, float drop)
        {
            Fulcrum = fulcrum;
            BallArm = ballArm;
            WeightArm = weightArm;
            Angle = angle;
            WeightSize = weightSize;
            WeightRows = weightRows;
            Drop = drop;
        }

        /// 공 쪽 방향의 단위 벡터.
        public Vector2 Along => new Vector2(Mathf.Cos(Angle), Mathf.Sin(Angle));

        /// 공이 얹히는 판 끝.
        public Vector2 BallEnd => Fulcrum + Along * BallArm;

        /// 추가 떨어지는 판 끝.
        public Vector2 WeightEnd => Fulcrum - Along * WeightArm;

        /// <summary>
        /// 공을 얹어 둘 자리. 판 끝에 반지름만큼 띄워 올린다.
        /// 판에 파묻힌 채로 시작하면 밀려나며 튄다.
        /// </summary>
        public Vector2 BallSeat => BallEnd + Vector2.up * LevelData.BallRadius;

        /// <summary>
        /// 떨어뜨릴 추. 상자 아래가 판 끝에서 Drop 만큼 위에 온다 —
        /// 낙차를 상자 크기와 무관하게 읽으려면 바닥을 기준으로 잡아야 한다.
        /// </summary>
        public WeightBlock Weight => new WeightBlock(
            WeightEnd + Vector2.up * (Drop + WeightSize.y * 0.5f),
            WeightSize,
            WeightRows);

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
                new List<Vector2> { WeightEnd, BallEnd }));

            solution.Strokes.Add(Weight.ToStroke());

            solution.Pivots.Add(new PivotJoint(plank, PivotJoint.WorldIndex, Fulcrum));
        }
    }
}
