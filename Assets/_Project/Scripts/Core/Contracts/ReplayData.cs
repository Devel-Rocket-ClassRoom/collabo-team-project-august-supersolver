using System;
using UnityEngine;

namespace PPS.Core
{
    // 하나의 스테이지와 해당 스테이지에서 완성된 풀이를 묶은 리플레이 데이터다.
    [Serializable]
    public class ReplayData
    {
        // 현재 리플레이 JSON 저장 형식의 버전이다.
        public const int CurrentVersion = 1;

        // 이 파일이 어떤 저장 형식으로 만들어졌는지 기록한다.
        public int Version = CurrentVersion;

        // 맵 에디터에서 만든 스테이지 전체 정보다.
        public StageData Stage = new StageData();

        // 드로잉 툴에서 완성한 Stroke와 Pivot 정보다.
        public Solution Solution = new Solution();

        // StageData와 Solution을 하나의 ReplayData로 묶는다.
        public static ReplayData Create(
            StageData stage,
            Solution solution)
        {
            // 필수 입력이 없으면 리플레이를 만들 수 없다.
            if (stage == null || solution == null)
                return null;

            // 두 입력 데이터를 하나의 리플레이 단위로 반환한다.
            return new ReplayData
            {
                // 스테이지 전체 정보를 저장한다.
                Stage = stage,

                // 외부에서 원본 Solution을 변경해도 영향을 받지 않도록 복제한다.
                Solution = solution.Clone(),
            };
        }

        // ReplayData를 JSON 문자열로 변환한다.
        public string ToJson(bool prettyPrint = true)
        {
            return JsonUtility.ToJson(this, prettyPrint);
        }

        // JSON 문자열을 ReplayData로 복원한다.
        public static ReplayData FromJson(string json)
        {
            // 빈 문자열은 복원할 수 없다.
            if (string.IsNullOrWhiteSpace(json))
                return null;

            // JSON 안의 Stage와 Solution을 함께 복원한다.
            return JsonUtility.FromJson<ReplayData>(json);
        }
    }
}