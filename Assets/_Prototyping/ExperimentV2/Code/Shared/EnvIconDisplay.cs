using Aqua;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.ExperimentV2
{
    public class EnvIconDisplay : MonoBehaviour
    {
        public GameObject Placeholder;
        public Image Icon;
        public LocText Label;
        public TextId EmptyText;

        static public void Populate(EnvIconDisplay inDisplay, BestiaryDesc inEnv)
        {
            if (inEnv)
            {
                inDisplay.Placeholder.SetActive(false);
                inDisplay.Icon.sprite = inEnv.Icon();
                inDisplay.Icon.gameObject.SetActive(true);
                if (inDisplay.Label)
                {
                    inDisplay.Label.SetText(inEnv.CommonName());
                }
            }
            else
            {
                inDisplay.Placeholder.SetActive(true);
                inDisplay.Icon.gameObject.SetActive(false);
                if (inDisplay.Label)
                {
                    inDisplay.Label.SetText(inDisplay.EmptyText);
                }
            }
        }
    }
}