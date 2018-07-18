using UnityEngine;

public class Utils {

    /* <para> magnitude of vector </para>
    *   <para> angle in degrees </para>
    */
    public static Vector2 getVector2(float magnitude, float angleDeg) {
        float angleRad = angleDeg * (Mathf.PI / 180f);
        return magnitude * new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
    }

    public static Bounds2D getBounds(Camera _camera) {
        float height = _camera.orthographicSize;
        float width = height * _camera.aspect;

        return new Bounds2D(
            (Vector2)_camera.transform.position, new Vector2(width, height)
        );
    }
}