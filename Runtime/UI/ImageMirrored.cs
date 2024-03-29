﻿using UnityEngine.Sprites;
using UnityEngine;
using UnityEngine.UI;
using Transient.Container;

namespace Transient.UI {
    public class ImageMirrored : Graphic {
        //NOTE both sprites need to be on the same texture
        public Sprite mirrored;
        public Sprite middle;
        public override Texture mainTexture =>
            middle != null ? middle.texture :
            mirrored != null ? mirrored.texture : base.mainTexture;

        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();
            //TODO extend direction
            var offset = new Vector2(0, 0);
            var width = 0f;
            var height = rectTransform.rect.height * 0.5f;
            var center = TransformUtility.CheckCenter(rectTransform);
            var centerLocal = center;
            Vector4 uv, uvInner;
            Vector4 rect, border;
            if (middle != null) {
                width = middle.rect.width * 0.5f;
                offset = new Vector2(width, 0);
                uv = DataUtility.GetOuterUV(middle);
                uvInner = DataUtility.GetInnerUV(middle);
                rect = new Vector4(-width, -height, width, height);
                if (uv == uvInner) {
                    MeshUtility.AddSimple(vh, color, centerLocal, rect, uv);
                }
                else {
                    border = middle.border;
                    MeshUtility.AddSliced(vh, color, centerLocal, rect, uv, uvInner, border);
                }
            }
            width = rectTransform.rect.width * 0.5f - width;
            //handle width < 0
            if (mirrored == null || width <= 0) return;
            //Debug.Log($"{width}");
            uv = DataUtility.GetOuterUV(mirrored);
            uvInner = DataUtility.GetInnerUV(mirrored);
            rect = new Vector4(-width, -height, 0, height);
            if (uv == uvInner) {
                centerLocal = center - offset;
                MeshUtility.AddSimple(vh, color, centerLocal, rect, uv);
                centerLocal = center + offset;
                rect.x = -rect.x;
                MeshUtility.AddSimple(vh, color, centerLocal, rect, uv);
            }
            else {
                border = mirrored.border;
                centerLocal = center - offset;
                MeshUtility.AddSliced(vh, color, centerLocal, rect, uv, uvInner, border);
                centerLocal = center + offset;
                rect.x = -rect.x;
                MeshUtility.AddSliced(vh, color, centerLocal, rect, uv, uvInner, border);
            }
        }
    }
}