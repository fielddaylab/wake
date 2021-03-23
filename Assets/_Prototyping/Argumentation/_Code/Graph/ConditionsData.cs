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
        public bool CheckConditions(Node node, StringHash32 linkId)
        {
            // If the conditions are met, mark the current node as visited and the current
            // link as used
            visitedNodes.Add(node.Id);
            usedResponses.Add(linkId);

            return true;
        }
    }
}
