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

        /// 모으면 점수가 되는 지점.
        /// 물리에 관여하지 않는다 — 콜라이더가 없다.
        public List<Vector2> Stars = new List<Vector2>();

        /// 공 중심이 이 안에 들어오면 수집.
        public float StarRadius = 0.35f;

        /// 플레이어가 쓸 수 있는 도구.
        /// 비어 있으면 제한 없음 — 기존 레벨 파일에
        /// 이 항목이 없어도 그대로 열린다.
        public List<ToolType> AllowedTools = new List<ToolType>();

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
