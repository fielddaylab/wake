using System;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Services;
using BeauUtil.Tags;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Animation
{
    /// <summary>
    /// Animates a sequence of sprites.
    /// </summary>
    public class SpriteAnimatorService : ServiceBehaviour
    {
        [NonSerialized] private readonly BufferedCollection<SpriteAnimator> m_Animators = new BufferedCollection<SpriteAnimator>(64);

        public void RegisterAnimator(SpriteAnimator inAnimator)
        {
            Assert.False(m_Animators.Contains(inAnimator), "SpriteAnimator '{0}' has already been registered", inAnimator);
            m_Animators.Add(inAnimator);
        }

        public void DeregisterAnimator(SpriteAnimator inAnimator)
        {
            Assert.True(m_Animators.Contains(inAnimator), "SpriteAnimator '{0}' has already been deregistered", inAnimator);
            m_Animators.Remove(inAnimator);
        }

        private void LateUpdate()
        {
            m_Animators.ForEach(UpdateSpriteAnimatorPtr, Time.deltaTime);
        }

        static private readonly Action<SpriteAnimator, float> UpdateSpriteAnimatorPtr = UpdateSpriteAnimator;
        static private void UpdateSpriteAnimator(SpriteAnimator inAnimator, float inDeltaTime)
        {
            inAnimator.AdvanceAnimation(inDeltaTime);
        }
    }
}