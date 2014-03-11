using System.Collections.Generic;

namespace MicroLispLib
{
    /// <summary>
    /// Just the list of ShLisp elements between brackets ()
    /// </summary>
    public class LList : List<ILNode>, ILNode
    {
        internal bool Quoted = false;

        public override string ToString()
        {
            return "(" + string.Join(" ", this) + ")";
        }
    }
}