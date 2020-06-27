using UnityEngine.UI;
using UnityEngine;

namespace Transient.UI {
    internal static class EventSystemRaycastAdapter {
        public static object Check(Transform transform_, out IEventSystemRaycastAdapter adapter_) {
            var graphic = transform_.GetComponent<Graphic>();
            if(graphic != null) {
                adapter_ = new RaycastAdapterGraphic { _v = graphic };
                return graphic;
            }
            var collider2d = transform_.GetComponent<Collider2D>();
            if(collider2d != null) {
                adapter_ = new RaycastAdapterCollider2D { _v = collider2d };
                return collider2d;
            }
            var collider3d = transform_.GetComponent<Collider>();
            if(collider3d != null) {
                adapter_ = new RaycastAdapterCollider3D { _v = collider3d };
                return collider3d;
            }
            adapter_ = new RaycastAdapterEmpty();
            Log.Warning($"no suitable raycast receiver found on {transform_.name}!");
            return null;
        }
    }

    internal interface IEventSystemRaycastAdapter {
        bool enabled { get; set; }
    }

    internal struct RaycastAdapterEmpty : IEventSystemRaycastAdapter {
        public bool enabled { get; set; }
    }

    internal struct RaycastAdapterGraphic : IEventSystemRaycastAdapter {
        public Graphic _v;
        public bool enabled { get => _v.raycastTarget; set => _v.raycastTarget = value; }
    }

    internal struct RaycastAdapterCollider2D : IEventSystemRaycastAdapter {
        public Collider2D _v;
        public bool enabled { get => _v.enabled; set => _v.enabled = value; }
    }

    internal struct RaycastAdapterCollider3D : IEventSystemRaycastAdapter {
        public Collider _v;
        public bool enabled { get => _v.enabled; set => _v.enabled = value; }
    }
}
