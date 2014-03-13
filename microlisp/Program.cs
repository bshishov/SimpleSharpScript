using System;
using System.Diagnostics;
using MicroLispLib;

namespace microlisp
{
    class Program
    {
        static void Main(string[] args)
        {
            var lisp = new ShLisp();
            var definitions = lisp.Parse(@"(
            (fun '++ '(set _0 (+ (eval _0) 1)))                                       
            (fun 'for '(
                while '(eval _0) '((eval _2) (eval _1))     
            ))
            )");
            lisp.Eval(definitions);

            var script = lisp.Parse(@"(                
                (set 'x 0)            
                (set 'y 0)            
                (for '(< x 2000) '(++ 'x) '(++ 'y))
                (print y)
            )");

            var s = new Stopwatch();
            s.Start();
            var res = lisp.Eval(script);    
            Console.Write(s.ElapsedMilliseconds + "ms");
            s.Stop();
            Console.ReadKey();
        }
    }
}
