namespace BotSharp.Abstraction.VectorStorage.Models;

public class VectorCollectionDetails
{
    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("optimizer_status")]
    public string OptimizerStatus { get; set; }

    [JsonPropertyName("segments_count")]
    public ulong SegmentsCount { get; set; }

    [JsonPropertyName("vectors_count")]
    public ulong VectorsCount { get; set; }

    [JsonPropertyName("indexed_vectors_count")]
    public ulong IndexedVectorsCount { get; set; }

    [JsonPropertyName("points_count")]
    public ulong PointsCount { get; set; }

    [JsonPropertyName("inner_config")]
    public VectorCollectionDetailConfig? InnerConfig { get; set; }

    [JsonPropertyName("basic_info")]
    public VectorCollectionConfig? BasicInfo { get; set; }
}

public class VectorCollectionDetailConfig
{
    public VectorCollectionDetailConfigParam? Param { get; set; }
}

public class VectorCollectionDetailConfigParam
{
    [JsonPropertyName("shard_number")]
    public uint? ShardNumber { get; set; }

    [JsonPropertyName("sharding_method")]
    public string? ShardingMethod { get; set; }

    [JsonPropertyName("replication_factor")]
    public uint? ReplicationFactor { get; set; }

    [JsonPropertyName("write_consistency_factor")]
    public uint? WriteConsistencyFactor { get; set; }

    [JsonPropertyName("read_fan_out_factor")]
    public uint? ReadFanOutFactor { get; set; }
}