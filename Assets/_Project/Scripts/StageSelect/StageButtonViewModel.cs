using PPS.Core;
using UnityEngine;

/// 스테이지 버튼 한 칸에 그릴 값 한 벌. 어느 칸이 열리는지,
/// 별이 몇 개인지, 어떤 스프라이트를 쓰는지는 뷰가 정하고
/// 버튼은 받은 대로 그리기만 한다.
public readonly struct StageButtonViewModel
{
    /// 눌렀을 때 들어갈 자리.
    public readonly StageEntry Entry;

    /// 잠긴 칸은 번호도 별도 감추고 눌러도 들어가지 않는다.
    public readonly bool IsLocked;

    /// 켜 둘 별 개수. 0 부터 3 까지다.
    public readonly int Stars;

    public readonly Sprite LockedSprite;

    /// 별 세 개는 개수만 나타내고 그림은 등급 하나로 통일한다.
    public readonly Sprite StarSprite;

    public StageButtonViewModel(StageEntry entry, bool isLocked, int stars,
                           Sprite lockedSprite, Sprite starSprite)
    {
        Entry = entry;
        IsLocked = isLocked;
        Stars = stars;
        LockedSprite = lockedSprite;
        StarSprite = starSprite;
    }
}
