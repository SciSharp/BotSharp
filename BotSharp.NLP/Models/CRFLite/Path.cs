using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Models.CRFLite
{
    public class Path
    {
        public int fid;
        public Node rnode;
        public Node lnode;
        public double cost;

        public Path()
        {
            rnode = null;
            lnode = null;
            cost = 0;
        }

        public void add(Node _lnode, Node _rnode)
        {
            lnode = _lnode;
            rnode = _rnode;

            lnode.rpathList.Add(this);
            rnode.lpathList.Add(this);
        }
    }
}
