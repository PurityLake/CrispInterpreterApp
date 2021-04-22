using System;
using System.Collections.Generic;
using System.IO;

namespace Crisp
{
    /// <summary>
    /// This is an object that stores the name, the parameter names and body of a 
    /// user defined function i.e. with define-func
    /// </summary>
    public class CrispUserFunction
    {
        // name of function for debugging purposes
        private String _name;
        // names of parameters so they can be added to the nested environment
        private List<String> _parameterNames;
        private Sexp _body;

        public CrispUserFunction(String name)
        {
            _name = name;
            _parameterNames = new List<String>(5);
        }

        public Sexp Body
        {
            get
            {
                return _body;
            }

            set
            {
                _body = value;
            }
        }

        public void AddParamName(String s)
        {
            _parameterNames.Add(s);
        }

        public Sexp Call(ref Environment e, MemoryStream ms, Sexp sexp)
        {
            if (sexp.Length == _parameterNames.Count)
            {
                int i = 0;
                Environment eClone = new Environment(e);
                // add the respective sexps to the parameter names
                foreach (String s in _parameterNames)
                {
                    eClone.SetVariable(s, Evaluator.Evaluate(ref eClone, ms, sexp[i]));
                    ++i;
                }
                // only return the last value so default to Void
                Sexp outSexp = Sexp.Void;
                foreach (Sexp s in Body)
                {
                    outSexp = Evaluator.Evaluate(ref eClone, ms, s);
                }
                return outSexp;
            }
            throw new CrispArgumentException(String.Format("'{0}' function expects {1} arguments, got {1}", _name, _parameterNames.Count, sexp.Length), sexp.Line);
        }
    }
}
