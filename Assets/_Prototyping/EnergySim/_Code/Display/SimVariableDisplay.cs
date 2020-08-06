using System.Runtime.InteropServices;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using BeauPools;
using BeauRoutine;
using BeauUtil;

namespace ProtoAqua.Energy
{
    public class SimVariableDisplay : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private TMP_Text m_Header = null;
        [SerializeField] private TMP_Text m_Value = null;
        [SerializeField] private Image m_DifferenceIcon = null;

        [SerializeField] private Transform m_SecondDivider = null;
        [SerializeField] private TMP_Text m_SecondValue = null;
        [SerializeField] private Image m_SecondDifferenceIcon = null;

        #endregion // Inspector

        #region Unity Events

        #endregion // Unity Events

        public void Display(string inHeader, string inValue, float inDifference, string inSecondValue = null, float inSecondDifference = 0)
        {
            m_Header.SetText(inHeader);
            m_Value.SetText(inValue);

            EnergyConfig config = Services.Tweaks.Get<EnergyConfig>();
            EnergyConfig.ColorSpritePair colorSettings = config.GetLabelSettings(inDifference);

            m_Value.color = colorSettings.Color;
            m_DifferenceIcon.color = colorSettings.Color;
            m_DifferenceIcon.sprite = colorSettings.Icon;
            m_DifferenceIcon.gameObject.SetActive(colorSettings.Icon != null);

            if (inDifference < 0)
            {
                m_DifferenceIcon.transform.SetRotation(90, Axis.Z, Space.Self);
            }
            else if (inDifference > 0)
            {
                m_DifferenceIcon.transform.SetRotation(-90, Axis.Z, Space.Self);
            }
            else
            {
                m_DifferenceIcon.transform.SetRotation(0, Axis.Z, Space.Self);
            }

            if (string.IsNullOrEmpty(inSecondValue))
            {
                m_SecondValue.gameObject.SetActive(false);
                m_SecondDifferenceIcon.gameObject.SetActive(false);
                m_SecondDivider.gameObject.SetActive(false);
            }
            else
            {
                colorSettings = config.GetLabelSettings(inSecondDifference);

                m_SecondDivider.gameObject.SetActive(true);
                m_SecondValue.gameObject.SetActive(true);
                m_SecondValue.SetText(inSecondValue);

                m_SecondValue.color = colorSettings.Color;
                m_SecondDifferenceIcon.color = colorSettings.Color;
                m_SecondDifferenceIcon.sprite = colorSettings.Icon;
                m_SecondDifferenceIcon.gameObject.SetActive(colorSettings.Icon != null);

                if (inSecondDifference < 0)
                {
                    m_SecondDifferenceIcon.transform.SetRotation(90, Axis.Z, Space.Self);
                }
                else if (inSecondDifference > 0)
                {
                    m_SecondDifferenceIcon.transform.SetRotation(-90, Axis.Z, Space.Self);
                }
                else
                {
                    m_SecondDifferenceIcon.transform.SetRotation(0, Axis.Z, Space.Self);
                }
            }
        }
    }
}