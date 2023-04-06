/**************************************									
	Copyright 2015 Unluck Software	
 	www.chemicalbliss.com								
***************************************/

using System;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;


public class SchoolChild : MonoBehaviour
{
	[HideInInspector]
	public SchoolController _spawner;

	[HideInInspector]
	public float _speed = 10.0f;                //Fish Speed

	[NonSerialized] float _stuckCounter;            //prevents looping around a waypoint
	[NonSerialized] float _damping;                 //Turn speed
	public Transform _model;        //transform of fish model

	[NonSerialized] private float _targetSpeed;             //Fish target speed
	// public Transform _scanner;              //Scanner object used for push, this rotates to check for collisions

    [NonSerialized] private Vector3 _wayPoint;
    // [NonSerialized] private bool _scan = true;
    // [NonSerialized] private float _rotateCounterR;          //Used to increase avoidance speed over time
	// [NonSerialized] private float _rotateCounterL;
	[NonSerialized] private float tParam = 0.0f;                //
	[NonSerialized] private Renderer fishRenderer;   //Renderer with material with wave speed parameter

    [NonSerialized] private bool _instantiated;         //Has this been instantiated
	[NonSerialized] private int _updateSeed = -1;
    [NonSerialized] private float randomAnimSpeed;
	[NonSerialized] private  Transform _cacheTransform;
	[NonSerialized] private float m_WanderDelay;

#if UNITY_EDITOR
	public static bool _sWarning;
#endif

    static private int AnimParam_FishAnimSpeed;
    static private MaterialPropertyBlock s_PropertyBlock;
	static uint _updateNextSeed = 0; //When using frameskip seed will prevent calculations for all fish to be on the same frame

	public void Initialize()
	{
        if (AnimParam_FishAnimSpeed == 0) {
            AnimParam_FishAnimSpeed = Shader.PropertyToID("fishWaveSpeed");
        }

        if (s_PropertyBlock == null) {
            s_PropertyBlock = new MaterialPropertyBlock();
        }

		//Check if there is a controller attached
		if (_cacheTransform == null) _cacheTransform = transform;
		if (_spawner != null)
		{
			randomAnimSpeed = RNG.Instance.NextFloat(_spawner._minAnimationSpeed, _spawner._maxAnimationSpeed);
			SetRandomScale();
			LocateRequiredChildren();
			//RandomizeStartAnimationFrame();
			SkewModelForLessUniformedMovement();
			_speed = RNG.Instance.NextFloat(_spawner._minSpeed, _spawner._maxSpeed);
			Wander(0.0f);
			SetRandomWaypoint();
			CheckForBubblesThenInvoke();
			_instantiated = true;
			GetStartPos();
			FrameSkipSeedInit();
			_spawner._activeChildren++;
            this.enabled = true;
			return;
		}


		this.enabled = false;
		Debug.Log(gameObject + " found no school to swim in: " + this + " disabled... Standalone fish not supported, please use the SchoolController");
	}

	public void Update()
	{
		//Skip frames
		if (Time.timeScale > 0 && (_spawner._updateDivisor <= 1 || _spawner._updateCounter == _updateSeed))
		{
			CheckForDistanceToWaypoint();
			RotationBasedOnWaypointOrAvoidance();
			ForwardMovement();
			RayCastToPushAwayFromObstacles();
			SetAnimationSpeed();

            if (m_WanderDelay > 0) {
                m_WanderDelay -= _spawner._newDelta;
                if (m_WanderDelay <= 0) {
                    SetRandomWaypoint();
                }
            }
		}
	}

	public void FrameSkipSeedInit()
	{
		if (_spawner._updateDivisor > 1)
		{
			int _updateSeedCap = _spawner._updateDivisor;
			this._updateSeed = (int) (_updateNextSeed % _updateSeedCap);
			_updateNextSeed++;
		}
	}

	public void CheckForBubblesThenInvoke()
	{
		if (_spawner._bubbles != null)
			InvokeRepeating("EmitBubbles", (_spawner._bubbles._emitEverySecond * RNG.Instance.NextFloat()) + 1, _spawner._bubbles._emitEverySecond);
	}

