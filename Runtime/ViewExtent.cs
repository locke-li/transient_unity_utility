#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Transient {
    public class ViewExtent : MonoBehaviour {
        public Vector2 origin;
        public Vector2 center;
        public Vector2 extent;
        public float extend;
        Vector3 zero;
        Vector3 axisX;
        Vector3 axisY;
        Vector3 axisZ;

        private void OnValidate() {
            zero = Vector3.zero;
            axisX = transform.right;
            axisY = transform.up;
            axisZ = transform.forward;
        }

        private Vector3 SystemToWorld(Vector2 pos) {
            return zero + pos.x * axisX + pos.y * axisY;
        }

        private void OnDrawGizmos() {
            var pos = SystemToWorld(center);
            var sizeX = extent.x * axisX;
            var sizeY = extent.y * axisY;
            Gizmos.DrawLine(pos + sizeX + sizeY, pos + sizeX - sizeY);
            Gizmos.DrawLine(pos - sizeX - sizeY, pos + sizeX - sizeY);
            Gizmos.DrawLine(pos - sizeX - sizeY, pos - sizeX + sizeY);
            Gizmos.DrawLine(pos + sizeX + sizeY, pos - sizeX + sizeY);
            sizeX += 0.3f * extend * axisX;
            sizeY += 0.3f * extend * axisY;
            Gizmos.color = new Color(0.2f, 0.8f, 0.8f);
            Gizmos.DrawLine(pos + sizeX + sizeY, pos + sizeX - sizeY);
            Gizmos.DrawLine(pos - sizeX - sizeY, pos + sizeX - sizeY);
            Gizmos.DrawLine(pos - sizeX - sizeY, pos - sizeX + sizeY);
            Gizmos.DrawLine(pos + sizeX + sizeY, pos - sizeX + sizeY);
            pos = SystemToWorld(origin);
            Gizmos.color = Color.black;
            sizeX = 4 * axisX * 0.5f;
            sizeY = 2 * axisY * 0.5f;
            Gizmos.DrawLine(pos + sizeX + sizeY, pos + sizeX - sizeY);
            Gizmos.DrawLine(pos - sizeX - sizeY, pos + sizeX - sizeY);
            Gizmos.DrawLine(pos - sizeX - sizeY, pos - sizeX + sizeY);
            Gizmos.DrawLine(pos + sizeX + sizeY, pos - sizeX + sizeY);
        }
    }
}
#endif