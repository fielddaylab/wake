using BeauUtil;
using UnityEngine;

namespace Aqua
{
    public struct BestiaryFactFragment
    {
        public BestiaryFactFragmentType Type;
        public BestiaryFactFragmentWord Word;
        public StringSlice String;

        static public BestiaryFactFragment CreateWord(BestiaryFactFragmentType inType, StringSlice inWord)
        {
            return new BestiaryFactFragment()
            {
                Type = inType,
                Word = BestiaryFactFragmentWord.String,
                String = inWord
            };
        }

        static public BestiaryFactFragment CreateNoun(StringSlice inWord)
        {
            return new BestiaryFactFragment()
            {
                Type = BestiaryFactFragmentType.Noun,
                Word = BestiaryFactFragmentWord.String,
                String = inWord
            };
        }

        static public BestiaryFactFragment CreateVerb(StringSlice inWord)
        {
            return new BestiaryFactFragment()
            {
                Type = BestiaryFactFragmentType.Verb,
                Word = BestiaryFactFragmentWord.String,
                String = inWord
            };
        }

        static public BestiaryFactFragment CreateSubjectVariant()
        {
            return new BestiaryFactFragment()
            {
                Type = BestiaryFactFragmentType.Adjective,
                Word = BestiaryFactFragmentWord.SubjectVariant
            };
        }

        static public BestiaryFactFragment CreateTargetVariant()
        {
            return new BestiaryFactFragment()
            {
                Type = BestiaryFactFragmentType.Adjective,
                Word = BestiaryFactFragmentWord.TargetVariant
            };
        }

        static public BestiaryFactFragment CreateAmount()
        {
            return new BestiaryFactFragment()
            {
                Type = BestiaryFactFragmentType.Amount,
                Word = BestiaryFactFragmentWord.Amount
            };
        }
    }

    public enum BestiaryFactFragmentType : byte
    {
        Noun,
        Verb,
        Adjective,
        Amount,
        Conjunction,
        Condition,
    }

    public enum BestiaryFactFragmentWord : byte
    {
        String,
        Amount,
        SubjectVariant,
        TargetVariant,
        ConditionQuality,
        ConditionOperator,
        ConditionOperand
    }
}