using System;

namespace PPS.Core
{
    public enum DeviceEditKind { Value, Position, Radius, Angle }

    /// 데이터의 의미를 편집기에 알려준다.
    /// 장치 종류별 UI 분기 대신 필드에 선언한다.
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class DeviceParameterAttribute : Attribute
    {
        public string Label { get; }
        public DeviceEditKind Kind { get; set; }
        public string Unit { get; set; } = "";
        public int Order { get; set; }

        public DeviceParameterAttribute(string label) => Label = label;
    }
}
