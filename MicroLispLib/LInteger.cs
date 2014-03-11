using System;

namespace MicroLispLib
{
    public class LInteger : ILNumeric, IEquatable<int>
    {
        public readonly int Value;

        public LInteger(int value)
        {
            Value = value;
        }

        public int CompareTo(ILNumeric other)
        {
            return ((float)Value).CompareTo(other.ToFloat());
        }

        public bool Equals(int other)
        {
            return Value.Equals(other);
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public float ToFloat()
        {
            return Value;
        }

        public static explicit operator int(LInteger val)
        {
            return val.Value;
        }

        public static explicit operator LInteger(int val)
        {
            return new LInteger(val);
        }

        public static explicit operator LFloat(LInteger val)
        {
            return new LFloat((float)val);
        }
    }
}