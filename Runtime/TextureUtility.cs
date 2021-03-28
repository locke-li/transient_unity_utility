#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using Transient;

public class TextureUtility {
    private static Material _matCopy;
    private static Material MatCopy {
        get {
            if (_matCopy == null) {
                var shader = Shader.Find("Hidden/Transient/ColorCopy");
                _matCopy = new Material(shader);
            }
            return _matCopy;
        }
    }

    private static int[] NextPoTLookup = new int[] {
        1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384
    };
    public static int NextPoT(int value_) {
        for(int i = 2; i < NextPoTLookup.Length; ++i) {
            if (value_ <= NextPoTLookup[i]) {
                return NextPoTLookup[i];
            }
        }
        var pot = (int)Math.Ceiling(Math.Sqrt(value_));
        pot *= pot;
        Debug.LogWarning($"Next PoT exceeding maximum texture size {value_} {pot}");
        return pot;
    }

    //[MenuItem("Tools/Resize/POT")]
    [ExtendableTool("PoT", "Texture Resize")]
    public static void ResizePOT() {
        AssetDatabase.StartAssetEditing();
        foreach(var tex in Selection.GetFiltered<Texture2D>(SelectionMode.DeepAssets)) {
            int xr = NextPoT(tex.width);
            int yr = NextPoT(tex.height);
            if(xr == tex.width && yr == tex.height)
                continue;
            SaveResized(tex, xr, yr, (xr - tex.width)/2, (yr - tex.height)/2);
        }
        AssetDatabase.StopAssetEditing();
    }

    //[MenuItem("Tools/Resize/4 Multiple")]
    [ExtendableTool("4 Multiple", "Texture Resize")]
    public static void Resize4Multiple() {
        AssetDatabase.StartAssetEditing();
        foreach(var tex in Selection.GetFiltered<Texture2D>(SelectionMode.DeepAssets)) {
            const int N = 4;
            int xr = (N - tex.width % N) % N;
            int yr = (N - tex.height % N) % N;
            if(xr == 0 && yr == 0)
                continue;
            SaveResized(tex, tex.width+xr, tex.height+yr, xr/2, yr/2);
        }
        AssetDatabase.StopAssetEditing();
    }

    //[MenuItem("Tools/Resize/Square")]
    [ExtendableTool("Square", "Texture Resize")]
    public static void ResizeSquare() {
        AssetDatabase.StartAssetEditing();
        foreach(var tex in Selection.GetFiltered<Texture2D>(SelectionMode.DeepAssets)) {
            if(tex.width == tex.height)
                continue;
            int size = tex.width;
            int x = 0, y = 0;
            if(tex.height > tex.width) {
                size = tex.height;
                x = (size - tex.width) / 2;
            }
            else {
                y = (size - tex.height) / 2;
            }
            SaveResized(tex, size, size, x, y);
        }
        AssetDatabase.StopAssetEditing();
    }

