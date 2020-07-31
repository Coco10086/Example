using UnityEngine;
using UnityEngine.UI;

public class GuideMask : Graphic {
	/// <summary>
	/// Fill the vertex buffer data.
	/// </summary>
	protected override void OnPopulateMesh(Mesh m)
	{
		var r = GetPixelAdjustedRect();
		var v = new Vector4(r.x, r.y, r.x + r.width, r.y + r.height);
		
		Color32 color32 = color;
		using (var vh = new VertexHelper())
		{
			vh.AddVert(new Vector3(v.x, v.y), color32, new Vector2(0f, 0f));
			vh.AddVert(new Vector3(v.x, v.w), color32, new Vector2(0f, 1f));
			vh.AddVert(new Vector3(v.z, v.w), color32, new Vector2(1f, 1f));
			vh.AddVert(new Vector3(v.z, v.y), color32, new Vector2(1f, 0f));
			
			vh.AddTriangle(0, 1, 2);
			vh.AddTriangle(2, 3, 0);
			vh.FillMesh(m);
		}
		
		
	}
}