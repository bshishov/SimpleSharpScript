using System.Collections.Generic;

namespace MicroLispLib
{
    /// <summary>
    /// Just the list of ShLisp elements between brackets ()
    /// </summary>
    public class LList : List<ILNode>, ILNode
    {
        public bool Quoted { get; private set; }

        public LList(bool quoted = false)
        {
            Quoted = quoted;
        }

        public override string ToString()
        {
            return "(" + string.Join(" ", this) + ")";
        }
    }
}