using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
[AddComponentMenu("UI/SlicedMaskImage", 11)]
public class SlicedMaskImage  : MaskableGraphic, ILayoutElement, ICanvasRaycastFilter
{
	public float paddingLeft;
	public float paddingRight;
	public float paddingTop;
	public float paddingBottom;

	[FormerlySerializedAs("m_Frame")]
	[SerializeField] private Sprite m_Sprite;
	public Sprite sprite { get { return m_Sprite; } set { SetAllDirty(); } }

	[NonSerialized]
	private Sprite m_OverrideSprite;
	public Sprite overrideSprite { get { return m_OverrideSprite == null ? sprite : m_OverrideSprite; } set { SetAllDirty(); } }


	private float m_EventAlphaThreshold = 1;
	public float eventAlphaThreshold { get { return m_EventAlphaThreshold; } set { m_EventAlphaThreshold = value; } }

	protected SlicedMaskImage()
	{
		useLegacyMeshGeneration = false;
	}

	/// <summary>
	/// Image's texture comes from the UnityEngine.Image.
	/// </summary>
	public override Texture mainTexture
	{
		get
		{
			if (overrideSprite == null)
			{
				if (material != null && material.mainTexture != null)
				{
					return material.mainTexture;
				}
				return s_WhiteTexture;
			}

			return overrideSprite.texture;
		}
	}

	/// <summary>
	/// Whether the Image has a border to work with.
	/// </summary>

	public bool hasBorder
	{
		get
		{
			if (overrideSprite != null)
			{
				Vector4 v = overrideSprite.border;
				return v.sqrMagnitude > 0f;
			}
			return false;
		}
	}

	public float pixelsPerUnit
	{
		get
		{
			float spritePixelsPerUnit = 100;
			if (sprite)
				spritePixelsPerUnit = sprite.pixelsPerUnit;

			float referencePixelsPerUnit = 100;
			if (canvas)
				referencePixelsPerUnit = canvas.referencePixelsPerUnit;

			return spritePixelsPerUnit / referencePixelsPerUnit;
		}
	}

	/// Image's dimensions used for drawing. X = left, Y = bottom, Z = right, W = top.
	private Vector4 GetDrawingDimensions(bool shouldPreserveAspect)
	{
		var padding = overrideSprite == null ? Vector4.zero : UnityEngine.Sprites.DataUtility.GetPadding(overrideSprite);
		var size = overrideSprite == null ? Vector2.zero : new Vector2(overrideSprite.rect.width, overrideSprite.rect.height);

		Rect r = GetPixelAdjustedRect();

		int spriteW = Mathf.RoundToInt(size.x);
		int spriteH = Mathf.RoundToInt(size.y);

		var v = new Vector4(
			padding.x / spriteW,
			padding.y / spriteH,
			(spriteW - padding.z) / spriteW,
			(spriteH - padding.w) / spriteH);

		if (shouldPreserveAspect && size.sqrMagnitude > 0.0f)
		{
			var spriteRatio = size.x / size.y;
			var rectRatio = r.width / r.height;

			if (spriteRatio > rectRatio)
			{
				var oldHeight = r.height;
				r.height = r.width * (1.0f / spriteRatio);
				r.y += (oldHeight - r.height) * rectTransform.pivot.y;
			}
			else
			{
				var oldWidth = r.width;
				r.width = r.height * spriteRatio;
				r.x += (oldWidth - r.width) * rectTransform.pivot.x;
			}
		}

		v = new Vector4(
			r.x + r.width * v.x,
			r.y + r.height * v.y,
			r.x + r.width * v.z,
			r.y + r.height * v.w
		);

		return v;
	}
	[ContextMenu("change")]
	public override void SetNativeSize()
	{
		if (overrideSprite != null)
		{
			float w = overrideSprite.rect.width / pixelsPerUnit;
			float h = overrideSprite.rect.height / pixelsPerUnit;
			rectTransform.anchorMax = rectTransform.anchorMin;
			rectTransform.sizeDelta = new Vector2(w, h);
			SetAllDirty();
		}
	}

	/// <summary>
	/// Update the UI renderer mesh.
	/// </summary>
	protected override void OnPopulateMesh(VertexHelper toFill)
	{
		if (overrideSprite == null)
		{
			base.OnPopulateMesh(toFill);
			return;
		}
		GenerateSlicedSprite(toFill);
	}

	#region Various fill functions

	/// <summary>
	/// Generate vertices for a 9-sliced Image.
	/// </summary>
	static readonly Vector2[] s_VertScratch = new Vector2[4];
	static readonly Vector2[] s_UVScratch = new Vector2[4];

