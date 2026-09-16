namespace PPS.Core
{
    // 타이틀 씬이 읽어 둔 UserData 를 다음 씬으로 넘긴다.
    // 씬을 갈아타면 MonoBehaviour 가 사라져서 정적으로 둔다.
    public static class UserDataHandoff
    {
        static UserData _pending;

        // 씬을 넘기기 직전에 넣는다.
        public static void Put(UserData data)
        {
            _pending = data;
        }

        // 한 번만 꺼낸다. 남겨 두면 다음 진입에서 갱신된
        // 진척도 대신 옛날 것을 쓰게 된다.
        public static UserData Take()
        {
            UserData data = _pending;
            _pending = null;
            return data;
        }
    }
}
