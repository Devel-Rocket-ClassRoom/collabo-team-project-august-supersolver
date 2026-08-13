using System.Collections.Generic;
using PPS.Core;
using UnityEngine;

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

        /// 긋는 중인 선. 아직 도형이 아니라 여기로 온다.
        /// 없으면 null.
        public readonly List<Vector2> Stroke;

        /// 지우개가 닿는 범위. 0 이면 지우는 중이 아니다.
        public readonly float EraserRadius;

        public readonly Vector2 EraserAt;

        public MapDrawModel(
            LevelData level, MapShapes shapes, MapSelection selection,
            bool editMode, bool insertReady, int activeVertex, float handleRadius,
            List<Vector2> stroke, float eraserRadius, Vector2 eraserAt)
        {
            Level = level;
            Shapes = shapes;
            Selection = selection;
            EditMode = editMode;
            InsertReady = insertReady;
            ActiveVertex = activeVertex;
            HandleRadius = handleRadius;
            Stroke = stroke;
            EraserRadius = eraserRadius;
            EraserAt = eraserAt;
        }
    }
}
