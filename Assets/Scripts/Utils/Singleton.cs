using UnityEngine;

/*
* this object has global access.
*/
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour {

    private static T _instance;

    public static T Instance { get { return _instance; } }


    protected virtual void Awake() {

        if (_instance != null && _instance != this) {
            Destroy(gameObject);
        } else {
            _instance = this as T;
        }
    }
}
