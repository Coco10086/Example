using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
/// <summary>
/// 左右对称的Image组件
/// </summary>
[AddComponentMenu("UI/Tiled2Image", 11)]
public class Tiled2Image : MaskableGraphic, ISerializationCallbackReceiver, ILayoutElement, ICanvasRaycastFilter
{

	[FormerlySerializedAs("m_Frame")]
		[SerializeField] private Sprite m_Sprite;
	public Sprite sprite { get { return m_Sprite; } set { if (SetPropertyUtility.SetClass(ref m_Sprite, value)) SetAllDirty(); } }

		[NonSerialized]
		private Sprite m_OverrideSprite;
		public Sprite overrideSprite { get { return m_OverrideSprite == null ? sprite : m_OverrideSprite; } set { if (SetPropertyUtility.SetClass(ref m_OverrideSprite, value)) SetAllDirty(); } }

		[SerializeField] private bool m_PreserveAspect = false;
		public bool preserveAspect { get { return m_PreserveAspect; } set { if (SetPropertyUtility.SetStruct(ref m_PreserveAspect, value)) SetVerticesDirty(); } }

		[SerializeField] private bool m_FillCenter = true;
		public bool fillCenter { get { return m_FillCenter; } set { if (SetPropertyUtility.SetStruct(ref m_FillCenter, value)) SetVerticesDirty(); } }
		
		/// Whether the Image should be filled clockwise (true) or counter-clockwise (false).
		[SerializeField] private bool m_FillClockwise = true;
		public bool fillClockwise { get { return m_FillClockwise; } set { if (SetPropertyUtility.SetStruct(ref m_FillClockwise, value)) SetVerticesDirty(); } }

		/// Controls the origin point of the Fill process. Value means different things with each fill method.
		[SerializeField] private int m_FillOrigin;
		public int fillOrigin { get { return m_FillOrigin; } set { if (SetPropertyUtility.SetStruct(ref m_FillOrigin, value)) SetVerticesDirty(); } }

		// Not serialized until we support read-enabled sprites better.
		private float m_EventAlphaThreshold = 1;
		public float eventAlphaThreshold { get { return m_EventAlphaThreshold; } set { m_EventAlphaThreshold = value; } }

		protected Tiled2Image()
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

		public virtual void OnBeforeSerialize() {}

		public virtual void OnAfterDeserialize()
		{
				m_FillOrigin = 0;
		}

		[ContextMenu("SetNativeSize")]
		public override void SetNativeSize()
		{
			if (overrideSprite != null)
			{
				float w = overrideSprite.rect.width / pixelsPerUnit;
				float h = overrideSprite.rect.height / pixelsPerUnit;
				rectTransform.anchorMax = rectTransform.anchorMin;
				rectTransform.sizeDelta = new Vector2(w*2, h);
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
			GenerateTiledSprite(toFill);
		}

		void GenerateTiledSprite(VertexHelper toFill)
		{
			Vector4 outer, inner, border;
			Vector2 spriteSize;

			if (overrideSprite != null)
			{
				outer = UnityEngine.Sprites.DataUtility.GetOuterUV(overrideSprite);
				inner = UnityEngine.Sprites.DataUtility.GetInnerUV(overrideSprite);
				border = overrideSprite.border;
				spriteSize = overrideSprite.rect.size;
			}
			else
			{
				outer = Vector4.zero;
				inner = Vector4.zero;
				border = Vector4.zero;
				spriteSize = Vector2.one * 100;
			}

			Rect rect = GetPixelAdjustedRect();

			float tileWidth = (spriteSize.x - border.x - border.z) / pixelsPerUnit;
			float tileHeight = (spriteSize.y - border.y - border.w) / pixelsPerUnit;
			border = GetAdjustedBorders(border / pixelsPerUnit, rect);

			var uvMin = new Vector2(inner.x, inner.y);
			var uvMax = new Vector2(inner.z, inner.w);

			var v = UIVertex.simpleVert;
			v.color = color;

			// Min to max max range for tiled region in coordinates relative to lower left corner.
			float xMin = border.x;
			float xMax = tileWidth - border.z;
			float yMin = border.y;
			float yMax = tileHeight - border.w;

			toFill.Clear();
			var clipped = uvMax;

			// if either with is zero we cant tile so just assume it was the full width.
			if (tileWidth == 0)
				tileWidth = xMax - xMin;

			if (tileHeight == 0)
				tileHeight = yMax - yMin;
			AddQuad(toFill, new Vector2(0, 0) + rect.position, new Vector2(tileWidth, tileHeight) + rect.position, color, uvMin, clipped);
			AddQuad(toFill, new Vector2(0, 0) + rect.position, new Vector2(tileWidth, tileHeight) + rect.position, color, uvMin, clipped,true);
		}

		static void AddQuad(VertexHelper vertexHelper, Vector3[] quadPositions, Color32 color, Vector3[] quadUVs)
		{
			int startIndex = vertexHelper.currentVertCount;

			for (int i = 0; i < 4; ++i)
				vertexHelper.AddVert(quadPositions[i], color, quadUVs[i]);

			vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
			vertexHelper.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
		}

		static void AddQuad(VertexHelper vertexHelper, Vector2 posMin, Vector2 posMax, Color32 color, Vector2 uvMin, Vector2 uvMax, bool flag = false)
		{
			int startIndex = vertexHelper.currentVertCount;
			if(!flag)
			{
				vertexHelper.AddVert(new Vector3(posMin.x, posMin.y, 0), color, new Vector2(uvMin.x, uvMin.y));
				vertexHelper.AddVert(new Vector3(posMin.x, posMax.y, 0), color, new Vector2(uvMin.x, uvMax.y));
			}
			else
			{
				vertexHelper.AddVert(new Vector3(-posMin.x, posMin.y, 0), color, new Vector2(uvMin.x, uvMin.y));
				vertexHelper.AddVert(new Vector3(-posMin.x, posMax.y, 0), color, new Vector2(uvMin.x, uvMax.y));
			}
			vertexHelper.AddVert(new Vector3(posMax.x, posMax.y, 0), color, new Vector2(uvMax.x, uvMax.y));
			vertexHelper.AddVert(new Vector3(posMax.x, posMin.y, 0), color, new Vector2(uvMax.x, uvMin.y));
			vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
			vertexHelper.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
		}

		Vector4 GetAdjustedBorders(Vector4 border, Rect rect)
		{
			for (int axis = 0; axis <= 1; axis++)
			{
				// If the rect is smaller than the combined borders, then there's not room for the borders at their normal size.
				// In order to avoid artefacts with overlapping borders, we scale the borders down to fit.
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

		public virtual void CalculateLayoutInputHorizontal() {}
		public virtual void CalculateLayoutInputVertical() {}

		public virtual float minWidth { get { return 0; } }

		public virtual float preferredWidth
		{
			get
			{
				if (overrideSprite == null)
					return 0;
				return overrideSprite.rect.size.x / pixelsPerUnit;
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
				return overrideSprite.rect.size.y / pixelsPerUnit;
			}
		}

		public virtual float flexibleHeight { get { return -1; } }

		public virtual int layoutPriority { get { return 0; } }

		public virtual bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
		{
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

				local[i] -= adjustedBorder[i];
				local[i] = Mathf.Repeat(local[i], spriteRect.size[i] - border[i] - border[i + 2]);
				local[i] += border[i];
				continue;
			}

			return local;
		}
}