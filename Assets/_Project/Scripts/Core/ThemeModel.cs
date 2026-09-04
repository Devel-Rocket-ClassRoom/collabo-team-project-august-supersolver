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
        public readonly IReadOnlyList<Tutorial> Tutorials;
        public readonly Sprite SprLocked;
        public readonly Sprite SprStar1;
        public readonly Sprite SprStar2;
        public readonly Sprite SprStar3;
        public ThemeModel(
            IReadOnlyList<StageData> stages,
            Sprite stageSelectBackground,
            Sprite playBackground,
            MapEditStyle MapStyle,
            IReadOnlyList<Tutorial> tutorials,
            Sprite sprLocked,
            Sprite sprStar1,
            Sprite sprStar2,
            Sprite sprStar3)
        {
            Stages = stages;
            StageSelectBackground = stageSelectBackground;
            PlayBackground = playBackground;
            this.MapStyle = MapStyle;
            Tutorials = tutorials;
            SprLocked = sprLocked;
            SprStar1 = sprStar1;
            SprStar2 = sprStar2;
            SprStar3 = sprStar3;
        }
    }

}