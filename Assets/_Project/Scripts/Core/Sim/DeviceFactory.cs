using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        /// <param name="index">레벨 데이터에서의 위치. 바디 이름(<c>Device_0</c> …)에 쓴다.</param>
        /// <param name="scene">
        /// 격리 물리 씬. **장치는 바디를 만들어도 된다** — 장애물이나 문처럼 물리적 실체가 있는 장치는
        /// 콜라이더가 있어야 성립한다. 바람 구역처럼 영역 효과만 있는 장치가 안 만들 뿐이다.
        /// </param>
        /// <param name="bodies">
        /// 월드의 바디 목록. 장치가 바디를 만들었다면 **여기에 생성 순서대로 넣는다.**
        /// <see cref="WorldBuilder"/> 의 고정 순서(공 → 지형 → 장치 → 스트로크 → 회전축)에서
        /// 장치 구간이 바로 이 자리다. 넣지 않으면 해시에도 <c>AllBodiesSleeping</c> 판정에도 잡히지 않는다.
        ///
        /// 동시에 장치가 **영향을 줄 대상 목록**이기도 하다. 살아 있는 참조이므로
        /// 나중에 등록될 스트로크까지 <see cref="IStepLogic.Tick"/> 시점에는 전부 보인다.
        /// </param>
        public static IStepLogic Create(in DeviceData data, int index, Scene scene, List<Rigidbody2D> bodies)
        {
            switch (data.Type)
            {
                case DeviceType.Bomb:
                {
                    // 바디를 만들고 **먼저 목록에 넣은 뒤** 장치를 만든다.
                    // 등록 순서를 결정하는 것은 장치가 아니라 이곳이라야 순서 계약이 한곳에 모인다.
                    var body = BombDevice.CreateBody(scene, data, $"Device_{index}");
                    bodies.Add(body);

                    return new BombDevice(data, body, bodies);
                }

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