	public void EmitBubbles()
	{
		_spawner._bubbles.EmitBubbles(_cacheTransform.position, _speed);
	}

	private void OnDisable()
	{
		CancelInvoke();
        m_WanderDelay = 0;
        if (_instantiated) {
		    _spawner._activeChildren--;
        }
	}

	private void OnEnable()
	{
		if (_instantiated)
		{
			CheckForBubblesThenInvoke();
			_spawner._activeChildren++;
		}
	}

	private void LocateRequiredChildren()
	{
		if (_model == null) _model = this.gameObject.transform.GetChild(0);
// 		if (_scanner == null && _spawner._avoidance)
// 		{
// 			_scanner = new GameObject().transform;
// 			_scanner.parent = this.transform;
// 			_scanner.localRotation = Quaternion.identity;
// 			_scanner.localPosition = Vector3.zero;
// #if UNITY_EDITOR
// 			if (!_sWarning)
// 			{
// 				Debug.Log("No scanner assigned: creating... (Increase instantiate performance by manually creating a scanner object)");
// 				_sWarning = true;
// 			}
// #endif
// 		}

		fishRenderer = _model.GetComponent<Renderer>();
	}

	private void SkewModelForLessUniformedMovement()
	{
		// Adds a slight rotation to the model so that the fish get a little less uniformed movement	
		Quaternion rx = Quaternion.identity;
		rx.eulerAngles = new Vector3(0.0f, 0.0f, (float)RNG.Instance.NextFloat(-25, 25));
		_model.rotation = rx;
	}

	private void SetRandomScale()
	{
		float sc = RNG.Instance.NextFloat(_spawner._minScale, _spawner._maxScale);
		_cacheTransform.localScale = Vector3.one * sc;
	}

	private void GetStartPos()
	{
		//-Vector is to avoid zero rotation warning
		_cacheTransform.position = _wayPoint - new Vector3(.1f, .1f, .1f);
	}

	private Vector3 findWaypoint()
	{
		Vector3 t = Vector3.zero;
		t.x = RNG.Instance.NextFloat(-_spawner._spawnSphere, _spawner._spawnSphere) + _spawner._posBuffer.x;
		t.z = RNG.Instance.NextFloat(-_spawner._spawnSphereDepth, _spawner._spawnSphereDepth) + _spawner._posBuffer.z;
		t.y = RNG.Instance.NextFloat(-_spawner._spawnSphereHeight, _spawner._spawnSphereHeight) + _spawner._posBuffer.y;
		return t;
	}

	//Uses scanner to push away from obstacles
	public void RayCastToPushAwayFromObstacles()
	{
		// if (_spawner._push)
		// {
		// 	RotateScanner();
		// 	RayCastToPushAwayFromObstaclesCheckForCollision();
		// }
	}

	// public void RayCastToPushAwayFromObstaclesCheckForCollision()
	// {
	// 	RaycastHit hit = new RaycastHit();
	// 	float d = 0.0f;
	// 	Vector3 cacheForward = _scanner.forward;
	// 	if (Physics.Raycast(_cacheTransform.position, cacheForward, out hit, _spawner._pushDistance, _spawner._avoidanceMask))
	// 	{
	// 		SchoolChild s = null;
	// 		s = hit.transform.GetComponent<SchoolChild>();
	// 		d = (_spawner._pushDistance - hit.distance) / _spawner._pushDistance;   // Equals zero to one. One is close, zero is far	
	// 		if (s != null)
	// 		{
	// 			_cacheTransform.position -= cacheForward * _spawner._newDelta * d * _spawner._pushForce;
	// 		}
	// 		else
	// 		{
	// 			_speed -= .01f * _spawner._newDelta;
	// 			if (_speed < .1f)
	// 				_speed = .1f;
	// 			_cacheTransform.position -= cacheForward * _spawner._newDelta * d * _spawner._pushForce * 2;
	// 			//Tell scanner to rotate slowly
	// 			_scan = false;
	// 		}
	// 	}
	// 	else
	// 	{
	// 		//Tell scanner to rotate randomly
	// 		_scan = true;
	// 	}
	// }

