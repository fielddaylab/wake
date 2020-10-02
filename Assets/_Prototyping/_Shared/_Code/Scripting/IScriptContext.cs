using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using TMPro;
using System.Collections;
using System;
using ProtoAudio;
using BeauUtil.Tags;
using BeauUtil.Variants;

namespace ProtoAqua.Scripting
{
    public interface IScriptContext
    {
        ScriptObject Object { get; }
        VariantTable Vars { get; }
    }
}