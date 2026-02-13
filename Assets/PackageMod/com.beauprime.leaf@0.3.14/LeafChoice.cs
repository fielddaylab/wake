/*
 * Copyright (C) 2020. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    24 Oct 2020
 * 
 * File:    LeafChoice.cs
 * Purpose: Set of options to display, linked to different nodes.
 */

#if CSHARP_7_3_OR_NEWER
#define EXPANDED_REFS
#endif // CSHARP_7_3_OR_NEWER

using System;
using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Variants;

namespace Leaf
{
    /// <summary>
    /// A set of options to display.
    /// </summary>
    public class LeafChoice : IReadOnlyList<LeafChoice.Option>
    {
        public delegate bool OptionPredicate(LeafChoice inChoice, Option inOption);

        /// <summary>
        /// Single option within a choice.
        /// </summary>
        public struct Option
        {
            public int Index { get { return m_Index; } }

            public readonly Variant TargetId;
            public readonly StringHash32 LineCode;
            public OptionFlags Flags;

            public bool IsAvailable { get { return (Flags & OptionFlags.IsAvailable) != 0; } }

            internal int m_Index;

            internal ushort m_AnswersOffset;
            internal ushort m_AnswersLength;
            internal Variant m_DefaultAnswerTarget;

            internal ushort m_DataOffset;
            internal ushort m_DataLength;

            public Option(Variant inTargetId, StringHash32 inLineCode, OptionFlags inFlags = OptionFlags.IsAvailable)
            {
                TargetId = inTargetId;
                LineCode = inLineCode;
                Flags = inFlags;

                m_Index = 0;

                m_AnswersOffset = 0;
                m_AnswersLength = 0;
                m_DefaultAnswerTarget = Variant.Null;

                m_DataOffset = 0;
                m_DataLength = 0;
            }
        }

        /// <summary>
        /// Answer selector within a special option.
        /// </summary>
        public struct Answer
        {
            public readonly Variant AnswerId;
            public readonly Variant TargetId;

            public Answer(Variant inAnswerId, Variant inTargetId)
            {
                AnswerId = inAnswerId;
                TargetId = inTargetId;
            }
        }

        /// <summary>
        /// Custom property on an option.
        /// </summary>
        public struct Datum
        {
            public readonly StringHash32 Id;
            public readonly Variant Value;

            public Datum(StringHash32 inId, Variant inValue)
            {
                Id = inId;
                Value = inValue;
            }
        }

        /// <summary>
        /// Flags describing intended option behavior.
        /// </summary>
        [Flags]
        public enum OptionFlags : byte
        {
            IsAvailable = 0x01,
            IsSelector = 0x02,
            HasData = 0x04
        }

        private enum State
        {
            Accumulating,
            Choosing,
            Chosen
        }

        private readonly RingBuffer<Option> m_AllOptions = new RingBuffer<Option>(4, RingBufferMode.Expand);
        private readonly RingBuffer<Answer> m_AllAnswers = new RingBuffer<Answer>(4, RingBufferMode.Expand);
        private readonly RingBuffer<Datum> m_AllData = new RingBuffer<Datum>(4, RingBufferMode.Expand);
        private State m_State = State.Accumulating;
        private Variant m_ChosenOption;
        private int m_ChosenIndex;

        #region IReadOnlyList

        public int Count { get { return m_AllOptions.Count; } }

        public int AvailableCount { get; private set; }

        public Option this[int index]
        {
            get { return m_AllOptions[index]; }
        }

        public IEnumerable<Option> AvailableOptions()
        {
            for(int i = 0; i < m_AllOptions.Count; ++i)
            {
                Option op = m_AllOptions[i];
                if (op.IsAvailable)
                    yield return op;
            }
        }

        public IEnumerable<Option> AvailableOptions(OptionPredicate inPredicate)
        {
            for(int i = 0; i < m_AllOptions.Count; ++i)
            {
                Option op = m_AllOptions[i];
                if (op.IsAvailable && inPredicate(this, op))
                    yield return op;
            }
        }

