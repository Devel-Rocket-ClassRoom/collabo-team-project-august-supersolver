using System;
using UnityEngine;

namespace PPS.Core
{
    /// <summary>
    /// 키와 프리팹 실물을 짝지은 표. 프리팹 참조는
    /// 여기에만 있어 튜토리얼 SO 는 값만 든다.
    /// </summary>
    [CreateAssetMenu(fileName = "TutorialPrefabTable", menuName = "Scriptable Objects/TutorialPrefabTable")]
    public class TutorialPrefabTable : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public TutorialPrefabKey Key;
            public GameObject Prefab;
        }

        [SerializeField] Entry[] _entries;

        /// <summary>못 찾으면 null 을 돌려준다.</summary>
        public GameObject Find(TutorialPrefabKey key)
        {
            if (_entries == null) return null;

            foreach (var entry in _entries)
                if (entry.Key == key) return entry.Prefab;

            return null;
        }
    }
}
