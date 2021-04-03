using UnityEngine;
using Transient.DataAccess;
using Transient.Mathematical;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Rendering;

namespace Transient.Graphics {
    public static class GenericGraphics {
        #region Graphics Helper

        [StructLayout(LayoutKind.Explicit, Size = 16)]
        public struct Block128Bit { }

        public static void CopyTextureRawData<Block>(Texture2D atlas_, Texture2D[] texture_, System.Collections.Generic.List<Rect> rect_, int ppb_x, int ppb_y)
        where Block : struct {
            var data = atlas_.GetRawTextureData<Block>();
            var atlas_block = atlas_.width / ppb_x;
            for(int r = 0;r < rect_.Count;++r) {
                var tex = texture_[r];
                var tex_block = tex.width / ppb_x;
                var t = tex.GetRawTextureData<Block>();
                var offset_x = (int)rect_[r].x / ppb_x;
                var offset_y = (int)rect_[r].y / ppb_y;
                for(int y = 0;y<tex.height/ppb_y;++y) {
                    for(int x = 0;x<tex_block;++x) {
                        data[offset_x+x + (offset_y+y)*atlas_block] = t[x+y*tex_block];
                    }
                }
            }
        }

        public static Rect[] PackTexture(out Texture2D atlas_, Texture2D[] texture_, int size_, int padding_) {
            //TODO test on device
            var format = texture_[0].graphicsFormat;
            if(!SystemInfo.IsFormatSupported(format, FormatUsage.Sample)) {
                Log.Warning($"unsupported format {format}");
                atlas_ = new Texture2D(1, 1);
                return atlas_.PackTextures(texture_, padding_, size_);
            }
            var rect = new System.Collections.Generic.List<Rect>(texture_.Length);
            var texArray = texture_.Select(t => new Vector2(t.width, t.height)).ToArray();
            if(!Texture2D.GenerateAtlas(texArray, padding_, size_, rect)) {
                Log.Error("failed to pack textures");
                atlas_ = null;
                return null;
            }
            atlas_ = new Texture2D(size_, size_, format, TextureCreationFlags.None);
            //etc2+eac 8bpp 4x4 = 128bits
            //dxt5/bc3 8bpp 4x4 = 128bits
            //pvrtc 4bpp: 4x4 = 64bits 2bpp: 8x4=64bits
            //astc fixed 128bits
            var blockSize = GraphicsFormatUtility.GetBlockSize(format);
            var blockWidth = (int)GraphicsFormatUtility.GetBlockWidth(format);
            var blockHeight = (int)GraphicsFormatUtility.GetBlockHeight(format);
            if(blockSize == 16) CopyTextureRawData<Block128Bit>(atlas_, texture_, rect, blockWidth, blockHeight);
            else if(blockSize == 8) CopyTextureRawData<UInt64>(atlas_, texture_, rect, blockWidth, blockHeight);
            else if(blockSize == 4) CopyTextureRawData<UInt32>(atlas_, texture_, rect, blockWidth, blockHeight);
            else Log.Warning($"unsupported block size {blockSize} byte");
            atlas_.Apply(false, true);
            float sizeInv = 1f/size_;
            for(int k = 0;k<rect.Count;++k) {
                var r = rect[k];
                rect[k] = new Rect(r.x*sizeInv, r.y*sizeInv, r.width*sizeInv, r.height*sizeInv);
            }
            return rect.ToArray();
        }

        public static void ClearRenderTexture(RenderTexture rt, Color c) {
            RenderTexture rta = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(false, true, c);
            RenderTexture.active = rta;
        }

        #endregion Graphics Helper

#if UNITY_EDITOR

        public static void DrawCross2D (Vector3 c, float s) {
            Gizmos.DrawLine(c - Vector3.right * s, c + Vector3.right * s);
            Gizmos.DrawLine(c - Vector3.up * s, c + Vector3.up * s);
        }

        public static void DrawCross3D (Vector3 c, float s) {
            Gizmos.DrawLine(c - Vector3.right * s, c + Vector3.right * s);
            Gizmos.DrawLine(c - Vector3.up * s, c + Vector3.up * s);
            Gizmos.DrawLine(c - Vector3.forward * s, c + Vector3.forward * s);
        }

#endif
    }
}