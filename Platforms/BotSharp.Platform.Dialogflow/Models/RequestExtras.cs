using BotSharp.Platform.Models.Contexts;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Dialogflow.Models
{
    public class RequestExtras
    {
        public List<AIContext> Contexts { get; set; }

        public List<EntityType> Entities { get; set; }

        public bool HasContexts
        {
            get
            {
                if (Contexts != null && Contexts.Count > 0)
                {
                    return true;
                }
                return false;
            }
        }

        public bool HasEntities
        {
            get
            {
                if (Entities != null && Entities.Count > 0)
                {
                    return true;
                }
                return false;
            }
        }


        public RequestExtras()
        {
        }

        public RequestExtras(List<AIContext> contexts, List<EntityType> entities)
        {
            this.Contexts = contexts;
            this.Entities = entities;
        }

        public void CopyTo(AIRequest request)
        {
            if (HasContexts)
            {
                request.Contexts = Contexts;
            }

            if (HasEntities)
            {
                request.Entities = Entities;
            }
        }

    }
}
