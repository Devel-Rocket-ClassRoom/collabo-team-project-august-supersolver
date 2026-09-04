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
    }
}
