using UnityEngine;

public struct Bounds2D {
    public Vector2 min;
    public Vector2 max;

    public Bounds2D(Vector2 center, Vector2 size) {
        this.max = new Vector2(center.x + size.x, center.y + size.y);
        this.min = new Vector2(center.x - size.x, center.y - size.y);
    }

    public bool isPointOutside(Vector2 point) {
        return (point.x > max.x || point.y > max.y || point.x < min.x || point.y < min.y);
    }

    public override string ToString() {
        return "[" + min + "," + max + "]";
    }
}