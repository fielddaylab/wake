# BeauMacro

**Current Version: 0.1.0**  
Updated 24 July 2019 | Changelog (Not yet available)

## About
BeauMacro is a string replacement and macro expansion library, intended to facilitate efficient contextual string formatting. It supports ahead-of-time compilation of conditionally-formatted strings into efficient instruction lists, as well as providing context at runtime for modifying string results.

### Table of Contents
1. [Syntax](#syntax)
	* [Basic Syntax](#basic-syntax)
	* [Conditional Syntax](#conditional-syntax)
	* [Optimized Syntax](#optimized-syntax)
2. [Evaluation](#evaluation)
	* [Macro Banks](#macro-banks)
	* [Macro Environment](#macro-environment)
	* [Runtime Banks](#runtime-banks)
	* [Compiled Banks](#compiled-banks)
	* [Default Source Format](#default-source-format)
	* [Customizing The Compiler](#customizing-the-compiler)
3. [Reference](#reference)
    * [MacroBank Members](#macrobank-members)
	* [RuntimeMacroBank Members](#runtimemacrobank-members)
	* [MacroEnvironment Members](#macroenvironment-members)
	* [MacroUtil Members](#macroutil-members)
	* [IMacroCompilerConfig Members](#imacrocompilerconfig-members)
	* [IMacroCompilerSource Members](#imacrocompilersource-members)
	* [MacroCompiler Members](#macrocompiler-members)
	* [MacroCompilerResult Members](#macrocompilerresult-members)
	* [CompilerRules Members](#compilerrules-members)
----------------

## Syntax

### Basic Syntax

BeauMacro represents string replacement with the syntax ``$(MACRO)``, where ``MACRO`` is the name of the macro to substitute.

```
Hello, $(PLAYER_NAME), and welcome to our kingdom!
```

### Conditional Syntax

BeauMacro also supports simple ``if``/``elif``/``else`` statements. These conditional blocks must be closed off with an ``endif``.

```
?<if MACRO>
	string to output
?<elif ANOTHER_MACRO>
	another string to output
?<else>
	fallback string
?<endif>
```

True/false is determined by an empty/non-empty string. So ``string.Empty`` is interpreted as false, any non-empty string, such as ``"1"``, ``"text"``, or ``"!"``, is interpreted as true.

```
This is $(PLAYER_NAME)!

?<if PLAYER_GENDER_MALE>
	He is heading to the castle!
?<elif PLAYER_GENDER_FEMALE>
	She is heading to the castle!
?<else>
	They are heading to the castle!
?<endif>
```

These conditional statements can also be negated with the ``not`` symbol.

```
?<if not CASTLE_DESTROYED>
?<elif not SWAMP_TRAVERSED>
```

### Optimized Syntax

If you know the id of a macro ahead of time, it can be represented in a more optimized form which bakes the id into the string.

``#(MACRO_ID)``, where ``MACRO_ID`` is an integer.

```
#(-993629262) is not a good place to go.
```

## Evaluation

### Macro Banks

Macros are stored in ``MacroBank`` objects. These provide context when evaluating macros.

### Macro Environment

Macros must be evaluated from a ``MacroEnvironment``. A MacroEnvironment provides space for a set of ``MacroBank`` objects, along with methods for evaluating a given macro key.

```csharp
MacroEnvironment env = new MacroEnvironment();
MacroBank localizationBank = [...];
env.Banks.LocalizationGlobal = localizationBank;

string stringToEvaluate = "$(UI/TitleScreen/Header)";
string evaluated = env.Evaluate(stringToEvaluate);
```

Keys must be provided as either ``$(MACRO_NAME)`` or ``#(MACRO_ID)``. Strings deviating from that format will not be evaluated.

Note: BeauMacro does not support just-in-time compilation. All compilation of macro strings must be done ahead-of-time. Evaluating a string like ``A non-compiled $(macro_name) string`` will not result in string expansion.

### Runtime Banks

Context can be constructed at runtime in the form of ``RuntimeMacroBank`` objects.

RuntimeMacroBanks are able to be modified at runtime, and support storing strings, macro keys, and bools. Strings provided to a RuntimeMacroBanks are not compiled.

```csharp
MacroEnvironment env = new MacroEnvironment();
RuntimeMacroBank runtimeBank = new RuntimeMacroBank();
runtimeBank.Set("NAME", "Alice");
env.Banks.Variables = runtimeBank;

string stringToEvaluate = "$(NAME)";
string evaluated = env.Evaluate(stringToEvaluate); // returns "Alice"

RuntimeMacroBank overrideBank = new RuntimeMacroBank();
overrideBank.Set("NAME", "Bob");

evaluated = env.Evaluate(stringToEvaluate, overrideBank); // returns "Bob"
```

### Compiled Banks

Context can also be constructed ahead-of-time in the form of ``CompiledMacroBank`` objects. These are compiled and optimized sets of instructions for string construction and replacement.

```csharp
MacroCompiler compiler = new MacroCompiler();
string sourceContent = "[...]";
IMacroCompilerSource source = new SimpleMacroCompilerSource(sourceContent);
MacroCompilerResult result = compiler.Compile(source);
CompiledMacroBank bank = result.Bank;
```

A CompiledMacroBank is serializable, and can be stored in any serialized Unity object.

It's suggested that you compile these banks at edit time or build time, rather than runtime.

### Default Source Format

By default, BeauMacro provides a simple format for key-value pairs for compilation into a CompiledStringBank. This can be consumed with the ``SimpleMacroCompilerSource`` source type.

```
KEY=Some value
ANOTHER_KEY= Whitespace is ignored at the start of the value.
// This line is a comment and is ignored
 WHITESPACE_IS_IGNORED_AT_START_AND_END_OF_KEY =Another value
```

### Customizing The Compiler

The MacroCompiler can be configured with an implementation of the ``IMacroCompilerConfig`` interface. This configuration object is passed into the MacroCompiler constructor. This can allow you to define your own syntax for your source and transform it into the appropriate syntax for the MacroCompiler.

```csharp
public class CustomCompilerConfig : IMacroCompilerConfig
{
    // This is a compiler config for dealing with specific formatting variations.
    // Specifically, this allows the following rules:
    //      - Macros can be specified as {MACRO} instead of $(MACRO)
    //      - Text variations specific to certain genders are represented as [male_variant|female_variant|nb_variant]

    public void PreReplace(StringBuilder ioString)
    {
        // This is called at the start of macro compilation
    }

    public IEnumerable<KeyValuePair<string, string>> SimpleReplace
    {
        get
        {
            // This should define key-value pairs
            // that represent simple find-replace rules
            // (not regular expressions)
            yield return CompilerRules.Rule("\\n", "\n");
        }
    }

    public IEnumerable<KeyValuePair<string, string>> RegexReplace
    {
        get
        {
            // This should define key-value pairs
            // that represent regular expression find-replace rules

            // This one replaces instances of [MACRO] with $(MACRO)
            yield return CompilerRules.RegexReplace.CurlyBraceMacros;

            // This will replace instances of [textA|textB|textC]
            // with ?if(PLAYER_MALE)textA?(elif PLAYER_FEMALE)textB?(else)textC?(endif)
            yield return CompilerRules.RegexReplace.BracketTriad("PLAYER_MALE", "PLAYER_FEMALE");
        }
    }

    public void PostReplace(StringBuilder ioString)
    {
        // This is called after all other replacement rules have been evaluated
    }

    public IEnumerable<string> RecognizedMacros
    {
        get
        {
            // This should define a set of recognized macros
            // that may not be defined in the macros being compiled.
            // Any macros that are programmatically set should be listed here.
            yield return "PLAYER_MALE";
            yield return "PLAYER_FEMALE";
        }
    }
}

[...]

MacroCompiler compiler = new MacroCompiler(new CustomCompilerConfig());
```

See [IMacroCompilerConfig Members](#imacrocompilerconfig-members), [IMacroCompilerSource Members](#imacrocompilersource-members), [MacroCompiler Members](#macrocompiler-members), [MacroCompilerResult Members](#macrocompilerresult-members), and [CompilerRules Members](#compilerrules-members) for reference on configuration options.

## Reference

### MacroBank Members

| Member | Type | Description |
| - | - | - |
| ``Contains(String key)`` | Method | Returns if the given macro exists in the bank. |
| ``Contains(int key)`` | Method | Returns if the given macro id exists in the bank. |

### RuntimeMacroBank Members

| Member | Type | Description |
| - | - | - |
| ``Clear()`` | Method | Clears all macros from the bank. |
| ``CopyTo(RuntimeMacroBank bank)`` | Method | Copies all macros to the given bank. |
| ``Set(String key, string value)`` | Method | Sets the given macro to the given string. |
| ``Set(int key, string value)`` | Method | Sets the given macro id to the given string. |
| ``Set(String key, bool value)`` | Method | Sets the given macro to the given bool. |
| ``Set(int key, bool value)`` | Method | Sets the given macro id to the given bool. |
| ``Remove(String key)`` | Method | Removes the given macro from the bank. |
| ``Remove(int key)`` | Method | Removes the given macro id from the bank. |

### MacroEnvironment Members

| Member | Type | Description |
| - | - | - |
| ``Banks`` | Property | Set of MacroBanks for context when evaluating macros. |
| ``TryEvaluate(ref String string, MacroBank context = null)`` | Method | Attempts to evaluate and replace the given string, with an optional MacroBank context override. |
| ``TryEvaluate(int macroId, out string value, MacroBank context = null)`` | Method | Attempts to evaluate the macro with the given id, with the given id with an optional MacroBank context override and outputs the result. |
| ``Evaluate(String string, MacroBank context = null)`` | Method | Evaluates the given string, with an optional MacroBank context override. |
| ``Evaluate(int macroId, MacroBank context = null)`` | Method | Evaluates the macro with the given id, with an optional MacroBank context override. |

### MacroUtil Members

| Member | Type | Description |
| - | - | - |
| ``Hash(String string)`` | Method | Returns the hash value of the given string. |
| ``IsMacro(ref String key, out int hash)`` | Method | Determines if the given string is a valid macro reference. If so, it replaces the string with the macro name and outputs the hash value. |
| ``Optimize(ref String key)`` | Method | If the given string is a macro, replaces the string with the most optimized form of the macro reference. |

### IMacroCompilerConfig Members

| Member | Type | Description |
| - | - | - |
| ``RecognizedMacros`` | ``IEnumerable<String>`` | Collection of pre-recognized macro names. |
| ``PreReplace(StringBuilder string)`` | Method | Method to execute before replace rules are executed. |
| ``SimpleReplace`` | ``IEnumerable<KeyValuePair<String, String>>`` | Collection of simple find-replace rules. |
| ``RegexReplace`` | ``IEnumerable<KeyValuePair<String, String>>`` | Collection of regular expression find-replace rules. |
| ``PostReplace(StringBuilder string)`` | Method | Method to execute after replace rules are executed. |

### IMacroCompilerSource Members

| Member | Type | Description |
| - | - | - |
| ``Entries`` | ``IEnumerable<KeyValuePair<String, String>>`` | Collection of key-value entries to compile. |

### MacroCompiler Members

| Member | Type | Description |
| - | - | - |
| ``Compile(IMacroCompilerSource source, ICollection<MacroBank> links = null)`` | Method | Compiles the given source into a MacroBank. |
| ``CompileAsync(IMacroCompilerSource source, ICollection<MacroBank> links, MacroCompilerResult target)`` | Method | Asynchrounously compiles the given source to the target result. Each call to the returned IEnumerator's MoveNext will compile one macro entry. |

### MacroCompilerResult Members

| Member | Type | Description |
| - | - | - |
| ``Bank`` | ``CompiledMacroBank`` | Compiled macro bank. |
| ``UnrecognizedMacros`` | ``ICollection<String>`` | Collection of unrecognized macro names. |
| ``UnrecognizedMacroIds`` | ``ICollection<int>`` | Collection of unrecognized macro ids. |
| ``MacroNames`` | ``ICollection<String>`` | Collection of all compiled macro names. |
| ``MacroCount`` | ``int`` | Number of compiled macros. |
| ``StringCount`` | ``int`` | Number of strings in the bank's string table. |
| ``ReusedStrings`` | ``int`` | Number of times a string was reused during compilation. |

### CompilerRules Members

| Member | Type | Description |
| - | - | - |
| ``Rule(String find, String replace)`` | Method | Returns a key-value pair for the given find-replace rule. |
| ``RegexReplace.CurlyBraceMacros`` | ``KeyValuePair<string, string>`` | Regex for replacing curly brace enclosures with macro enclosures. |
| ``RegexReplace.BracketMacros`` | ``KeyValuePair<string, string>`` | Regex for replacing square bracket enclosures with macro enclosures. |
| ``RegexReplace.CustomMacros(String open, String close)`` | Method | Returns a key-value pair for replacing custom enclosures with macro enclosures. |
| ``RegexReplace.BracketToggle(String variableA)`` | Method | Returns a key-value pair for replacing a ``[A|B]`` pattern with a branch based on variableA. |
| ``RegexReplace.BracketTriad(String variableA, String variableB)`` | Method | Returns a key-value pair for replacing a ``[A|B|C]`` pattern with a branch based on variableA and variableB. |
| ``RegexReplace.PrefixedBracketToggle(String prefix, String variableA)`` | Method | Returns a key-value pair for replacing a ``PREFIX [A|B]`` pattern with a branch based on variableA. |
| ``RegexReplace.PrefixedBracketTriad(String prefix, String variableA, String variableB)`` | Method | Returns a key-value pair for replacing a ``PREFIX [A|B|C]`` pattern with a branch based on variableA and variableB. |
| ``RegexReplace.CustomToggle(String open, String separator, String close, String variableA, String formatA, String formatB)`` | Method | Returns a key-value pair for replacing an enclosed toggle group with a branch based on variableA. |
| ``RegexReplace.CustomTriad(String open, String separator, String close, String variableA, String formatA, String variableB, String formatB, String formatC)`` | Method | Returns a key-value pair for replacing an enclosed toggle group with a branch based on variableA and variableB. |
| ``RegexReplace.AnyNonWhitespaceCaptureGroup`` | ``String`` | Regex for capturing a sequence of non-whitespace characters. |
| ``RegexReplace.AnyCaptureGroup`` | ``String`` | Regex for capturing a sequence of characters. |
| ``RegexReplace.Escape(String string)`` | Method | Returns an escaped string used for matching the given sequence in RegexReplace. |