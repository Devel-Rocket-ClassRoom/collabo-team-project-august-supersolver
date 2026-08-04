using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 금지 API 가 섞였는지 소스를 직접 읽는다.
    /// 컴파일러도 실행 테스트도 못 잡고,
    /// 나중에 다른 환경에서 조용히 갈라진다.
    /// </summary>
    public class CoreSourceGuardTests
    {
        static readonly (string Token, string Why)[] Banned =
        {
            ("Time.deltaTime", "프레임 경과 시간이 시뮬에 들어오면 프레임레이트가 결과를 바꾼다."),
            ("Time.fixedDeltaTime", "dt 는 SimWorld.FixedDt 하나뿐이다."),
            ("Time.timeScale", "시간 배율은 dt 를 바꾼다. 배속은 Step 호출 횟수로 낸다."),
            ("Time.time", "경과 시간 기반 로직은 스텝 카운트로 바꿔야 한다."),
            ("Time.frameCount", "프레임 수는 스텝 수가 아니다."),
            ("UnityEngine.Random", "난수는 주입된 System.Random(seed) 하나만 쓴다."),
            ("WaitForSeconds", "시간 기반 대기. 장치 로직은 IStepLogic.Tick 의 step 으로 센다."),
            ("void FixedUpdate", "호출 시점·횟수를 Unity 가 관리해 통제할 수 없다."),
            ("DateTime.Now", "실행 시각이 결과에 섞이면 재현이 불가능해진다."),
            ("Guid.NewGuid", "실행마다 값이 달라진다."),
        };

        [Test]
        public void 코어_소스에_결정론_금지_API가_없다()
        {
            var offenses = new List<string>();
            int scanned = 0;

            foreach (string path in CoreSourceFiles())
            {
                scanned++;

                string code = StripCommentsAndStrings(File.ReadAllText(path));
                string fileName = Path.GetFileName(path);

                foreach (var (token, why) in Banned)
                {
                    if (code.Contains(token))
                        offenses.Add($"{fileName}: '{token}' — {why}");
                }
            }

            // 스캔이 비면 조용히 초록불이 된다.
            // 일했다는 증거를 남긴다.
            Assert.Greater(scanned, 0, "코어 소스를 한 파일도 읽지 못했다 — 검사가 헛돌았다.");

            Assert.IsEmpty(offenses, "\n" + string.Join("\n", offenses));
        }

        [Test]
        public void 코어_asmdef가_UI나_렌더링을_참조하지_않는다()
        {
            string asmdefPath = Path.Combine(CoreRoot(), "PPS.Core.asmdef");
            Assert.IsTrue(File.Exists(asmdefPath), "PPS.Core.asmdef 를 찾을 수 없다: " + asmdefPath);

            string json = File.ReadAllText(asmdefPath);
            var references = Regex.Match(json, "\"references\"\\s*:\\s*\\[(?<body>[^\\]]*)\\]").Groups["body"].Value;

            string[] forbidden = { "UnityEngine.UI", "Unity.RenderPipelines", "Unity.TextMeshPro", "Unity.InputSystem" };
            foreach (string name in forbidden)
            {
                Assert.IsFalse(references.Contains(name),
                    $"코어가 {name} 을 참조한다. 코어는 프레임도 화면도 몰라야 한다.");
            }
        }

        static string CoreRoot() => Path.Combine(Application.dataPath, "_Project", "Scripts", "Core");

        static IEnumerable<string> CoreSourceFiles()
        {
            string root = CoreRoot();
            Assert.IsTrue(Directory.Exists(root), "코어 소스 폴더를 찾을 수 없다: " + root);
            return Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories);
        }

        /// <summary>
        /// 주석과 문자열을 지운다.
        /// 금지 API 를 설명하는 주석까지 잡으면
        /// 팀원이 그 설명을 지우게 된다.
        /// </summary>
        static string StripCommentsAndStrings(string source)
        {
            source = Regex.Replace(source, @"/\*.*?\*/", " ", RegexOptions.Singleline);
            source = Regex.Replace(source, @"//[^\n]*", " ");
            source = Regex.Replace(source, "@\"(?:[^\"]|\"\")*\"", "\"\"");
            source = Regex.Replace(source, "\"(?:\\\\.|[^\"\\\\])*\"", "\"\"");
            return source;
        }
    }
}
