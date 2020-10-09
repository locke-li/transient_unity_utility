using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TransformUtility {
    public static Vector2 CheckCenter(RectTransform root) {
        var pivot = root.pivot;
        var rect = root.rect;
        return new Vector2((0.5f - pivot.x) * rect.width, (0.5f - pivot.y) * rect.height);
    }

    public static Vector2 CheckOffsetBiased(RectTransform root, float spacing, int count) {
        var pivot = root.pivot;
        var rect = root.rect;
        var y = (0.5f - pivot.y) * rect.height;
        if (pivot.x == 0) {
            return new Vector2(0, y);
        }
        var size = root.sizeDelta.x;
        size = spacing * (count - 1) + size * count;
        if (pivot.x == 1) {
            return new Vector2(-size, y);
        }
        return new Vector2(
            (0.5f - pivot.x) * rect.width - size * 0.5f,
            y
        );
    }
}
