using System;
using System.Collections.Generic;
using System.IO;
using PPS.Core;
using UnityEngine;

namespace PPS.Solver
{
    /// <summary>
    /// 실측이 찾아 둔 지렛대 하나. 다섯 축과 그 결과인 발사 상태다.
    /// 발사 상태를 들고 있으면 도달 여부를 시뮬 없이 풀 수 있다 —
    /// 공이 판을 떠난 뒤로는 중력밖에 안 남기 때문이다.
    /// 자리를 안 들고 있어서 어디에나 그대로 옮겨 쓴다.
    /// </summary>
    [Serializable]
    public struct LeverPreset
    {
        public float Length;
        public float FulcrumAt;
        public float BallAt;
        public int WeightRows;
        public float Drop;

        /// 공이 판을 떠난 자리. 공이 얹혀 있던 자리 기준이다.
        public Vector2 LaunchOffset;

        /// 떠날 때의 속도.
        public Vector2 LaunchVelocity;

        /// 떠나기까지 걸린 스텝. 도달 시간에 이만큼 얹힌다.
        public int LaunchStep;

        /// 잉크. 조회 순서를 정한다.
        public float Ink;

        public static LeverPreset From(
            in Lever lever, Vector2 launchOffset, Vector2 launchVelocity, int launchStep)
            => new LeverPreset
            {
                Length = lever.Length,
                FulcrumAt = lever.FulcrumAt,
                BallAt = lever.BallAt,
                WeightRows = lever.WeightRows,
                Drop = lever.Drop,
                LaunchOffset = launchOffset,
                LaunchVelocity = launchVelocity,
                LaunchStep = launchStep,
                Ink = lever.Ink,
            };

        /// <param name="facingRight">목표가 공의 오른쪽인지. 표는 오른쪽으로만 만든다.</param>
        public Lever ToLever(Vector2 ballSeat, bool facingRight = true)
            => new Lever(ballSeat, Length, FulcrumAt, BallAt, WeightRows, Drop, facingRight);

        /// <summary>
        /// 공 자리에서 target 만큼 떨어진 곳을 지나가는가.
        /// 지나면 그때까지의 스텝, 아니면 -1 이다.
        /// </summary>
        /// <param name="target">공이 얹힌 자리 기준의 어긋남.</param>
        public int StepTo(Vector2 target, float tolerance, int maxSteps)
        {
            int flight = Ballistic.StepNear(
                LaunchOffset, LaunchVelocity, target, tolerance, maxSteps);

            return flight < 0 ? -1 : LaunchStep + flight;
        }

        /// 왼쪽으로 쓰는 프리셋. 가로만 뒤집으면 된다.
        public LeverPreset Mirrored
        {
            get
            {
                LeverPreset copy = this;
                copy.LaunchOffset = new Vector2(-LaunchOffset.x, LaunchOffset.y);
                copy.LaunchVelocity = new Vector2(-LaunchVelocity.x, LaunchVelocity.y);
                return copy;
            }
        }
    }

    /// <summary>JsonUtility 는 최상위 배열을 못 다뤄 한 겹 감싼다.</summary>
    [Serializable]
    public class LeverPresetTable
    {
        public List<LeverPreset> Presets = new List<LeverPreset>();
    }

    /// <summary>
    /// 목표를 주면 그리로 보낼 수 있는 지렛대들을 낸다.
    /// 잉크가 적은 것부터 내는 것은 솔버가 앞에서부터 굴려 보기 때문이다 —
    /// 먼저 통하는 것이 곧 답이므로 싼 것부터 봐야 한다.
    /// </summary>
    public sealed class LeverPresets
    {
        /// <summary>
        /// 도달했다고 볼 거리. 공 반지름의 절반이다.
        /// 스치는 것을 도달로 치면 시뮬에서 재현되지 않는다.
        /// </summary>
        public const float ReachTolerance = LevelData.BallRadius * 0.5f;

        /// 포물선을 몇 스텝까지 따라가 볼지.
        public const int MaxFlight = 600;

        readonly List<LeverPreset> _byInk;

        public LeverPresets(List<LeverPreset> presets)
        {
            _byInk = new List<LeverPreset>(presets);
            _byInk.Sort((x, y) => x.Ink.CompareTo(y.Ink));
        }

        public static LeverPresets Load()
            => new LeverPresets(LeverPresetFile.Load().Presets);

        public int Count => _byInk.Count;

        public LeverPreset this[int index] => _byInk[index];

        /// <summary>
        /// 공 자리에서 target 으로 보낼 수 있는 것들. 잉크가 적은 순이다.
        /// 목표가 왼쪽이면 표를 뒤집어 본다.
        /// </summary>
        /// <param name="into">앞을 비우고 채운다. 돌려 써도 된다.</param>
        public void Reaching(Vector2 target, List<LeverPreset> into)
        {
            into.Clear();

            bool right = target.x >= 0f;

            for (int i = 0; i < _byInk.Count; i++)
            {
                LeverPreset preset = right ? _byInk[i] : _byInk[i].Mirrored;

                if (preset.StepTo(target, ReachTolerance, MaxFlight) >= 0)
                    into.Add(preset);
            }
        }
    }

    /// <summary>
    /// 실측이 쓰고 솔버·뷰어가 읽는 자리.
    /// 파일로 두는 것은 스윕을 다시 돌릴 때 코드를 안 고치기 위해서다.
    /// </summary>
    public static class LeverPresetFile
    {
        public const string RelativePath = "_Project/Presets/LeverPresets.json";

        public static string FullPath => Path.Combine(Application.dataPath, RelativePath);

        public static bool Exists => File.Exists(FullPath);

        public static LeverPresetTable Load()
            => JsonUtility.FromJson<LeverPresetTable>(File.ReadAllText(FullPath));

        public static void Save(LeverPresetTable table)
        {
            string path = FullPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(table, true));
        }
    }
}
