using UnityEngine;
using PPS.Core;

namespace PPS.Tools
{
    public class SimWorldRenderer : MonoBehaviour
    {

        // SimWorld에서 허용하는 전체 최대 스텝 수.
        const int MaxSteps = SimWorld.DefaultMaxSteps;

        // 한 프레임에서 실행할 최대 물리 스텝 수
        // 목표값이 멀리 있어도 모든 계산이 한 프레임에 몰리지 않게 한다.
        const int MaxStepsPerFrame = 8;

        [SerializeField, Range(0, MaxSteps)] int _targetStep;

        // 현재 리플레이가 자동으로 재생 중인지 저장한다.
        [SerializeField] bool _isPlaying;

        // 한 프레임에 자동 실행할 물리 스텝 수다.
        [SerializeField, Range(1, MaxStepsPerFrame)] int _stepsPerFrame = 1;

        // 맵 에디터가 만든 StageData JSON 파일이다.
        [SerializeField] TextAsset _stageJson;
        // 드로잉 툴이 만든 Solution JSON 파일이다.
        [SerializeField] TextAsset _solutionJson;

        // 생성된 물리 월드를 실제 화면에 그릴 컴포넌트다.
        [SerializeField] ReplayWorldView _worldView;

        SimWorld _world;  // 현재 재생할 물리 세계를 담기.
        StageData _stage; // 어떤 스테이지(레벨)을 재생할지,
        Solution _solution; // 어떤 풀이(플레이어가 그린 그림)을 재생할지 담기.

        // Update is called once per frame
        void Update()
        {
            //아직 물리 월드가 만들어지지 않았다면 실행하지 않는다.
            if (_world == null)
                return;
            // ------------------------과거 스텝 재생성 -------------------
            // 음수는 0으로, MaxSteps보다 큰 값은 MaxSteps로 변경한다.
            // 목표 스템을 0부터 MaxSteps 사이의 값으로 제한한다.
            _targetStep = Mathf.Clamp(_targetStep, 0, MaxSteps);

            // 현재보다 과거 스텝을 선택하면 월드를 처음부터 다시 만든다.
            if (_targetStep < _world.CurrentStep)
            {
                // 재생성 중 초기화 되는 목표 스텝을 보관한다.
                int requestedStep = _targetStep;

                // 현재 StageData와 Solution으로 재생성한다.
                RebuildReplayWorld();

                // 사용자가 선택했던 목표 스텝을 복구한다.
                _targetStep = requestedStep;
            }

            // --------- 목표 스텝까지 전진 생성 ---------------------------
            //새로 만든 월드를 포함하여 Terminal 상태인지 확인한다.
            // 과거 이동 검사보다 먼저 배치하면 Terminal 상태에서 돌아갈 수 없다.
            // Clear, Fail 또는 Stalled가 확정된 월드는 더 진행하지 않는다.
            if (_world.IsTerminal || _world.CurrentStep >= MaxSteps)  // 판정 끝났거나 최대 스텝에 도달하면 자동 재생 중단.
            {
                _isPlaying = false;
                return;
            }

            // 자동 재생 중이면 현재 스텝을 기준으로 이번 프레임의 목표 스텝을 계산한다.
            if (_isPlaying)
            {
                _targetStep = Mathf.Min(_world.CurrentStep + _stepsPerFrame, MaxSteps);
            }

            // 이번 프레임에 실행한 수다.
            int stepped = 0;

            // 새로 만든 월드라면 0부터 목표 스텝까지 다시 전진한다.
            // 목표 스텝까지 진행하되 한 프레임의 실행 제한을 지킨다.
            while (_world.CurrentStep < _targetStep && stepped < MaxStepsPerFrame)
            {
                // 물리 월드를 한 스텝 진행한다.
                _world.Step();

                // 이번 프레임에서 실행한 횟수를 증가시킨다.
                stepped++;

                //이번 스텝에서 결과가 확정되었다면 즉시 중단한다.
                if (_world.IsTerminal)
                {
                    _isPlaying = false;
                    break;
                }
            }
            // 최대 스텝에 도달한 경우에도 자동 재생 상태를 종료한다.
            if (_world.CurrentStep >= MaxSteps)
                _isPlaying = false;
        }
        // 자동 재생을 시작한다.
        // 나중에 재생 버튼이 이 함수를 호출한다.
        public void Play()
        {
            // 재생할 월드가 없다면 시작 할 수 없다.
            if (_world == null)
                return;

            // 판정이 끝났거나 최대 스텝에 도달한 월드는 더 진행하지 않는다.
            if (_world.IsTerminal || _world.CurrentStep >= MaxSteps) return;

            _isPlaying = true;
        }

