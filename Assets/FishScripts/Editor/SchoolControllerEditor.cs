using UnityEngine;
using System;
using UnityEditor;
/****************************************	
	Copyright 2015 Unluck Software	
 	www.chemicalbliss.com																															
*****************************************/
[CustomEditor(typeof(SchoolController))]
[System.Serializable]
public class SchoolControllerEditor: Editor
{
	public SerializedProperty myProperty;
	public SerializedProperty bubbles;
	public SerializedProperty avoidanceMask;
	
	public void OnEnable()
	{
		var target_cs = (SchoolController)target;
        avoidanceMask= serializedObject.FindProperty("_avoidanceMask");
		if(target_cs._bubbles == null)
		{
			target_cs._bubbles = (SchoolBubbles)Transform.FindObjectOfType(typeof(SchoolBubbles));
		}
		myProperty = serializedObject.FindProperty("_childPrefab");
		bubbles = serializedObject.FindProperty("_bubbles");
	}

	public override void OnInspectorGUI()
	{
		var target_cs = (SchoolController)target;
        Color warningColor = new Color32((byte)255, (byte)174, (byte)0, (byte)255);
		Color warningColor2 = Color.yellow;
		Color dColor = new Color32((byte)175, (byte)175, (byte)175, (byte)255);
		GUIStyle warningStyle = new GUIStyle(GUI.skin.label);
		warningStyle.normal.textColor = warningColor;
		warningStyle.fontStyle = FontStyle.Bold;
		GUIStyle warningStyle2 = new GUIStyle(GUI.skin.label);
		warningStyle2.normal.textColor = warningColor2;
		warningStyle2.fontStyle = FontStyle.Bold;
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		if(UnityEditor.EditorApplication.isPlaying)
		{
			GUI.enabled = false;
		}
		target_cs._updateDivisor = (int)EditorGUILayout.Slider("Frame Skipping", (float)target_cs._updateDivisor, 1.0f, 10.0f);
		GUI.enabled = true;
		if(target_cs._updateDivisor > 4)
		{
			EditorGUILayout.LabelField("Will cause choppy movement", warningStyle);
		}
		else if(target_cs._updateDivisor > 2)
		{
			EditorGUILayout.LabelField("Can cause choppy movement	", warningStyle2);
		}
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		serializedObject.Update();
		EditorGUILayout.PropertyField(myProperty, new GUIContent("Fish Prefabs"), true);
		serializedObject.ApplyModifiedProperties();
		EditorGUILayout.LabelField("Prefabs must have SchoolChild component", EditorStyles.miniLabel);
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("Grouping", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("Move fish into a parent transform", EditorStyles.miniLabel);
		target_cs._groupChildToSchool = EditorGUILayout.Toggle("Group to School", target_cs._groupChildToSchool);
		if(target_cs._groupChildToSchool)
		{
			GUI.enabled = false;
		}
		target_cs._groupChildToNewTransform = EditorGUILayout.Toggle("Group to New GameObject", target_cs._groupChildToNewTransform);
		target_cs._groupName = EditorGUILayout.TextField("Group Name", target_cs._groupName);
		GUI.enabled = true;
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("Bubbles", EditorStyles.boldLabel);
		EditorGUILayout.PropertyField(bubbles, new GUIContent("Bubbles Object"), true);
		if(target_cs._bubbles != null)
		{
			target_cs._bubbles._emitEverySecond = EditorGUILayout.FloatField("Emit Every Second", target_cs._bubbles._emitEverySecond);
			target_cs._bubbles._speedEmitMultiplier = EditorGUILayout.FloatField("Fish Speed Emit Multiplier", target_cs._bubbles._speedEmitMultiplier);
			target_cs._bubbles._minBubbles = EditorGUILayout.IntField("Minimum Bubbles Emitted", target_cs._bubbles._minBubbles);
			target_cs._bubbles._maxBubbles = EditorGUILayout.IntField("Maximum Bubbles Emitted", target_cs._bubbles._maxBubbles);
			if(GUI.changed)
			{
				EditorUtility.SetDirty(target_cs._bubbles);
			}
		}
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("Area Size", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("Size of area the school roams within", EditorStyles.miniLabel);
		target_cs._positionSphere = EditorGUILayout.FloatField("Roaming Area Width", target_cs._positionSphere);
		target_cs._positionSphereDepth = EditorGUILayout.FloatField("Roaming Area Depth", target_cs._positionSphereDepth);
		target_cs._positionSphereHeight = EditorGUILayout.FloatField("Roaming Area Height", target_cs._positionSphereHeight);
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("Size of the school", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("Size of area the Fish swim towards", EditorStyles.miniLabel);
		target_cs._childAmount = (int)EditorGUILayout.Slider("Fish Amount", (float)target_cs._childAmount, 1.0f, 500.0f);
		target_cs._spawnSphere = EditorGUILayout.FloatField("School Width", target_cs._spawnSphere);
		target_cs._spawnSphereDepth = EditorGUILayout.FloatField("School Depth", target_cs._spawnSphereDepth);
		target_cs._spawnSphereHeight = EditorGUILayout.FloatField("School Height", target_cs._spawnSphereHeight);
		target_cs._posOffset = EditorGUILayout.Vector3Field("Start Position Offset", target_cs._posOffset);
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("Speed and Movement ", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("Change Fish speed, rotation and movement behaviors", EditorStyles.miniLabel);
		target_cs._childSpeedMultipler = EditorGUILayout.FloatField("Random Speed Multiplier", target_cs._childSpeedMultipler);
		target_cs._speedCurveMultiplier = EditorGUILayout.CurveField("Speed Curve Multiplier", target_cs._speedCurveMultiplier);
		if(target_cs._childSpeedMultipler < 0.01f) target_cs._childSpeedMultipler = 0.01f;
		target_cs._minSpeed = EditorGUILayout.FloatField("Min Speed", target_cs._minSpeed);
		target_cs._maxSpeed = EditorGUILayout.FloatField("Max Speed", target_cs._maxSpeed);
		target_cs._acceleration = EditorGUILayout.Slider("Fish Acceleration", target_cs._acceleration, .001f, 0.07f);
		target_cs._brake = EditorGUILayout.Slider("Fish Brake Power", target_cs._brake, .001f, 0.025f);
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("Turn Speed", EditorStyles.boldLabel);
		target_cs._minDamping = EditorGUILayout.FloatField("Min Turn Speed", target_cs._minDamping);
		target_cs._maxDamping = EditorGUILayout.FloatField("Max Turn Speed", target_cs._maxDamping);
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("Randomize Fish Size ", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("Change scale of Fish when they are added to the stage", EditorStyles.miniLabel);
		target_cs._minScale = EditorGUILayout.FloatField("Min Scale", target_cs._minScale);
		target_cs._maxScale = EditorGUILayout.FloatField("Max Scale", target_cs._maxScale);
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("Fish Random Animation Speeds", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("Animation speeds are also increased by movement speed", EditorStyles.miniLabel);
		target_cs._minAnimationSpeed = EditorGUILayout.FloatField("Min Animation Speed", target_cs._minAnimationSpeed);
		target_cs._maxAnimationSpeed = EditorGUILayout.FloatField("Max Animation Speed", target_cs._maxAnimationSpeed);
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("Fish Waypoint Distance", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("Waypoints inside small sphere", EditorStyles.miniLabel);
		target_cs._waypointDistance = EditorGUILayout.FloatField("Distance To Waypoint", target_cs._waypointDistance);
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("Fish Triggers School Waypoint", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("Fish waypoint triggers a new School waypoint", EditorStyles.miniLabel);
		target_cs._childTriggerPos = EditorGUILayout.Toggle("Fish Trigger Waypoint", target_cs._childTriggerPos);
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("Automatically New Waypoint", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("Automatically trigger new school waypoint", EditorStyles.miniLabel);
		target_cs._autoRandomPosition = EditorGUILayout.Toggle("Auto School Waypoint", target_cs._autoRandomPosition);
		if(target_cs._autoRandomPosition)
		{
			target_cs._randomPositionTimerMin = EditorGUILayout.FloatField("Min Delay", target_cs._randomPositionTimerMin);
			target_cs._randomPositionTimerMax = EditorGUILayout.FloatField("Max Delay", target_cs._randomPositionTimerMax);
			if(target_cs._randomPositionTimerMin < 1)
			{
				target_cs._randomPositionTimerMin = 1.0f;
			}
			if(target_cs._randomPositionTimerMax < 1)
			{
				target_cs._randomPositionTimerMax = 1.0f;
			}
		}
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("Fish Force School Waypoint", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("Force all Fish to change waypoints when school changes waypoint", EditorStyles.miniLabel);
		target_cs._forceChildWaypoints = EditorGUILayout.Toggle("Force Fish Waypoints", target_cs._forceChildWaypoints);
		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Force New Waypoint Delay", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("How many seconds until the Fish in school will change waypoint", EditorStyles.miniLabel);
		target_cs._forcedRandomDelay = EditorGUILayout.FloatField("Waypoint Delay", target_cs._forcedRandomDelay);
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		EditorGUILayout.LabelField("Obstacle Avoidance", EditorStyles.boldLabel);
		EditorGUILayout.LabelField("Steer and push away from obstacles (uses more CPU)", EditorStyles.miniLabel);
		EditorGUILayout.PropertyField(avoidanceMask, new GUIContent("Collider Mask"));
		target_cs._avoidance = EditorGUILayout.Toggle("Avoidance (enable/disable)", target_cs._avoidance);
		if(target_cs._avoidance)
		{
			target_cs._avoidAngle = EditorGUILayout.Slider("Avoid Angle", target_cs._avoidAngle, .05f, .95f);
			target_cs._avoidDistance = EditorGUILayout.FloatField("Avoid Distance", target_cs._avoidDistance);
			if(target_cs._avoidDistance <= 0.1f) target_cs._avoidDistance = 0.1f;
			target_cs._avoidSpeed = EditorGUILayout.FloatField("Avoid Speed", target_cs._avoidSpeed);
			target_cs._stopDistance = EditorGUILayout.FloatField("Stop Distance", target_cs._stopDistance);
			target_cs._stopSpeedMultiplier = EditorGUILayout.FloatField("Stop Speed Multiplier", target_cs._stopSpeedMultiplier);
			if(target_cs._stopDistance <= 0.1f) target_cs._stopDistance = 0.1f;
		}
		EditorGUILayout.EndVertical();
		GUI.color = dColor;
		EditorGUILayout.BeginVertical("Box");
		GUI.color = Color.white;
		target_cs._push = EditorGUILayout.Toggle("Push (enable/disable)", target_cs._push);
		if(target_cs._push)
		{
			target_cs._pushDistance = EditorGUILayout.FloatField("Push Distance", target_cs._pushDistance);
			if(target_cs._pushDistance <= 0.1f) target_cs._pushDistance = 0.1f;
			target_cs._pushForce = EditorGUILayout.FloatField("Push Force", target_cs._pushForce);
			if(target_cs._pushForce <= 0.01f) target_cs._pushForce = 0.01f;
		}
		EditorGUILayout.EndVertical();
		if(GUI.changed) EditorUtility.SetDirty(target_cs);
	}
}