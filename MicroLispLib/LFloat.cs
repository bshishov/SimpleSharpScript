using System;

namespace MicroLispLib
{
    public class LFloat : ILNumeric, IEquatable<float>
    {
        public readonly float Value;

        public LFloat(float value)
        {
            Value = value;
        }

        public bool Equals(float other)
        {
            return Value.Equals(other);
        }

        public int CompareTo(float other)
        {
            return Value.CompareTo(other);
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public int CompareTo(ILNumeric value)
        {
            return Value.CompareTo(value.ToFloat());
        }

        public float ToFloat()
        {
            return Value;
        }

        public static explicit operator float(LFloat val)
        {
            return val.Value;
        }

        public static explicit operator LFloat(float val)
        {
            return new LFloat(val);
        }
    }
}