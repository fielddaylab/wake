using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using BeauMacro;
using UnityEngine;

public class MacroTest : MonoBehaviour, IMacroCompilerConfig
{
    #region Types

    [Serializable]
    private struct Entry
    {
        public string Key;
        public string Value;
    }

    #endregion // Types

    #region Inspector

    [SerializeField]
    private TextAsset m_Asset = null;

    [SerializeField]
    private string[] m_KnownMacros = null;

    [SerializeField]
    private CompiledMacroBank m_Bank = null;

    [SerializeField]
    private string m_Evaluate = null;

    #endregion // Inspector

    private MacroEnvironment m_Environment = new MacroEnvironment();

    [ContextMenu("Compile")]
    private void Compile()
    {
        MacroCompiler compiler = new MacroCompiler(this);
        SimpleMacroCompilerSource asset = new SimpleMacroCompilerSource(m_Asset.text);
        MacroCompilerResult result = compiler.Compile(asset);
        m_Bank = result.Bank;
        foreach (var unknown in result.UnrecognizedMacros)
            Debug.LogWarning("Unknown macro " + unknown);
        foreach (var unknown in result.UnrecognizedMacroIds)
            Debug.LogWarning("Unknown macro id " + unknown);
        foreach (var exported in result.MacroNames)
            Debug.Log("Exported macro " + exported);
        Debug.Log("Reused strings: " + result.ReusedStrings);
    }

    [ContextMenu("Evaluate")]
    private void Evaluate()
    {
        m_Environment.Banks.LocalizationGlobal = m_Bank;
        Debug.Log(m_Environment.Evaluate(m_Evaluate));
    }

    #region IMacroCompilerConfig

    void IMacroCompilerConfig.PreReplace(StringBuilder ioString) { }

    void IMacroCompilerConfig.PostReplace(StringBuilder ioString) { }

    IEnumerable<string> IMacroCompilerConfig.RecognizedMacros { get { return m_KnownMacros; } }

    IEnumerable<KeyValuePair<string, string>> IMacroCompilerConfig.SimpleReplace { get { return null; } }

    IEnumerable<KeyValuePair<string, string>> IMacroCompilerConfig.RegexReplace
    {
        get
        {
            yield return CompilerRules.RegexReplace.CurlyBraceMacros;
            yield return CompilerRules.RegexReplace.CustomTriad("C[", "|", "]", "PARTNER_GENDER_MALE", "<b><color=cyan>{0}</color></b>", "PARTNER_GENDER_FEMALE", "<b><color=magenta>{0}</color></b>", "<b><color=white>{0}</color></b>");
            yield return CompilerRules.RegexReplace.CustomTriad("[", "|", "]", "PLAYER_GENDER_MALE", "<b><color=cyan>{0}</color></b>", "PLAYER_GENDER_FEMALE", "<b><color=magenta>{0}</color></b>", "<b><color=white>{0}</color></b>");
        }
    }

    #endregion // IMacroCompilerConfig
}