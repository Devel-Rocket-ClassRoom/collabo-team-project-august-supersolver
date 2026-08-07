using PPS.Core;

namespace PPS.Solver
{
    /// <summary>
    /// 벡터 하나를 시행 하나로 바꾼다.
    /// 탐색이 붙는 면은 여기뿐이다 — 벡터를 던지면
    /// 시뮬 결과나 거부 사유가 돌아온다.
    /// 레벨마다 하나 만들어 재사용한다. 코덱이
    /// 플레이 영역을 미리 재 두기 때문이다.
    /// </summary>
    public sealed class PrimitiveTrial
    {
        readonly LevelData _level;
        readonly int _seed;
        readonly PrimitiveCodec _codec;

        public PrimitiveTrial(StageData stage) : this(stage.Level, stage.Seed)
        {
        }

        public PrimitiveTrial(LevelData level, int seed)
        {
            _level = level;
            _seed = seed;
            _codec = new PrimitiveCodec(level);
        }

        /// <summary>
        /// 검사 순서는 싼 것부터다 — 길이, 배치, 그리고 시뮬.
        /// 시뮬 한 번이 앞의 둘을 합친 것보다 훨씬 비싸므로
        /// 거부는 시뮬 전에 끝난다.
        /// </summary>
        public TrialResult Run(float[] vector, int maxSteps = SimWorld.DefaultMaxSteps)
            => Run(vector, null, maxSteps);

        /// <summary>
        /// 궤적을 담으며 돌린다.
        /// 거부된 시행은 시뮬이 없으므로 버퍼가 비어서 온다 —
        /// 지난 시행 값이 남으면 남의 궤적을 읽게 된다.
        /// </summary>
        public TrialResult RunSampled(
            float[] vector, TrajectoryBuffer buffer, int maxSteps = SimWorld.DefaultMaxSteps)
        {
            if (buffer == null) throw new System.ArgumentNullException(nameof(buffer));
            return Run(vector, buffer, maxSteps);
        }

        TrialResult Run(float[] vector, TrajectoryBuffer buffer, int maxSteps)
        {
            if (vector.Length % PrimitiveCodec.Dimensions != 0)
                return Reject(PlacementReject.BadVector, buffer);

            var primitives = _codec.Decode(vector);

            float usedInk = 0f;
            for (int i = 0; i < primitives.Length; i++)
            {
                var reject = PrimitiveValidator.Validate(primitives[i], _level, usedInk);
                if (reject != PlacementReject.None)
                    return Reject(reject, buffer);

                usedInk += PrimitiveValidator.Ink(primitives[i]);
            }

            return new TrialResult(buffer == null
                ? PrimitiveRunner.Run(_level, primitives, _seed, maxSteps)
                : PrimitiveRunner.RunSampled(_level, primitives, _seed, buffer, maxSteps));
        }

        static TrialResult Reject(PlacementReject reject, TrajectoryBuffer buffer)
        {
            buffer?.Clear();
            return new TrialResult(reject);
        }
    }
}
