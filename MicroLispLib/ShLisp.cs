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
        private readonly Dictionary<string, ILNode> _userFuns;
        private readonly Dictionary<string, ILNode> _globals;
        private readonly Dictionary<string, Action<string, ILNode>> _events;

        public ShLisp()
        {
            _funcs = new Dictionary<int, Dictionary<string, Delegate>>();
            _globals = new Dictionary<string, ILNode>();
            _events = new Dictionary<string, Action<string, ILNode>>();
            _userFuns = new Dictionary<string, ILNode>();

            // CORE
            Bind<ILNode, ILNode>("eval", Eval);
            Bind<LString, ILNode>("parse", Parse);
            Bind<LIdentifier, ILNode, LNull>("fun", Fun);
            Bind<LIdentifier, ILNode, ILNode>("set", Set);
            Bind<ILNode, ILNode>("pass", Pass);
            Bind<ILNode, LNull>("print", Print);
            Bind<ILNode, LNull>("debug", PrintRepr);
            Bind<ILNode, ILNode, ILNode, ILNode>("if", IfClause);
            Bind<ILNode, ILNode, LNull>("while", WhileClause);
            Bind<LIdentifier, ILNode>("get", Get);
            Bind<LString, ILNode>("rem", Rem);
            
            // Arithmetic
            Bind<ILNode, ILNode, ILNode>("+", Add);
            Bind<ILNode, ILNode, ILNode>("-", Sub);
            Bind<ILNode, ILNode, ILNode>("*", Mul);
            Bind<ILNode, ILNode, ILNode>("/", Div);

            // Logic
            Bind<ILNode, ILNode, ILNode>("and", And);
            Bind<ILNode, ILNode, ILNode, ILNode>("and", And);
            Bind<ILNode, ILNode, ILNode>("&", And);
            Bind<ILNode, ILNode, ILNode, ILNode>("&", And);
            Bind<ILNode, ILNode, ILNode>("or", Or);
            Bind<ILNode, ILNode, ILNode, ILNode>("or", Or);
            Bind<ILNode, ILNode, ILNode>("|", Or);
            Bind<ILNode, ILNode, ILNode, ILNode>("|", Or);

            // Comparison
            Bind<ILNode, ILNode, ILNode>("=", Eq);
            Bind<ILNode, ILNode, ILNode>("<", Less);
            Bind<ILNode, ILNode, ILNode>("<=", LessEq);
            Bind<ILNode, ILNode, ILNode>(">", Greater);
            Bind<ILNode, ILNode, ILNode>(">=", GreaterEq);
            Bind<ILNode, ILNode, ILNode>("!=", NotEq);
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
            if (input.Length >= 2 && input[0] == '\'')
            {
                quoted = true;
                input = input.Substring(1);
            }

            if (input[0] == '(' && input[input.Length - 1] == ')')
            {
                input = input.Substring(1, input.Length - 2).Trim();
                if (input.Length == 2)
                    return new LList(quoted);

                var depth = 0;
                char ch;
                var lastComma = 0;
                var lastWasWhitespace = true;
                var ignorance = false;
                var list = new LList(quoted);
                for (var i = 0; i < input.Length; i++)
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

                if (lastComma != input.Length)
                {
                    var node = Parse(input.Substring(lastComma, input.Length - lastComma));
                    list.Add(node);
                }

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
                 return new LString(input.Substring(1, input.Length - 2));
            
            return new LIdentifier(input, quoted);
        }

        private ILNode Parse(LString input)
        {
            return Parse(input.Value);
        }
        
        private ILNode Dofunction(string cmd, List<ILNode> args)
        {
            var del = _funcs[args.Count][cmd];
            return (ILNode)del.DynamicInvoke(args.ToArray());
        }
        
        private ILNode DoUserFunction(string cmd, IList<ILNode> args)
        {
            const string format = "_{0}";
            var action = _userFuns[cmd];

            var old = new List<Tuple<string,ILNode>>();
            for (var i = 0; i < args.Count; i++)
            {
                var key = String.Format(format, i);
                if (_globals.ContainsKey(key))
                    old.Add(new Tuple<string, ILNode>(key, _globals[key]));

                SetVar(key, args[i]);
            }
            
            var res = Eval(action);

            foreach (var tuple in old)
                SetVar(tuple.Item1,tuple.Item2);

            return res;
        }
        
        public ILNode Eval(ILNode node)
        {
            var id = node as LIdentifier;
            if (id != null)
            {
                if (id.Quoted)
                    return new LIdentifier(id.Value);

                if (_globals.ContainsKey(id.Value))
                    return _globals[id.Value];
                return id;
            }

            var list = node as LList;
            if (list != null)
            {
                if (list.Quoted)
                {
                    var n = new LList();
                    n.AddRange(list);
                    return n;
                }

                var newList = new LList();
                foreach (var n in list)
                {
                    var res = Eval(n);
                    //if (res is LNull) continue;
                    newList.Add(res);
                }

                if (newList.Count >= 1 && newList[0] is LIdentifier)
                {
                    // key is 1st argument
                    var key = (LIdentifier)newList[0];

                    // if there is no function with such command - return as a list
                    if (HasFunction(key.Value, newList.Count - 1)) 
                        return Dofunction((string)key, newList.GetRange(1, newList.Count - 1));

                    if(_userFuns.ContainsKey((string)key))
                        return DoUserFunction((string)key, newList.GetRange(1, newList.Count - 1));

                    throw new Exception("Undeclared identifier");
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
            Set(new LIdentifier(key), (ILNode)param);
        }

        /// <summary>
        /// Gets the global variable
        /// </summary>
        /// <typeparam name="T">Type of variable</typeparam>
        /// <param name="key">String key</param>
        /// <returns></returns>
        public T GetVar<T>(string key)
            where T : ILNode
        {
            return (T)Get(new LIdentifier(key));
        }

        #region STDLIB

        private LNull Fun(LIdentifier id, ILNode act)
        {
            _userFuns.Add(id.Value, act);
            return LNullVal;
        }

        private ILNode IfClause(ILNode condition, ILNode iftrue, ILNode ifelse)
        {
            var val = ((LBool) Eval(condition)).Value;
            return val ? Eval(iftrue) : Eval(ifelse);
        }

        private LNull WhileClause(ILNode condition, ILNode action)
        {
            while (((LBool)Eval(condition)).Value)
                Eval(action);
            return LNullVal;
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

        private ILNode Get(LIdentifier id)
        {
            if (_globals.ContainsKey(id.Value))
                return _globals[id.Value];

            throw new Exception("No such variable");
        }

        private ILNode Set(LIdentifier id, ILNode val)
        {
            if (_globals.ContainsKey(id.Value))
                _globals[id.Value] = val;
            else
                _globals.Add(id.Value, val);

            if (_events.ContainsKey(id.Value))
                _events[id.Value].Invoke(id.Value, val);
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

        private LNull Print(ILNode node)
        {
            Console.WriteLine(node);
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
#endregion
    }
}