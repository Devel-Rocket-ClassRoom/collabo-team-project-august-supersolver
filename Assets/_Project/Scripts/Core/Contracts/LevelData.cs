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
        public const float BallRadius = 0.25f;
        public const float GoalRadius = 0.5f;
        public const float StarCaptureRadius = 0.35f;

        public float InkLimit = 20f;

        public Vector2 BallStart;

        public Vector2 GoalPosition;

        /// 리스트 순서 = 월드 등록 순서.
        public List<StaticSegment> Terrain = new List<StaticSegment>();

        /// 리스트 순서 = 로직 등록 순서 = 난수 소비 순서.
        public List<DeviceData> Devices = new List<DeviceData>();
        public List<Vector2> Stars = new List<Vector2>();

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
