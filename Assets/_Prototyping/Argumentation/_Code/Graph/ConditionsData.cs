using System.Collections.Generic;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Argumentation
{
    public class ConditionsData
    {
        private HashSet<StringHash32> visitedNodes = new HashSet<StringHash32>();
        private HashSet<StringHash32> usedResponses = new HashSet<StringHash32>();

        // Constructor for marking root node as visited
        public ConditionsData(StringHash32 id)
        {
            visitedNodes.Add(id);
        }

        // Given a link, traverse its requiredVisited and requiredUsed arrays and
        // check if every specified node or link has been marked visited or used.
        public bool CheckConditions(Node node, Link link)
        {
            //return true; //@TODO fix this
            if (link.RequiredVisited != null)
            {
                foreach (StringHash32 condition in link.RequiredVisited)
                {
                    if (!visitedNodes.Contains(condition))
                    {
                        Debug.Log("Visit requirements not met!");
                        return false;
                    }
                }
            }

            if (link.RequiredVisited != null)
            {
                foreach (StringHash32 condition in link.RequiredUsed)
                {
                    if (!usedResponses.Contains(condition))
                    {
                        return false;
                    }
                }
            }

            // If the conditions are met, mark the current node as visited and the current
            // link as used
            visitedNodes.Add(node.Id);
            usedResponses.Add(link.Id);

            return true;
        }
    }
}
