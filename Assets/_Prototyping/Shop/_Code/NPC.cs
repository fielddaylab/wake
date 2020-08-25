using System.Collections.Generic;
using BeauRoutine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Shop
{
    public class NPC : MonoBehaviour
    {
        [Header("NPC Dependencies")]
        [SerializeField] private TextMeshProUGUI NPCText;
        [SerializeField] private Button NPCButton;

        // Assign Interact method to NPCButton
        private void Awake()
        {
            NPCButton.onClick.AddListener(() => Interact());
        }

        // Change dialog displayed in speech bubble
        public void SetDialog(string text)
        {
            NPCText.SetText(text);
        }

        // Change dialog and animate when clicked
        public void Interact()
        {
            // Temporary until actual dialog is in place
            if (NPCText.text.Equals("Welcome!"))
            {
                NPCText.SetText("Hello!");
            }
            else
            {
                NPCText.SetText("Welcome!");
            }

            Routine.Start(this, ScaleIn());
            Routine.Start(this, ScaleOut());
        }

        // Squash and stretch inwards
        private IEnumerator<Tween> ScaleIn()
        {
            yield return this.transform.SquashStretchTo(new Vector3(0.5f, 0.5f, 0.5f), 0.25f, Axis.XY).Ease(Curve.CubeOut);
        }
        
        // Squash and stretch back to original scale
        private IEnumerator<Tween> ScaleOut()
        {
            yield return this.transform.SquashStretchTo(new Vector3(1.0f, 1.0f, 1.0f), 0.25f, Axis.XY).Ease(Curve.CubeOut);
        }
    }
}
