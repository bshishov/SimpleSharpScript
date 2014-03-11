using System;

namespace MicroLispLib
{
    public interface ILNumeric : ILNode, IComparable<ILNumeric>
    {
        float ToFloat();
    }
}
