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
        /// 지금 코드가 쓰는 저장 형식.
        public const int CurrentVersion = 1;

        /// 없는 파일 = 0 = 옛 공통 DeviceData 형식.
        public int Version;

        /// 판을 가리키는 유일한 식별자.
        public string StageId = "S000";

        /// 리플레이에도 함께 저장해야 한다.
        public int Seed;

        /// 임베드. 같은 맵을 쓰면 사본이 생긴다.
        public LevelData Level = new LevelData();

        /// <summary>
        /// 원본 문자열이 마이그레이션까지 함께 간다.
        /// JsonUtility 가 새 형식에 없는 필드를 이미 버려서
        /// 옛 값은 거기서만 되찾을 수 있다.
        /// </summary>
        public static StageData FromJson(string json)
        {
            var stage = JsonUtility.FromJson<StageData>(json);
            if (stage == null) return null;

            StageDataMigration.Migrate(stage, json);
            return stage;
        }

        public string ToJson(bool prettyPrint = true)
        {
            Version = CurrentVersion;
            Level?.PackDevices();

            return JsonUtility.ToJson(this, prettyPrint);
        }
    }
}
