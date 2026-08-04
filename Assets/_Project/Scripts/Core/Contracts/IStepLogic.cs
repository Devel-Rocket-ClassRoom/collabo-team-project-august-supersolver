namespace PPS.Core
{
    /// <summary>
    /// 장치가 구현하는 계약.
    /// 경과 시간이 인자에 없다는 것 자체가
    /// 결정론 강제 장치다.
    /// </summary>
    public interface IStepLogic
    {
        /// <param name="step">시간 계산은 이 값으로만.</param>
        /// <param name="rng">월드가 주입한 단일 인스턴스.</param>
        void Tick(int step, System.Random rng);

        /// <summary>
        /// 아직 할 일이 남았는가.
        /// true 면 전 바디가 잠들어도
        /// Stalled 로 끝내지 않는다.
        /// </summary>
        bool HasPendingWork { get; }
    }
}
