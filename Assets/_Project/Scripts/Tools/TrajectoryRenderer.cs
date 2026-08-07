using System;
using PPS.Core;
using UnityEngine;

#if UNITY_INCLUDE_TESTS
// 픽스처 어셈블리의 제약과 같은 심볼이어야 한다.
using PPS.Core.Tests;
#endif

namespace PPS.Tools
{
    /// <summary>
    /// 궤적 버퍼를 선 하나로 그린다.
    /// 버퍼는 시행마다 Clear 로 재사용되므로 참조를
    /// 들지 않고 받는 즉시 복사한다 — 참조만 들면
    /// 다음 시행이 덮어써 남의 궤적을 그리게 된다.
    /// </summary>
    public sealed class TrajectoryRenderer : MonoBehaviour
    {
        /// 진한 남색. SimScrubber 배경·선 색과 겹치지 않는다.
        [SerializeField] Color _color = new Color32(0x2E, 0x2E, 0x8F, 0xFF);

        /// 진한 주황. 궤적선과 겹쳐 그려도 구분된다.
        [SerializeField] Color _velocityColor = new Color32(0xD4, 0x4A, 0x00, 0xFF);

        /// <summary>
        /// 속도 틱의 길이 배율(월드 단위 / (m/s)).
        /// 속력을 길이로 읽는 것이라 고정값이어야 한다 —
        /// 자동 정규화하면 구간끼리 비교가 안 된다.
        /// </summary>
        [SerializeField] float _velocityScale = 0.1f;

        [SerializeField] int _sampleInterval = 10;

        /// 위치만 두면 속도를 그릴 수 없어 샘플째로 든다.
        BallSample[] _samples = Array.Empty<BallSample>();
        int _count;

        /// 숫자로 읽고 있는 스냅샷.
        int _pickedIndex;

        Material _material;

        /// <summary>
        /// 버퍼를 옮겨 담는다.
        /// 배열은 모자랄 때만 다시 잡는다.
        /// </summary>
        public void Show(TrajectoryBuffer buffer)
        {
            if (_samples.Length < buffer.Count) _samples = new BallSample[buffer.Count];

            for (int i = 0; i < buffer.Count; i++)
                _samples[i] = buffer[i];

            _count = buffer.Count;
        }

        public void Clear() => _count = 0;

        void Start()
        {
            Rebuild();
        }

        /// <summary>
        /// 기본 판을 다시 돌려 궤적을 갈아 끼운다.
        /// SimScrubber 의 기본 항목과 같은 레벨·시드다.
        /// 같은 입력이므로 선이 그대로여야 정상이다 —
        /// 달라지면 결정론이 깨진 것이다.
        /// </summary>
        void Rebuild()
        {
#if UNITY_INCLUDE_TESTS
            var buffer = new TrajectoryBuffer(Mathf.Max(1, _sampleInterval));
            SimRunner.RunSampled(
                ViewerLevels.BombRamp(), ViewerLevels.BombRampSolution(), 3, buffer);
            Show(buffer);
#endif
        }

        void OnGUI()
        {
#if UNITY_INCLUDE_TESTS
            // SimScrubber 패널 아래에 붙는다.
            GUILayout.BeginArea(new Rect(10f, 370f, 460f, 110f), GUI.skin.box);

            if (GUILayout.Button("궤적 다시 시뮬", GUILayout.Height(24f)))
                Rebuild();

            GUILayout.Label($"스냅샷 {_count}개   간격 {Mathf.Max(1, _sampleInterval)}스텝");

            // 틱은 방향과 크기만 보여준다.
            // 정확한 값이 필요할 때는 하나 집어 읽는다.
            if (_count > 0)
            {
                _pickedIndex = Mathf.Clamp(_pickedIndex, 0, _count - 1);
                _pickedIndex = Mathf.RoundToInt(
                    GUILayout.HorizontalSlider(_pickedIndex, 0f, _count - 1));

                GUILayout.Label(_samples[_pickedIndex].ToString());
            }

            GUILayout.EndArea();
#endif
        }

        void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }

        void OnRenderObject()
        {
            if (_count < 2) return;

            EnsureMaterial();
            _material.SetPass(0);

            GL.PushMatrix();
            GL.Begin(GL.LINES);

            GL.Color(_color);
            for (int i = 0; i + 1 < _count; i++)
            {
                Vertex(_samples[i].Position);
                Vertex(_samples[i + 1].Position);
            }

            // 스냅샷마다 속도 방향으로 뻗은 틱.
            // 궤적의 접선과 어긋나 있으면 그 샘플 사이에
            // 충돌이 묻혀 있다는 뜻이다.
            GL.Color(_velocityColor);
            for (int i = 0; i < _count; i++)
            {
                Vector2 at = _samples[i].Position;
                Vertex(at);
                Vertex(at + _samples[i].Velocity * _velocityScale);
            }

            GL.End();
            GL.PopMatrix();
        }

        static void Vertex(Vector2 at) => GL.Vertex3(at.x, at.y, 0f);

        void EnsureMaterial()
        {
            if (_material != null) return;

            // 에셋을 늘리지 않으려고 내장 셰이더를 쓴다.
            _material = new Material(Shader.Find("Hidden/Internal-Colored"))
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            _material.SetFloat("_ZWrite", 0f);
            _material.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
        }
    }
}
