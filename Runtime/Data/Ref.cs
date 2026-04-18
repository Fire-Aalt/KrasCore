using System;
using Unity.Collections.LowLevel.Unsafe;

namespace KrasCore
{
    public readonly unsafe struct Ref<T> : IEquatable<Ref<T>>
        where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        private readonly T* _value;

        /// <summary> Initializes a new instance of the <see cref="Ref{T}" /> struct. </summary>
        /// <param name="value"> The pointer to hold. </param>
        public Ref(T* value)
        {
            _value = value;
        }

        /// <summary> Initializes a new instance of the <see cref="Ref{T}" /> struct. </summary>
        /// <param name="value"> The pointer to hold. </param>
        public Ref(ref T value)
        {
            _value = (T*)UnsafeUtility.AddressOf(ref value);
        }

        public bool IsCreated => _value != null;

        public ref T Value => ref UnsafeUtility.AsRef<T>(_value);

        public T* GetUnsafePtr() => _value;
        
        public static implicit operator T*(Ref<T> node)
        {
            return node._value;
        }

        public static implicit operator Ref<T>(T* ptr)
        {
            return new Ref<T>(ptr);
        }

        public static bool operator ==(Ref<T> left, Ref<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Ref<T> left, Ref<T> right)
        {
            return !left.Equals(right);
        }

        /// <inheritdoc />
        public bool Equals(Ref<T> other)
        {
            return _value == other._value;
        }

        public override bool Equals(object obj)
        {
            return obj is Ref<T> other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return unchecked((int)(long)_value);
        }
    }
}