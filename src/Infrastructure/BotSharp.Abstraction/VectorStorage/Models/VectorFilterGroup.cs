using BotSharp.Abstraction.VectorStorage.Enums;

namespace BotSharp.Abstraction.VectorStorage.Models;

public class VectorFilterGroup
{
    [JsonPropertyName("filters")]
    public List<VectorFilterSubGroup>? Filters { get; set; }

    [JsonPropertyName("logical_operator")]
    public string LogicalOperator { get; set; } = "or";
}

public class VectorFilterSubGroup
{
    [JsonPropertyName("operands")]
    public List<VectorFilterOperand> Operands { get; set; } = [];

    [JsonPropertyName("logical_operator")]
    public string LogicalOperator { get; set; } = "or";
}

public class VectorFilterOperand
{
    [JsonPropertyName("match")]
    public VectorFilterMatch? Match { get; set; }

    [JsonPropertyName("range")]
    public VectorFilterRange? Range { get; set; }
}

public class VectorFilterMatch : KeyValue
{
    /// <summary>
    /// Match operator: eq, neq
    /// </summary>
    [JsonPropertyName("operator")]
    public string Operator { get; set; } = "eq";

    [JsonPropertyName("data_type")]
    public VectorPayloadDataType DataType { get; set; } = VectorPayloadDataType.String;

    public override string ToString()
    {
        return $"{Key} => {Value} ({DataType}), Operator: {Operator}";
    }
}

public class VectorFilterRange
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = null!;

    [JsonPropertyName("conditions")]
    public List<VectorFilterRangeCondition> Conditions { get; set; } = [];

    [JsonPropertyName("data_type")]
    public VectorPayloadDataType DataType { get; set; } = VectorPayloadDataType.String;

    public override string ToString()
    {
        var str = $"Data type: {DataType};";

        foreach (var condition in Conditions)
        {
            str += $"{Key} {condition.Operator} {condition.Value};";
        }
        return str;
    }
}

public class VectorFilterRangeCondition
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = null!;

    /// <summary>
    /// Range operator: lt, lte, gt, gte
    /// </summary>
    [JsonPropertyName("operator")]
    public string Operator { get; set; } = "lt";

    public override string ToString()
    {
        return $"Value: {Value}, Operator: {Operator}";
    }
}