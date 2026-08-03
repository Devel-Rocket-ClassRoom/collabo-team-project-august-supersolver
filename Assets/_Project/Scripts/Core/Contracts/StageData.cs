using System;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 한 판의 정의. 레벨 + 시드.
    /// 시드는 스테이지의 성질이라
    /// 플레이마다 바뀌면 안 된다.
    /// </summary>
    [Serializable]
    public class StageData
    {
        /// 판을 가리키는 유일한 식별자.
        public string StageId = "S000";

        /// 리플레이에도 함께 저장해야 한다.
        public int Seed;

        /// 임베드. 같은 맵을 쓰면 사본이 생긴다.
        public LevelData Level = new LevelData();

        public static StageData FromJson(string json) => JsonUtility.FromJson<StageData>(json);

        public string ToJson(bool prettyPrint = true) => JsonUtility.ToJson(this, prettyPrint);
    }
}
