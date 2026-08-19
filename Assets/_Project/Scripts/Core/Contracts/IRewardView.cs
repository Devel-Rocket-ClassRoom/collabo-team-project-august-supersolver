using System;

namespace PPS.Core
{
    /// <summary>보상 UI 를 띄우는 쪽이 아는 유일한 창구.</summary>
    public interface IRewardView
    {
        void Show(RewardViewModel vm);
        void BindButtonListener(Action retry, Action home, Action next);
    }
}
