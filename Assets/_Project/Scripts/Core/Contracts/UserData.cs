using System;
using System.Collections.Generic;
using UnityEngine;

namespace PPS.Core
{
    [Serializable]
    public class UserData
    {
        // 지금 코드가 다룰 수 있는 저장 데이터 형식의 버전이다.
        // 스키마를 바꾸는 커밋에서 함께 올린다.
        public const int CurrentVersion = 3;

        // 이 저장물이 어느 형식으로 쓰였는지 나타낸다.
        // 옛 저장물을 복원하면 그때의 값이 그대로 들어온다.
        public int Version = CurrentVersion;

        // 아직 아무 스테이지도 클리어하지 않았음을 나타낸다.
        public const int NoStageCleared = -1;

        // 플레이어가 가장 멀리 클리어한 스테이지의 전역 번호다.
        // 테마와 스테이지를 한 축으로 합친 값이며, 진척도를
        // 판정하는 유일한 기준이다. 변환은 CurrentStageIndex 가 한다.
        public int LastClearedStageIndex = NoStageCleared;

        // 각 스테이지의 클리어 결과를 보관한다.
        public List<StageClearData> StageClears = new List<StageClearData>();
    }

    [Serializable]
    public class StageClearData
    {
        // 이 기록이 어느 스테이지에 해당하는지 나타낸다.
        // LastClearedStageIndex 와 같은 전역 번호다.
        public int StageIndex;

        // 해당 스테이지를 한 번이라도 클리어했는지 나타낸다.
        public bool IsCleared;

        // 해당 스테이지에서 획득한 최고 별 개수다.
        public int BestStars;

        // 클리어할 때 쓴 잉크로 정해진 별 등급이다.
        // InkGrade의 Bronze(0) / Silver(1) / Gold(2) 중 하나다.
        public int StarGrade;
    }
}
