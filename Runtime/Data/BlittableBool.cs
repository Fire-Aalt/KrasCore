using System;

namespace KrasCore
{
    public readonly struct BlittableBool : IEquatable<BlittableBool>
    {
        private readonly byte _value;

        public BlittableBool(bool value)
        {
            this._value = Convert.ToByte(value);
        }

        public static implicit operator bool(BlittableBool blittableBool)
        {
            return blittableBool._value != 0;
        }

        public static implicit operator BlittableBool(bool value)
        {
            return new BlittableBool(value);
        }

        public bool Equals(BlittableBool other)
        {
            return _value == other._value;
        }
        
        public override bool Equals(object obj)
        {
            return obj is BlittableBool other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value;
        }
        
        public static bool operator ==(BlittableBool left, BlittableBool right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BlittableBool left, BlittableBool right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return ((bool) this).ToString();
        }
    }
}