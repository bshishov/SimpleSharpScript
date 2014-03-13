using System;

namespace MicroLispLib
{
    class LIdentifier : ILNode
    {
        public readonly string Value;
        public readonly bool Quoted;

        public LIdentifier(string value, bool quoted = false)
        {
            Value = value;
            Quoted = quoted;
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
            return Value;
        }

        public static explicit operator string(LIdentifier val)
        {
            return val.Value;
        }

        public static explicit operator LIdentifier(String val)
        {
            return new LIdentifier(val);
        }
    }
}
