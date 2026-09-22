using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PPS.Core
{
    /// <summary>
    /// 장치가 자기를 세우는 데 필요한 것 묶음.
    /// 등록 순서가 결과를 바꾸므로 목록은 살아 있는 참조다.
    /// </summary>
    public readonly struct DeviceBuildContext
    {
        /// 레벨의 장치 번호. 바디 이름과 알림에 쓴다.
        public readonly int Index;

        /// 이 장치가 놓인 판. 움직이는 장치가 범위를 읽는다.
        public readonly LevelData Level;

        /// 장치는 바디를 만들어도 된다.
        public readonly Scene Scene;

        /// 만든 바디를 생성 순서대로 넣는다.
        public readonly List<Rigidbody2D> Bodies;

        /// 닿으면 실패하는 콜라이더 목록.
        public readonly List<Collider2D> Hazards;

        /// 터졌음을 알릴 통로. 표시 전용이다.
        public readonly SimEvents Events;

        /// 바디 이름(Device_0 …).
        public readonly string Name;

        public DeviceBuildContext(
            int index,
            LevelData level,
            Scene scene,
            List<Rigidbody2D> bodies,
            List<Collider2D> hazards,
            SimEvents events)
        {
            Index = index;
            Level = level;
            Scene = scene;
            Bodies = bodies;
            Hazards = hazards;
            Events = events;
            Name = $"Device_{index}";
        }
    }
}
