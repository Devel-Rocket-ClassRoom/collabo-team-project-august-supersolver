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
        public readonly Sprite PlayBackground;
        public ThemeModel(
            IReadOnlyList<StageData> stages,
            Sprite stageSelectBackground,
            Sprite playBackground,
            MapEditStyle MapStyle)
        {
            Stages = stages;
            StageSelectBackground = stageSelectBackground;
            PlayBackground = playBackground;
            this.MapStyle = MapStyle;
        }
    }

}