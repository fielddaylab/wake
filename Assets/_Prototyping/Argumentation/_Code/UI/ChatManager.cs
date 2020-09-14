using TMPro;
using UnityEngine;

namespace ProtoAqua.Argumentation 
{
    [RequireComponent(typeof(DropSlot))]
    public class ChatManager : MonoBehaviour 
    {
        [Header("Chat Manager Dependencies")]
        [SerializeField] Graph graph = null;
        [SerializeField] Transform chatGrid = null;
        [SerializeField] GameObject nodePrefab = null;
        [SerializeField] private LinkManager linkManager;
        [SerializeField] private PopupPanel m_EndPopup;
        
        private DropSlot dropSlot;
        
        // Start is called before the first frame update
        void Start()
        {
            //Set up the listener for Drop Slot
            dropSlot = GetComponent<DropSlot>();
            dropSlot.OnDropped.AddListener(OnDrop);

            //Spawn some starting nodes
            GameObject newNode = Instantiate(nodePrefab, chatGrid);
            newNode.GetComponent<ChatBubble>().bubbleType = BubbleType.Node;
            newNode.transform.Find("NodeText").GetComponent<TextMeshProUGUI>().SetText("Hello, My name is Kevin. INSERT MESSAGE THAT IS A QUESTION??!?? beep boop bop");
        }

        //Activates when an item is dropped (called from DropSlot.cs)
        void OnDrop(GameObject droppedItem) 
        {

            //Make sure the object is draggable (This should never occur that its not)
            if(droppedItem.GetComponent<DraggableObject>() == null ) 
            {
                return;
            }

            droppedItem.transform.SetParent(chatGrid); //Set it into the grid 
            droppedItem.GetComponent<DraggableObject>().enabled = false; //Make it no longer able to be dragged
            
            string linkId = droppedItem.GetComponent<ChatBubble>().id;
            RespondWithNextNode(linkId); //TODO make sure has this component

            // Add response back into list for reuse
            linkManager.ResetLink(droppedItem, linkId);
        }

        //Rename, bad naming
        //Add functionality to respond with more nodes, etc. This is where the NPC "talks back"
        void RespondWithNextNode(string factId) 
        {
            Node nextNode = graph.NextNode(factId); //Get the next node for the factId

            if (nextNode.Id.Equals(graph.EndNodeId))
            {
                EndConversationPopup();
            }
            else
            {
                //Create the node bubble, and set its properties
                GameObject newNode = Instantiate(nodePrefab, chatGrid);
                newNode.GetComponent<ChatBubble>().bubbleType = BubbleType.Node;
                newNode.GetComponent<ChatBubble>().id = nextNode.Id;
                newNode.transform.Find("NodeText").GetComponent<TextMeshProUGUI>().SetText(nextNode.DisplayText);
            }
        }
    
        private void EndConversationPopup()
        {
            NamedOption[] options = {new NamedOption("Continue")};
            Services.UI.Popup().Present("Congratulations!", "End of conversation", options);
        }
    }
}
