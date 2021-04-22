using System;
using System.Collections.Generic;
using System.IO;

namespace Crisp
{
    /// <summary>
    /// Contains the functions that are available in the Crisp runtime.
    /// Must follow the delgate template defined in Evironment:<para/>
    /// 
    /// public delegate Sexp CrispFunction(ref Environment e, MemoryStream ms, Sexp sexp);
    /// </summary>
    static class CrispBuiltin
    {
        public static Sexp Help(ref Environment e, MemoryStream ms, Sexp arguments)
        {
            StreamWriter sw = new StreamWriter(ms);
            sw.WriteLine("List of predefined functions:");
            foreach (String k in e.Builtins.Keys)
            {
                sw.WriteLine(k);
            }
            sw.WriteLine("\nList of user-defined functions:");
            foreach (String k in e.UserFunctions.Keys)
            {
                sw.WriteLine(k);
            }
            sw.WriteLine("List of Variables:");
            foreach (String k in e.Variables.Keys)
            {
                sw.WriteLine(k);
            }
            sw.Flush();
            return Sexp.Void;
        }
        /*****************************************************************
         * Variable Functions
         *****************************************************************/
        /// <summary>
        /// Creates a variable that is available in the current context (usually global).<para/>
        /// Requires 2 arguments:<para/>
        /// - name: ident<para/>
        /// - value: sexp to be stored<para/>
        /// <example>Example:<para/>
        /// <code>
        /// (define x (1 2 3 4))
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <param name="ms">Output Stream</param>
        /// <param name="arguments">Arguments to be passed</param>
        /// <returns>Sexp.Void</returns>
        public static Sexp Define(ref Environment e, MemoryStream ms, Sexp arguments)
        {
            if (arguments.Length != 2)
            {
                throw new CrispArgumentException(String.Format("'define' function expects exactly 2 arguments, got {0}"), arguments.Line);
            }
            if (arguments[0].Type != SexpType.IDENT)
            {
                throw new CrispArgumentException("'define' expects a valid identifer as it's first argument", arguments[0].Line);
            }
            e.SetVariable(arguments[0].Value, arguments[1]);
            return Sexp.Void;
        }

        /// <summary>
        /// Creates a user defined function (CrispUserFunction) that is stored in the environment<para/>
        /// Requires at least 3 parameters<para/>
        /// - name: ident<para/>
        /// - argumentList: list<para/>
        /// - body: 1+ sexps<para/>
        /// </summary>
        /// <example>Example:<para/>
        /// (define-func f (x y)<para/>
        ///     (+ x y)<para/>
        ///     (- x y)) ; returns this value only<para/>
        /// </example>
        /// <param name="e">Current environment</param>
        /// <param name="ms">Output Stream</param>
        /// <param name="arguments">Arguments to be passed</param>
        /// <returns>Value of last sexp in body</returns>
        public static Sexp DefineFunction(ref Environment e, MemoryStream ms, Sexp arguments)
        {
            if (arguments.Length < 3)
            {
                throw new CrispArgumentException(String.Format("'define-func' expects at least 3 arguments, got {0}", arguments.Length), arguments.Line);
            }
            if (arguments[0].Type != SexpType.IDENT)
            {
                throw new CrispArgumentException("'define-func' excpects a valid identifier as it's first argument", arguments[0].Line);
            }
            if (arguments[1].Type != SexpType.LIST)
            {
                throw new CrispArgumentException("'define-func' expects a list as it's second argument", arguments[1].Line);
            }
            
            // see the docstring attached to CrispUserFunction
            CrispUserFunction cuf = new CrispUserFunction(arguments[0].Value);
            foreach (Sexp s  in arguments[1])
            {
                cuf.AddParamName(s.Value);
            }

            // get the slice of arguments from index 2 to the amount of elements = arguments.Length - 2
            cuf.Body = arguments[2, arguments.Length - 2];

            e.SetUserFunction(arguments[0].Value, cuf);

            return Sexp.Void;
        }

        /// <summary>
        /// This function makes a new context of variables that are set at the start of the function
        /// and persist to the end of this function call and are discarded.<para/>
        /// Requires at least 3 parameters<para/>
        /// - argument list - list of lists<para/>
        /// - - inner list contains two items (ident value)<para/>
        /// - body: 1+ sexps<para/>
        /// <example>Example:<para/>
        /// <code>
        /// (let ((x 2) (y 3))<para/>
        /// (print-line (+ x y))<para/>
        /// (+ x y))) ; returns this value<para/>
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <param name="ms">Output Stream</param>
        /// <param name="arguments">Arguments to be passed</param>
        /// <returns>Value of last sexp in body</returns>
        public static Sexp LetBinding(ref Environment e, MemoryStream ms, Sexp arguments)
        {
            if (arguments.Length >= 1)
            {
                Sexp tempVars = arguments[0];
                // uses an inner environment so that variables defined in a higher score are not
                // overwritten
                Environment innerEnv = new Environment(e);
                foreach (Sexp tuple in tempVars)
                {
                    if (tuple.Length == 2)
                    {
                        Sexp name = tuple[0];
                        Sexp value = tuple[1];
                        if (name.Type == SexpType.IDENT)
                        {
                            innerEnv.SetVariable(name.Value, Evaluator.Evaluate(ref e, ms, value));
                        }
                    }
                }
                bool first = true;
                Sexp o = Sexp.Void;
                foreach (Sexp s in arguments)
                {
                    if (first)
                    {
                        first = false;
                        continue;
                    }
                    o = Evaluator.Evaluate(ref innerEnv, ms, s);
                }
                return o;
            }
            throw new CrispArgumentException(String.Format("'let' function requires at least 1 argument, got {0}", arguments.Length), arguments.Line);
        }

