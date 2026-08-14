namespace PPS.Core
{
    public interface IUserDataRepository
    {
        public UserData Data { get; }
    }
    public class UserDataRepository : IUserDataRepository
    {
        public UserData Data { get; private set; }
        public UserDataRepository(UserData data)
        {
            Data = data;
        }
    }
}
