using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class KnowledgeCollectionDetailsViewModel : VectorCollectionDetails
{
    public static KnowledgeCollectionDetailsViewModel? From(VectorCollectionDetails? model)
    {
        if (model == null) return null;

        return new KnowledgeCollectionDetailsViewModel
        {
            Status = model.Status,
            OptimizerStatus = model.OptimizerStatus,
            SegmentsCount = model.SegmentsCount,
            VectorsCount = model.VectorsCount,
            IndexedVectorsCount = model.IndexedVectorsCount,
            PointsCount = model.PointsCount,
            InnerConfig = model.InnerConfig,
            BasicInfo = model.BasicInfo,
            PayloadSchema = model.PayloadSchema
        };
    }
}
