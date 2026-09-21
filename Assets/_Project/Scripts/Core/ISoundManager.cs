namespace PPS.Core
{
    public enum BgmType
    {
        Title,
        Stage,
    }

    public enum SfxType
    {
        UIClick,
        Bomb,
        Death,
        Slime,
        Reward,
        Star,
    }

    /// <summary>
    /// 소리를 울리는 쪽의 계약. 재생기는 Assembly-CSharp 에
    /// 있어 어셈블리가 갈린 뷰에서 직접 부를 수 없다.
    /// </summary>
    public interface ISoundManager
    {
        void PlayBgm(BgmType type);
        void StopBgm();
        void PlaySfx(SfxType type);
    }
}
