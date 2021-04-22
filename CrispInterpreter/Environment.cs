using System;
using System.Collections.Generic;
using System.IO;

namespace Crisp
{
    /// <summary>
    /// Stores all builtin (static) functions, user defined functions and variables defined throughout
    /// the programs execution
    /// </summary>
    public class Environment
    {
        public delegate Sexp CrispFunction(ref Environment e, MemoryStream ms, Sexp sexp);

        // next level of context to check if no values found, null if it is the top level scope
        private Environment _parent;

        private static bool _initialised = false;
        // buitlin functions defined in C#
        private static Dictionary<String, CrispFunction> _functions;
        // user defined functions
        private Dictionary<String, CrispUserFunction> _userFunctions;
        // variables defined by user
        private Dictionary<String, Sexp> _variables;

        public Dictionary<String, CrispFunction> Builtins
        {
            get
            {
                return _functions;
            }
        }

        public Dictionary<String, CrispUserFunction> UserFunctions
        {
            get
            {
                return _userFunctions;
            }
        }

        public Dictionary<String, Sexp> Variables
        {
            get
            {
                return _variables;
            }
        }

        public Environment(Environment e)
        {
            _parent = e;
            if (!_initialised)
            {
                _functions = new Dictionary<String, CrispFunction>(32);
                // help function
                _functions["help"] = CrispBuiltin.Help;

                // print functions 
                _functions["print"] = CrispBuiltin.Print;
                _functions["print-line"] = CrispBuiltin.PrintLine;

                // list functions
                _functions["map"] = CrispBuiltin.Map;
                _functions["foldl"] = CrispBuiltin.Foldl;
                _functions["foldr"] = CrispBuiltin.Foldr;
                _functions["car"] = CrispBuiltin.Car;
                _functions["cdr"] = CrispBuiltin.Cdr;
                _functions["empty?"] = CrispBuiltin.Empty;
                _functions["quote"] = CrispBuiltin.Quote;

                // definition functions
                _functions["define"] = CrispBuiltin.Define;
                _functions["define-func"] = CrispBuiltin.DefineFunction;
                _functions["let"] = CrispBuiltin.LetBinding;

                // conditional functions
                _functions["if"] = CrispBuiltin.IfStatement;
                _functions["="] = CrispBuiltin.EqualsOp;
                _functions[">"] = CrispBuiltin.GreaterThanOp;
                _functions["<"] = CrispBuiltin.LessThanOp;
                _functions[">="] = CrispBuiltin.GreaterThanEqualOp;
                _functions["<="] = CrispBuiltin.LessThanEqualOp;
                _functions["not"] = CrispBuiltin.NotFunc;
                _functions["and"] = CrispBuiltin.AndFunc;
                _functions["or"] = CrispBuiltin.OrFunc;

                // string functions
                _functions["string-append"] = CrispBuiltin.StringAppend;

                // arithmetic 
                _functions["+"] = CrispBuiltin.Plus;
                _functions["-"] = CrispBuiltin.Sub;
                _functions["/"] = CrispBuiltin.Div;
                _functions["*"] = CrispBuiltin.Mult;
                _functions["pow"] = CrispBuiltin.Pow;
                _functions["sqrt"] = CrispBuiltin.Sqrt;

                _initialised = true;
            }
            _userFunctions = new Dictionary<string, CrispUserFunction>(10);
            _variables = new Dictionary<String, Sexp>(10);
        }

        public bool TryGetBuiltinFunction(String key, out CrispFunction cf)
        {
            return _functions.TryGetValue(key, out cf);
        }

        public bool TryGetUserFunction(String key, out CrispUserFunction cuf)
        {
            bool found = _userFunctions.TryGetValue(key, out cuf);
            if (!found)
            {
                if (_parent != null)
                {
                    return _parent.TryGetUserFunction(key, out cuf);
                }
            }
            return found;
        }

        public bool TryGetVariable(String key, out Sexp s)
        {
            bool found = _variables.TryGetValue(key, out s);
            if (!found)
            {
                if (_parent != null)
                {
                    return _parent.TryGetVariable(key, out s);
                }
            }
            return found;
        }

        public void SetVariable(String key, Sexp value)
        {
            _variables[key] = value;
        }

        public void SetUserFunction(String key, CrispUserFunction cuf)
        {
            _userFunctions[key] = cuf;
        }
    }
}
