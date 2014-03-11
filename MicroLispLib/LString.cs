using System;

namespace MicroLispLib
{
    public class LString : ILNode, IComparable<string>, IEquatable<string>
    {
        public readonly string Value;

        public LString(string value)
        {
            Value = value;
        }

        public int CompareTo(string other)
        {
            return Value.CompareTo(other);
        }

        public bool Equals(string other)
        {
            return Value.Equals(other);
        }

        public override string ToString()
        {
            return String.Format("\"{0}\"",Value);
        }

        public static explicit operator string(LString val)
        {
            return val.Value;
        }

        public static explicit operator LString(string val)
        {
            return new LString(val);
        }
    }
}