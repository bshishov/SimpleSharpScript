using System;

namespace MicroLispLib
{
    public class LBool : ILNode, IComparable<bool>, IEquatable<bool>
    {
        public readonly bool Value;

        public LBool(bool value)
        {
            Value = value;
        }

        public int CompareTo(bool other)
        {
            return Value.CompareTo(other);
        }

        public bool Equals(bool other)
        {
            return Value.Equals(other);
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static explicit operator bool(LBool val)
        {
            return val.Value;
        }

        public static explicit operator LBool(bool val)
        {
            return new LBool(val);
        }
    }
}