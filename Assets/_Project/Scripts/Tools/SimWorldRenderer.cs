using UnityEngine;
using PPS.Core;
using System;
using System.IO;


#if UNITY_INCLUDE_TESTS
using PPS.Core.Tests;
#endif

namespace PPS.Tools
{
    public class SimWorldRenderer : MonoBehaviour
    {
        readonly struct Entry
        {
            public readonly string Name;
            public readonly Func<StageData> MakeStage;
            public readonly Func<Solution> MakeSolution;

            public Entry(string name, Func<StageData> makeStage, Func<Solution> makeSolution)
            {
                Name = name;
                MakeStage = makeStage;
                MakeSolution = makeSolution;
            }
        }
        // SimWorld에서 허용하는 전체 최대 스텝 수.
        const int MaxSteps = SimWorld.DefaultMaxSteps;

        // 한 프레임에서 실행할 최대 물리 스텝 수
        // 목표값이 멀리 있어도 모든 계산이 한 프레임에 몰리지 않게 한다.
        const int MaxStepsPerFrame = 8;
        
        [SerializeField] bool _autoFitCamera = true;
        [SerializeField, Range(0, MaxSteps)] int _targetStep;
        [SerializeField] int _levelIndex;
        // 현재 리플레이가 자동으로 재생 중인지 저장한다.
        [SerializeField] bool _isPlaying;

        // 한 프레임에 자동 실행할 물리 스텝 수다.
        [SerializeField, Range(1, MaxStepsPerFrame)] int _stepsPerFrame = 1;

        SimWorld _world;  // 현재 재생할 물리 세계를 담기.
        StageData _stage; // 어떤 스테이지(레벨)을 재생할지,
        Solution _solution; // 어떤 풀이(플레이어가 그린 그림)을 재생할지 담기.

        Entry[] _catalog;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _catalog = BuildCatalog();

            if(_catalog.Length == 0)
            {
                enabled = false;
                return;
            }
            _levelIndex = Mathf.Clamp(_levelIndex, 0, _catalog.Length - 1);

            Rebuild();
        }

