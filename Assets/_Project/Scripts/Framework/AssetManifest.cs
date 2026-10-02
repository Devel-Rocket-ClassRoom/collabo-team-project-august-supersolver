using PPS.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct ThemeStageSetEntry
{
    public ThemeLabel Label;

    /// 그 테마의 ThemeStageSet.Stages.Length 를 옮겨
    /// 적은 값. 스테이지 개수만 알면 되는 곳이
    /// 번들을 받지 않아도 되게 한다. 손으로 맞춘다.
    public int StageNum;
}

[CreateAssetMenu(fileName = "AssetManifest", menuName = "Scriptable Objects/AssetManifest")]
public class AssetManifest : ScriptableObject
{
    public List<ThemeStageSetEntry> Themes;

    public int ThemeCount => Themes.Count;

    public int GetStageNum(int theme)
    {
        if (theme < 0 || theme >= Themes.Count)
        {
            Debug.LogWarning($"AssetManifest: 테마 {theme} 가 범위 밖이다");
            return 0;
        }
        return Themes[theme].StageNum;
    }

    public ThemeLabel GetThemeLabel(int theme)
    {
        if (theme < 0 || theme >= Themes.Count)
        {
            Debug.LogWarning($"AssetManifest: 테마 {theme} 가 범위 밖이다");
            return Themes.Count > 0 ? Themes[0].Label : default;
        }
        return Themes[theme].Label;
    }
}
