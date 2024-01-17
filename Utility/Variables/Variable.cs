using UnityEngine;

public class Variable<T> : ScriptableObject
{
    [SerializeField] private T value;

    public T Value
    {
        get { return value; }
        set { this.value = value; }
    }

    public static implicit operator T(Variable<T> value) {
        return value.value;
    }
}
