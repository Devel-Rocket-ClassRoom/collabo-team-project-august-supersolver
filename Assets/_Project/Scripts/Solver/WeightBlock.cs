using System.Collections.Generic;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver
{
    /// <summary>
    /// 정해진 부피 안에 선을 접어 넣어 만든 추.
    /// FreeBody 는 길이에 비례해 무거워서, 길이로 무게를 정하면
    /// 무거운 추가 곧 긴 추가 된다 — 놓을 자리가 없어진다.
    /// 상자를 고정하고 줄 수로 무게를 정하면 자리와 무게가 갈린다.
    /// </summary>
    public readonly struct WeightBlock
    {
        /// <summary>
        /// 줄 간격이 이보다 좁으면 두꺼운 콜라이더끼리 겹친다.
        /// 겹쳐도 물리는 맞지만 도형만 늘어 시뮬이 느려진다.
        /// </summary>
        public const float MinRowGap = 2f * ColliderFactory.FreeBodyHalfWidth;

        public readonly Vector2 Center;

        /// 상자 크기. 자리를 정하는 값이고 무게와는 무관하다.
        public readonly Vector2 Size;

        /// 상자를 가로지르는 줄 수. 이것이 무게다.
        public readonly int Rows;

        public WeightBlock(Vector2 center, Vector2 size, int rows)
        {
            Center = center;
            Size = size;
            Rows = Mathf.Max(1, rows);
        }

        /// <summary>
        /// 접어 넣은 선의 총 길이.
        /// 가로 줄 Rows 개에, 그 줄들을 잇는 세로 토막을 다 합치면
        /// 정확히 상자 높이 하나다.
        /// </summary>
        public float Length => Rows <= 1 ? Size.x : Rows * Size.x + Size.y;

        public float Mass => Length * ColliderFactory.FreeBodyMassPerUnit;

        /// 이 상자에 줄을 몇 개까지 넣어야 겹치지 않는지.
        public int MaxRows => RowsIn(Size.y);

        /// 높이 height 인 상자가 겹치지 않게 담을 수 있는 줄 수.
        public static int RowsIn(float height)
            => Mathf.Max(1, Mathf.FloorToInt(height / MinRowGap) + 1);

        /// <summary>
        /// 상자를 왕복하며 접은 한 줄짜리 폴리라인.
        /// 끝에서 방향을 뒤집어 이어야 선이 끊기지 않는다 —
        /// 스트로크 하나가 곧 바디 하나다.
        /// </summary>
        public Stroke ToStroke()
        {
            var points = new List<Vector2>(Rows * 2);

            float halfWidth = Size.x * 0.5f;
            float halfHeight = Size.y * 0.5f;

            for (int r = 0; r < Rows; r++)
            {
                float t = Rows == 1 ? 0.5f : (float)r / (Rows - 1);
                float y = Mathf.Lerp(-halfHeight, halfHeight, t);

                // 홀짝으로 뒤집어야 줄 끝끼리 이어진다.
                float from = (r & 1) == 0 ? -halfWidth : halfWidth;

                points.Add(Center + new Vector2(from, y));
                points.Add(Center + new Vector2(-from, y));
            }

            return new Stroke(ToolType.FreeBody, points);
        }
    }
}
