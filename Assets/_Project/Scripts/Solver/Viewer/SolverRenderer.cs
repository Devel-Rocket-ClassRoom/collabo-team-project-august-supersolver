using PPS.Core;
using UnityEngine;

#if UNITY_INCLUDE_TESTS
// 픽스처 어셈블리의 제약과 같은 심볼이어야 한다.
using PPS.Core.Tests;
#endif

namespace PPS.Solver.Viewer
{
    /// <summary>
    /// 시행 하나를 돌려 레벨·배치·궤적을 겹쳐 띄운다.
    /// 시행을 도는 쪽은 여기 하나뿐이다 — 렌더러가
    /// 각자 돌리면 같은 벡터를 두 번 시뮬하게 되고
    /// 세 그림이 서로 다른 시행을 보여줄 수 있다.
    /// </summary>
    [RequireComponent(typeof(LevelRenderer))]
    [RequireComponent(typeof(PrimitiveRenderer))]
    [RequireComponent(typeof(TrajectoryRenderer))]
    [RequireComponent(typeof(WorldRenderer))]
    public sealed class SolverRenderer : MonoBehaviour
    {
        [SerializeField] int _sampleInterval = 10;

        /// 벡터가 담는 프리미티브 개수.
        [SerializeField] int _primitiveCount = 2;

        /// <summary>
        /// 레벨이 화면에 들어오게 카메라를 맞춘다.
        /// SimScrubber 와 같은 씬에 두면 서로 카메라를
        /// 뺏으므로 한쪽을 꺼야 한다.
        /// </summary>
        [SerializeField] bool _fitCamera = true;

        [SerializeField] Color _background = new Color32(0xFF, 0xE6, 0xB3, 0xFF);

        LevelRenderer _levelView;
        PrimitiveRenderer _primitiveView;
        TrajectoryRenderer _trajectoryView;
        WorldRenderer _worldView;

        /// <summary>
        /// 재생용 월드. 시행이 돌린 것과 같은 입력으로
        /// 따로 짓는다 — 시행은 끝까지 돌고 궤적만 남기므로
        /// 중간 스텝을 되짚을 월드가 남지 않는다.
        /// 결정론이 지켜지면 이 공은 궤적선 위를 지나야 한다.
        /// </summary>
        SimWorld _world;

        Solution _solution;

        /// 재생이 따라갈 스텝.
        int _targetStep;

        /// 레벨마다 하나. 코덱이 플레이 영역을 미리 잰다.
        PrimitiveTrial _trial;
        PrimitiveCodec _codec;

        /// 지금 보고 있는 벡터를 만든 시드.
        int _vectorSeed;

        /// 시드 입력칸. 확정 전까지는 시드와 다를 수 있다.
        string _seedInput = "0";

        TrialResult _result;

        /// 숫자로 읽고 있는 스냅샷.
        int _pickedIndex;

        /// 거부돼 궤적이 없을 때 공을 되돌릴 자리를 안다.
        LevelData _level;

        /// 시행이 쓰는 시드. 월드를 다시 지을 때 같아야 한다.
        int _stageSeed;

        void Start()
        {
            _levelView = GetComponent<LevelRenderer>();
            _primitiveView = GetComponent<PrimitiveRenderer>();
            _trajectoryView = GetComponent<TrajectoryRenderer>();
            _worldView = GetComponent<WorldRenderer>();

#if UNITY_INCLUDE_TESTS
            // 레벨은 픽스처에서만 온다.
            // SimScrubber 기본 항목과 같은 판·시드다.
            _level = ViewerLevels.BombRamp();
            _stageSeed = 3;
            _trial = new PrimitiveTrial(_level, _stageSeed);
            _codec = new PrimitiveCodec(_level);

            // 레벨은 시행마다 바뀌지 않아 한 번만 넘긴다.
            _levelView.Show(_level);
            if (_fitCamera) FitCamera(_level);
#endif
            Rebuild();
        }

        /// <summary>
        /// 지형과 공이 다 들어오게 맞춘다.
        /// 목표는 넣지 않는다 — 닿을 수 없는 먼 목표를
        /// 담으면 화면이 쓸모없이 축소된다.
        /// </summary>
        void FitCamera(LevelData level)
        {
            var cam = Camera.main;
            if (cam == null) return;

            Vector2 min = level.BallStart;
            Vector2 max = level.BallStart;

            var terrain = level.Terrain;
            if (terrain != null)
            {
                for (int i = 0; i < terrain.Count; i++)
                {
                    Encapsulate(ref min, ref max, terrain[i].A);
                    Encapsulate(ref min, ref max, terrain[i].B);
                }
            }

            Vector2 center = (min + max) * 0.5f;
            Vector2 extent = (max - min) * 0.5f;

            float halfHeight = Mathf.Max(extent.y, extent.x / Mathf.Max(cam.aspect, 0.1f));

            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(halfHeight * 1.25f, 2f);   // 1.25 = 여백
            cam.transform.position = new Vector3(center.x, center.y, -10f);

            // 선 색이 이 배경 기준이라 함께 고정한다.
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = _background;
        }

        static void Encapsulate(ref Vector2 min, ref Vector2 max, Vector2 point)
        {
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }

