using BeauUtil;
using UnityEngine;

namespace Aqua
{
    public struct BestiaryFactFragment
    {
        public BestiaryFactFragmentType Type;
        public StringSlice String;

        static public BestiaryFactFragment CreateWord(BestiaryFactFragmentType inType, StringSlice inWord)
        {
            return new BestiaryFactFragment()
            {
                Type = inType,
                String = inWord
            };
        }

        static public BestiaryFactFragment CreateNoun(StringSlice inWord)
        {
            return new BestiaryFactFragment()
            {
                Type = BestiaryFactFragmentType.Noun,
                String = inWord
            };
        }

        static public BestiaryFactFragment CreateVerb(StringSlice inWord)
        {
            return new BestiaryFactFragment()
            {
                Type = BestiaryFactFragmentType.Verb,
                String = inWord
            };
        }

        static public BestiaryFactFragment CreateAdjective(StringSlice inWord)
        {
            return new BestiaryFactFragment()
            {
                Type = BestiaryFactFragmentType.Adjective,
                String = inWord
            };
        }

        static public BestiaryFactFragment CreateAmount(float inAmount)
        {
            return new BestiaryFactFragment()
            {
                Type = BestiaryFactFragmentType.Amount,
                String = inAmount.ToString()
            };
        }

        static public BestiaryFactFragment CreateAmount(StringSlice inWord)
        {
            return new BestiaryFactFragment()
            {
                Type = BestiaryFactFragmentType.Amount,
                String = inWord
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
}