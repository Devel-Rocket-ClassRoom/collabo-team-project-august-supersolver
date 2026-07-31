using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 코어 소스에 결정론을 깨뜨리는 API 가 섞여 들어왔는지 정적으로 검사한다.
    ///
    /// asmdef 는 "코어가 UI 를 참조하지 않는다"까지만 강제할 수 있다.
    /// Time.deltaTime 한 줄이나 UnityEngine.Random 한 줄은 컴파일러가 잡아주지 않으며,
    /// 실행 테스트로도 잘 드러나지 않는다 — 에디터에서 한 번 돌릴 때는 멀쩡히 통과하고
    /// 나중에 다른 환경에서 조용히 갈라지기 때문이다. 그래서 소스를 직접 읽는다.
    ///
    /// 새 팀원이 코어에 손을 댈 때 이 테스트가 리뷰보다 먼저 반응하는 것이 목적이다.
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

            foreach (string path in CoreSourceFiles())
            {
                string code = StripCommentsAndStrings(File.ReadAllText(path));
                string fileName = Path.GetFileName(path);

                foreach (var (token, why) in Banned)
                {
                    if (code.Contains(token))
                        offenses.Add($"{fileName}: '{token}' — {why}");
                }
            }

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
        /// 주석과 문자열 리터럴을 지운다. 금지 API 를 설명하는 주석까지 위반으로 잡으면
        /// 팀원이 그 주석을 지우게 되고, 정작 알아야 할 이유가 코드에서 사라진다.
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
