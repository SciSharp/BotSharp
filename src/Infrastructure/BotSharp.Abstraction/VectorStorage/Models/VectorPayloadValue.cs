using BotSharp.Abstraction.VectorStorage.Enums;

namespace BotSharp.Abstraction.VectorStorage.Models;

public class VectorPayloadValue
{
    [JsonPropertyName("data_value")]
    public object DataValue { get; set; } = null!;

    [JsonPropertyName("data_type")]
    public VectorPayloadDataType DataType { get; set; } = VectorPayloadDataType.String;

    public VectorPayloadValue()
    {
        
    }

    public VectorPayloadValue(object data, VectorPayloadDataType dataType)
    {
        DataValue = data;
        DataType = dataType;
    }

    public override string ToString()
    {
        return $"{DataValue} ({DataType})";
    }

    public static VectorPayloadValue BuildStringValue(string data) => new(data, VectorPayloadDataType.String);
    public static VectorPayloadValue BuildIntegerValue(long data) => new(data, VectorPayloadDataType.Integer);
    public static VectorPayloadValue BuildIntegerValue(int data) => new(data, VectorPayloadDataType.Integer);
    public static VectorPayloadValue BuildIntegerValue(short data) => new(data, VectorPayloadDataType.Integer);
    public static VectorPayloadValue BuildIntegerValue(byte data) => new(data, VectorPayloadDataType.Integer);
    public static VectorPayloadValue BuildDoubleValue(double data) => new(data, VectorPayloadDataType.Double);
    public static VectorPayloadValue BuildDoubleValue(float data) => new(data, VectorPayloadDataType.Double);
    public static VectorPayloadValue BuildBooleanValue(bool data) => new(data, VectorPayloadDataType.Boolean);
    public static VectorPayloadValue BuildDatetimeValue(DateTime data) => new(data, VectorPayloadDataType.Datetime);
    public static VectorPayloadValue BuildUnkownValue(object data) => new(data, VectorPayloadDataType.Unknown);


    public static explicit operator VectorPayloadValue(string data) => VectorPayloadValue.BuildStringValue(data);
    public static explicit operator VectorPayloadValue(long data) => VectorPayloadValue.BuildIntegerValue(data);
    public static explicit operator VectorPayloadValue(int data) => VectorPayloadValue.BuildIntegerValue(data);
    public static explicit operator VectorPayloadValue(short data) => VectorPayloadValue.BuildIntegerValue(data);
    public static explicit operator VectorPayloadValue(byte data) => VectorPayloadValue.BuildIntegerValue(data);
    public static explicit operator VectorPayloadValue(double data) => VectorPayloadValue.BuildDoubleValue(data);
    public static explicit operator VectorPayloadValue(float data) => VectorPayloadValue.BuildDoubleValue(data);
    public static explicit operator VectorPayloadValue(bool data) => VectorPayloadValue.BuildBooleanValue(data);
    public static explicit operator VectorPayloadValue(DateTime data) => VectorPayloadValue.BuildDatetimeValue(data);

    public static implicit operator string(VectorPayloadValue value) => value.DataValue?.ToString() ?? string.Empty;
    public static implicit operator long(VectorPayloadValue value) => long.TryParse(value.DataValue?.ToString(), out var res) ? res : default;
    public static implicit operator int(VectorPayloadValue value) => int.TryParse(value.DataValue?.ToString(), out var res) ? res : default;
    public static implicit operator short(VectorPayloadValue value) => short.TryParse(value.DataValue?.ToString(), out var res) ? res : default;
    public static implicit operator byte(VectorPayloadValue value) => byte.TryParse(value.DataValue?.ToString(), out var res) ? res : default;
    public static implicit operator double(VectorPayloadValue value) => double.TryParse(value.DataValue?.ToString(), out var res) ? res : default;
    public static implicit operator float(VectorPayloadValue value) => float.TryParse(value.DataValue?.ToString(), out var res) ? res : default;
    public static implicit operator bool(VectorPayloadValue value) => bool.TryParse(value.DataValue?.ToString(), out var res) ? res : default;
    public static implicit operator DateTime(VectorPayloadValue value) => DateTime.TryParse(value.DataValue?.ToString(), out var res) ? res : default;
}
