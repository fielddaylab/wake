/****************************************		
	Copyright 2015 Unluck Software	
 	www.chemicalbliss.com
 	
 	Changelog
 	v1.2
 	IMPORTANT!	_posBuffer is now used for flock position, not controller position.
 	Changed from _roamers Array To List
 	Code cleanup
	v1.21
	Added grouping
	Fixed gizmo bug
	v1.3																																																																																																																		
*****************************************/

using UnityEngine;
using System.Collections.Generic;


public class SchoolController:MonoBehaviour{
	
	public SchoolChild[] _childPrefab;			// Assign prefab with SchoolChild script attached
	public bool _groupChildToNewTransform;	// Parents fish transform to school transform
	public Transform _groupTransform;			// New game object created for group
	public string _groupName = "";				// Name of group (if no name, the school name will be used)
	public bool _groupChildToSchool;			// Parents fish transform to school transform
	public int _childAmount = 250;				// Number of objects
	public float _spawnSphere = 3.0f;				// Range around the spawner waypoints will created //changed to box
	public float _spawnSphereDepth = 3.0f;			
	public float _spawnSphereHeight = 1.5f;		
	public float _childSpeedMultipler = 2.0f;		// Adjust speed of entire school
	public float _minSpeed = 6.0f;					// minimum random speed
	public float _maxSpeed = 10.0f;				// maximum random speed
	public AnimationCurve _speedCurveMultiplier = new AnimationCurve(new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 1.0f));
	public float _minScale = .7f;				// minimum random size
	public float _maxScale = 1.0f;					// maximum random size
	public float _minDamping = 1.0f;				// Rotation tween damping, lower number = smooth/slow rotation (if this get stuck in a loop, increase this value)
	public float _maxDamping = 2.0f;
	public float _waypointDistance = 1.0f;			// How close this can get to waypoint before creating a new waypoint (also fixes stuck in a loop)
	public float _minAnimationSpeed = 2.0f;
	public float _maxAnimationSpeed = 4.0f;		
	public float _randomPositionTimerMax = 10.0f;	// When _autoRandomPosition is enabled
	public float _randomPositionTimerMin = 4.0f;	
	public float _acceleration = .025f;			// How fast child speeds up
	public float _brake = .01f;					// How fast child slows down 
	public float _positionSphere = 25.0f;			// If _randomPositionTimer is bigger than zero the controller will be moved to a random position within this sphere
	public float _positionSphereDepth = 5.0f;		// Overides height of sphere for more controll
	public float _positionSphereHeight = 5.0f;		// Overides height of sphere for more controll
	public bool _childTriggerPos;			// Runs the random position function when a child reaches the controller
	public bool _forceChildWaypoints;		// Forces all children to change waypoints when this changes position
	public bool _autoRandomPosition;			// Automaticly positions waypoint based on random values (_randomPositionTimerMin, _randomPositionTimerMax)
	public float _forcedRandomDelay = 1.5f;		// Random delay added before forcing new waypoint
	public float _schoolSpeed;					// Value multiplied to child speed
	public List<SchoolChild> _roamers;
	public Vector3 _posBuffer;
	public Vector3 _posOffset;
	
	///AVOIDANCE
	public bool _avoidance;				//Enable/disable avoidance
	public float _avoidAngle = 0.35f; 		//Angle of the rays used to avoid obstacles left and right
	public float _avoidDistance = 1.0f;		//How far avoid rays travel
	public float _avoidSpeed = 75.0f;			//How fast this turns around when avoiding	
	public float _stopDistance	= .5f;		//How close this can be to objects directly in front of it before stopping and backing up. This will also rotate slightly, to avoid "robotic" behaviour
	public float _stopSpeedMultiplier = 2.0f;	//How fast to stop when within stopping distance
	public LayerMask _avoidanceMask = (LayerMask)(-1);
	
	///PUSH
	public bool _push;					//Enable/disable push
	public float _pushDistance;				//How far away obstacles can be before starting to push away	
	public float _pushForce = 5.0f;			//How fast/hard to push away
	
	///BUBBLES
	public SchoolBubbles _bubbles;
	
	//FRAME SKIP
	public int _updateDivisor = 1;				//Skip update every N frames (Higher numbers might give choppy results, 3 - 4 on 60fps , 2 - 3 on 30 fps)
	public float _newDelta;
	public int _updateCounter;
	public int _activeChildren;
	
	public void Start() {
		_posBuffer = transform.position + _posOffset;
		_schoolSpeed = Random.Range(1.0f , _childSpeedMultipler);
		AddFish(_childAmount);
		Invoke("AutoRandomWaypointPosition", RandomWaypointTime());
	}
	
	public void Update() {
		if(_activeChildren > 0){
			if(_updateDivisor > 1){
				_updateCounter++;
			    _updateCounter = _updateCounter % _updateDivisor;	
				_newDelta = Time.deltaTime*_updateDivisor;	
			}else{
				_newDelta = Time.deltaTime;
			}
			UpdateFishAmount();
		}
	}
	
	public void InstantiateGroup(){
		if(_groupTransform != null) return;
		GameObject g = new GameObject();
		_groupTransform = g.transform;
		_groupTransform.position = transform.position;
		if(_groupName != ""){
			g.name = _groupName;
			return;
		}	
		g.name = transform.name + " Fish Container";
	}
	
	public void AddFish(int amount){
		if(_groupChildToNewTransform)InstantiateGroup();	
		for(int i=0;i<amount;i++){
			int child = Random.Range(0,_childPrefab.Length);
			SchoolChild obj = (SchoolChild)Instantiate(_childPrefab[child]);		
		    obj._spawner = this;
		    _roamers.Add(obj);
			AddChildToParent(obj.transform);
		}	
	}
	
	public void AddChildToParent(Transform obj){	
	    if(_groupChildToSchool){
			obj.parent = transform;
			return;
		}
		if(_groupChildToNewTransform){
			obj.parent = _groupTransform;
			return;
		}
	}
	
	public void RemoveFish(int amount){
		SchoolChild dObj = _roamers[_roamers.Count-1];
		_roamers.RemoveAt(_roamers.Count-1);
		Destroy(dObj.gameObject);
	}
	
	public void UpdateFishAmount(){
		if(_childAmount>= 0 && _childAmount < _roamers.Count){
			RemoveFish(1);
			return;
		}
		if (_childAmount > _roamers.Count){	
			AddFish(1);
			return;
		}
	}
	
	//Set waypoint randomly inside box
	public void SetRandomWaypointPosition() {
		_schoolSpeed = Random.Range(1.0f , _childSpeedMultipler);
		Vector3 t = Vector3.zero;
		t.x = Random.Range(-_positionSphere, _positionSphere) + transform.position.x;
		t.z = Random.Range(-_positionSphereDepth, _positionSphereDepth) + transform.position.z;
		t.y = Random.Range(-_positionSphereHeight, _positionSphereHeight) + transform.position.y;
		_posBuffer = t;	
		if(_forceChildWaypoints){
			for(int i = 0; i < _roamers.Count; i++) {
	  		 	(_roamers[i]).Wander(Random.value*_forcedRandomDelay);
			}	
		}
	}
	
	public void AutoRandomWaypointPosition() {
		if(_autoRandomPosition && _activeChildren > 0){
			SetRandomWaypointPosition();
		}
		CancelInvoke("AutoRandomWaypointPosition");
		Invoke("AutoRandomWaypointPosition", RandomWaypointTime());
	}
	
	public float RandomWaypointTime(){
		return Random.Range(_randomPositionTimerMin, _randomPositionTimerMax);
	}
	
	public void OnDrawGizmos() {
		if(!Application.isPlaying && _posBuffer != transform.position+ _posOffset) _posBuffer = transform.position + _posOffset;
	   	Gizmos.color = Color.blue;
		Gizmos.DrawWireCube (_posBuffer, new Vector3(_spawnSphere*2, _spawnSphereHeight*2 ,_spawnSphereDepth*2));
	    Gizmos.color = Color.cyan;
	    Gizmos.DrawWireCube (transform.position, new Vector3((_positionSphere*2)+_spawnSphere*2, (_positionSphereHeight*2)+_spawnSphereHeight*2 ,(_positionSphereDepth*2)+_spawnSphereDepth*2));
	}
}
