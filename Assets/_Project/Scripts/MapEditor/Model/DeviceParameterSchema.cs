using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using PPS.Core;
using UnityEngine;

namespace PPS.MapEditor
{
    /// 직렬화되는 장치 필드에서 편집 항목을 만든다.
    public sealed class DeviceParameterSchema
    {
        static readonly Dictionary<Type, DeviceParameterSchema> Cache = new Dictionary<Type, DeviceParameterSchema>();
        public readonly IReadOnlyList<DeviceParameter> Parameters;

        DeviceParameterSchema(Type type)
        {
            var parameters = new List<DeviceParameter>();
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (field.IsInitOnly || field.IsNotSerialized
                    || (!field.IsPublic && !field.IsDefined(typeof(SerializeField)))
                    || field.IsDefined(typeof(HideInInspector))) continue;
                parameters.Add(new DeviceParameter(field));
            }
            parameters.Sort((a, b) => a.Order != b.Order ? a.Order.CompareTo(b.Order) : a.DeclarationOrder.CompareTo(b.DeclarationOrder));
            Parameters = parameters.AsReadOnly();
        }

        public static DeviceParameterSchema For(IDeviceData device)
        {
            var type = device.GetType();
            if (!Cache.TryGetValue(type, out var schema))
                Cache.Add(type, schema = new DeviceParameterSchema(type));
            return schema;
        }

        public DeviceParameter Find(DeviceEditKind kind)
        {
            foreach (var parameter in Parameters)
                if (parameter.Kind == kind) return parameter;
            return null;
        }
    }

    public sealed class DeviceParameter
    {
        readonly FieldInfo _field;
        readonly double _min;
        readonly double _max;
        public string Name => _field.Name;
        public Type ValueType => _field.FieldType;
        public string Label { get; }
        public string Unit { get; }
        public string Help { get; }
        public DeviceEditKind Kind { get; }
        public int Order { get; }
        public int DeclarationOrder => _field.MetadataToken;
        public bool IsNumber => ValueType == typeof(float) || ValueType == typeof(int);
        public bool CanEdit => IsNumber || ValueType == typeof(bool) || ValueType.IsEnum;

        public DeviceParameter(FieldInfo field)
        {
            _field = field;
            var metadata = field.GetCustomAttribute<DeviceParameterAttribute>();
            Label = metadata?.Label ?? field.Name;
            Unit = metadata?.Unit ?? "";
            Help = field.GetCustomAttribute<TooltipAttribute>()?.tooltip ?? "";
            Kind = metadata?.Kind ?? DeviceEditKind.Value;
            Order = metadata?.Order ?? 0;
            var range = field.GetCustomAttribute<RangeAttribute>();
            _min = range != null ? range.min : field.GetCustomAttribute<MinAttribute>()?.min ?? double.NegativeInfinity;
            _max = range != null ? range.max : double.PositiveInfinity;
        }

        public object Read(IDeviceData device) => _field.GetValue(device);

        public string Format(IDeviceData device)
        {
            object value = Read(device);
            if (value == null) return "None";
            if (value is System.Collections.ICollection items) return $"{items.Count} items";
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        public bool TryParse(string text, out object value, out string error)
        {
            value = null;
            if (!CanEdit)
            {
                error = "Read only: no editor for this type.";
                return false;
            }
            error = "Enter a valid value.";
            if (ValueType == typeof(bool) && bool.TryParse(text, out bool flag)) value = flag;
            else if (ValueType.IsEnum && Enum.TryParse(ValueType, text, out var choice)
                && Enum.IsDefined(ValueType, choice)) value = choice;
            else if (IsNumber)
            {
                double number;
                if (ValueType == typeof(int))
                {
                    if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int integer))
                    {
                        error = "Enter a whole number.";
                        return false;
                    }
                    number = integer;
                    value = integer;
                }
                else
                {
                    if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float scalar)
                        || float.IsNaN(scalar) || float.IsInfinity(scalar)) return false;
                    number = scalar;
                    value = scalar;
                }
                if (number < _min || number > _max)
                {
                    error = double.IsPositiveInfinity(_max)
                        ? $"Minimum: {_min}." : $"Range: {_min} to {_max}.";
                    value = null;
                    return false;
                }
            }
            if (value == null) return false;
            error = "";
            return true;
        }

        public void Write(IDeviceData device, object value) => _field.SetValue(device, value);

        public float Constrain(float value) => (float)Math.Max(_min, Math.Min(_max, value));
    }
}
