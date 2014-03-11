using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MicroLispLib
{
    public class ShLisp
    {
        public readonly static LNull LNullVal = new LNull();
        public readonly static LBool LTrue = new LBool(true);
        public readonly static LBool LFalse = new LBool(false);
        private readonly Dictionary<int, Dictionary<string, Delegate>> _funcs;
        private readonly Dictionary<string, ILNode> _globals;
        private readonly Dictionary<string, Tuple<LList,LLambda>> _funs;
        private readonly Dictionary<string, Action<string, ILNode>> _events;

        public ShLisp()
        {
            _funcs = new Dictionary<int, Dictionary<string, Delegate>>();
            _globals = new Dictionary<string, ILNode>();
            _funs = new Dictionary<string, Tuple<LList, LLambda>>();
            _events = new Dictionary<string, Action<string, ILNode>>();

            Bind<LLambda, ILNode>("do", DoClause);
            Bind<ILNode, ILNode, ILNode, ILNode>("if", IfClause);
            Bind<LString, ILNode, ILNode>("set", Set);
            Bind<LString, ILNode>("get", Get);
            Bind<LString, ILNode>("rem", Rem);
            Bind<ILNode, ILNode, ILNode>("+", Add);
            Bind<ILNode, ILNode, ILNode>("add", Add);

            Bind<ILNode, ILNode, ILNode>("-", Sub);
            Bind<ILNode, ILNode, ILNode>("sub", Sub);

            Bind<ILNode, ILNode, ILNode>("*", Mul);
            Bind<ILNode, ILNode, ILNode>("mul", Mul);

            Bind<ILNode, ILNode, ILNode>("/", Div);
            Bind<ILNode, ILNode, ILNode>("div", Div);

            Bind<ILNode, ILNode, ILNode>("and", And);
            Bind<ILNode, ILNode, ILNode, ILNode>("and", And);
            Bind<ILNode, ILNode, ILNode>("&", And);
            Bind<ILNode, ILNode, ILNode, ILNode>("&", And);
            Bind<ILNode, ILNode, ILNode>("or", Or);
            Bind<ILNode, ILNode, ILNode, ILNode>("or", Or);
            Bind<ILNode, ILNode, ILNode>("|", Or);
            Bind<ILNode, ILNode, ILNode, ILNode>("|", Or);

            Bind<ILNode, ILNode, ILNode>("=", Eq);
            Bind<ILNode, ILNode, ILNode>("<", Less);
            Bind<ILNode, ILNode, ILNode>("<=", LessEq);
            Bind<ILNode, ILNode, ILNode>(">", Greater);
            Bind<ILNode, ILNode, ILNode>(">=", GreaterEq);
            Bind<ILNode, ILNode, ILNode>("!=", NotEq);

            Bind<ILNode, ILNode>("pass", Pass);
            Bind<LString, LNull>("print", Print);
            Bind<ILNode, LNull>("debug", PrintRepr);
            Bind<LString, LList, LLambda, LNull>("fun", FunFake);
        }

        /// <summary>
        /// Converts string to ast using only LList and basic atoms
        /// such as (LInteger, LBool, LNull, etc.)
        /// </summary>
        /// <param name="input">Input string</param>
        /// <returns>Ast tree</returns>
        public ILNode Parse(string input)
        {
            input = input.Trim();

            if (input.Length == 0)
                return LNullVal;

            var quoted = false;
            if (input.Length >= 3 && input[0] == '\'' && input[1] == '(')
            {
                quoted = true;
                input = input.Substring(1);
            }

            if (input[0] == '(' && input[input.Length - 1] == ')')
            {
                if (input.Length == 2)
                    return new LList();

                var depth = 0;
                char ch;
                var lastComma = 1;
                var lastWasWhitespace = false;
                var ignorance = false;
                var list = new LList();
                for (var i = 1; i < input.Length - 2; i++)
                {
                    ch = input[i];
                    if (ch == '(') depth++;
                    if (ch == ')') depth--;
                    if (ch == '"') ignorance = !ignorance;
                    if (ignorance)
                    {
                        lastWasWhitespace = false;
                        continue;
                    }
                    if (ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n')
                    {
                        if (!lastWasWhitespace && depth == 0)
                        {
                            var node = Parse(input.Substring(lastComma, i - lastComma));
                            if(!(node is LNull))
                                list.Add(node);
                            lastComma = i;
                        }
                        lastWasWhitespace = true;
                    }
                    else
                    {
                        lastWasWhitespace = false;
                    }
                }

                if (lastComma != input.Length - 1)
                {
                    var node = Parse(input.Substring(lastComma, input.Length - 1 - lastComma));
                    if (!(node is LNull))
                        list.Add(node);
                }

                list.Quoted = quoted;
                return list;
            }

            if (input == "null")
                return LNullVal;

            int intval;
            if (int.TryParse(input, out intval))
                return new LInteger(intval);

            float floatval;
            if (float.TryParse(input, out floatval))
                return new LFloat(floatval);

            bool boolval;
            if (bool.TryParse(input, out boolval))
                return new LBool(boolval);

            if ( input[0] == '"')
                input = input.Substring(1, input.Length - 2);
            return new LString(input);
        }

        private ILNode Dofunction(string cmd, List<ILNode> args)
        {
            if (HasFun(cmd, args.Count))
            {
                var lambda = _funs[cmd];
                for(var i = 0; i < args.Count; i++)
                    Set((LString)lambda.Item1[i], args[i]);
                return ExecLambda(lambda.Item2);
            }

            var del = _funcs[args.Count][cmd];
            return (ILNode)del.DynamicInvoke(args.ToArray());
        }

        private ILNode ExecLambda(ILNode node)
        {
            if(!(node is LLambda))
                throw new Exception("Lambda expected");

            var lambda = node as LLambda;
            var newlambda = new LLambda(lambda.Command);
            
            foreach (var n in lambda)
            {
                var res = Exec(n);
                if (res is LNull) continue;
                newlambda.Add(res);
            }

            return Dofunction((string)newlambda.Command, newlambda);
        }

        public ILNode Exec(ILNode node)
        {
            if (node is LLambda) return node;

            if (node is LList)
            {
                var list = node as LList;

                var newList = new LList();
                if (!list.Quoted)
                    foreach (var n in list)
                    {
                        var res = Exec(n);
                        if (res is LNull) continue;
                        newList.Add(res);
                    }
                else
                    newList = list;

                if (newList.Count >= 1 && newList[0] is LString)
                {
                    // key is 1st argument
                    var key = (LString)newList[0];
                    
                    // if a key command is "fun" then use this as function definition
                    if (key.Value == "fun")
                        Fun(key, (LList)newList[1], (LLambda)newList[2]);

                    // if there is no function with such command - return as a list
                    if (!HasFun(key.Value, newList.Count - 1) && !HasFunction(key.Value, newList.Count - 1)) 
                        return newList;
                    
                    // if marked with ' symbol, then it's lambda
                    if (newList.Quoted)
                        return new LLambda(key, newList.GetRange(1, newList.Count - 1));

                    // Execute funtion
                    return Dofunction((string)key, newList.GetRange(1, newList.Count - 1));
                }

                return newList;
            }

            return node;
        }

        public void Bind<TOut>(string key, Func<TOut> func)
        {
            if (!_funcs.ContainsKey(0))
                _funcs.Add(0, new Dictionary<string, Delegate>());
            _funcs[0].Add(key, func);
        }

        public void Bind<TIn, TOut>(string key, Func<TIn, TOut> func)
        {
            if (!_funcs.ContainsKey(1))
                _funcs.Add(1, new Dictionary<string, Delegate>());
            _funcs[1].Add(key, func);
        }

        public void Bind<TIn1, TIn2, TOut>(string key, Func<TIn1, TIn2, TOut> func)
        {
            if (!_funcs.ContainsKey(2))
                _funcs.Add(2, new Dictionary<string, Delegate>());
            _funcs[2].Add(key, func);
        }

        public void Bind<TIn1, TIn2, TIn3, TOut>(string key, Func<TIn1, TIn2, TIn3, TOut> func)
        {
            if (!_funcs.ContainsKey(3))
                _funcs.Add(3, new Dictionary<string, Delegate>());
            _funcs[3].Add(key, func);
        }

        private bool HasFunction(string key, int paramsCount)
        {
            if (!_funcs.ContainsKey(paramsCount))
                return false;

            return _funcs[paramsCount].ContainsKey(key);
        }

        private bool HasFun(string key, int paramsCount)
        {
            if (!_funs.ContainsKey(key))
                return false;

            return _funs[key].Item1.Count == paramsCount;
        }

        public void Subscribe(string key, Action<string, ILNode> action)
        {
            if (_events.ContainsKey(key))
                _events[key] += action;
            else
                _events.Add(key, action);    
        }

        /// <summary>
        /// Sets the global variable
        /// </summary>
        /// <typeparam name="T">Type of variable</typeparam>
        /// <param name="key">String key</param>
        /// <param name="param">Value</param>
        public void SetVar<T>(string key, T param)
        {
            Set(new LString(key), (ILNode)param);
        }

        /// <summary>
        /// Gets the global variable
        /// </summary>
        /// <typeparam name="T">Type of variable</typeparam>
        /// <param name="key">String key</param>
        /// <returns></returns>
        public TLType GetVar<TLType>(string key)
            where TLType : ILNode
        {
            return (TLType)Get(new LString(key));
        }

        #region STDLIB
        private ILNode IfClause(ILNode condition, ILNode iftrue, ILNode ifelse)
        {
            bool val;
            if (condition is LLambda)
            {
                val = (bool)(LBool) ExecLambda(condition);
            }
            else if (condition is LBool)
            {
                val = (bool)(LBool)condition;
            }
            else
                val = false;

            if (val)
            {
                if (iftrue is LLambda) return ExecLambda(iftrue);
                return iftrue;
            }

            if (ifelse is LLambda) return ExecLambda(ifelse);
            return ifelse;
        }

        private ILNode And(ILNode condition1, ILNode condition2)
        {
            var val1 = (bool)(LBool)condition1;
            var val2 = (bool)(LBool)condition2;
            return new LBool(val1 && val2);
        }

        private ILNode Or(ILNode condition1, ILNode condition2)
        {
            var val1 = (bool)(LBool)condition1;
            var val2 = (bool)(LBool)condition2;
            return new LBool(val1 || val2);
        }

        private ILNode And(ILNode condition1, ILNode condition2, ILNode condition3)
        {
            var val1 = (bool)(LBool)condition1;
            var val2 = (bool)(LBool)condition2;
            var val3 = (bool)(LBool)condition3;
            return new LBool(val1 && val2 && val3);
        }

        private ILNode Or(ILNode condition1, ILNode condition2, ILNode condition3)
        {
            var val1 = (bool)(LBool)condition1;
            var val2 = (bool)(LBool)condition2;
            var val3 = (bool)(LBool)condition3;
            return new LBool(val1 || val2 || val3);
        }


        private ILNode DoClause(LLambda lambda)
        {
            if (lambda is LLambda)
                return ExecLambda(lambda);
            throw new Exception("Lambda expected");
        }

        private ILNode Get(LString name)
        {
            if (_globals.ContainsKey(name.Value))
                return _globals[name.Value];

            throw new Exception("No such variable");
        }

        private ILNode Set(LString name, ILNode val)
        {
            if (_globals.ContainsKey(name.Value))
                _globals[name.Value] = val;
            else
                _globals.Add(name.Value, val);
            
            if(_events.ContainsKey(name.Value))
                _events[name.Value].Invoke(name.Value, val);
            return val;
        }

        private ILNode Rem(LString name)
        {
            if (_globals.ContainsKey((string)name))
               return new LBool(_globals.Remove((string)name));

            throw new Exception("No such variable");
        }

        private ILNode Add(ILNode a, ILNode b)
        {
            if(a is LInteger && b is LInteger)
                return new LInteger((int)(LInteger)a + (int)(LInteger)b);

            if (a is LFloat && b is LFloat)
                return new LFloat((float)(LFloat)a + (float)(LFloat)b);

            throw new Exception("Type mismatch");
        }

        private ILNode Sub(ILNode a, ILNode b)
        {
            if (a is LInteger && b is LInteger)
                return new LInteger((int)(LInteger)a - (int)(LInteger)b);

            if (a is LFloat && b is LFloat)
                return new LFloat((float)(LFloat)a - (float)(LFloat)b);

            throw new Exception("Type mismatch");
        }

        private ILNode Mul(ILNode a, ILNode b)
        {
            if (a is LInteger && b is LInteger)
                return new LInteger((int)(LInteger)a * (int)(LInteger)b);

            if (a is LFloat && b is LFloat)
                return new LFloat((float)(LFloat)a * (float)(LFloat)b);

            throw new Exception("Type mismatch");
        }

        private ILNode Div(ILNode a, ILNode b)
        {
            if (a is LInteger && b is LInteger)
                return new LInteger((int)(LInteger)a / (int)(LInteger)b);

            if (a is LFloat && b is LFloat)
                return new LFloat((float)(LFloat)a / (float)(LFloat)b);

            throw new Exception("Type mismatch");
        }

        private LNull Print(LString node)
        {
            Console.WriteLine((string)node);
            return LNullVal;
        }

        private LNull PrintRepr(ILNode node)
        {
            Debug.WriteLine(node.ToString());
            return LNullVal;
        }

        private LBool Eq(ILNode a, ILNode b)
        {
            return new LBool(a.ToString() == b.ToString());
        }

        private LBool NotEq(ILNode a, ILNode b)
        {
            return new LBool(a.ToString() != b.ToString());
        }

        private LBool Less(ILNode a, ILNode b)
        {
            a = a as ILNumeric;
            b = b as ILNumeric;

            if (a == null || b == null)
                throw new Exception("ILNumeric type expected");

            if (((ILNumeric)a).CompareTo((ILNumeric)b) < 0)
                return LTrue;

            return LFalse;
        }

        private LBool LessEq(ILNode a, ILNode b)
        {
            a = a as ILNumeric;
            b = b as ILNumeric;

            if (a == null || b == null)
                throw new Exception("ILNumeric type expected");

            if (((ILNumeric)a).CompareTo((ILNumeric)b) <= 0)
                return LTrue;

            return LFalse;
        }

        private LBool Greater(ILNode a, ILNode b)
        {
            a = a as ILNumeric;
            b = b as ILNumeric;

            if (a == null || b == null)
                throw new Exception("ILNumeric type expected");

            if (((ILNumeric)a).CompareTo((ILNumeric)b) > 0)
                return LTrue;

            return LFalse;
        }

        private LBool GreaterEq(ILNode a, ILNode b)
        {
            a = a as ILNumeric;
            b = b as ILNumeric;

            if (a == null || b == null)
                throw new Exception("ILNumeric type expected");

            if (((ILNumeric)a).CompareTo((ILNumeric)b) >= 0)
                return LTrue;

            return LFalse;
        }

        private ILNode Pass(ILNode node)
        {
            return node;
        }

        private LNull Fun(LString name, LList args, LLambda body)
        {
            _funs.Add(
                (string)name,
                new Tuple<LList, LLambda>(args, body)
            );
            return LNullVal;
        }

        private LNull FunFake(LString name, LList args, LLambda body)
        {
            return LNullVal;
        }
#endregion
    }
}