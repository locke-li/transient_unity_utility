using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TransformUtility {
    public static Vector2 CheckCenter(RectTransform root) {
        var pivot = root.pivot;
        var rect = root.rect;
        return new Vector2((0.5f - pivot.x) * rect.width, (0.5f - pivot.y) * rect.height);
    }

    public static Vector2 CheckOffsetBiased(RectTransform root, Vector2 direction, float spacing, float count) {
        var pivot = root.pivot;
        var rect = root.rect;
        var normal = new Vector2(direction.y, direction.x);
        var pivotValue = Vector2.Dot(pivot, direction);
        var orthoSize = rect.width * direction.y + rect.height * direction.x;
        var orthoValue = Vector2.Dot(pivot, normal);
        if (orthoValue == 1) {
            orthoValue = -orthoSize;
        }
        else {
            orthoValue = -orthoValue * orthoSize;
        }
        if (pivotValue == 0) {
            return pivotValue * rect.width * direction + orthoValue * normal;
        }
        var pivotSize = rect.width * direction.x + rect.height * direction.y;
        var length = spacing * (count - 1) + pivotSize * count;
        if (pivotValue == 1) {
            return -length * direction + orthoValue * normal;
        }
        return ((0.5f - pivotValue) * pivotSize - length * 0.5f) * direction + orthoValue * normal;
    }
}