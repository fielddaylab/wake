using System;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauUtil;
using ProtoAqua;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProtoAqua
{
    public interface IInputLayer
    {
        int Priority { get; }
        InputLayerFlags Flags { get; }
        bool? Override { get; set; }

        bool IsInputEnabled { get; }

        void UpdateSystemPriority(int inSystemPriority);
        void UpdateSystemFlags(InputLayerFlags inFlags);

        UnityEvent OnInputEnabled { get; }
        UnityEvent OnInputDisabled { get; }
    }

    [Flags]
    public enum InputLayerFlags : UInt32
    {
        PlayerControls = 0x01,

        AllWorld = PlayerControls,
        
        WorldUI = 0x100,
        GameUI = 0x200,
        TutorialUI = 0x400,

        AllUI = WorldUI | GameUI | TutorialUI,

        System = 0x100000,
        SystemError = 0x20000,

        AllSystem = System | SystemError,

        All = AllWorld | AllUI | AllSystem
    }
}