        /*****************************************************************
         * Condtional Functions
         *****************************************************************/
        /// <summary>
        /// Conditional function that tests its first argument if it is true or false.<para/>
        /// Requires exactly 3 parameters:<para/>
        /// - condition - true or false value<para/>
        /// - true-branch - sexp<para/>
        /// - false branch - sexp<para/>
        /// If the condition is true it returns the value of it's second pararmeter.<para/>
        /// If the condition is false it returns the value of it's third parameter<para/>
        /// <example>Example:<para/>
        /// <code>
        /// (if (= 1 1)<para/>
        /// (print-line "true")<para/>
        /// (print-line "false"))<para/>
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <param name="ms">Output Stream</param>
        /// <param name="arguments">Arguments to be passed</param>
        /// <returns>Value of the second parameter if true or value of third parameter is false</returns>
        public static Sexp IfStatement(ref Environment e, MemoryStream ms, Sexp arguments)
        {
            if (arguments.Length != 3)
            {
                throw new CrispArgumentException(String.Format("'if' function requires exactly 3 arguments, got {0}", arguments.Length), arguments.Line);
            }

            Sexp condition = Evaluator.Evaluate(ref e, ms, arguments[0]);
            Sexp trueCond = arguments[1];
            Sexp falseCond = arguments[2];

            if (condition.BoolValue)
            {
                return Evaluator.Evaluate(ref e, ms, trueCond);
            }
            return Evaluator.Evaluate(ref e, ms, falseCond);
        }

