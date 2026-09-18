using Cysharp.Threading.Tasks;
using PPS.Core;
using PPS.Game;
using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 판정이 확정되면 판정별로 뒤처리를 한다.
    /// 클리어는 보상 화면, 사망은 자동 재시작, 정지는
    /// 재시도 버튼 반짝임 — 판정을 글로 알리지 않아야
    /// 플레이어가 무엇이 막혔는지 화면에서 직접 읽는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StageOutcomeHandler : MonoBehaviour
    {
        /// 죽고 나서 그리기로 돌아가기까지. 낙사 이펙트가
        /// 터질 시간을 준다 — 0 이면 죽는 순간이 안 보인다.
        const float DeathRestartDelay = 0.6f;

        [SerializeField] StageLoader _stageLoader;
        [SerializeField] GameSimDriver _driver;
        [SerializeField] DrawingSession _session;
        [SerializeField] StageFlow _flow;
        [SerializeField] RetryBlink _retryBlink;

        /// 이번 판의 판정을 이미 처리했는가.
        bool _handled;

        /// 판정이 확정되고 흐른 시간.
        float _sinceDecided;

        // 드라이버가 Update 에서 스텝을 돌린다. 여기가
        // Update 면 실행 순서에 따라 한 프레임 늦는다.
        void LateUpdate()
        {
            SimWorld world = _driver.World;

            // 월드가 없으면 그리기 중이다. 재시작이 월드를
            // 버려도 이 갈래로 돌아와 다음 판을 기다린다.
            if (world == null || !world.IsTerminal)
            {
                _handled = false;
                return;
            }

            // 판정은 한 번 확정되면 안 뒤집힌다.
            if (!_handled)
            {
                _handled = true;
                _sinceDecided = 0f;

                if (world.Judge.Cleared)
                {
                    SaveReplay();
                    ShowReward(world);
                }
                else if (world.Judge.Stalled) _retryBlink.Play();
            }

            // 정지(Stalled)는 되돌리지 않는다. 유저가 직접
            // 재시도를 눌러 무엇이 멈췄는지 보게 한다.
            if (world.Judge.Failed) RestartAfterDeath();
        }

        /// <summary>
        /// 떨어져 죽든 스파이크에 죽든 플레이어가 누를
        /// 것이 없다. 그리기로 되돌리며 그린 선은 남는다.
        /// </summary>
        void RestartAfterDeath()
        {
            _sinceDecided += Time.deltaTime;
            if (_sinceDecided < DeathRestartDelay) return;

            _flow.Retry();
        }
        void SaveReplay()
        {
            if (_stageLoader == null ||
                _stageLoader.Stage == null ||
                _session == null)
            {
                Debug.LogWarning(
                    "[Replay] 저장에 필요한 StageLoader 또는 DrawingSession 참조가 없습니다.");

                return;
            }

            ReplayStorage.Save(
                _stageLoader.Stage,
                _session.Solution);
        }

        void ShowReward(SimWorld world)
        {
            SaveThatStageCleared(world.Judge.Stars,
                InkGrade.Of(_session.Solution.TotalInk(), world.Level.InkLimit));

            RewardViewModel vm = new RewardViewModel()
            {
                InkUsed = _session.Solution.TotalInk(),
                InkLimit = world.Level.InkLimit,
                EndStep = world.Judge.DecidedStep,
                Entry = StageSelection.Current,
                StarCount = world.Judge.Stars,
            };
            ServiceLocator.Get<IRewardView>().Show(vm);
        }

        void SaveThatStageCleared(int stars, int starGrade)
        {
            var repo = ServiceLocator.Get<IUserDataRepository>();

            repo.RecordClear(StageSelection.Current, stars, starGrade);

            ServiceLocator.Get<IUserDataService>().SaveAsync(repo.Data).Forget();
        }
    }
}
