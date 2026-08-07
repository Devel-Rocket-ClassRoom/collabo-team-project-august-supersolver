using PPS.Core;
using UnityEngine;

namespace PPS.Solver
{
    /// <summary>
    /// 레벨 하나가 덮는 위치 격자의 범위와 셀 개수.
    /// 셀 개수가 곧 휴리스틱 맵의 크기라
    /// 폭을 정하기 전에 비용을 재는 자다.
    /// 속도 축은 범위가 없어 여기서 세지 않는다.
    /// </summary>
    public sealed class BallGrid
    {
        /// 격자가 덮는 영역. 공용 플레이 영역 그대로다.
        public readonly Rect Area;

        /// 영역 안에 들어오는 셀 인덱스의 하한.
        public readonly int MinX;
        public readonly int MinY;

        public readonly int Columns;
        public readonly int Rows;

        public int CellCount => Columns * Rows;

        /// <summary>
        /// 셀 경계는 양자화기가 정하므로 인덱스를 직접
        /// 계산하지 않고 영역의 두 모서리를 물어본다.
        /// 폭으로 나눈 값과 어긋날 여지를 없앤다.
        /// </summary>
        public BallGrid(LevelData level, BallQuantizer quantizer)
        {
            Area = LevelDataArea.Calculate(level);

            BallCell min = quantizer.Quantize(Area.min, Vector2.zero);
            BallCell max = quantizer.Quantize(Area.max, Vector2.zero);

            MinX = min.X;
            MinY = min.Y;
            Columns = max.X - min.X + 1;
            Rows = max.Y - min.Y + 1;
        }
    }
}
