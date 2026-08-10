using UnityEngine;

namespace PPS.Solver
{
    /// <summary>
    /// 프리미티브를 놓을 근거가 되는 지점.
    /// 레벨 데이터에서 유도되므로 시뮬을 돌리기 전에 정해진다.
    /// </summary>
    public readonly struct SolverAnchor
    {
        /// 이 앵커가 가리키는 자리.
        public readonly Vector2 Position;

        /// <summary>
        /// 이 자리에 놓을 수 있다고 셀렉터가 정한 그림들.
        /// 앵커마다 다르다 — 같은 자리라도 왜 그 자리인지에 따라
        /// 그릴 만한 그림이 갈린다.
        /// </summary>
        public readonly Primitive[] Primitives;

        public SolverAnchor(Vector2 position, Primitive[] primitives)
        {
            Position = position;
            Primitives = primitives;
        }

        /// <summary>
        /// 같은 앵커로 볼 것인가. 지금은 자리만 본다.
        /// Vector2 == 가 근사 비교라 이어진 선분이 공유하는 끝점처럼
        /// 계산 경로가 달라 미세하게 어긋난 좌표도 하나로 잡힌다.
        /// </summary>
        public bool Matches(SolverAnchor other) => Position == other.Position;
    }
}
