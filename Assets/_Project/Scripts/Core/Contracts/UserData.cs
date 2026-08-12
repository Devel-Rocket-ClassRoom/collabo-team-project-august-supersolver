using System;
using System.Collections.Generic;
using UnityEngine;

namespace PPS.Core
{
    [Serializable]
    public class UserData
    {
        // 저장 데이터 형식의 버전이다.
        public int Version = 1;

        // 플레이어가 마지막으로 클리어한 스테이지의 순서다.
        // 아직 아무 스테이지도 클리어하지 않았다면 -1을 사용한다.
        public int LastClearedStageIndex = -1;

        // 각 스테이지의 클리어 결과를 보관한다.
        public List<StageClearData> StageClears = new List<StageClearData>();
    }

    [Serializable]
    public class StageClearData
    {
        // 이 기록이 어느 스테이지에 해당하는지 나타낸다.
        public int StageIndex;

        // 해당 스테이지를 한 번이라도 클리어했는지 나타낸다.
        public bool IsCleared;

        // 해당 스테이지에서 획득한 최고 별 개수다.
        public int BestStars;
    }
}
