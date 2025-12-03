using UnityEngine;
using UnityEngine.UI;

namespace Veneer.Grid
{
    /// <summary>
    /// Draws a grid overlay for edit mode positioning.
    /// </summary>
    public class VeneerEditModeGrid : Graphic
    {
        /// <summary>
        /// Size of each grid cell in pixels.
        /// </summary>
        public float GridSize { get; set; } = 10f;

        /// <summary>
        /// Color of the grid lines.
        /// </summary>
        public Color GridColor { get; set; } = new Color(1, 1, 1, 0.1f);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var rect = rectTransform.rect;
            float width = rect.width;
            float height = rect.height;

            if (GridSize <= 0) return;

            // Draw vertical lines
            for (float x = 0; x < width; x += GridSize)
            {
                DrawLine(vh, new Vector2(x, 0), new Vector2(x, height), 1f);
            }

            // Draw horizontal lines
            for (float y = 0; y < height; y += GridSize)
            {
                DrawLine(vh, new Vector2(0, y), new Vector2(width, y), 1f);
            }
        }

        private void DrawLine(VertexHelper vh, Vector2 start, Vector2 end, float thickness)
        {
            var rect = rectTransform.rect;

            // Offset to local coordinates
            start.x += rect.x;
            start.y += rect.y;
            end.x += rect.x;
            end.y += rect.y;

            // Calculate perpendicular for thickness
            Vector2 dir = (end - start).normalized;
            Vector2 perp = new Vector2(-dir.y, dir.x) * (thickness * 0.5f);

            int index = vh.currentVertCount;

            vh.AddVert(start + perp, GridColor, Vector2.zero);
            vh.AddVert(start - perp, GridColor, Vector2.zero);
            vh.AddVert(end - perp, GridColor, Vector2.zero);
            vh.AddVert(end + perp, GridColor, Vector2.zero);

            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index + 2, index + 3, index);
        }
    }
}