	// public void RotateScanner()
	// {
	// 	//Scan random if not pushing
	// 	if (_scan)
	// 	{
	// 		_scanner.rotation = UnityEngine.Random.rotation;
	// 		return;
	// 	}
	// 	//Scan slow if pushing
	// 	_scanner.Rotate(new Vector3(150 * _spawner._newDelta, 0.0f, 0.0f));
	// }

	public void ForwardMovement()
	{
		_cacheTransform.position += _cacheTransform.TransformDirection(Vector3.forward) * _speed * _spawner._newDelta;
		if (tParam < 1)
		{
			if (_speed > _targetSpeed)
			{
				tParam += _spawner._newDelta * _spawner._acceleration;
			}
			else
			{
				tParam += _spawner._newDelta * _spawner._brake;
			}
			_speed = Mathf.Lerp(_speed, _targetSpeed, tParam);
		}
	}

	public void RotationBasedOnWaypointOrAvoidance()
	{
		Quaternion rotation = Quaternion.identity;
		rotation = Quaternion.LookRotation(_wayPoint - _cacheTransform.position);
		_cacheTransform.rotation = Quaternion.Slerp(_cacheTransform.rotation, rotation, _spawner._newDelta * _damping);

		//Limit rotation up and down to avoid freaky behavior
		float angle = _cacheTransform.localEulerAngles.x;
		angle = (angle > 180) ? angle - 360 : angle;
		Quaternion rx = _cacheTransform.rotation;
		Vector3 rxea = rx.eulerAngles;
		rxea.x = ClampAngle(angle, -25.0f, 25.0f);
		rx.eulerAngles = rxea;
        
		_cacheTransform.rotation = rx;
	}

	public void CheckForDistanceToWaypoint()
	{
		if ((_cacheTransform.position - _wayPoint).magnitude < _spawner._waypointDistance + _stuckCounter)
		{
			Wander(0.0f);   //create a new waypoint
			_stuckCounter = 0.0f;
			CheckIfThisShouldTriggerNewFlockWaypoint();
			return;
		}
		_stuckCounter += _spawner._newDelta * (_spawner._waypointDistance * .25f);
	}

	public void CheckIfThisShouldTriggerNewFlockWaypoint()
	{
		if (_spawner._childTriggerPos)
		{
			_spawner.SetRandomWaypointPosition();
		}
	}

	public static float ClampAngle(float angle, float min, float max)
	{
		if (angle < -360) angle += 360.0f;
		if (angle > 360) angle -= 360.0f;
		return Mathf.Clamp(angle, min, max);
	}

	public void SetAnimationSpeed()
	{
		//foreach(AnimationState state in _model.GetComponent<Animation>()) {
		//	state.speed = (RNG.Instance.NextFloat(_spawner._minAnimationSpeed, _spawner._maxAnimationSpeed)*_spawner._schoolSpeed*this._speed)+.1f;}

        fishRenderer.GetPropertyBlock(s_PropertyBlock);
        s_PropertyBlock.SetFloat(AnimParam_FishAnimSpeed, randomAnimSpeed);
		fishRenderer.SetPropertyBlock(s_PropertyBlock);
	}

	public void Wander(float delay)
	{
		_damping = RNG.Instance.NextFloat(_spawner._minDamping, _spawner._maxDamping);
		_targetSpeed = RNG.Instance.NextFloat(_spawner._minSpeed, _spawner._maxSpeed) * _spawner._speedCurveMultiplier.Evaluate(RNG.Instance.NextFloat()) * _spawner._schoolSpeed;
		m_WanderDelay = Math.Max(delay, 0.01f);
	}

	public void SetRandomWaypoint()
	{
		tParam = 0.0f;
		_wayPoint = findWaypoint();
	}
}
