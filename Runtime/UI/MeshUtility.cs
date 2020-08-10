using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Transient.UI {
    public static class MeshUtility {
        public static List<(int, int, int)> TriangleBuffer { get; private set; }

        public static void AddSimple(VertexHelper vh, Color color, Vector2 center, Vector4 rect, Vector4 uv) {
            var offset = vh.currentVertCount;
            vh.AddVert(center + new Vector2(rect.x, rect.y), color, new Vector2(uv.x, uv.y));
            vh.AddVert(center + new Vector2(rect.x, rect.w), color, new Vector2(uv.x, uv.w));
            vh.AddVert(center + new Vector2(rect.z, rect.w), color, new Vector2(uv.z, uv.w));
            vh.AddVert(center + new Vector2(rect.z, rect.y), color, new Vector2(uv.z, uv.y));
            if (rect.x < rect.z) {
                vh.AddTriangle(offset, offset + 1, offset + 2);
                vh.AddTriangle(offset, offset + 2, offset + 3);
            }
            else {
                vh.AddTriangle(offset, offset + 2, offset + 1);
                vh.AddTriangle(offset, offset + 3, offset + 2);
            }
        }

        public static void AddSliced(VertexHelper vh, Color color, Vector2 center, Vector4 rect, Vector4 uv, Vector4 uvInner, Vector4 border) {
            if (TriangleBuffer == null) {
                TriangleBuffer = new List<(int, int, int)>(16);
            }
            else {
                TriangleBuffer.Clear();
            }
            var offset = vh.currentVertCount;
            var scaleX = rect.x > rect.z ? -1 : 1;
            var scaleY = rect.y > rect.w ? -1 : 1;
            Vector4 rectInner = new Vector4(rect.x + border.x * scaleX, rect.y + border.y * scaleY, rect.z - border.z * scaleX, rect.w - border.w * scaleY);
            //middle-center
            vh.AddVert(center + new Vector2(rectInner.x, rectInner.y), color, new Vector2(uvInner.x, uvInner.y));
            vh.AddVert(center + new Vector2(rectInner.x, rectInner.w), color, new Vector2(uvInner.x, uvInner.w));
            vh.AddVert(center + new Vector2(rectInner.z, rectInner.w), color, new Vector2(uvInner.z, uvInner.w));
            vh.AddVert(center + new Vector2(rectInner.z, rectInner.y), color, new Vector2(uvInner.z, uvInner.y));
            TriangleBuffer.Add((offset, offset + 1, offset + 2));
            TriangleBuffer.Add((offset, offset + 2, offset + 3));
            int m = offset + 4, n, t = 0;
            //TODO check for fill region collapsing, when a given size is less than sprite size
            if (border.y > 0) {
                //bottom-center
                vh.AddVert(center + new Vector2(rectInner.x, rect.y), color, new Vector2(uvInner.x, uv.y));
                vh.AddVert(center + new Vector2(rectInner.z, rect.y), color, new Vector2(uvInner.z, uv.y));
                TriangleBuffer.Add((m, offset, offset + 3));
                TriangleBuffer.Add((m, offset + 3, m + 1));
                m += 2;
            }
            if (border.w > 0) {
                //top-center
                vh.AddVert(center + new Vector2(rectInner.x, rect.w), color, new Vector2(uvInner.x, uv.w));
                vh.AddVert(center + new Vector2(rectInner.z, rect.w), color, new Vector2(uvInner.z, uv.w));
                TriangleBuffer.Add((offset + 1, m, m + 1));
                TriangleBuffer.Add((offset + 1, m + 1, offset + 2));
                t = m;
                m += 2;
            }
            if (border.x > 0) {
                //middle-left
                vh.AddVert(center + new Vector2(rect.x, rectInner.y), color, new Vector2(uv.x, uvInner.y));
                vh.AddVert(center + new Vector2(rect.x, rectInner.w), color, new Vector2(uv.x, uvInner.w));
                TriangleBuffer.Add((m, m + 1, offset + 1));
                TriangleBuffer.Add((m, offset + 1, offset));
                n = m + 2;
                if (border.y > 0) {
                    //bottom-left
                    vh.AddVert(center + new Vector2(rect.x, rect.y), color, new Vector2(uv.x, uv.y));
                    TriangleBuffer.Add((n, m, offset));
                    TriangleBuffer.Add((n, offset, offset + 4));
                    n += 1;
                }
                if (border.w > 0) {
                    //top-left
                    vh.AddVert(center + new Vector2(rect.x, rect.w), color, new Vector2(uv.x, uv.w));
                    TriangleBuffer.Add((m + 1, n, t));
                    TriangleBuffer.Add((m + 1, t, offset + 1));
                    n += 1;
                }
                m = n;
            }
            if (border.z > 0) {
                //middle-right
                vh.AddVert(center + new Vector2(rect.z, rectInner.y), color, new Vector2(uv.z, uvInner.y));
                vh.AddVert(center + new Vector2(rect.z, rectInner.w), color, new Vector2(uv.z, uvInner.w));
                TriangleBuffer.Add((offset + 3, offset + 2, m + 1));
                TriangleBuffer.Add((offset + 3, m + 1, m));
                n = m + 2;
                if (border.y > 0) {
                    //bottom-right
                    vh.AddVert(center + new Vector2(rect.z, rect.y), color, new Vector2(uv.z, uv.y));
                    TriangleBuffer.Add((offset + 5, offset + 3, m));
                    TriangleBuffer.Add((offset + 5, m, n));
                    n += 1;
                }
                if (border.w > 0) {
                    //top-right
                    vh.AddVert(center + new Vector2(rect.z, rect.w), color, new Vector2(uv.z, uv.w));
                    TriangleBuffer.Add((offset + 2, t + 1, n));
                    TriangleBuffer.Add((offset + 2, n, m + 1));
                }
            }
            var flip = scaleX * scaleY < 0;
            foreach (var (t0, t1, t2) in TriangleBuffer) {
                if (flip) {
                    vh.AddTriangle(t0, t2, t1);
                }
                else {
                    vh.AddTriangle(t0, t1, t2);
                }
            }
        }
    }
}