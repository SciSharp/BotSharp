using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines.NERs
{
    /// <summary>
    /// Ontology encompasses a representation, formal naming, and definition of the categories, properties, and relations between the concepts, data, and entities that substantiate one, many, or all domains.
    /// </summary>
    public enum OntologyEnum
    {
        /// <summary>
        /// 6 miles
        /// </summary>
        Distance = 1,
        /// <summary>
        /// 3 mins
        /// </summary>
        Duration = 2,
        /// <summary>
        /// team@botsharp.com
        /// </summary>
        Email = 3,
        /// <summary>
        /// eighty eight
        /// </summary>
        Numeral = 4,
        /// <summary>
        /// 33rd
        /// </summary>
        Ordinal = 5,
        /// <summary>
        /// +1 (312) 292-6741
        /// </summary>
        PhoneNumber = 6,
        /// <summary>
        /// 3 cups of sugar
        /// </summary>
        Quantity = 7,
        /// <summary>
        /// 80F
        /// </summary>
        Temperature = 8,
        /// <summary>
        /// today at 9am
        /// </summary>
        DateTime = 9,
        /// <summary>
        /// https://github.com/Oceania2018/BotSharp
        /// </summary>
        Url = 10,
        /// <summary>
        /// 4 gallons
        /// </summary>
        Volume = 11,
        /// <summary>
        /// Chicago
        /// </summary>
        Location = 12
    }
}
