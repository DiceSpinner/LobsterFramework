
namespace LobsterFramework.Utility
{
    /// <summary>
    /// Represents a signal that can be queried for value. It will go automatically go back to default state every time after queried.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Signal<T> where T : struct
    {
        private readonly T defaultValue;
        private T value;

        public Signal(T defaultValue) { this.defaultValue = defaultValue; value = defaultValue; }
        public Signal() { defaultValue = default; value = default; }

        public void Put(T newValue) {
            value = newValue;
        }

        public void Reset() { value = defaultValue; }

        public static implicit operator T(Signal<T> signal) {
            T ret = signal.value;
            signal.value = signal.defaultValue;
            return ret;
        }
    }
}
