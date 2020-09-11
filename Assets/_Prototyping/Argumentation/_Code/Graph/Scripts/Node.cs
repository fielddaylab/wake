using System.Collections.Generic;

namespace ProtoAqua.Argumentation
{
    public class Node
    {
        private string id;
        private bool rootNode;
        private string displayText;
        private string invalidFactNodeId;
        private string defaultNextNodeId;
        private List<string> responses = new List<string>();
        private bool visited;

        public Node(string inId, bool inRootNode, string inDisplayText, string inInvalidFactNodeId, 
                    string inDefaultNextNodeId, string inResponses)
        {
            id = inId;
            rootNode = inRootNode;
            displayText = inDisplayText;
            invalidFactNodeId = inInvalidFactNodeId;
            defaultNextNodeId = inDefaultNextNodeId;
            
            if (inResponses != null)
            {
                ParseResponses(inResponses);
            }
        }
        
        #region Accessors

        public string Id
        {
            get { return id; }
        }

        public bool RootNode
        {
            get { return rootNode; }
        }

        public string DisplayText
        {
            get { return displayText; }
        }

        public string InvalidFactNodeId
        {
            get { return invalidFactNodeId; }
        }

        public string DefaultNextNodeId
        {
            get { return defaultNextNodeId; }
        }

        public List<string> Responses
        {
            get { return responses; }
        }

        public bool Visited
        {
            get { return visited; }
            set { visited = value; }
        }

        #endregion // Accessors

        private void ParseResponses(string inResponses)
        {
            string[] splitResponses = inResponses.Split(',');

            foreach (string response in splitResponses)
            {
                responses.Add(response.Trim());
            }
        }

        // Checks if a given link id is a valid response to this node
        public bool CheckResponse(string id)
        {
            foreach (string response in responses)
            {
                if (response.Equals(id))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
