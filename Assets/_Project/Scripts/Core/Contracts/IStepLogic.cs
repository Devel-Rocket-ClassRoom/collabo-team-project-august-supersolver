namespace PPS.Core
{
    /// <summary>
    /// 레벨 장치 6종(목표·별·장애물·스위치와 문·바람 구역·폭탄)이 구현하는 계약.
    ///
    /// **이 인터페이스의 시그니처 자체가 결정론 강제 장치다.**
    /// 경과 시간이 인자에 없으므로 장치는 스텝 카운트로만 시간을 셀 수 있고,
    /// 난수는 주입된 인스턴스만 쓸 수 있다. Time 계열이나 UnityEngine.Random 을
    /// 쓰려면 계약을 어기고 밖에서 끌어와야 하며, 그 순간 리뷰에서 걸린다.
    /// </summary>
    public interface IStepLogic
    {
        /// <param name="step">현재 스텝. 시간 계산은 오직 이 값으로만 한다.</param>
        /// <param name="rng">월드가 시드로 생성해 주입한 단일 인스턴스.</param>
        void Tick(int step, System.Random rng);

        /// <summary>
        /// 아직 처리할 일이 남아 있는가. 폭탄이 폭발을 기다리는 중이면 true.
        ///
        /// Stalled 판정에 쓰인다. 모든 바디가 잠들어도 이 값이 true 인 장치가 하나라도
        /// 있으면 시뮬을 계속해야 한다 — 대기 중인 폭탄이 곧 상황을 바꿀 수 있기 때문.
        /// </summary>
        bool HasPendingWork { get; }
    }
}
