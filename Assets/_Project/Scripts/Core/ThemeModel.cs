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
        public readonly Sprite SprStarBronze;
        public readonly Sprite SprStarSilver;
        public readonly Sprite SprStarGold;
        public ThemeModel(
            IReadOnlyList<StageData> stages,
            Sprite stageSelectBackground,
            Sprite playBackground,
            MapEditStyle MapStyle,
            IReadOnlyList<Tutorial> tutorials,
            Sprite sprLocked,
            Sprite sprStarBronze,
            Sprite sprStarSilver,
            Sprite sprStarGold)
        {
            Stages = stages;
            StageSelectBackground = stageSelectBackground;
            PlayBackground = playBackground;
            this.MapStyle = MapStyle;
            Tutorials = tutorials;
            SprLocked = sprLocked;
            SprStarBronze = sprStarBronze;
            SprStarSilver = sprStarSilver;
            SprStarGold = sprStarGold;
        }
    }

}