        public int AvailableOptionCount(OptionPredicate inPredicate)
        {
            int counter = 0;
            for(int i = 0; i < m_AllOptions.Count; ++i)
            {
                Option op = m_AllOptions[i];
                if (op.IsAvailable && inPredicate(this, op))
                    counter++;
            }
            return counter;
        }

        public IEnumerator<Option> GetEnumerator()
        {
            return m_AllOptions.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion // IReadOnlyList

        #region Add

        /// <summary>
        /// Adds an option to the choice.
        /// </summary>
        public void AddOption(Option inOption)
        {
            if (m_State != State.Accumulating)
                throw new InvalidOperationException(string.Format("Cannot add options while in {0} state", m_State));
            inOption.m_Index = m_AllOptions.Count;
            inOption.m_AnswersOffset = (ushort) m_AllAnswers.Count;
            inOption.m_DataOffset = (ushort) m_AllData.Count;
            m_AllOptions.PushBack(inOption);
            if (inOption.IsAvailable)
            {
                ++AvailableCount;
            }
        }

        /// <summary>
        /// Adds an answer to the last option.
        /// </summary>
        public void AddAnswer(Answer inAnswer)
        {
            if (m_State != State.Accumulating)
                throw new InvalidOperationException(string.Format("Cannot add answers while in {0} state", m_State));
            if (m_AllOptions.Count == 0)
                throw new InvalidOperationException("Cannot add answers when no choices have been added");

            Variant answerId = inAnswer.AnswerId;

            // increment previous choice answer count
            #if EXPANDED_REFS
            ref Option option = ref m_AllOptions[m_AllOptions.Count - 1];
            #else
            Option option = m_AllOptions[m_AllOptions.Count - 1];
            #endif // EXPANDED_REFS

            option.Flags |= OptionFlags.IsSelector;

            if (answerId.IsNull() || answerId.AsStringHash().IsEmpty)
            {
                option.m_DefaultAnswerTarget = inAnswer.TargetId;
            }
            else
            {
                m_AllAnswers.PushBack(inAnswer);
                option.m_AnswersLength++;
            }
            
            #if !EXPANDED_REFS
            m_AllOptions[m_AllOptions.Count - 1] = option;
            #endif // !EXPANDED_REFS
        }

        /// <summary>
        /// Adds custom data to the last option.
        /// </summary>
        public void AddData(Datum inDatum)
        {
            if (m_State != State.Accumulating)
                throw new InvalidOperationException(string.Format("Cannot add answers while in {0} state", m_State));
            if (m_AllOptions.Count == 0)
                throw new InvalidOperationException("Cannot add answers when no choices have been added");

            #if EXPANDED_REFS
            ref Option option = ref m_AllOptions[m_AllOptions.Count - 1];
            #else
            Option option = m_AllOptions[m_AllOptions.Count - 1];
            #endif // EXPANDED_REFS

            option.Flags |= OptionFlags.HasData;

            m_AllData.PushBack(inDatum);
            option.m_DataLength++;
            
            #if !EXPANDED_REFS
            m_AllOptions[m_AllOptions.Count - 1] = option;
            #endif // !EXPANDED_REFS
        }

        #endregion // Add

        #region Query

        /// <summary>
        /// Returns if an option with the given target exists.
        /// </summary>
        public bool HasOption(Variant inTargetId)
        {
            return IndexOf(inTargetId) >= 0;
        }

        /// <summary>
        /// Returns if an answer with the given target id and answer id exists.
        /// </summary>
        public bool HasAnswer(Variant inTargetId, Variant inAnswerId)
        {
            return HasAnswer(IndexOf(inTargetId), inAnswerId);
        }

        /// <summary>
        /// Returns if an answer with the given index and answer id exists.
        /// </summary>
        public bool HasAnswer(int inIndex, Variant inAnswerId)
        {
            if (inIndex < 0)
                return false;
            
            bool bDefault;
            GetAnswerResponse(inIndex, inAnswerId, out bDefault);
            return !bDefault;
        }

        /// <summary>
        /// Returns the custom data associated with the given target.
        /// </summary>
        public ListSlice<Datum> GetCustomData(Variant inTargetId)
        {
            return GetCustomData(IndexOf(inTargetId));
        }

        /// <summary>
        /// Returns the custom data associated with the given choice index.
        /// </summary>
        public ListSlice<Datum> GetCustomData(int inIndex)
        {
            if (inIndex < 0)
                return default(ListSlice<Datum>);

            Option option = m_AllOptions[inIndex];
            return new ListSlice<Datum>(m_AllData, option.m_DataOffset, option.m_DataLength);
        }

        /// <summary>
        /// Returns the custom data associated with the given target.
        /// </summary>
        public bool TryGetCustomData(Variant inTargetId, StringHash32 inDataId, out Variant outData)
        {
            return TryGetCustomData(IndexOf(inTargetId), inDataId, out outData);
        }

        /// <summary>
        /// Returns the custom data associated with the given target.
        /// </summary>
        public bool TryGetCustomData(int inIndex, StringHash32 inDataId, out Variant outData)
        {
            if (inIndex < 0)
            {
                outData = default(Variant);
                return false;
            }

            bool bFound;
            outData = GetDataValue(inIndex, inDataId, out bFound);
            return bFound;
        }

        /// <summary>
        /// Returns the custom data associated with the given target.
        /// </summary>
        public Variant GetCustomData(Variant inTargetId, StringHash32 inDataId, Variant inDefault = default)
        {
            return GetCustomData(IndexOf(inTargetId), inDataId, inDefault);
        }

        /// <summary>
        /// Returns the custom data associated with the given option index.
        /// </summary>
        public Variant GetCustomData(int inIndex, StringHash32 inDataId, Variant inDefault = default)
        {
            if (inIndex < 0)
                return inDefault;

            bool _;
            return GetDataValue(inIndex, inDataId, out _);
        }

        /// <summary>
        /// Returns if the given target has custom data with the given id.
        /// </summary>
        public bool HasCustomData(Variant inTargetId, StringHash32 inDataId)
        {
            return HasCustomData(IndexOf(inTargetId), inDataId);
        }

        /// <summary>
        /// Returns if the given target has custom data with the given id.
        /// </summary>
        public bool HasCustomData(int inIndex, StringHash32 inDataId)
        {
            if (inIndex < 0)
                return false;

            bool bFound;
            GetDataValue(inIndex, inDataId, out bFound);
            return bFound;
        }

        #endregion // Query

        #region Modifications

        /// <summary>
        /// Locks options in place to present.
        /// </summary>
        public void Offer()
        {
            if (m_State != State.Accumulating)
                throw new InvalidOperationException(string.Format("Cannot offer options while in {0} state", m_State));
            m_State = State.Choosing;
        }

        /// <summary>
        /// Chooses the option with the given target id.
        /// </summary>
        public void Choose(Variant inTargetId)
        {
            if (m_State != State.Choosing)
                throw new InvalidOperationException(string.Format("Cannot choose an option while in {0} state", m_State));

            int index = IndexOf(inTargetId);
            if (index >= 0)
            {
                m_ChosenIndex = index;
                m_ChosenOption = inTargetId;
                m_State = State.Chosen;
                return;
            }

            throw new Exception(string.Format("No option with target id {0} is present in this choice", inTargetId.ToDebugString()));
        }

        /// <summary>
        /// Chooses the option with the given target id and answer id.
        /// </summary>
        public void Choose(Variant inTargetId, Variant inAnswerId)
        {
            if (m_State != State.Choosing)
                throw new InvalidOperationException(string.Format("Cannot choose an option while in {0} state", m_State));

            int index = IndexOf(inTargetId);
            if (index >= 0)
            {
                bool _;
                m_ChosenIndex = index;
                m_ChosenOption = GetAnswerResponse(index, inAnswerId, out _);
                m_State = State.Chosen;
                return;
            }

            throw new Exception(string.Format("No option with target id {0} is present in this choice", inTargetId.ToDebugString()));
        }

        /// <summary>
        /// Chooses the option with the given index.
        /// </summary>
        public void Choose(int inIndex)
        {
            if (m_State != State.Choosing)
                throw new InvalidOperationException(string.Format("Cannot choose an option while in {0} state", m_State));
            if (inIndex < 0 || inIndex >= m_AllOptions.Count)
                throw new ArgumentOutOfRangeException("inIndex");
            
            m_ChosenIndex = inIndex;
            m_ChosenOption = m_AllOptions[inIndex].TargetId;
            m_State = State.Chosen;
        }

        /// <summary>
        /// Chooses the option with the given index and answer id.
        /// </summary>
        public void Choose(int inIndex, Variant inAnswerId)
        {
            if (m_State != State.Choosing)
                throw new InvalidOperationException(string.Format("Cannot choose an option while in {0} state", m_State));
            if (inIndex < 0 || inIndex >= m_AllOptions.Count)
                throw new ArgumentOutOfRangeException("inIndex");
            
            bool _;
            m_ChosenIndex = inIndex;
            m_ChosenOption = GetAnswerResponse(inIndex, inAnswerId, out _);
            m_State = State.Chosen;
        }

        #endregion // Modifications

        #region Post-Choice

        /// <summary>
        /// Returns if an option has been chosen.
        /// </summary>
        public bool HasChosen()
        {
            return m_State == State.Chosen;
        }

        /// <summary>
        /// Returns the chosen option's target id.
        /// </summary>
        public Variant ChosenTarget()
        {
            return m_ChosenOption;
        }

        /// <summary>
        /// Returns the index of the chosen option.
        /// </summary>
        public int ChosenIndex()
        {
            return m_ChosenIndex;
        }

        #endregion // Post-Choice

        /// <summary>
        /// Resets choice state.
        /// </summary>
        public void Reset()
        {
            m_AllOptions.Clear();
            m_AllAnswers.Clear();
            m_AllData.Clear();
            AvailableCount = 0;
            m_State = State.Accumulating;
            m_ChosenOption = Variant.Null;
            m_ChosenIndex = -1;
        }

        private int IndexOf(Variant inTargetId)
        {
            for(int i = 0, length = m_AllOptions.Count; i < length; i++)
            {
                if (m_AllOptions[i].TargetId == inTargetId)
                {
                    return i;
                }
            }

            return -1;
        }

        private Variant GetAnswerResponse(int inOptionIndex, Variant inAnswer, out bool outbFound)
        {
            Option option = m_AllOptions[inOptionIndex];
            ListSlice<Answer> answers = new ListSlice<Answer>(m_AllAnswers, option.m_AnswersOffset, option.m_AnswersLength);
            Answer check;
            for(int i = 0; i < answers.Length; i++)
            {
                check = answers[i];
                if (check.AnswerId == inAnswer)
                {
                    outbFound = true;
                    return check.TargetId;
                }
            }

            outbFound = false;
            return option.m_DefaultAnswerTarget;
        }

        private Variant GetDataValue(int inOptionIndex, StringHash32 inDataId, out bool outbFound)
        {
            Option option = m_AllOptions[inOptionIndex];
            ListSlice<Datum> data = new ListSlice<Datum>(m_AllData, option.m_DataOffset, option.m_DataLength);
            Datum check;
            for(int i = 0; i < data.Length; i++)
            {
                check = data[i];
                if (check.Id == inDataId)
                {
                    outbFound = true;
                    return check.Value;
                }
            }

            outbFound = false;
            return Variant.Null;
        }
    }
}