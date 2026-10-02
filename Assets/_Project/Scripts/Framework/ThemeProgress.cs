using PPS.Core;

/// <summary>
/// 테마 해금 진척. 테마는 앞에서부터 순서대로 열리므로
/// 개수 하나로 어디까지 열렸는지가 정해진다.
/// </summary>
public static class ThemeProgress
{
    /// 열린 테마 수. 지금 열려 있는 다음 자리가 속한
    /// 테마까지가 열린 범위다.
    public static int UnlockedCount(UserData data, AssetManifest manifest)
        => data.LastCleared.Next(manifest).Theme + 1;

    /// 아직 보여 주지 않은 해금 연출이 남았는가.
    public static bool HasPendingUnlock(UserData data, AssetManifest manifest)
        => data.ThemeUnlockAnimShown < UnlockedCount(data, manifest);
}
