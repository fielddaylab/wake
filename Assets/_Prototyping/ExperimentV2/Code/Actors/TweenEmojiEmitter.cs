using BeauRoutine;
using BeauUtil;
using UnityEngine;
using Aqua;
using System.Collections;

namespace ProtoAqua.ExperimentV2
{
	public class TweenEmojiEmitter : ITweenData
	{
		private ActorWorld inWorld;
		private ActorInstance inActor;
		private float m_Start;
		private float m_End;
		
		public TweenEmojiEmitter( ActorInstance _inActor, ActorWorld _inWorld )
		{
			inWorld = _inWorld;
			inActor = _inActor;
		}

		public void OnTweenStart(){}
		
		public void ApplyTween(float inPercent)
		{
			ActorWorld.EmitEmoji(inActor, inWorld);
		}

		public void OnTweenEnd(){}
	}
}