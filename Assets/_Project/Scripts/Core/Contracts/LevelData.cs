using System;
using System.Collections.Generic;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 레벨 정의. 외부 JSON 으로 정의되고 **데이터 추가만으로 동작해야 한다**
    /// (미션 가이드: 레벨 추가에 코드 수정이 필요하면 요구사항 미충족).
    ///
    /// M01 범위에서는 "공 하나가 굴러가는 최소 코어"에 필요한 골격만 둔다.
    /// 장치 6종·별·앵커는 팀원 C 의 레벨 에디터 작업과 함께 확장되며,
    /// 이 클래스는 그 확장을 전제로 한 **합류 지점**이다.
    ///
    /// JsonUtility 직렬화 대상이므로 public 필드 + [Serializable] 만 사용한다.
    /// (프로퍼티·Dictionary·nullable 은 JsonUtility 가 처리하지 못한다)
    /// </summary>
    [Serializable]
    public class LevelData
    {
        public string Id = "L000";

        /// 그릴 수 있는 총 길이 제한. 없으면 화면을 가득 채우는 것이 최적해가 되어 퍼즐이 성립하지 않는다.
        public float InkLimit = 20f;

        public Vector2 BallStart;
        public float BallRadius = 0.25f;

        public Vector2 GoalPosition;
        public float GoalRadius = 0.5f;

        /// 레벨 지형. **리스트 순서가 곧 월드 등록 순서**이므로 순서를 바꾸면 결과가 달라진다.
        public List<StaticSegment> Terrain = new List<StaticSegment>();

        /// <summary>
        /// 레벨에 배치된 장치. **리스트 순서가 곧 로직 등록 순서**이며,
        /// 그 순서가 곧 난수 소비 순서다 (스텝마다 이 순서로 <see cref="IStepLogic.Tick"/> 이 불린다).
        ///
        /// 스키마(<see cref="DeviceData"/>)는 장치 담당이 자유롭게 바꿔도 되는 placeholder 다.
        /// </summary>
        public List<DeviceData> Devices = new List<DeviceData>();

        /// 이 높이 아래로 떨어지면 낙사(Fail). 장애물 장치가 들어오기 전까지의 최소 실패 조건.
        public float KillY = -20f;

        public static LevelData FromJson(string json) => JsonUtility.FromJson<LevelData>(json);

        public string ToJson(bool prettyPrint = true) => JsonUtility.ToJson(this, prettyPrint);
    }

    /// <summary>레벨의 붙박이 지형 한 조각.</summary>
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
