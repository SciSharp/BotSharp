using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Plugin.FuzzySharp.Repository
{
    public class VocabularyRepository : IVocabularyRepository
    {
        public Task<Dictionary<string, HashSet<string>>> FetchTableColumnValuesAsync()
        {
            throw new NotImplementedException();
        }
    }
}
