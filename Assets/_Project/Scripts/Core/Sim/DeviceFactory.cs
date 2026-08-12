using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PPS.Core
{
    /// <summary>
    /// DeviceData → IStepLogic.
    /// 장치를 추가하는 곳이 여기다.
    /// DeviceType 값 하나 + switch 한 줄.
    /// </summary>
    public static class DeviceFactory
    {
        /// <summary>
        /// 이 종류가 바디를 만드는가.
        /// 장치 번호로 바디를 찾으려면 앞의 장치 중
        /// 바디를 만든 것만 세어야 한다.
        /// </summary>
        public static bool MakesBody(DeviceType type) => type != DeviceType.Wind;

        /// <param name="index">바디 이름(Device_0 …)에 쓴다.</param>
        /// <param name="scene">장치는 바디를 만들어도 된다.</param>
        /// <param name="bodies">만든 바디를 생성 순서대로 넣는다.</param>
        /// <param name="hazards">닿으면 실패하는 콜라이더 목록.</param>
        public static IStepLogic Create(
            in DeviceData data,
            int index,
            Scene scene,
            List<Rigidbody2D> bodies,
            List<Collider2D> hazards)
        {
            string name = $"Device_{index}";

            switch (data.Type)
            {
                case DeviceType.Bomb:
                {
                    // 바디를 먼저 목록에 넣은 뒤 장치를 만든다.
                    // 등록 순서를 정하는 곳을 여기 하나로 모은다.
                    var body = BombDevice.CreateBody(scene, data, name);
                    bodies.Add(body);

                    return new BombDevice(data, body, bodies);
                }

                case DeviceType.FragBomb:
                {
                    // 몸체는 위험하지 않다. 파편만 위험 목록에 든다.
                    var body = FragBombDevice.CreateBody(scene, data, name);
                    bodies.Add(body);

                    return new FragBombDevice(data, body, scene, name, bodies, hazards);
                }

                case DeviceType.Spike:
                {
                    // 몸 전체가 위험하다. 파편 폭탄과 달리
                    // 처음부터 끝까지 그 자리에 있다.
                    var body = SpikeDevice.CreateBody(scene, data, name);
                    bodies.Add(body);
                    hazards.Add(body.GetComponent<Collider2D>());

                    return new SpikeDevice();
                }

                case DeviceType.Wind:
                    // 바디가 없다. 등록 순서를 지키려고
                    // 자리만 비워 두지도 않는다 — 바디 목록은
                    // 만든 것만 담는다는 규칙이 먼저다.
                    return new WindDevice(data, bodies);

                default:
                    // 조용히 건너뛰면 레벨과 다른 월드가 서고,
                    // 솔버가 그걸 근거로 판정한다. 시끄럽게 죽는다.
                    throw new System.ArgumentOutOfRangeException(
                        nameof(data),
                        $"알 수 없는 장치 종류: {(int)data.Type}. DeviceFactory 에 등록되지 않았다.");
            }
        }
    }
}
