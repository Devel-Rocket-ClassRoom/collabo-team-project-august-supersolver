using System;

namespace PPS.Core
{
    /// <summary>
    /// 시뮬레이션이 알리는 사건.
    /// <para>
    /// 표시 전용이다. 구독자가 월드를 되돌려 건드리면
    /// 같은 입력에 다른 결과가 나와 솔버가 무너진다.
    /// </para>
    /// <para>
    /// 물리 콜백과 달리 발행 시점이 우리 손에 있다 —
    /// 로직·판정이 도는 정해진 자리에서만 부르므로
    /// 부르는 순서가 스텝마다 같다.
    /// </para>
    /// <para>
    /// 자세한 것은 번호로 월드에 물어본다. 장치는
    /// SimWorld.GetDevice, 별은 Level.Stars 를 본다.
    /// 별과 장치는 서로 다른 목록이라 번호가 겹친다 —
    /// 어느 쪽 번호인지는 어느 사건을 받았는지로 가른다.
    /// </para>
    /// </summary>
    public sealed class SimEvents
    {
        /// <summary>
        /// 장치가 발동했다. 번호는 Level.Devices 의 것이다.
        /// <para>
        /// GetDevice 로 얻는 바디는 <b>이 호출 안에서만</b> 유효하다.
        /// 폭탄은 알린 직후 제 몸을 지운다 — 참조를 들고 있다가
        /// 나중에 쓰면 파괴된 것을 만진다.
        /// 뒤에 쓸 값은 지금 복사해 둬야 한다.
        /// </para>
        /// </summary>
        public event Action<int> DeviceFired;

        /// <summary>
        /// 별을 먹었다. 번호는 Level.Stars 의 것이다.
        /// 별은 바디가 없어 자리는 그 목록에서 읽는다.
        /// </summary>
        public event Action<int> StarCollected;

        internal void RaiseDeviceFired(int index) => DeviceFired?.Invoke(index);

        internal void RaiseStarCollected(int index) => StarCollected?.Invoke(index);
    }
}
