using UnityEngine;
using NUnit.Framework;

namespace PPS.Core.Tests
{
    public class LevelDataAreaTests
    {
        // float 계산 결과를 비교할 때 허용 할 오차 범위다.
        const float Eps = 0.0001f;

        [Test]
        public void 영역은_공_목표_지형을_담고_마진만큼_넓다()
        {
            //플레이 영역 계산에 사용할 테스트 레벨을 만든다.
            var level = new LevelData
            {
                // 공은 원점에 있고 반지름은 0.25다.
                BallStart = Vector2.zero,

                // 목표는 오른쪽 5에 있고 반지름은 0.5다.
                GoalPosition = new Vector2(5f,0f),
            };
            // 왼쪽 -5부터 오른쪽 5까지의 지형 선분을 추가한다.
            level.Terrain.Add(new StaticSegment(new Vector2(-5f, -1f), new Vector2(5f, -1f)));

            // 테스트 레벨의 공용 플레이 영역을 계산한다.
            Rect area = LevelDataArea.Calculate(level);

            // 공용 클래스에 정의된 Margin 값을 가져온다.
            float margin = LevelDataArea.AreaMargin;

            // 지형의 왼쪽 끝에서 Margin만큼 확장 됐는지 확인한다.
            Assert.AreEqual(-5 - margin, area.xMin, Eps);

            // 목표의 오른쪽 끝에서 Margin만큼 확장 됐는지 확인한다.
            Assert.AreEqual(5.5f + margin, area.xMax, Eps);

            // 지형의 아래쪽에서 Margin만큼 확장 됐는지 확인한다.
            Assert.AreEqual(-1f - margin, area.yMin, Eps);

            // 목표의 위쪽에서 Margin만큼 확장 됐는지 확인한다.
            Assert.AreEqual(0.5f + margin, area.yMax, Eps);
        }
    }
}
