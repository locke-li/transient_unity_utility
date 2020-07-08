using UnityEngine;

namespace Transient
{
    public interface ICoordinateSystem {
        Vector3 AxisX { get; set; }
        Vector3 AxisY { get; set; }
        Vector3 AxisZ { get; set; }
        Vector2 PositionXY { get; set; }
        Vector3 Position { get; set; }
        Vector3 WorldPosition { get; set; }
        float X { get; set; }
        float Y { get; set; }
        float Z { get; set; }

        Vector3 SystemToWorld(float x, float y, float z);
        Vector3 SystemToWorld(Vector2 pos, float z);

        Vector2 WorldToSystemXY(Vector3 pos);
    }

    public class WorldCoordinateSystem : ICoordinateSystem {
        public Vector3 AxisX { get; set; }
        public Vector3 AxisY { get; set; }
        public Vector3 AxisZ { get; set; }
        public Vector2 PositionXY {
            get => new Vector2(X, Y);
            set { X = value.x; Y = value.y; }
        }
        public Vector3 Position {
            get => new Vector3(X, Y, Z);
            set { X = value.x; Y = value.y; Z = value.z; }
        }
        public Vector3 WorldPosition {
            get => new Vector3(X, Y, Z);
            set { X = value.x; Y = value.y; Z = value.z; }
        }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Vector3 SystemToWorld(float x, float y, float z) => new Vector3(x, y, z);
        public Vector3 SystemToWorld(Vector2 pos, float z) => SystemToWorld(pos.x, pos.y, z);

        public Vector2 WorldToSystemXY(Vector3 pos) => pos;
    }

    public class FlexibleCoordinateSystem : ICoordinateSystem {
        public Vector3 AxisX { get; set; }
        public Vector3 AxisY { get; set; }
        public Vector3 AxisZ { get; set; }
        public Vector2 PositionXY {
            get => new Vector2(X, Y);
            set { X = value.x; Y = value.y; }
        }
        public Vector3 Position {
            get => new Vector3(X, Y, Z);
            set { X = value.x; Y = value.y; Z = value.z; }
        }
        public Vector3 WorldPosition {
            get => SystemToWorld(X, Y, Z);
            set {
                X = Vector3.Dot(value, AxisX);
                Y = Vector3.Dot(value, AxisY);
                Z = Vector3.Dot(value, AxisZ);
            }
        }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Vector3 SystemToWorld(float x, float y, float z) {
            return AxisX * x + AxisY * y + AxisZ * z;
        }
        public Vector3 SystemToWorld(Vector2 pos, float z) => SystemToWorld(pos.x, pos.y, z);

        public Vector2 WorldToSystemXY(Vector3 pos) {
            return new Vector2(
                Vector3.Dot(pos, AxisX),
                Vector3.Dot(pos, AxisY)
            );
        }
    }
}