        /// <summary>
        /// 지금 시드의 벡터를 시행에 통째로 태운다.
        /// 코어가 아니라 PrimitiveTrial 을 거치는 것이 요점이다 —
        /// 디코드와 배치 검증까지 그림에 들어와야
        /// 눈으로 본 것이 솔버 경로의 근거가 된다.
        /// 같은 시드로 다시 눌러 그림이 그대로면 결정론이 지켜진 것이다.
        /// </summary>
        void Rebuild()
        {
            if (_trial == null) return;

            var vector = RandomVector(_vectorSeed);
            var buffer = new TrajectoryBuffer(Mathf.Max(1, _sampleInterval));

            // 거부되면 버퍼가 빈 채로 오므로 궤적은 그려지지 않는다.
            _result = _trial.RunSampled(vector, buffer);

            // 시행이 디코드한 것을 돌려주지 않아 같은 코덱으로 다시 편다.
            // 코덱은 (레벨, 벡터)만 보는 순수 함수라 결과가 같다.
            var primitives = _codec.Decode(vector);
            _primitiveView.Show(primitives, _result.Rejected);
            _trajectoryView.Show(buffer);

            _pickedIndex = 0;
            _targetStep = 0;
            _seedInput = _vectorSeed.ToString();

            // 거부된 시행은 시뮬이 돌지 않았다. 재생할 것도 없다.
            _solution = _result.Rejected ? null : PrimitiveDecoder.Decode(primitives);
            RebuildWorld();

            SyncBall();
        }

        /// <summary>
        /// 재생 월드를 스텝 0 으로 되돌린다.
        /// 뒤로 감기는 방법이 이것뿐이다 — 물리는
        /// 역행하지 않으므로 처음부터 다시 밟는다.
        /// </summary>
        void RebuildWorld()
        {
            _world?.Dispose();
            _world = null;

            if (_solution != null)
                _world = WorldBuilder.Build(_level, _solution, _stageSeed);

            _worldView.Show(_world);
        }

        void Update()
        {
            if (_world == null) return;

            if (_targetStep < _world.CurrentStep) RebuildWorld();

            while (_world.CurrentStep < _targetStep && !_world.IsTerminal)
                _world.Step();
        }

        void OnDestroy()
        {
            _world?.Dispose();
            _world = null;
        }

        /// <summary>
        /// 공을 집어 둔 스냅샷 자리로 옮긴다.
        /// 궤적이 없으면(거부된 시행) 출발점으로 되돌린다 —
        /// 지난 시행 자리에 남으면 돌지도 않은 시뮬의
        /// 결과처럼 보인다.
        /// </summary>
        void SyncBall()
        {
            if (_level == null) return;

            _levelView.MoveBall(_trajectoryView.Count > 0
                ? _trajectoryView[_pickedIndex].Position
                : _level.BallStart);
        }

        void ApplySeed(int seed)
        {
            _vectorSeed = seed;
            Rebuild();
        }

        /// <summary>
        /// 축이 전부 [0,1] 이라 난수를 그대로 쓴다.
        /// Unity 전역 난수를 건드리면 시뮬 재현이 흔들려
        /// System.Random 을 따로 쓴다.
        /// </summary>
        float[] RandomVector(int seed)
        {
            var rng = new System.Random(seed);
            var vector = new float[PrimitiveCodec.Length(Mathf.Max(1, _primitiveCount))];

            for (int i = 0; i < vector.Length; i++)
                vector[i] = (float)rng.NextDouble();

            return vector;
        }

        void OnGUI()
        {
            if (_trial == null) return;

            // SimScrubber 패널 아래에 붙는다.
            GUILayout.BeginArea(new Rect(10f, 370f, 460f, 215f), GUI.skin.box);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("◀", GUILayout.Width(36f), GUILayout.Height(24f)))
                ApplySeed(_vectorSeed - 1);
            if (GUILayout.Button("▶", GUILayout.Width(36f), GUILayout.Height(24f)))
                ApplySeed(_vectorSeed + 1);

            GUILayout.Label($"시드 {_vectorSeed}", GUILayout.Width(80f));

            // 같은 벡터를 다시 태운다. 그림이 그대로여야 정상이다.
            if (GUILayout.Button("다시 시뮬", GUILayout.Height(24f)))
                Rebuild();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("시드 직접 입력", GUILayout.Width(90f));
            _seedInput = GUILayout.TextField(_seedInput, GUILayout.Width(80f));
            if (GUILayout.Button("이동", GUILayout.Width(50f)) &&
                int.TryParse(_seedInput, out int typed))
                ApplySeed(typed);
            GUILayout.EndHorizontal();

            // 거부도 결과다. 시뮬이 안 돈 것과
            // 돌고도 못 푼 것은 다른 사건이다.
            GUILayout.Label(_result.ToString());

            int interval = Mathf.Max(1, _sampleInterval);
            int count = _trajectoryView.Count;
            int endStep = _result.Sim.EndStep;

            GUILayout.Label($"스냅샷 {count}개   간격 {interval}스텝");

            // 스텝을 옮기면 월드가 거기까지 밟고,
            // 그 스텝의 스냅샷이 함께 잡힌다.
            if (endStep > 0)
            {
                GUILayout.Label($"재생 스텝 {_targetStep} / {endStep}");
                _targetStep = Mathf.RoundToInt(
                    GUILayout.HorizontalSlider(_targetStep, 0f, endStep));

                if (count > 0)
                {
                    int picked = Mathf.Clamp(_targetStep / interval - 1, 0, count - 1);
                    if (picked != _pickedIndex)
                    {
                        _pickedIndex = picked;
                        SyncBall();
                    }

                    GUILayout.Label(_trajectoryView[_pickedIndex].ToString());
                }
            }

            GUILayout.EndArea();
        }
    }
}