        /// <summary>
        /// Function that tests the equality of it's two parameters.<para/>
        /// Requires exactly two arguments:<para/>
        /// - left: sexp<para/>
        /// - right: sexp<para/>
        /// <example>Example:<para/>
        /// <code>
        /// (= 1 1) ; true<para/>
        /// (= 1 2) ; false<para/>
        /// (= "foo" "foo") ; true<para/>
        /// (= 1 "foo") ; false
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <param name="ms">Output Stream</param>
        /// <param name="arguments">Arguments to be passed</param>
        /// <returns>Sexp.True or Sexp.False</returns>
        public static Sexp EqualsOp(ref Environment e, MemoryStream ms, Sexp arguments)
        {
            if (arguments.Length == 2)
            {
                Sexp left = Evaluator.Evaluate(ref e, ms, arguments[0]);
                Sexp right = Evaluator.Evaluate(ref e, ms, arguments[1]);

                if (left.Type == right.Type)
                {
                    switch (left.Type)
                    {
                        case SexpType.STRING:
                        case SexpType.CHAR:
                            if (left.Value.Equals(right.Value))
                            {
                                return Sexp.True;
                            }
                            break;
                        case SexpType.INT:
                            if (left.IntValue == right.IntValue)
                            {
                                return Sexp.True;
                            }
                            break;
                        case SexpType.FLOAT:
                            if (left.FloatValue == right.FloatValue)
                            {
                                return Sexp.True;
                            }
                            break;
                        case SexpType.BOOL:
                            if (left.BoolValue == right.BoolValue)
                            {
                                return Sexp.True;
                            }
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    if (left.Type == SexpType.INT)
                    {
                        if (right.Type == SexpType.FLOAT)
                        {
                            if (left.IntValue == right.FloatValue)
                            {
                                return Sexp.True;
                            }
                        }
                    }
                    else
                    {
                        if (right.Type == SexpType.INT)
                        {
                            if (left.FloatValue == right.IntValue)
                            {
                                return Sexp.True;
                            }
                        }
                    }
                }
            }
            else
            {
                throw new CrispArgumentException(String.Format("'=' function requires exactly 2 arguments, got {0}", arguments.Length), arguments.Line);
            }
            return Sexp.False;
        }

        /// <summary>
        /// Function that tests if its first argument is strictly greater than it's second parameter.<para/>
        /// Requires exactly two arguments:<para/>
        /// - left: float or int<para/>
        /// - right: float or int<para/>
        /// <example>Example:<para/>
        /// <code>
        /// (> 1 1) ; false<para/>
        /// (> 2 1) ; true<para/>
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <param name="ms">Output Stream</param>
        /// <param name="arguments">Arguments to be passed</param>
        /// <returns>Sexp.True or Sexp.False</returns>
        public static Sexp GreaterThanOp(ref Environment e, MemoryStream ms, Sexp arguments)
        {
            if (arguments.Length == 2)
            {
                Sexp left = Evaluator.Evaluate(ref e, ms, arguments[0]);
                Sexp right = Evaluator.Evaluate(ref e, ms, arguments[1]);

                if (left.Type == right.Type)
                {
                    switch (left.Type)
                    {
                        case SexpType.INT:
                            if (left.IntValue > right.IntValue)
                            {
                                return Sexp.True;
                            }
                            break;
                        case SexpType.FLOAT:
                            if (left.FloatValue > right.FloatValue)
                            {
                                return Sexp.True;
                            }
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    if (left.Type == SexpType.INT)
                    {
                        if (right.Type == SexpType.FLOAT)
                        {
                            if (left.IntValue > right.FloatValue)
                            {
                                return Sexp.True;
                            }
                        }
                    }
                    else
                    {
                        if (right.Type == SexpType.INT)
                        {
                            if (left.FloatValue > right.IntValue)
                            {
                                return Sexp.True;
                            }
                        }
                    }
                }
            }
            else
            {
                throw new CrispArgumentException(String.Format("'>' function requires exactly 2 arguments, got {0}", arguments.Length), arguments.Line);
            }
            return Sexp.False;
        }

        /// <summary>
        /// Function that tests if its first argument is greater than or equal to it's second parameter.<para/>
        /// Requires exactly two arguments:<para/>
        /// - left: float or int<para/>
        /// - right: float or int<para/>
        /// <example>Example:<para/>
        /// <code>
        /// (>= 1 1) ; true<para/>
        /// (>= 2 1) ; true<para/>
        /// (>= 1 2) ; false<para/>
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <param name="ms">Output Stream</param>
        /// <param name="arguments">Arguments to be passed</param>
        /// <returns>Sexp.True or Sexp.False</returns>
        public static Sexp GreaterThanEqualOp(ref Environment e, MemoryStream ms, Sexp arguments)
        {
            if (arguments.Length == 2)
            {
                Sexp left = Evaluator.Evaluate(ref e, ms, arguments[0]);
                Sexp right = Evaluator.Evaluate(ref e, ms, arguments[1]);

                if (left.Type == right.Type)
                {
                    switch (left.Type)
                    {
                        case SexpType.INT:
                            if (left.IntValue >= right.IntValue)
                            {
                                return Sexp.True;
                            }
                            break;
                        case SexpType.FLOAT:
                            if (left.FloatValue >= right.FloatValue)
                            {
                                return Sexp.True;
                            }
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    if (left.Type == SexpType.INT)
                    {
                        if (right.Type == SexpType.FLOAT)
                        {
                            if (left.IntValue >= right.FloatValue)
                            {
                                return Sexp.True;
                            }
                        }
                    }
                    else
                    {
                        if (right.Type == SexpType.INT)
                        {
                            if (left.FloatValue >= right.IntValue)
                            {
                                return Sexp.True;
                            }
                        }
                    }
                }
            }
            else
            {
                throw new CrispArgumentException(String.Format("'>=' function requires exactly 2 arguments, got {0}", arguments.Length), arguments.Line);
            }
            return Sexp.False;
        }

        /// <summary>
        /// Function that tests if its first argument is strictly less than to it's second parameter.<para/>
        /// Requires exactly two arguments:<para/>
        /// - left: float or int<para/>
        /// - right: float or int<para/>
        /// <example>Example:<para/>
        /// <code>
        /// (&lt; 1 1) ; false<para/>
        /// (&lt; 2 1) ; false<para/>
        /// (&lt; 1 2) ; true<para/>
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <param name="ms">Output Stream</param>
        /// <param name="arguments">Arguments to be passed</param>
        /// <returns>Sexp.True or Sexp.False</returns>
        public static Sexp LessThanOp(ref Environment e, MemoryStream ms, Sexp arguments)
        {
            if (arguments.Length == 2)
            {
                Sexp left = Evaluator.Evaluate(ref e, ms, arguments[0]);
                Sexp right = Evaluator.Evaluate(ref e, ms, arguments[1]);

                if (left.Type == right.Type)
                {
                    switch (left.Type)
                    {
                        case SexpType.INT:
                            if (left.IntValue < right.IntValue)
                            {
                                return Sexp.True;
                            }
                            break;
                        case SexpType.FLOAT:
                            if (left.FloatValue < right.FloatValue)
                            {
                                return Sexp.True;
                            }
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    if (left.Type == SexpType.INT)
                    {
                        if (right.Type == SexpType.FLOAT)
                        {
                            if (left.IntValue < right.FloatValue)
                            {
                                return Sexp.True;
                            }
                        }
                    }
                    else
                    {
                        if (right.Type == SexpType.INT)
                        {
                            if (left.FloatValue < right.IntValue)
                            {
                                return Sexp.True;
                            }
                        }
                    }
                }
            }
            else
            {
                throw new CrispArgumentException(String.Format("'<' function requires exactly 2 arguments, got {0}", arguments.Length), arguments.Line);
            }
            return Sexp.False;
        }

        /// <summary>
        /// Function that tests if its first argument is less than or equal to it's second parameter.<para/>
        /// Requires exactly two arguments:<para/>
        /// - left: float or int<para/>
        /// - right: float or int<para/>
        /// <example>Example:<para/>
        /// <code>
        /// (&lt;= 1 1) ; false<para/>
        /// (&lt;= 2 1) ; false<para/>
        /// (&lt;= 1 2) ; true<para/>
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <param name="ms">Output Stream</param>
        /// <param name="arguments">Arguments to be passed</param>
        /// <returns>Sexp.True or Sexp.False</returns>
        public static Sexp LessThanEqualOp(ref Environment e, MemoryStream ms, Sexp arguments)
        {
            if (arguments.Length == 2)
            {
                Sexp left = Evaluator.Evaluate(ref e, ms, arguments[0]);
                Sexp right = Evaluator.Evaluate(ref e, ms, arguments[1]);

                if (left.Type == right.Type)
                {
                    switch (left.Type)
                    {
                        case SexpType.INT:
                            if (left.IntValue <= right.IntValue)
                            {
                                return Sexp.True;
                            }
                            break;
                        case SexpType.FLOAT:
                            if (left.FloatValue <= right.FloatValue)
                            {
                                return Sexp.True;
                            }
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    if (left.Type == SexpType.INT)
                    {
                        if (right.Type == SexpType.FLOAT)
                        {
                            if (left.IntValue <= right.FloatValue)
                            {
                                return Sexp.True;
                            }
                        }
                    }
                    else
                    {
                        if (right.Type == SexpType.INT)
                        {
                            if (left.FloatValue <= right.IntValue)
                            {
                                return Sexp.True;
                            }
                        }
                    }
                }
            }
            else
            {
                throw new CrispArgumentException(String.Format("'<=' function requires exactly 2 arguments, got {0}", arguments.Length), arguments.Line);
            }
            return Sexp.False;
        }

        /// <summary>
        /// This function returns the inverse of the boolean value it is given.<para/>
        /// Requires exactly one parameter:<para/>
        /// - value: boolean<para/>
        /// <example>Example:<para/>
        /// <code>
        /// (not #F) ; true<para/>
        /// (not #T) ; false<para/>
        /// (not (> 1 2)) ; true<para/>
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <param name="ms">Output Stream</param>
        /// <param name="arguments">Arguments to be passed</param>
        /// <returns>Sexp.True or Sexp.False</returns>
        public static Sexp NotFunc(ref Environment e, MemoryStream ms, Sexp arguments)
        {
            if (arguments.Length == 1)
            {
                Sexp arg = arguments[0];
                Sexp value = Evaluator.Evaluate(ref e, ms, arg);
                if (value.Type == SexpType.BOOL)
                {
                    if (value.BoolValue)
                    {
                        return Sexp.False;
                    }
                    return Sexp.True;
                }
                else
                {
                    throw new CrispArgumentException("'not' function requires a boolean expression as it's argument", arg.Line);
                }
            }
            throw new CrispArgumentException(String.Format("'not' function requires exactly one argument, got {0}", arguments.Length), arguments.Line);
        }

        /// <summary>
        /// This function returns the logical 'and' to all parameters it is passed.
        /// It is also short circuited so the first false it receives it returns false.
        /// Requires more than two parameter:<para/>
        /// - parameters: boolean<para/>
        /// <example>Example:<para/>
        /// <code>
        /// (and #T #T #T) ; true<para/>
        /// (and #F #T #T) ; false<para/>
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <param name="ms">Output Stream</param>
        /// <param name="arguments">Arguments to be passed</param>
        /// <returns>Sexp.True or Sexp.False</returns>
        public static Sexp AndFunc(ref Environment e, MemoryStream ms, Sexp arguments)
        {
            if (arguments.Length >= 2)
            {
                foreach (Sexp s in arguments)
                {
                    Sexp val = Evaluator.Evaluate(ref e, ms, s);
                    if (val.Type == SexpType.BOOL)
                    {
                        if (val.BoolValue == false)
                        {
                            return Sexp.False;
                        }
                    }
                    else
                    {
                        throw new CrispArgumentException("'and' function requires all values to be bool or evaluate to bool", s.Line);
                    }
                }
                return Sexp.True;
            }
            else
            {
                throw new CrispArgumentException("'and' function requires at least 2 arguments", arguments.Line);
            }
        }

        /// <summary>
        /// This function returns the logical 'or' to all parameters it is passed.
        /// It is also short circuited so the first true it receives it returns true.
        /// Requires more than two parameter:<para/>
        /// - parameters: boolean<para/>
        /// <example>Example:<para/>
        /// <code>
        /// (or #T #F #T) ; true<para/>
        /// (or #F #F #F) ; false<para/>
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <param name="ms">Output Stream</param>
        /// <param name="arguments">Arguments to be passed</param>
        /// <returns>Sexp.True or Sexp.False</returns>
        public static Sexp OrFunc(ref Environment e, MemoryStream ms, Sexp arguments)
        {
            if (arguments.Length >= 2)
            {
                foreach (Sexp s in arguments)
                {
                    Sexp val = Evaluator.Evaluate(ref e, ms, s);
                    if (val.Type == SexpType.BOOL)
                    {
                        if (val.BoolValue == true)
                        {
                            return Sexp.True;
                        }
                    }
                    else
                    {
                        throw new CrispArgumentException("'or' function requires all values to be bool or evaluate to bool", s.Line);
                    }
                }
                return Sexp.False;
            }
            else
            {
                throw new CrispArgumentException("'or' function requires at least 2 arguments", arguments.Line);
            }
        }

        /*****************************************************************
         * String Functions
         *****************************************************************/
        /// <summary>
        /// This function takes 2 or more strings and combines them together.<para/>
        /// Requires two or more parameters:<para/>
        /// - strings: string<para/>
        /// <example>
        /// <code>
        /// (string-append "hello " "world") ; "hello world"
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <param name="ms">Output Stream</param>
        /// <param name="arguments">Arguments to be passed</param>
        /// <returns>String</returns>
        public static Sexp StringAppend(ref Environment e, MemoryStream ms, Sexp arguments)
        {
            if (arguments.Length >= 2)
            {
                String value = String.Empty;
                foreach (Sexp s in arguments)
                {
                    Sexp val = Evaluator.Evaluate(ref e, ms, s);
                    if (val.Type == SexpType.STRING)
                    {
                        value += val.Value;
                    }
                    else
                    {
                        throw new CrispArgumentException("'string-append' requires all arguments to evaluate to a string", s.Line);
                    }
                }
                return new Sexp(SexpType.STRING, value, -1);
            }
            throw new CrispArgumentException(String.Format("'string-append' requires at least two parameters, got {0}", arguments.Length), arguments.Line);
        }

        /*****************************************************************
         * Print Functions
         *****************************************************************/
        /// <summary>
        /// This function takes 0 or more sexps and prints them to the MemoryStream passed to it and then prints a newline character.<para/>
        /// Requires 0 or more parameters:<para/>
        /// - items: any sexp<para/>
        /// <example>
        /// <code>
        /// (print-line 0 1.1 "hello" (1 2 3)) ; 0 0.1 hello (1 2 3)
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <param name="ms">Output Stream</param>
        /// <param name="arguments">Arguments to be passed</param>
        /// <returns>Sexp.Void</returns>
        public static Sexp PrintLine(ref Environment e, MemoryStream ms, Sexp arguments)
        {
            StreamWriter sw = new StreamWriter(ms);
            foreach (Sexp sexp in arguments)
            {
                if (sexp.Type == SexpType.LIST || sexp.Type == SexpType.IDENT)
                {
                    Sexp s = Evaluator.Evaluate(ref e, ms, sexp);
                    sw.Write(s.ToString() + " ");
                }
                else
                {
                    sw.Write(sexp.ToString() + " ");
                }
            }
            sw.WriteLine();
            sw.Flush();
            return Sexp.Void;
        }

        /// <summary>
        /// This function takes 0 or more sexps and prints them to the MemoryStream passed to it but does not add a newline character to the end<para/>
        /// Requires 0 or more parameters:<para/>
        /// - items: any sexp<para/>
        /// <example>
        /// <code>
        /// (print 0 1.1 "hello" (1 2 3)) ; 0 0.1 hello (1 2 3)
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <param name="ms">Output Stream</param>
        /// <param name="arguments">Arguments to be passed</param>
        /// <returns>Sexp.Void</returns>
        public static Sexp Print(ref Environment e, MemoryStream ms, Sexp arguments)
        {
            StreamWriter sw = new StreamWriter(ms);
            foreach (Sexp sexp in arguments)
            {
                if (sexp.Type == SexpType.LIST || sexp.Type == SexpType.IDENT)
                {
                    Sexp s = Evaluator.Evaluate(ref e, ms, sexp);
                    sw.Write(s.ToString() + " ");
                }
                else
                {
                    sw.Write(sexp.ToString() + " ");
                }
            }
            sw.Flush();
            return Sexp.Void;
        }


        /*****************************************************************
         * List Functions
         *****************************************************************/
        /// <summary>
        /// This function take one list and returns the first sexp in the list<para/>
        /// Requires 1 argument:<para/>
        /// - list: list sexp<para/>
        /// <example>
        /// <code>
        /// (car (quote (1 2 3))) ; 1
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <param name="ms">Output Stream</param>
        /// <param name="arguments">Arguments to be passed</param>
        /// <returns>Sexp</returns>
        public static Sexp Car(ref Environment e, MemoryStream ms, Sexp arguments)
        {
            if (arguments.Length == 1)
            {
                Sexp arg = Evaluator.Evaluate(ref e, ms, arguments[0]); ;
                if (arg.Type == SexpType.LIST)
                {
                    if (arg.Length > 0)
                    {
                        return arg[0];
                    }
                    else
                    {
                        throw new CrispArgumentException("'car' function requires a list of at least length 1", arguments.Line);
                    }
                }
                else
                {
                    throw new CrispArgumentException("'car' function requires a list as it's parameter", arguments.Line);
                }
            }
            throw new CrispArgumentException(String.Format("'car' function requires exactly 1 argument, got {0}", arguments.Length), arguments.Line);
        }

        /// <summary>
        /// This function take one list and returns list of everything after the first sexp in the list<para/>
        /// Requires 1 argument:<para/>
        /// - list: list sexp<para/>
        /// <example>
        /// <code>
        /// (cdr (quote (1 2 3))) ; (2 3)
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <param name="ms">Output Stream</param>
        /// <param name="arguments">Arguments to be passed</param>
        /// <returns>List</returns>
        public static Sexp Cdr(ref Environment e, MemoryStream ms, Sexp arguments)
        {
            if (arguments.Length == 1)
            {
                Sexp arg = Evaluator.Evaluate(ref e, ms, arguments[0]);
                if (arg.Type == SexpType.LIST)
                {
                    if (arg.Length > 0)
                    {
                        return arg[1, arg.Length - 1];
                    }
                    else
                    {
                        return new Sexp();
                    }
                }
                else
                {
                    throw new CrispArgumentException("'cdr' function requires a list as it's parameter", arguments.Line);
                }
            }
            throw new CrispArgumentException(String.Format("'cdr' function requires exactly 1 argument, got {0}", arguments.Length), arguments.Line);
        }

        /// <summary>
        /// This function take one list as parameter and returns whether it is an empty list<para/>
        /// Requires 1 argument:<para/>
        /// - list: list sexp<para/>
        /// <example>
        /// <code>
        /// (empty? (quote (1 2 3))) ; faLse<para/>
        /// (empty? (quote ())) ; true
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <param name="ms">Output Stream</param>
        /// <param name="arguments">Arguments to be passed</param>
        /// <returns>Sexp.True or Sexp.False</returns>
        public static Sexp Empty(ref Environment e, MemoryStream ms, Sexp arguments)
        {
            if (arguments.Length == 1)
            {
                Sexp arg = Evaluator.Evaluate(ref e, ms, arguments[0]);
                if (arg.Type == SexpType.LIST)
                {
                    if (arg.Length == 0)
                    {
                        return Sexp.True;
                    }
                    return Sexp.False;
                }
                else
                {
                    throw new CrispArgumentException("'empty?' function requires its argument to be a list", arguments.Line);
                }
            }
            throw new CrispArgumentException(String.Format("'empty?' function requires exactly 1 argument, got {0}", arguments.Length), arguments.Line);
        }

        /// <summary>
        /// This function take one sexp and returns it.<para/>
        /// Requires 1 argument:<para/>
        /// - sexp: the sexp to return<para/>
        /// <example>
        /// <code>
        /// (quote (1 2 3)) ; (1 2 3)<para/>
        /// (quote 1) ; 1
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <param name="ms">Output Stream</param>
        /// <param name="arguments">Arguments to be passed</param>
        /// <returns>Sexp</returns>
        public static Sexp Quote(ref Environment e, MemoryStream sm, Sexp arguments)
        {
            if (arguments.Length == 1)
            {
                return arguments[0];
            }
            throw new CrispArgumentException(String.Format("'quote' function requires exactly 1 argument, got {0}", arguments.Length), arguments.Line);
        }

        /// <summary>
        /// This function takes 2 arguments one being either an ident or a partial functions e.g. (+ 1) and a list.
        /// It then applies that function to each element of that list, return the list of modifed values. The function
        /// that is passed should take 1 argument and return a value.<para/>
        /// Requires 2 argument:<para/>
        /// - func: ident or partial function<para/>
        /// - list: list to apply func to<para/>
        /// <example>
        /// <code>
        /// (map (+ 1) (1 2 3 4)) ; (2 3 4 5)
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <param name="ms">Output Stream</param>
        /// <param name="arguments">Arguments to be passed</param>
        /// <returns>List</returns>
        public static Sexp Map(ref Environment e, MemoryStream ms, Sexp arguments)
        {
            if (arguments.Length != 2)
            {
                throw new CrispArgumentException(String.Format("'map' function expects exactly 2 arguments, got {0}", arguments.Length), arguments.Line);
            }
            List<Sexp> o = new List<Sexp>(10);
            int mapIndex;
            Sexp func = arguments[0];
            Sexp apply;
            if (func.Type == SexpType.LIST)
            {
                apply = func;
                apply.Add(Sexp.Void);
                mapIndex = apply.Length - 1;
            }
            else
            {
                apply = new Sexp();
                apply.Add(func);
                apply.Add(Sexp.Void);
                mapIndex = 1;
            }
            foreach (Sexp s in arguments[1])
            {
                apply[mapIndex] = s;
                o.Add(Evaluator.Evaluate(ref e, ms, apply));
            }
            return Sexp.FromList(o);
        }

        /// <summary>
        /// This function takes 3 arguments one being either an ident or a partial functions e.g. (+ 1) an accumulator value
        /// which will be the initial value to be applied to the function and a list. It then applies that function to each element
        /// of that list along side the accumlator, saves that accumlator and uses it in the next value of the list, then returns the accumulator
        /// at the end of the list. It does this from the first element of the list.<para/>
        /// Requires 2 argument:<para/>
        /// - func: ident or partial function<para/>
        /// - accumlator: Sexp that is the initial value to be applied<para/>
        /// - list: list to apply func to<para/>
        /// <example>
        /// <code>
        /// (foldl (string-append) "" ("a" "b" "c" "d")) ; "abcd"
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <param name="ms">Output Stream</param>
        /// <param name="arguments">Arguments to be passed</param>
        /// <returns>Sexp</returns>
        public static Sexp Foldl(ref Environment e, MemoryStream ms, Sexp arguments)
        {
            if (arguments.Length == 3)
            {
                int mapIndex;
                Sexp func = arguments[0];
                Sexp acc = arguments[1];
                Sexp list = arguments[2];

                if (list.Type != SexpType.LIST)
                {
                    throw new CrispArgumentException("'foldl' function requires a list as it's third argument", list.Line);
                }

                Sexp apply;
                if (func.Type == SexpType.LIST)
                {
                    apply = func;
                    apply.Add(Sexp.Void);
                    apply.Add(Sexp.Void);
                    mapIndex = apply.Length - 1;
                }
                else if (func.Type == SexpType.IDENT)
                {
                    apply = new Sexp();
                    apply.Add(func);
                    apply.Add(Sexp.Void);
                    apply.Add(Sexp.Void);
                    mapIndex = 2;
                }
                else
                {
                    throw new CrispArgumentException("'foldl' function requires it's first argument to be a function or a partial function", func.Line);
                }

                foreach (Sexp s in list)
                {
                    apply[mapIndex] = acc;
                    apply[mapIndex - 1] = s;
                    acc = Evaluator.Evaluate(ref e, ms, apply);
                }
                return acc;
            }
            throw new CrispArgumentException(String.Format("'foldl' function requires exactly 3 arguments, got {0}", arguments.Length), arguments.Line);
        }

        /// <summary>
        /// This function takes 3 arguments one being either an ident or a partial functions e.g. (+ 1) an accumulator value
        /// which will be the initial value to be applied to the function and a list. It then applies that function to each element
        /// of that list along side the accumlator, saves that accumlator and uses it in the next value of the list, then returns the accumulator
        /// at the end of the list. It does this from the last element of the list to the first.<para/>
        /// Requires 2 argument:<para/>
        /// - func: ident or partial function<para/>
        /// - accumlator: Sexp that is the initial value to be applied<para/>
        /// - list: list to apply func to<para/>
        /// <example>
        /// <code>
        /// (foldr (string-append) "" ("a" "b" "c" "d")) ; "dcba"
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <param name="ms">Output Stream</param>
        /// <param name="arguments">Arguments to be passed</param>
        /// <returns>Sexp</returns>
        public static Sexp Foldr(ref Environment e, MemoryStream ms, Sexp arguments)
        {
            if (arguments.Length == 3)
            {
                int mapIndex;
                Sexp func = arguments[0];
                Sexp acc = arguments[1];
                Sexp list = arguments[2];

                if (list.Type != SexpType.LIST)
                {
                    throw new CrispArgumentException("'foldr' function requires a list as it's third argument", list.Line);
                }

                Sexp apply;
                if (func.Type == SexpType.LIST)
                {
                    apply = func;
                    apply.Add(Sexp.Void);
                    apply.Add(Sexp.Void);
                    mapIndex = apply.Length - 1;
                }
                else if (func.Type == SexpType.IDENT)
                {
                    apply = new Sexp();
                    apply.Add(func);
                    apply.Add(Sexp.Void);
                    apply.Add(Sexp.Void);
                    mapIndex = 2;
                }
                else
                {
                    throw new CrispArgumentException("'foldr' function requires it's first argument to be a function or a partial function", func.Line);
                }

                for (int i = list.Length - 1; i >= 0; --i)
                {
                    apply[mapIndex] = acc;
                    apply[mapIndex - 1] = list[i];
                    acc = Evaluator.Evaluate(ref e, ms, apply);
                }
                return acc;
            }
            throw new CrispArgumentException(String.Format("'foldr' function requires exactly 3 arguments, got {0}", arguments.Length), arguments.Line);
        }

        /*****************************************************************
         * Arithmetic Functions;
         *****************************************************************/
        /// <summary>
        /// This function performs the addition operation over two or more numeric values (int or float).
        /// If a float is introduced the resulting value will be float also.<para/>
        /// - sexps : ints or floats<para/>
        /// <example>
        /// <code>
        /// (+ 1 2 3 4) ; 10 (int)<para/>
        /// (+ 1 2 3 4.3) ; 10.3 (float)<para/>
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <param name="ms">Output Stream</param>
        /// <param name="arguments">Arguments to be passed</param>
        /// <returns>Sexp int or float</returns>
        public static Sexp Plus(ref Environment e, MemoryStream ms, Sexp arguments)
        {
            int intValue = 0;
            float floatValue = 0.0f;
            bool isInt = true;

            foreach (Sexp sexp in arguments)
            {
                if (sexp.Type == SexpType.LIST || sexp.Type == SexpType.IDENT)
                {
                    Sexp s = Evaluator.Evaluate(ref e, ms, sexp);
                    switch (s.Type)
                    {
                        case SexpType.INT:
                            if (isInt)
                            {
                                intValue += s.IntValue;
                            }
                            else
                            {
                                floatValue += (float)s.IntValue;
                            }
                            break;
                        case SexpType.FLOAT:
                            if (isInt)
                            {
                                isInt = false;
                                floatValue = (float)intValue;
                                floatValue += s.FloatValue;
                            }
                            else
                            {
                                floatValue += s.FloatValue;
                            }
                            break;
                        default:
                            throw new CrispArgumentException("'+' function expects arguments to be numbers", sexp.Line);
                    }
                }
                else
                {
                    switch (sexp.Type)
                    {
                        case SexpType.INT:
                            if (isInt)
                            {
                                intValue += sexp.IntValue;
                            }
                            else
                            {
                                floatValue += (float)sexp.IntValue;
                            }
                            break;
                        case SexpType.FLOAT:
                            if (isInt)
                            {
                                isInt = false;
                                floatValue = (float)intValue;
                                floatValue += sexp.FloatValue;
                            }
                            else
                            {
                                floatValue += sexp.FloatValue;
                            }
                            break;
                        default:
                            throw new CrispArgumentException("'+' function expects arguments to be numbers", sexp.Line);
                    }
                }
            }
            if (isInt)
            {
                return new Sexp(SexpType.INT, intValue.ToString(), -1);
            }
            return new Sexp(SexpType.FLOAT, floatValue.ToString(), -1);
        }

        /// <summary>
        /// This function performs the subtraction operation over two or more numeric values (int or float).
        /// If a float is introduced the resulting value will be float also.<para/>
        /// - sexps : ints or floats<para/>
        /// <example>
        /// <code>
        /// (- 10 2 3 5) ; 0 (int)<para/>
        /// (- 10 2 3 4.5) ; 0.5 (float)<para/>
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <param name="ms">Output Stream</param>
        /// <param name="arguments">Arguments to be passed</param>
        /// <returns>Sexp int or float</returns>
        public static Sexp Sub(ref Environment e, MemoryStream ms, Sexp arguments)
        {
            bool first = true;
            int intValue = 0;
            float floatValue = 0.0f;
            bool isInt = true;

            foreach (Sexp sexp in arguments)
            {
                if (sexp.Type == SexpType.LIST || sexp.Type == SexpType.IDENT)
                {
                    Sexp s = Evaluator.Evaluate(ref e, ms, sexp);
                    switch (s.Type)
                    {
                        case SexpType.INT:
                            if (first)
                            {
                                intValue = s.IntValue;
                                first = false;
                            }
                            else
                            {
                                if (isInt)
                                {
                                    intValue -= s.IntValue;
                                }
                                else
                                {
                                    floatValue -= (float)s.IntValue;
                                }
                            }
                            break;
                        case SexpType.FLOAT:
                            if (first)
                            {
                                isInt = false;
                                floatValue = s.FloatValue;
                                first = false;
                            }
                            else
                            {
                                if (isInt)
                                {
                                    isInt = false;
                                    floatValue = (float)intValue;
                                    floatValue -= s.FloatValue;
                                }
                                else
                                {
                                    floatValue -= s.FloatValue;
                                }
                            }
                            break;
                        default:
                            throw new CrispArgumentException("'-' function expects arguments to be numbers", sexp.Line);
                    }
                }
                else
                {
                    switch (sexp.Type)
                    {
                        case SexpType.INT:
                            if (first)
                            {
                                intValue = sexp.IntValue;
                                first = false;
                            }
                            else
                            {
                                if (isInt)
                                {
                                    intValue -= sexp.IntValue;
                                }
                                else
                                {
                                    floatValue -= (float)sexp.IntValue;
                                }
                            }
                            break;
                        case SexpType.FLOAT:
                            if (first)
                            {
                                first = false;
                                floatValue = sexp.FloatValue;
                                isInt = false;
                            }
                            else
                            {
                                if (isInt)
                                {
                                    isInt = false;
                                    floatValue = (float)intValue;
                                    floatValue -= sexp.FloatValue;
                                }
                                else
                                {
                                    floatValue -= sexp.FloatValue;
                                }
                            }
                            break;
                        default:
                            throw new CrispArgumentException("'-' function expects arguments to be numbers", sexp.Line);
                    }
                }
            }
            if (isInt)
            {
                return new Sexp(SexpType.INT, intValue.ToString(), -1);
            }
            return new Sexp(SexpType.FLOAT, floatValue.ToString(), -1);
        }

        /// <summary>
        /// This function performs the multiplication operation over two or more numeric values (int or float).
        /// If a float is introduced the resulting value will be float also.<para/>
        /// - sexps : ints or floats<para/>
        /// <example>
        /// <code>
        /// (* 1 2 3 4) ; 24 (int)<para/>
        /// (* 1 2 3 4) ; 24.0 (float)<para/>
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <param name="ms">Output Stream</param>
        /// <param name="arguments">Arguments to be passed</param>
        /// <returns>Sexp int or float</returns>
        public static Sexp Mult(ref Environment e, MemoryStream ms, Sexp arguments)
        {
            int intValue = 1;
            float floatValue = 0.0f;
            bool isInt = true;

            foreach (Sexp sexp in arguments)
            {
                if (sexp.Type == SexpType.LIST || sexp.Type == SexpType.IDENT)
                {
                    Sexp s = Evaluator.Evaluate(ref e, ms, sexp);
                    switch (s.Type)
                    {
                        case SexpType.INT:
                            if (isInt)
                            {
                                intValue *= s.IntValue;
                            }
                            else
                            {
                                floatValue *= (float)s.IntValue;
                            }
                            break;
                        case SexpType.FLOAT:
                            if (isInt)
                            {
                                isInt = false;
                                floatValue = (float)intValue;
                                floatValue *= s.FloatValue;
                            }
                            else
                            {
                                floatValue *= s.FloatValue;
                            }
                            break;
                        default:
                            throw new CrispArgumentException("'*' function expects arguments to be numbers", sexp.Line);
                    }
                }
                else
                {
                    switch (sexp.Type)
                    {
                        case SexpType.INT:
                            if (isInt)
                            {
                                intValue *= sexp.IntValue;
                            }
                            else
                            {
                                floatValue *= (float)sexp.IntValue;
                            }
                            break;
                        case SexpType.FLOAT:
                            if (isInt)
                            {
                                isInt = false;
                                floatValue = (float)intValue;
                                floatValue *= sexp.FloatValue;
                            }
                            else
                            {
                                floatValue *= sexp.FloatValue;
                            }
                            break;
                        default:
                            throw new CrispArgumentException("'*' function expects arguments to be numbers", sexp.Line);
                    }
                }
            }
            if (isInt)
            {
                return new Sexp(SexpType.INT, intValue.ToString(), -1);
            }
            return new Sexp(SexpType.FLOAT, floatValue.ToString(), -1);
        }

        /// <summary>
        /// This function performs the subtraction operation over two or more numeric values (int or float).
        /// If a float is introduced the resulting value will be float also. Division by 0 is an error.<para/>
        /// - sexps : ints or floats<para/>
        /// <example>
        /// <code>
        /// (/ 10 2) ; 5 (int)<para/>
        /// (/ 10 3) ; 3 (int)<para/>
        /// (/ 10 3.0) ; 3.33333 (int)<para/>
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <param name="ms">Output Stream</param>
        /// <param name="arguments">Arguments to be passed</param>
        /// <returns>Sexp int or float</returns>
        public static Sexp Div(ref Environment e, MemoryStream ms, Sexp arguments)
        {
            bool first = true;
            int intValue = 0;
            float floatValue = 0.0f;
            bool isInt = true;

            foreach (Sexp sexp in arguments)
            {
                if (sexp.Type == SexpType.LIST || sexp.Type == SexpType.IDENT)
                {
                    Sexp s = Evaluator.Evaluate(ref e, ms, sexp);
                    switch (s.Type)
                    {
                        case SexpType.INT:
                            if (first)
                            {
                                intValue = s.IntValue;
                                first = false;
                            }
                            else
                            {
                                if (isInt)
                                {
                                    intValue /= s.IntValue;
                                }
                                else
                                {
                                    floatValue /= (float)s.IntValue;
                                }
                            }
                            break;
                        case SexpType.FLOAT:
                            if (first)
                            {
                                isInt = false;
                                floatValue = s.FloatValue;
                                first = false;
                            }
                            else
                            {
                                if (isInt)
                                {
                                    isInt = false;
                                    floatValue = (float)intValue;
                                    floatValue /= s.FloatValue;
                                }
                                else
                                {
                                    floatValue /= s.FloatValue;
                                }
                            }
                            break;
                        default:
                            throw new CrispArgumentException("'/' function expects arguments to be numbers", sexp.Line);
                    }
                }
                else
                {
                    switch (sexp.Type)
                    {
                        case SexpType.INT:
                            if (first)
                            {
                                intValue = sexp.IntValue;
                                first = false;
                            }
                            else
                            {
                                if (isInt)
                                {
                                    if (sexp.IntValue == 0.0)
                                    {
                                        throw new CrispArgumentException("'/' function cannot divide by zero", sexp.Line);
                                    }
                                    intValue /= sexp.IntValue;
                                }
                                else
                                {
                                    if (sexp.IntValue == 0)
                                    {
                                        throw new CrispArgumentException("'/' function cannot divide by zero", sexp.Line);
                                    }
                                    floatValue /= (float)sexp.IntValue;
                                }
                            }
                            break;
                        case SexpType.FLOAT:
                            if (first)
                            {
                                first = false;
                                floatValue = sexp.FloatValue;
                                isInt = false;
                            }
                            else
                            {
                                if (isInt)
                                {
                                    isInt = false;
                                    floatValue = (float)intValue;
                                    if (sexp.FloatValue == 0.0)
                                    {
                                        throw new CrispArgumentException("'/' function cannot divide by zero", sexp.Line);
                                    }
                                    floatValue /= sexp.FloatValue;
                                }
                                else
                                {
                                    if (sexp.FloatValue == 0.0)
                                    {
                                        throw new CrispArgumentException("'/' function cannot divide by zero", sexp.Line);
                                    }
                                    floatValue /= sexp.FloatValue;
                                }
                            }
                            break;
                        default:
                            throw new CrispArgumentException("'/' function expects arguments to be numbers", sexp.Line);
                    }
                }
            }
            if (isInt)
            {
                return new Sexp(SexpType.INT, intValue.ToString(), -1);
            }
            return new Sexp(SexpType.FLOAT, floatValue.ToString(), -1);
        }

        /// <summary>
        /// This function performs the exponentiation operator on the first parameter to the power of the second parameter.
        /// If two integers are passed the resulting value will be an int but if either is a floating pointer number
        /// then the result will be a float.<para/>
        /// <example>
        /// <code>
        /// (pow 2 2) ; 4 (int)<para/>
        /// (pow 10 2.3) ; 199.52621 (float)<para/>
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <param name="ms">Output Stream</param>
        /// <param name="arguments">Arguments to be passed</param>
        /// <returns>Sexp int or float</returns>
        public static Sexp Pow(ref Environment e, MemoryStream ms, Sexp arguments)
        {
            if (arguments.Length == 2)
            {
                // a to the power of b
                Sexp a = Evaluator.Evaluate(ref e, ms, arguments[0]);
                Sexp b = Evaluator.Evaluate(ref e, ms, arguments[1]);

                if (a.Type == SexpType.INT)
                {
                    if (b.Type == SexpType.INT)
                    {
                        return new Sexp(SexpType.INT, ((int)Math.Pow((double)a.IntValue, (double)b.IntValue)).ToString(), arguments.Line);
                    }
                    else if (b.Type == SexpType.FLOAT)
                    {
                        return new Sexp(SexpType.FLOAT, ((float)Math.Pow((double)a.IntValue, (double)b.FloatValue)).ToString(), arguments.Line);
                    }
                    else
                    {
                        throw new CrispArgumentException("'pow' function requires both arguments to be either int or float", arguments.Line);
                    }
                }
                else if (a.Type == SexpType.FLOAT)
                {
                    if (b.Type == SexpType.INT)
                    {
                        return new Sexp(SexpType.FLOAT, ((float)Math.Pow((double)a.FloatValue, (double)b.IntValue)).ToString(), arguments.Line);
                    }
                    else if (b.Type == SexpType.FLOAT)
                    {
                        return new Sexp(SexpType.FLOAT, ((float)Math.Pow((double)a.FloatValue, (double)b.FloatValue)).ToString(), arguments.Line);
                    }
                    else
                    {
                        throw new CrispArgumentException("'pow' function requires both arguments to be either int or float", arguments.Line);
                    }
                }
                else
                {
                    throw new CrispArgumentException("'pow' function requires both arguments to be either int or float", arguments.Line);
                }
            }
            throw new CrispArgumentException(String.Format("'pow' function requires exactly 2 arguments, got {0}", arguments.Length), arguments.Line);
        }

        /// <summary>
        /// This function performs the square root operation on the only parameter. The return type is an int if
        /// the parameter is an int otherwise a float. It is an illegal operation to pass a negative number.<para/>
        /// <example>
        /// <code>
        /// (pow 2 2) ; 4 (int)<para/>
        /// (pow 10 2.3) ; 199.52621 (float)<para/>
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="e">Current environment</param>
        /// <param name="ms">Output Stream</param>
        /// <param name="arguments">Arguments to be passed</param>
        /// <returns>Sexp int or float</returns>
        public static Sexp Sqrt(ref Environment e, MemoryStream ms, Sexp arguments)
        {
            if (arguments.Length == 1)
            {
                Sexp arg = Evaluator.Evaluate(ref e, ms, arguments[0]);

                if (arg.Type == SexpType.INT)
                {
                    if (arg.IntValue >= 0)
                    {
                        return new Sexp(SexpType.INT, Math.Sqrt(arg.IntValue).ToString(), arguments.Line);
                    }
                    else
                    {
                        throw new CrispArgumentException(String.Format("'sqrt' function expects an non-negative numeric value, got {0}'", arg.IntValue), arguments.Line);
                    }
                }
                else if (arg.Type == SexpType.FLOAT)
                {
                    if (arg.FloatValue >= 0.0f)
                    {
                        return new Sexp(SexpType.FLOAT, Math.Sqrt(arg.FloatValue).ToString(), arguments.Line);
                    }
                    else
                    {
                        throw new CrispArgumentException(String.Format("'sqrt' function expects an non-negative numeric value, got {0}'", arg.FloatValue), arguments.Line);
                    }
                }
                else
                {
                    throw new CrispArgumentException("'sqrt' function expects an argument that is either float or int", arguments.Line);
                }
            }
            throw new CrispArgumentException(String.Format("'sqrt' function requires exactly 1 argument, got {0}", arguments.Length), arguments.Line);
        }
    }
}
