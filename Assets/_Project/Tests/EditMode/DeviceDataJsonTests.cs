using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PPS.Core.Tests
{
    /// <summary>
    /// 장치별 데이터가 디스크를 거쳐도 그대로인가,
    /// 옛 공통 형식이 매핑표대로 갈라지는가.
    /// 둘 중 하나라도 어긋나면 저장된 판이 조용히 달라진다.
    /// </summary>
    public class DeviceDataJsonTests
    {
        [Test]
        public void 장치_4종이_왕복해도_그대로다()
        {
            // 기본값과 일부러 다르게 잡는다.
            // 기본값이면 안 실려도 통과한다.
            var before = new List<IDeviceData>
            {
                new BombData
                {
                    Position = new Vector2(1.5f, -2.25f),
                    Radius = 3.5f, Power = 7.25f, DelaySteps = 12, JitterSteps = 4,
                },
                new FragBombData
                {
                    Position = new Vector2(-0.5f, 4f),
                    Power = 9.5f, DelaySteps = 7, JitterSteps = 3,
                },
                new SpikeData
                {
                    Position = new Vector2(2f, 2f),
                    Radius = 0.75f,
                },
                new WindData
                {
                    Position = new Vector2(-3f, 1f),
                    Radius = 4.5f, Power = 8f, Angle = 135f,
                },
            };

            var entries = new List<DeviceEntry>();
            var after = new List<IDeviceData>();

            DeviceSerialization.Pack(before, entries);
            DeviceSerialization.Unpack(entries, after);

            Assert.AreEqual(before.Count, after.Count, "장치 수가 달라졌다.");

            var bomb = Get<BombData>(after, 0);
            Assert.AreEqual(new Vector2(1.5f, -2.25f), bomb.Position);
            Assert.AreEqual(3.5f, bomb.Radius, 1e-5f);
            Assert.AreEqual(7.25f, bomb.Power, 1e-5f);
            Assert.AreEqual(12, bomb.DelaySteps);
            Assert.AreEqual(4, bomb.JitterSteps);

            var frag = Get<FragBombData>(after, 1);
            Assert.AreEqual(new Vector2(-0.5f, 4f), frag.Position);
            Assert.AreEqual(9.5f, frag.Power, 1e-5f);
            Assert.AreEqual(7, frag.DelaySteps);
            Assert.AreEqual(3, frag.JitterSteps);

            var spike = Get<SpikeData>(after, 2);
            Assert.AreEqual(new Vector2(2f, 2f), spike.Position);
            Assert.AreEqual(0.75f, spike.Radius, 1e-5f);

            var wind = Get<WindData>(after, 3);
            Assert.AreEqual(new Vector2(-3f, 1f), wind.Position);
            Assert.AreEqual(4.5f, wind.Radius, 1e-5f);
            Assert.AreEqual(8f, wind.Power, 1e-5f);
            Assert.AreEqual(135f, wind.Angle, 1e-5f);
        }

        [Test]
        public void 왕복이_순서를_지킨다()
        {
            // 순서 = 등록 순서 = 난수 소비 순서다.
            var before = new List<IDeviceData>
            {
                new WindData(), new BombData(), new SpikeData(), new BombData(), new FragBombData(),
            };

            var entries = new List<DeviceEntry>();
            var after = new List<IDeviceData>();

            DeviceSerialization.Pack(before, entries);
            DeviceSerialization.Unpack(entries, after);

            for (int i = 0; i < before.Count; i++)
                Assert.AreEqual(before[i].Type, after[i].Type, $"장치 {i} 의 종류가 밀렸다.");
        }

        [Test]
        public void 옛_형식이_매핑표대로_갈라진다()
        {
            var devices = StageDataMigration.ToV1Devices(LegacyStageJson);

            Assert.AreEqual(4, devices.Count, "옛 장치를 다 읽지 못했다.");

            // 폭탄은 전부 가져간다. 방향만 버린다.
            var bomb = Get<BombData>(devices, 0);
            Assert.AreEqual(new Vector2(1f, 2f), bomb.Position);
            Assert.AreEqual(2.5f, bomb.Radius, 1e-5f);
            Assert.AreEqual(11f, bomb.Power, 1e-5f);
            Assert.AreEqual(30, bomb.DelaySteps);
            Assert.AreEqual(5, bomb.JitterSteps);

            // 파편 폭탄은 Radius 를 버린다 — 파편이 어디까지 날지는 반경과 무관하다.
            var frag = Get<FragBombData>(devices, 1);
            Assert.AreEqual(new Vector2(3f, 4f), frag.Position);
            Assert.AreEqual(6f, frag.Power, 1e-5f);
            Assert.AreEqual(20, frag.DelaySteps);
            Assert.AreEqual(2, frag.JitterSteps);

            // 가시는 자리와 크기뿐이다.
            var spike = Get<SpikeData>(devices, 2);
            Assert.AreEqual(new Vector2(5f, 6f), spike.Position);
            Assert.AreEqual(0.3f, spike.Radius, 1e-5f);

            // 바람은 지연을 버린다 — 늘 불고 있다.
            var wind = Get<WindData>(devices, 3);
            Assert.AreEqual(new Vector2(7f, 8f), wind.Position);
            Assert.AreEqual(2f, wind.Radius, 1e-5f);
            Assert.AreEqual(5f, wind.Power, 1e-5f);
            Assert.AreEqual(90f, wind.Angle, 1e-5f);
        }

        [Test]
        public void 장치가_없는_옛_판은_빈_목록이_된다()
        {
            var devices = StageDataMigration.ToV1Devices("{\"StageId\":\"S000\",\"Seed\":0,\"Level\":{}}");

            Assert.IsNotNull(devices);
            Assert.IsEmpty(devices);
        }

        [Test]
        public void 복제본은_원본과_따로_움직인다()
        {
            // 붙여넣기가 이것에 기댄다.
            IDeviceData original = new BombData { Position = new Vector2(1f, 1f), Radius = 3f };
            var copy = original.Clone();

            copy.Position = new Vector2(9f, 9f);

            Assert.AreEqual(new Vector2(1f, 1f), original.Position, "원본이 따라 움직였다.");
            Assert.AreEqual(3f, ((BombData)copy).Radius, 1e-5f, "복제가 값을 흘렸다.");
        }

        static T Get<T>(IReadOnlyList<IDeviceData> devices, int index) where T : class, IDeviceData
        {
            var typed = devices[index] as T;
            Assert.IsNotNull(typed, $"장치 {index} 가 {typeof(T).Name} 이 아니다: {devices[index]?.GetType().Name}");
            return typed;
        }

        /// 옛 공통 DeviceData 로 저장된 판. 네 종류를 한 번씩 담았다.
        const string LegacyStageJson = @"{
            ""StageId"": ""S999"",
            ""Seed"": 3,
            ""Level"": {
                ""Devices"": [
                    { ""Type"": 0, ""Position"": { ""x"": 1.0, ""y"": 2.0 }, ""Radius"": 2.5, ""Power"": 11.0, ""DelaySteps"": 30, ""JitterSteps"": 5, ""Angle"": 45.0 },
                    { ""Type"": 1, ""Position"": { ""x"": 3.0, ""y"": 4.0 }, ""Radius"": 1.5, ""Power"": 6.0, ""DelaySteps"": 20, ""JitterSteps"": 2, ""Angle"": 10.0 },
                    { ""Type"": 2, ""Position"": { ""x"": 5.0, ""y"": 6.0 }, ""Radius"": 0.3, ""Power"": 4.0, ""DelaySteps"": 9, ""JitterSteps"": 1, ""Angle"": 20.0 },
                    { ""Type"": 3, ""Position"": { ""x"": 7.0, ""y"": 8.0 }, ""Radius"": 2.0, ""Power"": 5.0, ""DelaySteps"": 8, ""JitterSteps"": 7, ""Angle"": 90.0 }
                ]
            }
        }";
    }
}
