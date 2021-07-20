using Aqua;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.ExperimentV2
{
    public class EnvIconDisplay : MonoBehaviour
    {
        public Image Placeholder;
        public Image Icon;
        public LocText Label;
        public TextId EmptyText;

        static public void Populate(EnvIconDisplay inDisplay, BestiaryDesc inEnv)
        {
            if (inEnv)
            {
                inDisplay.Placeholder.gameObject.SetActive(false);
                inDisplay.Icon.sprite = inEnv.Icon();
                inDisplay.Icon.gameObject.SetActive(true);
                inDisplay.Label.SetText(inEnv.CommonName());
            }
            else
            {
                inDisplay.Placeholder.gameObject.SetActive(true);
                inDisplay.Icon.gameObject.SetActive(false);
                inDisplay.Label.SetText(inDisplay.EmptyText);
            }
        }
    }
}