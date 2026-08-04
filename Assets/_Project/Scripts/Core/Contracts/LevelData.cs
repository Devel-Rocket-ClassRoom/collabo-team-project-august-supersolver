using System;
using System.Collections.Generic;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 레벨 정의. 코드 수정 없이
    /// 데이터만으로 추가할 수 있어야 한다.
    /// JSON 진입점은 StageData 에 있다.
    /// </summary>
    [Serializable]
    public class LevelData
    {
        /// 그릴 수 있는 총 길이 제한.
        public float InkLimit = 20f;

        public Vector2 BallStart;
        public float BallRadius = 0.25f;

        public Vector2 GoalPosition;
        public float GoalRadius = 0.5f;

        /// 리스트 순서 = 월드 등록 순서.
        public List<StaticSegment> Terrain = new List<StaticSegment>();

        /// 리스트 순서 = 로직 등록 순서
        /// = 난수 소비 순서.
        public List<DeviceData> Devices = new List<DeviceData>();

        /// 이 아래로 떨어지면 Fail.
        public float KillY = -20f;
    }

    /// <summary>붙박이 지형 한 조각.</summary>
    [Serializable]
    public struct StaticSegment
    {
        public Vector2 A;
        public Vector2 B;

        public StaticSegment(Vector2 a, Vector2 b)
        {
            A = a;
            B = b;
        }
    }
}
