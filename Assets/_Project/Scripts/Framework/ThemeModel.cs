using PPS.Core;
using System.Collections.Generic;
using UnityEngine;

public readonly struct ThemeModel
{
    public readonly IReadOnlyList<StageData> Stages;
    public readonly Sprite StageSelectBackground;
    public ThemeModel(IReadOnlyList<StageData> stages, Sprite stageSelectBackground)
    {
        Stages = stages;
        StageSelectBackground = stageSelectBackground;
    }
}
