using ProtoAqua.ExperimentV2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NavArrow : MonoBehaviour
{
    [SerializeField] private SelectableTank m_DestTank;
    [SerializeField] private Button m_Button;

    public Button Button {
        get { return m_Button; }
    }
    public SelectableTank DestTank {
        get { return m_DestTank; }
    }
}
