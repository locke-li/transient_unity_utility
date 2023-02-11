using UnityEngine;

namespace Transient
{
    public abstract class AbstractCoordinateSystem
    {
        public Vector3 AxisX { get; set; }
        public Vector3 AxisY { get; set; }
        public Vector3 AxisZ { get; set; }
        public Vector2 PositionXY
        {
            get => new Vector2(X, Y);
            set { X = value.x; Y = value.y; }
        }
        public Vector3 Position
        {
            get => new Vector3(X, Y, Z);
            set { X = value.x; Y = value.y; Z = value.z; }
        }
        public abstract Vector3 WorldPosition { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public abstract Vector3 SystemToWorld(float x, float y, float z);
        public Vector3 SystemToWorld(Vector2 pos, float z) => SystemToWorld(pos.x, pos.y, z);

        public abstract Vector2 WorldToSystemXY(Vector3 pos);

        public void SyncPosition(Transform t_) {
            if (t_ == null) return;
            WorldPosition = t_.position;
        }
    }

    public class WorldCoordinateSystem : AbstractCoordinateSystem {
        public override Vector3 WorldPosition {
            get => new Vector3(X, Y, Z);
            set { X = value.x; Y = value.y; Z = value.z; }
        }

        public override Vector3 SystemToWorld(float x, float y, float z) => new Vector3(x, y, z);
        public override Vector2 WorldToSystemXY(Vector3 pos) => pos;
    }

    public class FlexibleCoordinateSystem : AbstractCoordinateSystem {
        public override Vector3 WorldPosition {
            get => SystemToWorld(X, Y, Z);
            set {
                X = Vector3.Dot(value, AxisX);
                Y = Vector3.Dot(value, AxisY);
                Z = Vector3.Dot(value, AxisZ);
            }
        }

        public override Vector3 SystemToWorld(float x, float y, float z) {
            return AxisX * x + AxisY * y + AxisZ * z;
        }

        public override Vector2 WorldToSystemXY(Vector3 pos) {
            return new Vector2(
                Vector3.Dot(pos, AxisX),
                Vector3.Dot(pos, AxisY)
            );
        }
    }

    public class ScaledCoordinateSystem : AbstractCoordinateSystem {
        public float ScaleX { get; set; } = 1;
        public float ScaleY { get; set; } = 1;
        public float ScaleZ { get; set; } = 1;

        public override Vector3 WorldPosition {
            get => SystemToWorld(X, Y, Z);
            set {
                X = Vector3.Dot(value, AxisX) / ScaleX;
                Y = Vector3.Dot(value, AxisY) / ScaleY;
                Z = Vector3.Dot(value, AxisZ) / ScaleZ;
            }
        }

        public override Vector3 SystemToWorld(float x, float y, float z) {
            return AxisX * x * ScaleX + AxisY * y * ScaleY + AxisZ * z * ScaleZ;
        }

        public override Vector2 WorldToSystemXY(Vector3 pos) {
            return new Vector2(
                Vector3.Dot(pos, AxisX) / ScaleX,
                Vector3.Dot(pos, AxisY) / ScaleY
            );
        }
    }
}
