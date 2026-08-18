namespace PPS.Core
{
    /// <summary>클리어 직후 보상 UI 에 뿌릴 값.</summary>
    public struct RewardViewModel
    {
        /// 먹은 별.
        public int StarCount;

        /// 클리어한 스텝.
        public int EndStep;

        /// 사용한 잉크.
        public float InkUsed;

        /// 그 판의 잉크 상한. 판마다 달라
        /// 아낀 정도는 사용량이 아니라 비율로 본다.
        public float InkLimit;

        /// 깬 스테이지 인덱스.
        public int StageIndex;
    }
}
