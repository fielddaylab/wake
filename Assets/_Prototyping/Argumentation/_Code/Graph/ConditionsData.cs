using System.Collections.Generic;
using UnityEngine;

namespace ProtoAqua.Argumentation
{
    public class ConditionsData
    {
        private HashSet<string> visitedNodes = new HashSet<string>();
        private HashSet<string> usedResponses = new HashSet<string>();

        // Constructor for marking root node as visited
        public ConditionsData(string id)
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
                foreach (string condition in link.RequiredVisited)
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
                foreach (string condition in link.RequiredUsed)
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
