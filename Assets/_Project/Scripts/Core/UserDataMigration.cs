namespace PPS.Core
{
    // 옛 형식으로 저장된 유저 데이터를 현재 형식으로 올린다.
    public static class UserDataMigration
    {
        // 단계는 순서대로 적용된다. 각 단계는 앞 단계가
        // 끝난 상태를 전제한다. 버전은 끝에서 한 번만 올린다.
        public static void Migrate(UserData data)
        {
            if (data.Version < 2) ToV2(data);
            if (data.Version < 3) ToV3(data);

            data.Version = UserData.CurrentVersion;
        }

        // v1에는 잉크 등급이 없었다. 쓴 잉크를 남기지 않아
        // 되살릴 근거가 없으므로 가장 낮은 등급으로 둔다.
        static void ToV2(UserData data)
        {
            for (int i = 0; i < data.StageClears.Count; i++)
            {
                data.StageClears[i].StarGrade = InkGrade.Bronze;
            }
        }

        // v2 의 스테이지 번호는 테마 안에서의 번호였다. 그때는
        // 테마가 하나뿐이어서 전역 번호와 값이 같다 — 번호는
        // 손대지 않는다. 다만 클리어가 없는 상태를 0 으로 적어
        // 0번 클리어와 구분할 수 없었고, 그것만 되돌린다.
        static void ToV3(UserData data)
        {
            if (data.StageClears.Count == 0)
                data.LastClearedStageIndex = UserData.NoStageCleared;
        }
    }
}