        // Update is called once per frame
        void Update()
        {
            //아직 물리 월드가 만들어지지 않았다면 실행하지 않는다.
            if(_world == null)
                return;
            // ------------------------과거 스텝 재생성 -------------------
            // 음수는 0으로, MaxSteps보다 큰 값은 MaxSteps로 변경한다.
            // 목표 스템을 0부터 MaxSteps 사이의 값으로 제한한다.
            _targetStep = Mathf.Clamp(_targetStep, 0, MaxSteps);

            // 현재보다 과거 스텝을 선택하면 월드를 처음부터 다시 만든다.
            if(_targetStep < _world.CurrentStep)
            {
                Rebuild();
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
        private void OnDestroy()
        {
            _world?.Dispose();
            _world = null;
        }
        void Rebuild()
        {
            // 이전에 생성된 물리 월드가 있으면 종료한다.
            _world?.Dispose();
            // 보정된 레벨 번호를 사용하여 카탈로그에서 Entry 하나를 선택한다.
            Entry entry = _catalog[_levelIndex];
            // 선택한 Entry의 생성 함수를 실행하여 StageData를 만든다.
            _stage = entry.MakeStage();
            //등록된 풀이가 있으면 사용하고, 없으면 빈 Solution을 생성한다.
            _solution = entry.MakeSolution() ?? new Solution();
            // 새 StageData와 Solution으로 실제 물리 월드를 생성한다.
            _world = WorldBuilder.Build(_stage, _solution);
        }
        
        ReplayData CreateReplayData()
        {
            // 저장 이후 Solution이 변경 되어도 저장 데이터가 함께 바뀌지 않도록 복제한다
            Solution solutionCopy = _solution.Clone();

            return new ReplayData
            {
                // ReplayData 저장 형식을 기록한다.
                Version = ReplayData.CurrentVersion,
                // 6.1 현재 플레이의 StageId를 수집한다.
                StageId = _stage.StageId,
                // 6.2 현재 플레이의 Seed를 수집한다.
                Seed = _stage.Seed,
                // 6.3 확정된 Stroke와 Pivot을 복제하여 수집한다.
                Solution = solutionCopy
            };
        }

        string CreateReplayJson()
        {
            // 현재 플레이 정보를 ReplayData로 수집한다.
            ReplayData replayData = CreateReplayData();

            // 파일 내용을 확인 할 수 있도록 들여쓰기가 적용된 Json으로 변환한다.
            return JsonUtility.ToJson(replayData, true);
        }

        string GetReplayFilePath()
        {
            // 운영체제에 맞는 Unity 전용 저장 폴더와 리플레이 파일 이름을 하나의 결로로 조합 한다.
            return Path.Combine(Application.persistentDataPath, "replay.json");
        }

        // Inspector에서 저장 기능을 직접 실행 할 수 있게 한다.
        [ContextMenu("Save Replay")]
        public void SaveReplay()
        {
            //Start() 이전에는 저장할 플레이 정보가 없다.
            if(_stage == null)
            {
                Debug.LogWarning("Play Mode에서 월드 생성 후 저장해야 합니다.");
                return;
            }
            // 현재 플레이 데이터를 Json 문자열로 만든다.
            string json = CreateReplayJson();

            // Json 파일을 저장 할 전체 경로를 가져온다.
            string filePath = GetReplayFilePath();

            // 지정한 경로에 Json 문자열을 저장한다.
            File.WriteAllText(filePath, json);

            // 저장된 파일의 위치를 Console에서 확인한다.
            Debug.Log($"리플레이 저장 완료: {filePath}");
        }

        ReplayData ReadReplayData()
        {
            // 저장 할 때 사용한 것과 동일한 파일 경로룰 가져온다.
            string filePath = GetReplayFilePath();

            // 저장된 replay.json이 없으면 불러 올 수 없다.
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"리플레이 파일이 없습니다. {filePath}");
                return null;
            }

            //로컬 replay.json의 내용을 문자열로 읽는다.
            string json = File.ReadAllText(filePath);

            // Json 문자열을 ReplayData 객체로 복원한다.
            ReplayData replayData = JsonUtility.FromJson<ReplayData>(json);

            // Json을 ReplayData로 복우너하지 못했다면 중단한다.
            if (replayData == null)
            {
                Debug.LogWarning("리플레이 데이터를 복원하지 못했습니다.");
                return null;
            }
            // 현재 코드가 지원하는 저장 형식인지 확인한다.
            if (replayData.Version != ReplayData.CurrentVersion)
            {
                Debug.LogWarning($"지원하지 않는 리플레이 버전입니다. : {replayData.Version}");
                return null;
            }
            // 원본 레벨을 찾을 StageId가 있는지 확인한다.
            if (string.IsNullOrEmpty(replayData.StageId))
            {
                Debug.LogWarning("리플레이에 StageId가 없습니다.");
                return null;
            }
            // 플레이어 입력 정보가 존재하는지 확인한다.
            if (replayData.Solution == null)
            {
                Debug.LogWarning("리플레이에 Solution이 없습니다.");
                return null;
            }
            // 검증을 통과한 ReplayData를 반환한다.
            return replayData;
        }

        // Inspector에서 Json 불러오기를 실행 할 수 있게 한다.
        [ContextMenu("Load Replay Data")]
        public void LoadReplayData()
        {
            // replay.json을 읽고 검증된 ReplayData를 가져온다.
            ReplayData replayData = ReadReplayData();

            // 파일 읽기나 데이터 검증에 실패했다면 중단한다.
            if (replayData == null)
                return;

            // 불러온 Solution의 입력 개수를 확인한다.
            int strokeCount = replayData.Solution.Strokes.Count;
            int pivotCount = replayData.Solution.Pivots.Count;

            // 복원된 리플레이 정보를 Console에 출력한다.
            Debug.Log($"리플레이 불러오기 완료: " + $"Version={replayData.Version}, " + $"StageId={replayData.StageId}, " +
                       $"Seed={replayData.Seed}, " + $"Strokes={strokeCount}, " + $"Pivots={pivotCount}");
        }
        static Entry Stage(string  name, Func<LevelData> makeLevel, Func<Solution> makeSolution, int seed = 0)
        {
            return new Entry(name, () => new StageData
            {
                StageId = name,
                Seed = seed,
                Level = makeLevel()
            },
            makeSolution);
        }
        static Entry[] BuildCatalog()
        {
#if UNITY_INCLUDE_TESTS
            return new Entry[]
            {
        Stage("Ramp -> Clear",TestLevels.RampToGoal,() => null)
            };
#else
    return Array.Empty<Entry>();
#endif
        }
    }
}

