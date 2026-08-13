using PPS.MapEditor;
using System.Collections.Generic;
using UnityEngine;

namespace PPS.Core
{
    public enum ThemeLabel
    {
        KOREA, JAPAN
    }
    public readonly struct ThemeModel
    {
        public readonly MapEditStyle MapStyle;
        public readonly IReadOnlyList<StageData> Stages;
        public readonly Sprite StageSelectBackground;
        public ThemeModel(
            IReadOnlyList<StageData> stages,
            Sprite stageSelectBackground,
            MapEditStyle MapStyle)
        {
            Stages = stages;
            StageSelectBackground = stageSelectBackground;
            this.MapStyle = MapStyle;
        }
    }

}