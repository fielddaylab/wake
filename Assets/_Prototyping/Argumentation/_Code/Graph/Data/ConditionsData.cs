using System.Collections.Generic;
using UnityEngine;

namespace ProtoAqua.Argumentation
{
    public class ConditionsData
    {
        private Dictionary<string, Node> visitedNodes = new Dictionary<string, Node>();
        private Dictionary<string, Link> usedResponses = new Dictionary<string, Link>();

        // Constructor for marking root node as visited
        public ConditionsData(Node node)
        {
            MarkVisited(node);
        }

        // Helper method for marking a node as visited and adding to visited dictionary
        private void MarkVisited(Node node)
        {
            if (!visitedNodes.TryGetValue(node.Id, out Node visited))
            {
                node.Visited = true;
                visitedNodes.Add(node.Id, node);
            }
        }

        // Helper method for marking a link as used and adding to used dictionary
        private void MarkUsed(Link link)
        {
            if (!usedResponses.TryGetValue(link.Id, out Link used))
            {
                link.Used = true;
                usedResponses.Add(link.Id, link);
            }
        }

        // Helper method for checking if a node is visited given its id
        private bool CheckVisited(string id)
        {
            if (visitedNodes.TryGetValue(id, out Node visited))
            {
                return true;
            }

            return false;
        }

        // Helper method for checking if a link is used given its id
        private bool CheckUsed(string id)
        {
            if (usedResponses.TryGetValue(id, out Link used))
            {
                return true;
            }

            return false;
        }

        // Given a link, traverse its requiredVisited and requiredUsed arrays and
        // check if every specified node or link has been marked visited or used.
        public bool CheckConditions(Node node, Link link)
        {
            foreach (string condition in link.RequiredVisited)
            {
                if (!CheckVisited(condition))
                {
                    Debug.Log("Visit requirements not met!");
                    return false;
                }
            }

            foreach (string condition in link.RequiredUsed)
            {
                if (!CheckUsed(condition))
                {
                    Debug.Log("Use requirements not met!");
                    return false;
                }
            }

            // If the conditions are met, mark the current node as visited and the current
            // link as used
            Debug.Log("Requirements met!");
            MarkVisited(node);
            MarkUsed(link);

            return true;
        }
    }
}
