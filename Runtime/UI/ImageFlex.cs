﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace Transient.UI {
    public class ImageFlex : Image {
        public bool hidden;
        public bool raycast;

        protected override void Awake() {
            base.Awake();
            raycast = raycastTarget;
        }

        public void Hide(bool value) {
            if (hidden == value) return;
            hidden = value;
            raycastTarget = raycast && !hidden;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh) {
            if (hidden) {
                vh.Clear();
                return;
            }
            base.OnPopulateMesh(vh);
        }
    }
}