using System;
using System.Collections.Generic;
using Transient;
using UnityEngine;

namespace Transient {
    public class CombinedMesh {
        public Transform Asset { get; private set; }
        public List<MeshRenderer> RendererRef { get; private set; }
        public MeshFilter Filter { get; private set; }
        public MeshRenderer Renderer { get; private set; }
        public Mesh Mesh { get; private set; }
        private bool invalid;
        public static string tagFilter = "merge_object";
        private static bool executing;
        private static Queue<(IEnumerable<MeshRenderer>, CombinedMesh)> queue;

        public void Hide(bool value_) {
            value_ = value_ || invalid;
            foreach(var r in RendererRef) {
                r.enabled = value_;
            }
            Renderer.enabled = !value_;
        }

        public void Generate(IEnumerable<MeshRenderer> source, Transform target) {
            if (Asset == null) {
                Asset = target;
                RendererRef = new List<MeshRenderer>(256);
                Filter = Asset.gameObject.AddComponent<MeshFilter>();
                Renderer = Asset.gameObject.AddComponent<MeshRenderer>();
                Mesh = new Mesh();
                Filter.sharedMesh = Mesh;
            }
            queue = queue ?? new Queue<(IEnumerable<MeshRenderer>, CombinedMesh)>();
            //TODO replace existing
            queue.Enqueue((source, this));
        }

        public static void Execute() {
            if (!executing && queue != null && queue.Count > 0) {
                executing = true;
                MainLoop.Coroutine.Execute(Generate);
            }
        }

        private static IEnumerator<CoroutineState> Generate() {
            next:
            IEnumerable<MeshRenderer> source;
            CombinedMesh target;
            (source, target) = queue.Dequeue();
            var rootRenderer = target.Renderer;
            var mesh = target.Mesh;
            var asset = target.Asset;
            var rendererRef = target.RendererRef;
            rendererRef.Clear();
            var materialRef = new List<Material>(4);
            var vertices = new List<Vector3>(4096);
            var normals = new List<Vector3>(4096);
            var uv = new List<Vector3>(4096);
            var triangles = new List<List<int>>(128);
            var vector3Buffer = new List<Vector3>(2048);
            var intBuffer = new List<int>(1024);
            int step = 0;
            foreach (var renderer in source) {
                MeshFilter filter;
                if (renderer == rootRenderer ||
                    !renderer.gameObject.activeSelf ||
                    !renderer.gameObject.CompareTag(tagFilter) ||
                    (filter = renderer.GetComponent<MeshFilter>()) == null ||
                    filter.sharedMesh == null) continue;
                Performance.RecordProfiler(nameof(CombinedMesh));
                rendererRef.Add(renderer);
                var mat = renderer.sharedMaterial;
                var index = -1;
                for (int k = 0; k < materialRef.Count; ++k) {
                    if (materialRef[k] == mat) {
                        index = k;
                        break;
                    }
                }
                if (index < 0) {
                    index = materialRef.Count;
                    materialRef.Add(mat);
                    triangles.Add(new List<int>(1024));
                }
                var vertexStart = vertices.Count;
                var transform = renderer.transform;
                var m = filter.sharedMesh;
                m.GetVertices(vector3Buffer);
                for (int i = 0; i < vector3Buffer.Count; ++i) {
                    var position = transform.TransformPoint(vector3Buffer[i]);
                    vector3Buffer[i] = asset.InverseTransformPoint(position);
                }
                vertices.AddRange(vector3Buffer);
                m.GetNormals(vector3Buffer);
                normals.AddRange(vector3Buffer);
                m.GetUVs(0, vector3Buffer);
                uv.AddRange(vector3Buffer);
                for (int n = 0; n < m.subMeshCount; ++n) {
                    m.GetTriangles(intBuffer, n);
                    for (int i = 0; i < intBuffer.Count; ++i) {
                        intBuffer[i] += vertexStart;
                    }
                    triangles[index].AddRange(intBuffer);
                }
                Performance.End(nameof(CombinedMesh));
                if (++step > 4) {
                    step = 0;
                    yield return CoroutineState.Executing;
                }
            }
            Performance.RecordProfiler(nameof(CombinedMesh));
            try {
                var invalidCount = 0;
                //TODO validate data, for debugging
                foreach (var triList in triangles) {
                    for (int k = 0; k < triList.Count; ++k) {
                        if (triList[k] < 0 || triList[k] >= vertices.Count) {
                            ++invalidCount;
#if !DEBUG
                            goto triangle_invalid;
#endif
                        }
                    }
                }
#if !DEBUG
            triangle_invalid:
#endif
                target.invalid = invalidCount > 0;
                if (target.invalid) {
                    throw new Exception($"{invalidCount} triangle references out of bound vertex");
                }
                mesh.Clear();
                mesh.SetVertices(vertices);
                if (mesh.vertexCount != vertices.Count) {
                    //set failed, errors like the one below may have happened
                    //Mesh.vertices is too small. The supplied vertex array has less vertices than are referenced by the triangles array.
                    throw new Exception("failed SetVertices");
                }
                mesh.SetUVs(0, uv);
                if (normals.Count == vertices.Count) {
                    mesh.SetNormals(normals);
                }
                mesh.subMeshCount = triangles.Count;
                for (int n = 0; n < triangles.Count; ++n) {
                    mesh.SetTriangles(triangles[n], n);
                }
                mesh.UploadMeshData(false);
                rootRenderer.sharedMaterials = materialRef.ToArray();
                target.Hide(false);
            }
            catch (Exception e) {
                //somthing is wrong, disable combined and use loose renderers
                Log.Error($"exception when combining mesh: {e.Message}");
                target.Hide(true);
            }
            Performance.End(nameof(CombinedMesh));
            if (queue.Count > 0) {
                yield return CoroutineState.Executing;
                goto next;
            }
            executing = false;
            yield return CoroutineState.Done;
        }
    }
}