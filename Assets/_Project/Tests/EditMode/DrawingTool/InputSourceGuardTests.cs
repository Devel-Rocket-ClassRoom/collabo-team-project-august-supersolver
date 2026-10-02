using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace PPS.DrawingTool.Tests
{
    /// <summary>
    /// 파이프라인이 순수 C# 인지 소스를 직접 읽는다.
    /// PPS.DrawingTool asmdef 가 InputSystem·UI 를 이미
    /// 참조해 어셈블리 경계로는 막히지 않는다.
    /// </summary>
    public class InputSourceGuardTests
    {
        static readonly (string Token, string Why)[] Banned =
        {
            ("UnityEngine.InputSystem", "입력 장치를 알면 합성 궤적 테스트가 불가능해진다."),
            ("UnityEngine.UI", "파이프라인은 화면을 몰라야 한다."),
            ("UnityEngine.EventSystems", "소유권 판정은 프론트엔드 몫이다."),
            ("MonoBehaviour", "프레임 훅이 붙으면 60/120Hz 검증을 기기 없이 할 수 없다."),
            ("Time.deltaTime", "프레임 경과 시간이 섞이면 점 밀도가 주사율을 탄다."),
        };

        [Test]
        public void 궤적_파이프라인에_프레임과_화면이_섞이지_않는다()
        {
            string root = Path.Combine(
                Application.dataPath, "_Project", "Scripts", "DrawingTool", "Input");
            Assert.IsTrue(Directory.Exists(root), "파이프라인 소스 폴더를 찾을 수 없다: " + root);

            string[] files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories);
            Assert.Greater(files.Length, 0, "소스를 한 파일도 읽지 못했다 — 검사가 헛돌았다.");

            var offenses = new List<string>();
            foreach (string path in files)
            {
                string code = StripComments(File.ReadAllText(path));
                foreach (var (token, why) in Banned)
                {
                    if (code.Contains(token))
                        offenses.Add($"{Path.GetFileName(path)}: '{token}' — {why}");
                }
            }

            Assert.IsEmpty(offenses, "\n" + string.Join("\n", offenses));
        }

        /// <summary>
        /// 금지 API 를 설명하는 주석까지 잡으면
        /// 팀원이 그 설명을 지우게 된다.
        /// </summary>
        static string StripComments(string source)
        {
            source = Regex.Replace(source, @"/\*.*?\*/", " ", RegexOptions.Singleline);
            return Regex.Replace(source, @"//[^\n]*", " ");
        }
    }
}
