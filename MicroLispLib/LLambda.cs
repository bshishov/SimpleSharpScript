using System;
using System.Collections.Generic;

namespace MicroLispLib
{
    /// <summary>
    /// Lambda ShLisp type, TODO: Remove?
    /// </summary>
    public class LLambda : List<ILNode>, ILNode
    {
        public LString Command;

        public LLambda(LString command)
        {
            Command = command;
        }

        public LLambda(LString command, IEnumerable<ILNode> args)
        {
            Command = command;
            AddRange(args);
        }

        public override string ToString()
        {
            return "'(" + Command + " " + string.Join(" ", this) + ")";
        }

        public static explicit operator LLambda(LList val)
        {
            if (val.Count == 0)
                throw new Exception("Unable to cast LList to LLambda, no items in list");
            var first = val[0];
            var cmdList = new LLambda((LString)first);
            cmdList.AddRange(val.GetRange(1, val.Count - 1));
            return cmdList;
        }
    }
}