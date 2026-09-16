namespace PPS.Core
{
    // 옛 형식으로 저장된 유저 데이터를 현재 형식으로 올린다.
    public static class UserDataMigration
    {
        // 단계는 순서대로 적용된다. 각 단계는 앞 단계가
        // 끝난 상태를 전제한다. 버전은 끝에서 한 번만 올린다.
        public static void Migrate(UserData data)
        {
            if (data.Version < 4) ToV4(data);

            data.Version = UserData.CurrentVersion;
        }

        // 스테이지 지칭이 전역 번호에서 StageEntry 로 바뀌었다.
        // 옛 번호를 옮길 근거가 없어 진척도를 버린다.
        static void ToV4(UserData data)
        {
            data.StageClears.Clear();
            data.LastCleared = default;
            data.HasPlayed = true;
        }
    }
}