	private void GenerateSlicedSprite(VertexHelper toFill)
	{
		if (!hasBorder) {
			Debug.LogWarning (" Image no Border ");
			return;
		}
		Vector4 outer, inner, padding, border;

		if (overrideSprite != null)
		{
			outer = UnityEngine.Sprites.DataUtility.GetOuterUV(overrideSprite);
			inner = UnityEngine.Sprites.DataUtility.GetInnerUV(overrideSprite);
			padding = UnityEngine.Sprites.DataUtility.GetPadding(overrideSprite);
			border = overrideSprite.border;
		}
		else
		{
			outer = Vector4.zero;
			inner = Vector4.zero;
			padding = Vector4.zero;
			border = Vector4.zero;
		}

		Rect rect = GetPixelAdjustedRect();
		border = GetAdjustedBorders(border / pixelsPerUnit, rect);
		padding = padding / pixelsPerUnit;

		s_VertScratch[0] = new Vector2(padding.x - paddingLeft , padding.y - paddingRight );
		s_VertScratch[3] = new Vector2(rect.width - padding.z - paddingTop, rect.height - padding.w -paddingBottom );

		s_VertScratch[1].x = border.x;
		s_VertScratch[1].y = border.y;
		s_VertScratch[2].x = rect.width - border.z;
		s_VertScratch[2].y = rect.height - border.w;

		for (int i = 0; i < 4; ++i)
		{
			s_VertScratch[i].x += rect.x;
			s_VertScratch[i].y += rect.y;
		}

		s_UVScratch[0] = new Vector2(outer.x, outer.y);
		s_UVScratch[1] = new Vector2(inner.x, inner.y);
		s_UVScratch[2] = new Vector2(inner.z, inner.w);
		s_UVScratch[3] = new Vector2(outer.z, outer.w);

		toFill.Clear();

		for (int x = 0; x < 3; ++x)
		{
			int x2 = x + 1;

			for (int y = 0; y < 3; ++y)
			{
				if (x == 1 && y == 1)
					continue;

				int y2 = y + 1;

				AddQuad(toFill,
					new Vector2(s_VertScratch[x].x, s_VertScratch[y].y),
					new Vector2(s_VertScratch[x2].x, s_VertScratch[y2].y),
					color,
					new Vector2(s_UVScratch[x].x, s_UVScratch[y].y),
					new Vector2(s_UVScratch[x2].x, s_UVScratch[y2].y));
			}
		}
	}


	static void AddQuad(VertexHelper vertexHelper, Vector2 posMin, Vector2 posMax, Color32 color, Vector2 uvMin, Vector2 uvMax)
	{
		int startIndex = vertexHelper.currentVertCount;

		vertexHelper.AddVert(new Vector3(posMin.x, posMin.y, 0), color, new Vector2(uvMin.x, uvMin.y));
		vertexHelper.AddVert(new Vector3(posMin.x, posMax.y, 0), color, new Vector2(uvMin.x, uvMax.y));
		vertexHelper.AddVert(new Vector3(posMax.x, posMax.y, 0), color, new Vector2(uvMax.x, uvMax.y));
		vertexHelper.AddVert(new Vector3(posMax.x, posMin.y, 0), color, new Vector2(uvMax.x, uvMin.y));

		vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
		vertexHelper.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
	}

	Vector4 GetAdjustedBorders(Vector4 border, Rect rect)
	{
		for (int axis = 0; axis <= 1; axis++)
		{
			float combinedBorders = border[axis] + border[axis + 2];
			if (rect.size[axis] < combinedBorders && combinedBorders != 0)
			{
				float borderScaleRatio = rect.size[axis] / combinedBorders;
				border[axis] *= borderScaleRatio;
				border[axis + 2] *= borderScaleRatio;
			}
		}
		return border;
	}


	#endregion

	public virtual void CalculateLayoutInputHorizontal() {}
	public virtual void CalculateLayoutInputVertical() {}

	public virtual float minWidth { get { return 0; } }

	public virtual float preferredWidth
	{
		get
		{
			if (overrideSprite == null)
				return 0;
			return UnityEngine.Sprites.DataUtility.GetMinSize(overrideSprite).x / pixelsPerUnit;
		}
	}

	public virtual float flexibleWidth { get { return -1; } }

	public virtual float minHeight { get { return 0; } }

	public virtual float preferredHeight
	{
		get
		{
			if (overrideSprite == null)
				return 0;
			return UnityEngine.Sprites.DataUtility.GetMinSize(overrideSprite).y / pixelsPerUnit;
		}
	}

	public virtual float flexibleHeight { get { return -1; } }

	public virtual int layoutPriority { get { return 0; } }

	public virtual bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
	{
		return false;
		if (m_EventAlphaThreshold >= 1)
			return true;

		Sprite sprite = overrideSprite;
		if (sprite == null)
			return true;

		Vector2 local;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out local);

		Rect rect = GetPixelAdjustedRect();

		// Convert to have lower left corner as reference point.
		local.x += rectTransform.pivot.x * rect.width;
		local.y += rectTransform.pivot.y * rect.height;

		local = MapCoordinate(local, rect);

		// Normalize local coordinates.
		Rect spriteRect = sprite.textureRect;
		Vector2 normalized = new Vector2(local.x / spriteRect.width, local.y / spriteRect.height);

		// Convert to texture space.
		float x = Mathf.Lerp(spriteRect.x, spriteRect.xMax, normalized.x) / sprite.texture.width;
		float y = Mathf.Lerp(spriteRect.y, spriteRect.yMax, normalized.y) / sprite.texture.height;

		try
		{
			return sprite.texture.GetPixelBilinear(x, y).a >= m_EventAlphaThreshold;
		}
		catch (UnityException e)
		{
			Debug.LogError("Using clickAlphaThreshold lower than 1 on Image whose sprite texture cannot be read. " + e.Message + " Also make sure to disable sprite packing for this sprite.", this);
			return true;
		}
	}

	private Vector2 MapCoordinate(Vector2 local, Rect rect)
	{
		Rect spriteRect = sprite.rect;
		Vector4 border = sprite.border;
		Vector4 adjustedBorder = GetAdjustedBorders(border / pixelsPerUnit, rect);

		for (int i = 0; i < 2; i++)
		{
			if (local[i] <= adjustedBorder[i])
				continue;

			if (rect.size[i] - local[i] <= adjustedBorder[i + 2])
			{
				local[i] -= (rect.size[i] - spriteRect.size[i]);
				continue;
			}

			float lerp = Mathf.InverseLerp(adjustedBorder[i], rect.size[i] - adjustedBorder[i + 2], local[i]);
			local[i] = Mathf.Lerp(border[i], spriteRect.size[i] - border[i + 2], lerp);
			continue;
		}

		return local;
	}
}