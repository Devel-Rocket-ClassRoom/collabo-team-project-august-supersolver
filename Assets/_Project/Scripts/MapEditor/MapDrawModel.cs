using PPS.Core;

namespace PPS.MapEditor
{
    /// <summary>
    /// 이번 프레임에 그릴 것 전부.
    /// View 가 편집 상태를 캐물으면 둘이 얽히므로
    /// 편집 쪽이 한 번에 건네준다.
    /// </summary>
    public readonly struct MapDrawModel
    {
        public readonly LevelData Level;
        public readonly MapShapes Shapes;
        public readonly MapSelection Selection;

        /// 버텍스·크기 핸들을 띄우는 상태.
        public readonly bool EditMode;

        /// 지금 누르면 버텍스가 꽂히는 상태.
        public readonly bool InsertReady;

        /// 크고 밝게 그릴 점. 없으면 -1.
        public readonly int ActiveVertex;

        /// 핸들의 세계 좌표 크기. 기기와 무관해야 해서
        /// dp 환산을 아는 편집 쪽이 계산한다.
        public readonly float HandleRadius;

        public MapDrawModel(
            LevelData level, MapShapes shapes, MapSelection selection,
            bool editMode, bool insertReady, int activeVertex, float handleRadius)
        {
            Level = level;
            Shapes = shapes;
            Selection = selection;
            EditMode = editMode;
            InsertReady = insertReady;
            ActiveVertex = activeVertex;
            HandleRadius = handleRadius;
        }
    }
}
