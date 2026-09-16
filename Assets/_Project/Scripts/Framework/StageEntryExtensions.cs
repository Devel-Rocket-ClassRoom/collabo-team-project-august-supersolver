using PPS.Core;

public static class StageEntryExtensions
{
    /// <summary>
    /// 다음 자리. 테마의 끝이면 다음 테마 첫 칸으로
    /// 넘어간다. 맨 마지막 칸이면 자기 자신을 준다 —
    /// 없는 자리를 만들어 넘기지 않는다.
    /// </summary>
    public static StageEntry Next(this StageEntry e, AssetManifest m)
    {
        if (e.Stage < m.GetStageNum(e.Theme) - 1)
            return new StageEntry(e.Theme, e.Stage + 1);

        if (e.Theme < m.ThemeCount - 1)
            return new StageEntry(e.Theme + 1, 0);

        return e;
    }
}