    //[MenuItem("Tools/Resize/Preview 128")]
    [ExtendableTool("Preview\n128", "Texture Resize")]
    public static void CreatePreview128() {
        AssetDatabase.StartAssetEditing();
        foreach(var tex in Selection.GetFiltered<Texture2D>(SelectionMode.DeepAssets)) {
            if(tex.width == tex.height)
                continue;
            int size = tex.width;
            Rect drawRect;
            if(tex.height > tex.width) {
                size = tex.height;
                int xr = (size - tex.width) / 2;
                drawRect = new Rect((float)xr / size, 0, (float)tex.width / size, 1);
            }
            else {
                int yr = size - tex.height;
                drawRect = new Rect(0, (float)yr / size, 1, (float)tex.width / size);
            }
            int tsize = 128;
            Texture2D target = new Texture2D(tsize, tsize, TextureFormat.RGBA32, false);
            RenderTexture rt = RenderTexture.GetTemporary(tsize, tsize);
            RenderTexture rta = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(false, true, Color.clear);
            GL.LoadPixelMatrix(0, 1, 1, 0);
            Graphics.DrawTexture(drawRect, tex, MatCopy, 1);//bilinear copy
            target.ReadPixels(new Rect(0, 0, tsize, tsize), 0, 0);
            RenderTexture.active = rta;
            RenderTexture.ReleaseTemporary(rt);
            string projectPath = Application.dataPath.Replace("/Assets", "/");
            string path = AssetDatabase.GetAssetPath(tex) + "_sqprv.png";
            File.WriteAllBytes(projectPath + path, target.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
        AssetDatabase.StopAssetEditing();
    }

    private static void SaveResized(Texture2D tex, int width, int height, int x, int y) {
        var target = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var targetData = target.GetRawTextureData<Color32>();
        Color32 clearColor = Color.clear;
        for(int l = 0; l < height; ++l) {
            for(int k = 0; k < x; ++k) {//left border
                targetData[l * width + k] = clearColor;
            }
            for(int k = tex.width - x; k < width; ++k) {//right border
                targetData[l * width + k] = clearColor;
            }
        }
        for(int k = x; k < width - x; ++k) {
            for(int l = 0; l < y; ++l) {//bottom border
                targetData[l * width + k] = clearColor;
            }
            for(int l = tex.height - y; l < height; ++l) {//top border
                targetData[l * width + k] = clearColor;
            }
        }
        var rt = RenderTexture.GetTemporary(tex.width, tex.height);
        var rta = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(false, true, Color.clear);
        GL.LoadPixelMatrix(0, 1, 1, 0);
        Graphics.DrawTexture(new Rect(0, 0, 1, 1), tex, MatCopy, 0);//point copy
        target.ReadPixels(new Rect(0, 0, tex.width, tex.height), x, y);
        RenderTexture.active = rta;
        RenderTexture.ReleaseTemporary(rt);
        string projectPath = Application.dataPath.Replace("/Assets", "/");
        string path = AssetDatabase.GetAssetPath(tex);
        File.WriteAllBytes(projectPath + path, target.EncodeToPNG());
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }

    //[MenuItem("Tools/Resize/Split 1024 4n")]
    [ExtendableTool("Split 1024 4n", "Texture Resize")]
    private static void Split4n() {
        const int MAX_SIZE = 1024;
        const int N = 4;
        const int MIN_SIZE = N*4;
        foreach(var tex in Selection.GetFiltered<Texture2D>(SelectionMode.DeepAssets)) {
            var x = tex.width / MAX_SIZE;
            var y = tex.height / MAX_SIZE;
            if(x == 0 && y == 0) continue;
            var xr = tex.width % MAX_SIZE;
            var yr = tex.height % MAX_SIZE;
            var hx = xr>0 && xr<MIN_SIZE ? 1 : 0;
            var hy = yr>0 && yr<MIN_SIZE ? 1 : 0;
            x -= hx;
            y -= hy;
            for(int m = 0;m<x;++m) {
                for(int n = 0;n<y;++n) {
                    SaveSplitted($"{m}_{n}", tex, m*MAX_SIZE, n*MAX_SIZE, MAX_SIZE, MAX_SIZE, 0, 0, 0, 0);
                }
            }
            if(xr == 0 && yr == 0) continue;
            var xf = (N - xr % N) % N;
            var yf = (N - yr % N) % N;
            xr += xf;
            yr += yf;
            var xEdge = x*MAX_SIZE;
            var yEdge = y*MAX_SIZE;
            var xOuterEdge = xEdge;
            var yOuterEdge = yEdge;
            var halfSize = MAX_SIZE/2;
            var xSizeOffset = halfSize*hx;
            var ySizeOffset = halfSize*hy;
            for(int h = 0;h < hy;++h) {
                for(int k = 0;k < x;++k) {
                    SaveSplitted($"x1_{k}", tex, k*MAX_SIZE, yEdge, MAX_SIZE, halfSize, 0, 0, 0, 0);
                }
                yOuterEdge += halfSize;
            }
            for(int k = 0;k < x;++k) {
                SaveSplitted($"x0_{k}", tex, k*MAX_SIZE, yOuterEdge, MAX_SIZE, yr + ySizeOffset, 0, yf, 0, 0);
            }
            for(int k = 0;k < hx;++k) {
                SaveSplitted($"x0_{k+x}", tex, xEdge + k*halfSize, yOuterEdge, halfSize, yr + ySizeOffset, 0, yf, 0, 0);
            }
            for(int h = 0;h < hx;++h) {
                for(int k = 0;k < y;++k) {
                    SaveSplitted($"y1_{k}", tex, xEdge, k*MAX_SIZE, halfSize, MAX_SIZE, 0, 0, 0, 0);
                }
                xOuterEdge += halfSize;
            }
            for(int k = 0;k < y;++k) {
                SaveSplitted($"y0_{k}", tex, xOuterEdge, k*MAX_SIZE, xr + xSizeOffset, MAX_SIZE, xf, 0, 0, 0);
            }
            for(int k = 0;k < hy;++k) {
                SaveSplitted($"y0_{k+y}", tex, xOuterEdge, yEdge + k*halfSize, xr + xSizeOffset, halfSize, xf, 0, 0, 0);
            }
            for(int k = 0;k < hx*hy;++k) {
                SaveSplitted($"z1_{k}", tex, xEdge + k*halfSize, yEdge + k*halfSize, halfSize, halfSize, 0, 0, 0, 0);
            }
            SaveSplitted($"z0", tex, xOuterEdge, yOuterEdge, xr + xSizeOffset, yr + ySizeOffset, xf, yf, 0, 0);
        }
    }

    private static void SaveSplitted(string suffix, Texture2D tex, int xSrc, int ySrc, int width, int height,
        int xOffset, int yOffset, int x, int y) {
        Texture2D target = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var widthSrc = width-xOffset;
        var heightSrc = height-yOffset;
        Graphics.CopyTexture(tex, 0, 0, xSrc, ySrc, widthSrc, heightSrc, target, 0, 0, x, y);
        for(int k = 0;k < height && xOffset > 0;++k) {
            var c = target.GetPixel(widthSrc-1, k);
            for(int kx = widthSrc;kx < width;++kx) {
                target.SetPixel(kx, k, c);
            }
        }
        for(int k = 0;k < width && yOffset > 0;++k) {
            var c = target.GetPixel(k, heightSrc-1);
            for(int ky = heightSrc;ky < height;++ky) {
                target.SetPixel(k, ky, c);
            }
        }
        target.Apply();
        string projectPath = Application.dataPath.Replace("/Assets", "/");
        string path = AssetDatabase.GetAssetPath(tex);
        path = $"{path.Substring(0, path.LastIndexOf('.'))}_{suffix}.png";
        File.WriteAllBytes(projectPath + path, target.EncodeToPNG());
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }
}
#endif