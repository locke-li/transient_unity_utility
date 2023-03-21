#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Transient {
    public class ViewExtent : MonoBehaviour {
        public float plane;
        public float height;
        public Vector2 origin;
        public Vector2 center;
        public Vector2 extent;
        public float extend;
        private Vector3 viewAxisX, viewAxisY, viewAxisZ;
        public Camera target;
        private ScaledCoordinateSystem system;
        private OffsetByYRelative offset;
        private Transform tr;

        private void OnValidate() {
            target = target ?? GetComponent<Camera>();
            tr = target != null ? target.transform : transform;
            system = system ?? new();
            offset = offset ?? new();
            offset.Init(tr, height);
            var y = tr.rotation.eulerAngles.y;
            var axisRotation = Quaternion.AngleAxis(y, Vector3.up);
            system.AxisX = axisRotation * Vector3.right;
            system.AxisY = axisRotation * Vector3.forward;
            system.AxisZ = Vector3.up;
            system.ScaleX = 1;
            system.ScaleY = 2;
            system.ScaleZ = 1;
            var pos = SystemToWorld(origin);
            system.WorldPosition = pos;
            transform.position = pos;
            viewAxisX = tr.right;
            viewAxisY = tr.up;
            viewAxisZ = tr.forward;
        }

        private Vector3 SystemToWorld(Vector2 pos) {
            pos = system.ApplyOffset(pos, offset.Calculate(0));
            return system.SystemToWorld(pos, height);
        }

        private void DrawRect(Vector3 pos, Vector3 sizeX, Vector3 sizeY) {
            Gizmos.DrawLine(pos + sizeX + sizeY, pos + sizeX - sizeY);
            Gizmos.DrawLine(pos - sizeX - sizeY, pos + sizeX - sizeY);
            Gizmos.DrawLine(pos - sizeX - sizeY, pos - sizeX + sizeY);
            Gizmos.DrawLine(pos + sizeX + sizeY, pos - sizeX + sizeY);
        }

        private Vector3 ProjXZ(Vector3 p, float h_)
            => new Vector3(Vector3.Dot(p, Vector3.right), h_,  Vector3.Dot(p, Vector3.forward));
        private float Offset(Vector3 p, float h_) {
            return offset.CalculateRelative(p.y - h_).y;
        }

        private Vector3 ProjView(Vector3 p, float h_) {
            var pp = ProjXZ(p, h_);
            return pp + system.AxisY * Offset(p, h_);
        }

        private void DrawRectProj(Vector3 pos, Vector3 sizeX, Vector3 sizeY) {
            pos = ProjView(pos, plane);
            sizeX = ProjView(sizeX, 0);
            sizeY = ProjView(sizeY, 0);
            DrawRect(pos, sizeX, sizeY);
        }

        private void OnDrawGizmos() {
            var pos = SystemToWorld(center);
            var sizeX = extent.x * viewAxisX;
            var sizeY = extent.y * viewAxisY;
            DrawRect(pos, sizeX, sizeY);
            DrawRectProj(pos, sizeX, sizeY);
            sizeX += 0.3f * extend * viewAxisX;
            sizeY += 0.3f * extend * viewAxisY;
            Gizmos.color = new Color(0.2f, 0.8f, 0.8f);
            DrawRect(pos, sizeX, sizeY);
            DrawRectProj(pos, sizeX, sizeY);
            var diff = pos - tr.position;
            pos = tr.position + Vector3.Dot(diff, viewAxisZ) * viewAxisZ;
            Gizmos.color = Color.black;
            var viewY = 2f;
            float viewX;
            if (target != null) {
                viewY = target.orthographicSize;
                viewX = viewY * target.pixelWidth / target.pixelHeight;
            }
            else {
                viewX = viewY * Screen.width / Screen.height;
            }
            sizeX = viewX * viewAxisX;
            sizeY = viewY * viewAxisY;
            DrawRect(pos, sizeX, sizeY);
            DrawRectProj(pos, sizeX, sizeY);
        }
    }
}
#endif