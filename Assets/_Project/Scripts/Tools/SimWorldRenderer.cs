using UnityEngine;
using PPS.Core;
using System;


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
        const int MaxSteps = SimWorld.DefaultMaxSteps;
        
        [SerializeField] bool _autoFitCamera = true;
        [SerializeField, Range(0, MaxSteps)] int _targetStep;
        [SerializeField] int _levelIndex;

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
        }

        // Update is called once per frame
        void Update()
        {

        }
        private void OnDestroy()
        {
            _world?.Dispose();
            _world = null;
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

