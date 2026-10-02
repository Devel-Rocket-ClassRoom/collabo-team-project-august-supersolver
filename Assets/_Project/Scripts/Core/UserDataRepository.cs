using System.Collections.Generic;
using UnityEngine;

namespace PPS.Core
{
    public interface IUserDataRepository
    {
        public UserData Data { get; }

        /// 이 자리의 클리어 기록. 한 번도 안 깼으면 null.
        StageClearData ClearOf(StageEntry entry);

        /// 클리어를 기록하고 진척도를 갱신한다. 다시 깨서 더 못한
        /// 결과가 나와도 기록과 진척도는 최고치를 유지한다.
        void RecordClear(StageEntry entry, int stars, int starGrade);
    }
    public class UserDataRepository : IUserDataRepository
    {
        public UserData Data { get; }

        readonly Dictionary<StageEntry, StageClearData> _clears = new();

        public UserDataRepository(UserData data)
        {
            Data = data;

            foreach (StageClearData clear in data.StageClears)
            {
                _clears[clear.Entry] = clear;
            }
        }

        public StageClearData ClearOf(StageEntry entry)
        {
            return _clears.TryGetValue(entry, out StageClearData clear) ? clear : null;
        }

        public void RecordClear(StageEntry entry, int stars, int starGrade)
        {
            StageClearData clear = EnsureClear(entry);

            clear.IsCleared = true;
            clear.BestStars = Mathf.Max(stars, clear.BestStars);
            clear.StarGrade = Mathf.Max(starGrade, clear.StarGrade);

            // 이미 깬 앞 자리를 다시 깨도 진척도는 물러나지 않는다.
            if (entry > Data.LastCleared) Data.LastCleared = entry;

            // 해금 판정이 이 값으로 첫 판과 (0,0) 클리어를 가른다.
            Data.HasPlayed = true;
        }

        StageClearData EnsureClear(StageEntry entry)
        {
            if (_clears.TryGetValue(entry, out StageClearData clear)) return clear;

            clear = new StageClearData() { Entry = entry };

            // 저장은 List 를 내보내므로 양쪽에 같이 넣는다.
            Data.StageClears.Add(clear);
            _clears.Add(entry, clear);

            return clear;
        }
    }
}
