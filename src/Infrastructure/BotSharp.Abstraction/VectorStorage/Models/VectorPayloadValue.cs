using BotSharp.Abstraction.VectorStorage.Enums;

namespace BotSharp.Abstraction.VectorStorage.Models;

public class VectorPayloadValue
{
    [JsonPropertyName("data")]
    public object Data { get; set; } = null!;

    [JsonPropertyName("data_type")]
    public VectorPayloadDataType DataType { get; set; } = VectorPayloadDataType.String;

    public VectorPayloadValue()
    {
        
    }

    public VectorPayloadValue(object data, VectorPayloadDataType dataType)
    {
        Data = data;
        DataType = dataType;
    }

    public override string ToString()
    {
        return $"{Data} ({DataType})";
    }

    public static VectorPayloadValue BuildStringValue(string data)
    {
        return new(data, VectorPayloadDataType.String);
    }

    public static VectorPayloadValue BuildIntegerValue(long data)
    {
        return new(data, VectorPayloadDataType.Integer);
    }

    public static VectorPayloadValue BuildDoubleValue(double data)
    {
        return new(data, VectorPayloadDataType.Double);
    }

    public static VectorPayloadValue BuildBooleanValue(bool data)
    {
        return new(data, VectorPayloadDataType.Boolean);
    }

    public static VectorPayloadValue BuildDatetimeValue(DateTime data)
    {
        return new(data, VectorPayloadDataType.Datetime);
    }

    public static VectorPayloadValue BuildUnkownValue(object data)
    {
        return new(data, VectorPayloadDataType.Unknown);
    }
}
