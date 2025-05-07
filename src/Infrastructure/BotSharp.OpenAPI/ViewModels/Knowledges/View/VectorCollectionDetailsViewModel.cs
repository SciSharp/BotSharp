using BotSharp.Abstraction.VectorStorage.Models;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class VectorCollectionDetailsViewModel : VectorCollectionDetails
{
    public static VectorCollectionDetailsViewModel? From(VectorCollectionDetails? model)
    {
        if (model == null) return null;

        return new VectorCollectionDetailsViewModel
        {
            Status = model.Status,
            OptimizerStatus = model.OptimizerStatus,
            SegmentsCount = model.SegmentsCount,
            VectorsCount = model.VectorsCount,
            IndexedVectorsCount = model.IndexedVectorsCount,
            PointsCount = model.PointsCount,
            InnerConfig = model.InnerConfig,
            BasicInfo = model.BasicInfo
        };
    }
}
