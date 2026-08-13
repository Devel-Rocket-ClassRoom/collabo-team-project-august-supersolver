using System.Collections.Generic;

namespace PPS.Core
{
    public readonly struct ThemeModel
    {
        public readonly IReadOnlyList<StageData> Stages;

        public ThemeModel(IReadOnlyList<StageData> stages)
        {
            Stages = stages;
        }
    }
}
