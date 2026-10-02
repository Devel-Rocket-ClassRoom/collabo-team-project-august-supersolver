using NUnit.Framework;

namespace PPS.DrawingTool.Tests
{
    /// <summary>
    /// 전이표와 허용 입력만 잰다. 저장·물리·UI 는
    /// StageFlow 몫이라 여기서 재지 않는다.
    /// </summary>
    public class StageStateMachineTests
    {
        StageStateMachine _flow;

        [SetUp]
        public void 상태_머신을_만든다() => _flow = new StageStateMachine();

        [Test]
        public void 그리기에서_시작한다()
        {
            Assert.AreEqual(StageMode.Draw, _flow.Mode);
        }

        [Test]
        public void 플레이_일시정지_재개_재시도가_한_바퀴_돈다()
        {
            Assert.IsTrue(_flow.Play());
            Assert.AreEqual(StageMode.Simulate, _flow.Mode);

            Assert.IsTrue(_flow.PauseResume());
            Assert.AreEqual(StageMode.Paused, _flow.Mode);

            Assert.IsTrue(_flow.PauseResume());
            Assert.AreEqual(StageMode.Simulate, _flow.Mode);

            Assert.IsTrue(_flow.Retry());
            Assert.AreEqual(StageMode.Draw, _flow.Mode);
        }

        [Test]
        public void 일시정지에서도_재시도로_그리기에_돌아온다()
        {
            _flow.Play();
            _flow.PauseResume();

            Assert.IsTrue(_flow.Retry());
            Assert.AreEqual(StageMode.Draw, _flow.Mode);
        }

        [Test]
        public void 그리기에서는_일시정지와_재시도가_먹지_않는다()
        {
            // 둘 다 UI 에서 숨겨지지만, 숨김이 유일한 방어면
            // 버튼 하나가 남는 순간 조용히 상태가 어긋난다.
            Assert.IsFalse(_flow.PauseResume());
            Assert.IsFalse(_flow.Retry());
            Assert.AreEqual(StageMode.Draw, _flow.Mode);
        }

        [Test]
        public void 시뮬레이션_중_플레이는_월드를_다시_짓지_않는다()
        {
            _flow.Play();

            Assert.IsFalse(_flow.Play(), "다시 눌렸으면 StageFlow 가 월드를 새로 짓는다");
            Assert.AreEqual(StageMode.Simulate, _flow.Mode);

            _flow.PauseResume();
            Assert.IsFalse(_flow.Play());
            Assert.AreEqual(StageMode.Paused, _flow.Mode);
        }
    }
}