        // 현재 스텝에서 자동 재생을 중단한다.
        // 나중에 일시정지 버튼이 이 함수를 호출한다.
        public void Pause()
        {
            _isPlaying = false;

            // 기존 목표가 현재보다 앞에 남아 있으면 일시정지 후에도 진행할 수 있어 현재로 맞춘다.
            if (_world != null)
                _targetStep = _world.CurrentStep;
        }

        // 프레임당 실행할 물리 스텝 수를 설정한다.
        // 나중에 재생 속도 UI가 이 함수를 호출한다.
        public void SetPlaybackSpeed(int stepsPerFrame)
        {
            // 속도값을 1부터 MaxStepsPerFrame 사이로 제한한다.
            _stepsPerFrame = Mathf.Clamp(stepsPerFrame, 1, MaxStepsPerFrame);
        }

        // 현재 StageData와 Solution으로 처음부터 다시 재생한다.
        public void Restart()
        {
            // 아직 입력 데이터가 없다면 재시작할 수 없다.
            if (_stage == null || _solution == null)
                return;

            // 보관한 두 데이터로 월드를 다시 만든다.
            RebuildReplayWorld();

            // 자동 재생을 다시 시작한다.
            Play();
        }

        // 리플레이에서 이동할 목표 스텝을 설정한다.
        // 나중에 슬라이더나 스크러버 UI가 이 함수를 호출한다.
        public void SetTargetStep(int targetStep)
        {
            // 입력값을 SimWorld가 허용하는 범위로 제한한다.
            _targetStep = Mathf.Clamp(targetStep, 0, MaxSteps);

            // 사용자가 선택한 스텝에서 멈춰 볼 수 있도록 자동 재생을 중단한다.
            _isPlaying = false;
        }

        // 외부에서 받은 StageData와 Solution으로 재생할 물리 월드를 구성한다.
        public void SetReplay(StageData stage, Solution solution)
        {
            //입력 데이터가 없다면 월드를 만들 수 없다.
            if (stage == null || solution == null)
            {
                Debug.LogWarning("StageData와 Solution이 필요합니다.");
                return;
            }


            // 재시작에 사용할 입력 데이터를 보관한다.
            _stage = stage;
            _solution = solution.Clone();

            // 보관한 입력으로 물리 월드를 만든다.
            RebuildReplayWorld();
        }
        // 보관 중인 StageData와 Solution으로 물리 월드를 처음부터 다시 만든다.
        void RebuildReplayWorld()
        {
            // 아직 입력을 받지 않았다면 만들 수 없다.
            if (_stage == null || _solution == null)
                return;

            // 기존 물리 월드를 종료한다.
            _world?.Dispose();
            // 같은 StageData와 Solution으로 다시 생성한다.
            _world = WorldBuilder.Build(_stage, _solution);

            // 재생 위치와 상태를 초기화한다.
            _targetStep = 0;
            _isPlaying = false;

            // 새로 생성한 물리 월드와 레벨 정보를 화면 표시 컴포넌트에 전달한다.
            _worldView?.Show(_stage.Level, _world);
        }
        // Inspector에 연결된 두 Json 파일을 읽어 리플레이 월드를 구성한다.
        public void LoadReplayInputs()
        {
            // 두 파일이 모두 연결 됐는지 확인한다.
            if (_stageJson == null || _solutionJson == null)
            {
                Debug.LogWarning("Stage JSON과 Solution JSON을 연결해야 합니다.");
                return;
            }
            // 맵 Json을 StageData로 변환한다.
            StageData stage = StageData.FromJson(_stageJson.text);

            // 그림 Json을 Solution으로 변환한다.
            Solution solution = JsonUtility.FromJson<Solution>(_solutionJson.text);

            // 변환한 두 데이터로 재생 월드를 만든다.
            SetReplay(stage, solution);

            // 연결 결과를 Console에서 확인한다.
            Debug.Log(
        $"리플레이 입력 연결 완료: {stage.StageId}, " +
        $"Strokes={solution.Strokes.Count}, " +
        $"Pivots={solution.Pivots.Count}");
        }
        private void OnDestroy()
        {
            _world?.Dispose();
            _world = null;

            // 파괴된 물리 월드를 더 이상 화면에 그리지 않도록 연결을 해제한다.
            _worldView?.Clear();
        }
    }

}

