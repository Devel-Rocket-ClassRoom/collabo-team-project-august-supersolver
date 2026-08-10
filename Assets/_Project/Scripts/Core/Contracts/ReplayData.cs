using System;

namespace PPS.Core
{
    [Serializable]
    public class ReplayData
    {
        // 현재 리플레이 데이터 형식의 버전.
        // 저장 혁식이 바뀌면 이 값을 2,3 처럼 증가시킨다.
        public const int CurrentVersion = 1;

        // 이 리플레이 파일이 어떤 데이터 형식으로 작성 됐는지 저장한다.
        public int Version = CurrentVersion;

        // 리플레이에서 다시 불러올 원본 레벨의 식별자다.
        public string StageId = string.Empty;

        // 동일한 난수 결과를 재현하기 위해 사용한 시드다.
        public int Seed;

        // 플레이어가 확전한 Stroke와 Pivot 정보를 저장한다.
        // null 사용을 피하기 위해 빈 Solution을 기본으로 생성한다.
        public Solution Solution = new Solution();
    }
}
