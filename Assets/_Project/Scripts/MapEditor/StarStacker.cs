using PPS.Core;
using UnityEngine;

namespace PPS.MapEditor
{
    /// <summary>
    /// 먹은 별을 쌓는다. 획득을 판정하지 않는다 —
    /// 판정은 코어가 하고 여기는 결과만 받는다.
    /// </summary>
    public sealed class StarStacker : MonoBehaviour
    {
        SimEvents _events;

        /// 별마다 먹었는지. 인덱스는 레벨 순서.
        bool[] _collected;

        /// 지금까지 먹은 개수.
        public int Count { get; private set; }

        public int Total => _collected == null ? 0 : _collected.Length;

        public bool IsCollected(int index) =>
            _collected != null && index >= 0 && index < _collected.Length && _collected[index];

        /// <summary>
        /// 시뮬레이션이 시작될 때 붙는다.
        /// 0 스텝에 먹은 별은 구독 전에 지나가므로
        /// 판정 결과를 한 번 훑어 맞춰 둔다.
        /// </summary>
        public void Attach(SimWorld world)
        {
            Detach();

            int total = world.Level.Stars == null ? 0 : world.Level.Stars.Count;
            _collected = new bool[total];
            Count = 0;

            for (int i = 0; i < total; i++)
            {
                if (!world.Judge.IsCollected(i)) continue;

                _collected[i] = true;
                Count++;
            }

            _events = world.Events;
            _events.StarCollected += OnStarCollected;
        }

        public void Detach()
        {
            if (_events != null) _events.StarCollected -= OnStarCollected;

            _events = null;
        }

        void OnDestroy() => Detach();

        void OnStarCollected(int index, Vector2 at)
        {
            if (index < 0 || index >= _collected.Length || _collected[index]) return;

            _collected[index] = true;
            Count++;

            Debug.Log($"[맵 에디터] 별 {Count} / {_collected.Length}");
        }
    }
}
