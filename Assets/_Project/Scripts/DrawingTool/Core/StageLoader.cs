using PPS.Core;
using UnityEngine;

namespace PPS.DrawingTool
{
    /// <summary>
    /// 스테이지 파일 하나를 읽어 화면에 물린다.
    /// 목록에서 고르는 것은 나중 문제다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StageLoader : MonoBehaviour
    {
        [SerializeField] TextAsset _stageJson;
        [SerializeField] CanvasCameraFitter _fitter;
        [SerializeField] DrawInputBehaviour _input;
        [SerializeField] LevelView _levelView;

        /// 읽어 들인 판. 저장 경로가 StageId 를 쓴다.
        public StageData Stage { get; private set; }

        void Start()
        {
            if (_stageJson == null)
            {
                Debug.LogError("스테이지 파일이 안 꽂혀 있다.", this);
                return;
            }

            StageData stage = StageData.FromJson(_stageJson.text);

            // 최상위가 LevelData 인 파일도 예외 없이 읽힌다 —
            // Level 이 통째로 기본값이 되어 공과 목표가 겹친다.
            // 조용히 빈 판으로 도는 게 폴백보다 나쁘다.
            if (stage == null || stage.Level.BallStart == stage.Level.GoalPosition)
            {
                Debug.LogError(
                    $"스테이지로 읽히지 않는다: {_stageJson.name}. 최상위가 StageData 인지 본다.",
                    this);
                return;
            }

            Stage = stage;
            _fitter.SetLevel(stage.Level);
            _input.SetLevel(stage.Level);
            _levelView.SetLevel(stage.Level);
        }
    }
}
