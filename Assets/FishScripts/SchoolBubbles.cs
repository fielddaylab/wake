using UnityEngine;

public class SchoolBubbles:MonoBehaviour{
	
	public ParticleSystem _bubbleParticles;
	public float _emitEverySecond = 0.01f;
	public float _speedEmitMultiplier = 0.25f;
	public int _minBubbles = 0;
	public int _maxBubbles = 5;
	
	public void Start() {
		if(_bubbleParticles == null) transform.GetComponent<ParticleSystem>();
	}
	
	public void EmitBubbles(Vector3 pos,float amount) {
		float f = amount*_speedEmitMultiplier;
		if(f < 1) return;
		_bubbleParticles.transform.position = pos;
		_bubbleParticles.Emit(Mathf.Clamp((int)(amount*_speedEmitMultiplier), _minBubbles, _maxBubbles));	
	}
}