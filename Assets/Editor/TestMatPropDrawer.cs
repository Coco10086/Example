using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TestMatPropDrawer : MaterialPropertyDrawer {

	protected bool isArgumentGiven;
	protected int argValue;

	public TestMatPropDrawer()
	{
		isArgumentGiven = false;
		argValue = 0; // <- some default value
	}

	public TestMatPropDrawer(object i)
	{
		isArgumentGiven = true;
		Debug.Log (" ========object====="+i.GetType().ToString());
		//argValue = i;
	}

	public TestMatPropDrawer(string i)
	{
		isArgumentGiven = true;
		Debug.Log (" ========string====="+i.GetType().ToString());
		//argValue = i;
	}

	public TestMatPropDrawer(float i)
	{
		isArgumentGiven = true;
		Debug.Log (" ========float====="+i.GetType().ToString());
		//argValue = i;
	}

	public TestMatPropDrawer(Int32 i)
	{
		isArgumentGiven = true;
		Debug.Log (" ========int====="+i.GetType().ToString());
		//argValue = i;
	}


	public override void OnGUI (Rect position, MaterialProperty prop, string label, MaterialEditor editor)
	{
		if (GUILayout.Button ("Show Label")) {
			Debug.Log (label);
			EditorUtility.SetDirty (prop.targets [0]);
		}
		bool value = (prop.floatValue != 0.0f);

		EditorGUI.BeginChangeCheck();
		EditorGUI.showMixedValue = prop.hasMixedValue;
		// Show the toggle control
		value = EditorGUI.Toggle(position, label, value);
		EditorGUI.showMixedValue = false;
		if (EditorGUI.EndChangeCheck())
		{
			// Set the new value if it has changed
			prop.floatValue = value ? 1.0f : 0.0f;
		}

		//base.OnGUI (position, prop, label, editor);
	}

	public override void Apply (MaterialProperty prop)
	{
		Debug.Log ("Applying");
		base.Apply (prop);
	}

}
