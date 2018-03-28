using BotSharp.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Models
{
    public class RequestExtras
    {
        public List<AIContext> Contexts { get; set; }

        public List<Entity> Entities { get; set; }

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

        public RequestExtras(List<AIContext> contexts, List<Entity> entities)
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
