using System.Collections.Generic;
using System.Text;
using PPS.Core;

namespace PPS.MapEditor.Dev
{
    /// <summary>
    /// 스테이지 파일 이름 규칙 — {이름}_{장치}_{풀이}.
    /// 장치 토큰만 판 데이터로 검증할 수 있다.
    /// 풀이는 파일에 없어 이름의 것을 그대로 쓴다.
    /// </summary>
    public static class StageNameConvention
    {
        /// 장치 이니셜. 배열 순서 = DeviceType 순서 = 표기 순서.
        static readonly string[] DeviceInitials = { "BB", "FB", "SP", "WD", "SL", "BT", "BR" };

        static readonly string[] SolutionInitials = { "FL", "FB", "LK", "PV" };

        /// 솔버가 알아서 푼 판. 개수가 없어 이니셜 규칙을 벗어난다.
        const string AutoSolution = "Auto";

        /// <summary>판의 장치를 세어 BB2SL1 꼴로 만든다. 없으면 빈 문자열.</summary>
        public static string DeviceToken(LevelData level)
        {
            var counts = new int[DeviceInitials.Length];

            // 디스크 형태가 아니라 런타임 목록을 센다 —
            // 옛 형식으로 저장된 판은 마이그레이션이
            // 이쪽에만 장치를 세워 준다.
            var devices = level?.Devices;
            if (devices != null)
            {
                for (int i = 0; i < devices.Count; i++)
                {
                    int type = (int)devices[i].Type;
                    if (type >= 0 && type < counts.Length) counts[type]++;
                }
            }

            var token = new StringBuilder();
            for (int i = 0; i < counts.Length; i++)
                if (counts[i] > 0) token.Append(DeviceInitials[i]).Append(counts[i]);

            return token.ToString();
        }

        /// <summary>
        /// 파일 이름을 세 조각으로 가른다. 뒤에서부터 읽는다 —
        /// 이름 쪽에 _ 가 들어 있어도 나머지가 밀리지 않는다.
        /// </summary>
        public static void Parse(string fileName, out string name, out string devices, out string solution)
        {
            string[] parts = fileName.Split('_');
            int last = parts.Length - 1;

            devices = "";
            solution = "";

            // FB 는 파편폭탄이자 자유물체다. 토막이 하나뿐이면
            // 장치로 읽는다 — 장치만 데이터로 확인할 수 있다.
            if (last >= 1 && IsToken(parts[last], SolutionInitials, true)
                && (last >= 2 || !IsToken(parts[last], DeviceInitials, false)))
            {
                solution = parts[last];
                last--;
            }

            if (last >= 1 && IsToken(parts[last], DeviceInitials, false))
            {
                devices = parts[last];
                last--;
            }

            name = string.Join("_", parts, 0, last + 1);
        }

        /// <summary>규칙대로라면 이 판의 파일 이름은 이것이다.</summary>
        public static string Expected(string fileName, LevelData level)
        {
            Parse(fileName, out string name, out _, out string solution);

            var expected = new StringBuilder(name);

            string devices = DeviceToken(level);
            if (devices.Length > 0) expected.Append('_').Append(devices);
            if (solution.Length > 0) expected.Append('_').Append(solution);

            return expected.ToString();
        }

        public static bool HasSolution(string fileName)
        {
            Parse(fileName, out _, out _, out string solution);
            return solution.Length > 0;
        }

        /// <returns>이니셜+개수가 끝까지 이어지면 true.</returns>
        static bool IsToken(string part, IReadOnlyList<string> initials, bool allowAuto)
        {
            if (allowAuto && part == AutoSolution) return true;

            int i = 0;
            while (i < part.Length)
            {
                int matched = -1;
                for (int k = 0; k < initials.Count; k++)
                {
                    if (i + 2 > part.Length) break;
                    if (string.CompareOrdinal(part, i, initials[k], 0, 2) != 0) continue;

                    matched = k;
                    break;
                }

                if (matched < 0) return false;
                i += 2;

                int digits = 0;
                while (i < part.Length && part[i] >= '0' && part[i] <= '9')
                {
                    i++;
                    digits++;
                }

                if (digits == 0) return false;
            }

            return i > 0;
        }
    }
}
