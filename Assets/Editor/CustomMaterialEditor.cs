using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;


[CustomEditor(typeof(CustomMaterialEditor))]
public class CustomMaterialEditor  : MaterialEditor {
	void OnEnable()
	{
		base.OnEnable ();
	}

	public override void OnInspectorGUI() {
		Debug.Log ("==OnInspectorGUI== ");
		base.OnInspectorGUI();
		Material material = target as Material;
		Shader shader = material.shader;
		for (int i = 0; i < ShaderUtil.GetPropertyCount (shader); i++) {
			if (ShaderUtil.GetPropertyType(shader, i) != ShaderUtil.ShaderPropertyType.TexEnv)
				continue;
			//属性
			string name = ShaderUtil.GetPropertyName(shader, i);
			//文本值
			string label = ShaderUtil.GetPropertyDescription(shader, i);
			TextureDimension desiredTexdim = ShaderUtil.GetTexDim(shader, i);
			System.Type t;
			switch (desiredTexdim) {
			case TextureDimension.Tex2D:
				t = typeof(Texture);
				//Debug.Log (" Tex2D ");
				Texture t1 = material.GetTexture (name);
				break;
			case TextureDimension.Cube:
				t = typeof(Cubemap);
				//Debug.Log (" Cube ");
				break;
			case TextureDimension.Tex3D:
				t = typeof(Texture3D);
				//Debug.Log (" Tex3D ");
				break;
			case TextureDimension.Tex2DArray:
				t = typeof(Texture);
				//Debug.Log (" Tex2DArray ");
				break;
			default:
				t = null;
				break;
			}
		}
	}
}
