using System;
using MicroLispLib;

namespace microlisp
{
    class Program
    {
        static void Main(string[] args)
        {
            var lisp = new ShLisp();

            var ast = lisp.Parse(@"
            (
                (fun increment (a b) '(
                    set (get a) ( + (get a) (get b) )
                ))
                

                (set x 1)
                (debug (increment (get x) 2))
                (debug (get x))
            )
            ");


            //(do (get while))
            var res = lisp.Exec(ast);
            Console.Write(false);
        }
    }
}
