using System;
using System.Collections.Generic;
using System.IO;

namespace Crisp
{
    public class Evaluator
    {
        /// <summary>
        /// Evaluates an Sexp using a provided environment returning it's value
        /// </summary>
        /// <param name="e">Supplied environment</param>
        /// <param name="ms">Stream that functions can output to</param>
        /// <param name="sexp">Sexp to be evaluated</param>
        /// <returns>Sexp</returns>
        public static Sexp Evaluate(ref Environment e, MemoryStream ms, Sexp sexp)
        {
            foreach (Sexp innerSexp in sexp)
            {
                // lists can be evaluated if it doesn't start with a ident
                if (innerSexp.Type == SexpType.LIST)
                {
                    if (innerSexp[0].Type == SexpType.IDENT)
                    {
                        Evaluate(ref e, ms, innerSexp);
                    }
                    else
                    {
                        return innerSexp;
                    }
                }
                else if (innerSexp.Type == SexpType.IDENT) // has to be resolved
                {
                    // collects all sexps
                    List<Sexp> allSexps = new List<Sexp>(sexp);
                    if (allSexps.Count > 1) // has been given arguments
                    {
                        // check for builtin function, if found it executes it
                        Environment.CrispFunction cf;
                        if (e.TryGetBuiltinFunction(innerSexp.Value, out cf))
                        {
                            return cf(ref e, ms, Sexp.FromList(allSexps.GetRange(1, allSexps.Count - 1)));
                        }
                        else
                        {
                            // check for a user defined function, if found executes it
                            CrispUserFunction cuf;
                            if (e.TryGetUserFunction(innerSexp.Value, out cuf))
                            {
                                return cuf.Call(ref e, ms, Sexp.FromList(allSexps.GetRange(1, allSexps.Count - 1)));
                            }
                            else
                            {
                                // check for a varible, if found return it
                                Sexp s;
                                if (e.TryGetVariable(innerSexp.Value, out s))
                                {
                                    return s;
                                }
                                else
                                {
                                    // not found
                                    throw new CrispNotExistsException(String.Format("'{0}' does not exist in this namespace", innerSexp.Value), innerSexp.Line);
                                }
                            }
                        }
                    }
                    else // no args added
                    {
                        // check for builtin function, if found it executes it
                        Environment.CrispFunction cf;
                        if (e.TryGetBuiltinFunction(innerSexp.Value, out cf))
                        {
                            return cf(ref e, ms, Sexp.Void);
                        }
                        else
                        {
                            // check for a user defined function, if found executes it
                            CrispUserFunction cuf;
                            if (e.TryGetUserFunction(innerSexp.Value, out cuf))
                            {
                                return cuf.Call(ref e, ms, Sexp.Void);
                            }
                            else
                            {
                                // check for a varible, if found return it
                                Sexp s;
                                if (e.TryGetVariable(innerSexp.Value, out s))
                                {
                                    return s;
                                }
                                else
                                {
                                    // not found
                                    throw new CrispNotExistsException(String.Format("'{0}' does not exist in this namespace", innerSexp.Value), innerSexp.Line);
                                }
                            }
                        }
                    }
                }
                else
                {
                    return innerSexp;
                }
            }
            return Sexp.Void;
        }
    }
}
