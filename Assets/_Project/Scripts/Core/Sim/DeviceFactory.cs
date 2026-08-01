using System.Collections.Generic;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// <see cref="DeviceData"/> → <see cref="IStepLogic"/>.
    ///
    /// <see cref="WorldBuilder"/> 가 직접 분기하지 않는 이유는 장치 목록이 계속 늘어날 것이기
    /// 때문이다. 그 변경이 월드 구축 코드에 섞이면 "등록 순서가 이 클래스의 존재 이유"라는
    /// WorldBuilder 의 계약이 잡음에 묻힌다.
    ///
    /// **장치를 추가하는 곳이 여기다.** <see cref="DeviceType"/> 에 값을 뒤에 붙이고
    /// 이 switch 에 한 줄을 더하면 된다. 그 외의 코어 코드는 건드릴 필요가 없다.
    /// </summary>
    public static class DeviceFactory
    {
        public static IStepLogic Create(in DeviceData data, IReadOnlyList<Rigidbody2D> bodies)
        {
            switch (data.Type)
            {
                case DeviceType.Bomb:
                    return new BombDevice(data, bodies);

                default:
                    // 조용히 건너뛰지 않는다. 모르는 장치를 무시하면 레벨 데이터와 다른 월드가
                    // 만들어지고, 솔버는 그 월드를 근거로 "이 레벨은 풀린다"고 판정한다.
                    // 판정의 정당성이 걸려 있으므로 여기서는 시끄럽게 죽는 편이 맞다.
                    throw new System.ArgumentOutOfRangeException(
                        nameof(data),
                        $"알 수 없는 장치 종류: {(int)data.Type}. DeviceFactory 에 등록되지 않았다.");
            }
        }
    }
}
