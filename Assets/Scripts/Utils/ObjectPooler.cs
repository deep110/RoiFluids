using System.Collections.Generic;


public class ObjectPooler<T> where T: new() {

    public int FreeSize { get { return freeObjects.Count; } }

    private List<T> freeObjects;

    public ObjectPooler(int amount) {
        init(amount);
    }

    public T GetObject() {
        if (freeObjects == null) return default(T);

        T pooledObject;
        if (freeObjects.Count > 0) {
            pooledObject = freeObjects[0];
            freeObjects.RemoveAt(0);
        } else {
            // add 16 elements to free objects
            for (int i = 0; i < 16; i++) {
                freeObjects.Add(new T());
            }
            pooledObject = new T();
        }

        return pooledObject;
    }

    public void Recycle(T _object) {
        freeObjects.Add(_object);
    }

    public override string ToString() {
        return string.Format("Pool Size: {0}", FreeSize);
    }

    private void init(int amount) {
        if (amount < 1) {
            return;
        }

        freeObjects = new List<T>(amount);
        for (int i = 0; i < amount; i++) {
            freeObjects.Add(new T());
        }
    }
}
