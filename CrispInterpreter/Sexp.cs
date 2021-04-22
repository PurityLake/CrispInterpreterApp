using System;
using System.Collections;
using System.Collections.Generic;

namespace Crisp
{
    public enum SexpType
    {
        LIST,
        BOOL,
        IDENT,
        INT,
        FLOAT,
        STRING,
        CHAR,
        NONE
    }

    /// <summary>
    /// S Expression<para/>
    /// This is the universal type for objects in Crisp. It can be a list, an identifier,
    /// an int, a float, a char or a bool. The logic to get valid values is defined in this
    /// class alongside some helper methods that makes dealing with these values safe.
    /// </summary>
    public class Sexp : IEnumerable<Sexp>
    {
        private SexpType _type;
        // if is a list, here are the sexps it contains
        private List<Sexp> _sexps;
        // stores value for string, identifier or char
        private String _value;
        private int _int;
        private float _float;
        private bool _bool;

        // predefined value for function that returns nothing
        public static Sexp Void = new Sexp(SexpType.NONE, "", -1);
        // true or false objects
        public static Sexp True = new Sexp(SexpType.BOOL, "T", -1);
        public static Sexp False = new Sexp(SexpType.BOOL, "F", -1);

        public SexpType Type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;
            }
        }

        public String Value
        {
            get
            {
                if (Type == SexpType.IDENT || Type == SexpType.STRING || Type == SexpType.CHAR)
                {
                    return _value;
                }
                throw new CrispException("Internal error: tried to access a value with the wrong type", -1);
            }
        }

        public int IntValue
        {
            get
            {
                if (Type == SexpType.INT)
                {
                    return _int;
                }
                throw new CrispException("Internal error: tried to access a value with the wrong type", -1);
            }
        }

        public float FloatValue
        {
            get
            {
                if (Type == SexpType.FLOAT)
                {
                    return _float;
                }
                throw new CrispException("Internal error: tried to access a value with the wrong type", -1);
            }
        }

        public bool BoolValue
        {
            get
            {
                if (Type == SexpType.BOOL)
                {
                    return _bool;
                }
                throw new CrispException("Internal error: tried to access a value with the wrong type", -1);
            }
        }

        /// <summary>
        /// Defaults to 0 if the sexp is not a list otherwise it returns the number of sexps in the list
        /// </summary>
        public int Length
        {
            get
            {
                if (Type == SexpType.LIST)
                {
                    return _sexps.Count;
                }
                return 0;
            }
        }

        /// <summary>
        /// Line the Sexp was found on. -1 if the Sexp is created post parsing.
        /// </summary>
        public int Line
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the sexp at the index-th entry. Throws CrispException if not a list.
        /// </summary>
        /// <param name="index">index</param>
        /// <returns>Sexp</returns>
        public Sexp this[int index]
        {
            get
            {
                if (Type == SexpType.LIST)
                {
                    if (index < Length)
                    {
                        return _sexps[index];
                    }
                    throw new IndexOutOfRangeException();
                }
                if (index == 0)
                {
                    return this;
                }
                throw new CrispException("Internal error: tried to access index of a non-indexable value", -1);
            }
            set
            {
                if (Type == SexpType.LIST)
                {
                    if (index < Length)
                    {
                        _sexps[index] = value;
                    }
                    else
                    {
                        throw new CrispException("Internal error: tried to access index of a non-indexable value", -1);
                    }
                }
                else
                {
                    throw new CrispException("Internal error: tried to access index of a non-indexable value", -1);
                }
            }
        }

        /// <summary>
        /// Gets the the subset of a list from the index, counting upwards to <c>count</c> values. Throws if not a list
        /// </summary>
        /// <param name="start">index to start from</param>
        /// <param name="count">number of elements</param>
        /// <returns></returns>
        public Sexp this[int start, int count]
        {
            get
            {
                return Sexp.FromList(_sexps.GetRange(start, count));
            }
        }

        public Sexp()
        {
            Line = -2; // default value set elsewhere
            _type = SexpType.LIST;
            _sexps = new List<Sexp>(10); // reserve 10 pieces of memory
            _value = String.Empty;
        }

        /// <summary>
        /// Preferred constructor call for creating non list values
        /// </summary>
        /// <param name="type">Type of Sexp</param>
        /// <param name="value">Value to be interpreted</param>
        /// <param name="line">The line the Sexp is found at: defaults to -1</param>
        public Sexp(SexpType type, String value, int line)
        {
            Line = line;
            _type = type;
            if (type == SexpType.INT)
            {
                _int = int.Parse(value);
            }
            else if (type == SexpType.FLOAT)
            {
                _float = float.Parse(value);
            }
            else if (type == SexpType.BOOL)
            {
                if (value == "T")
                {
                    _bool = true;
                }
                else
                {
                    _bool = false;
                }
            }
            else
            {
                _value = value;
            }
        }

        /// <summary>
        /// Helper method to create a Sexp from a generic List&lt;Sexp&gt; type</Sexp>
        /// </summary>
        /// <param name="list">list to be transformed</param>
        /// <returns>Sexp</returns>
        public static Sexp FromList(List<Sexp> list)
        {
            Sexp output = new Sexp();
            output._sexps = list;
            output._type = SexpType.LIST;
            return output;
        }

        /// <summary>
        /// Adds to a list Sexp
        /// </summary>
        /// <param name="sexp"></param>
        public void Add(Sexp sexp)
        {
            _sexps.Add(sexp);
            if (Line == -2)
            {
                Line = sexp.Line;
            }
        }

        public override string ToString()
        {
            String output = ""; 
            switch (_type)
            {
                case SexpType.LIST:
                    output += "(";
                    foreach (Sexp sexp in _sexps)
                    {
                        output += sexp.ToString();
                        output += ' ';
                    }
                    output = output.TrimEnd();
                    output += ")";
                    break;
                case SexpType.STRING:
                    output += _value;
                    break;
                case SexpType.CHAR:
                    output += _value;
                    break;
                case SexpType.INT:
                    output += _int.ToString();
                    break;
                case SexpType.FLOAT:
                    output += _float.ToString();
                    break;
                case SexpType.BOOL:
                    output += _bool.ToString();
                    break;
                default:
                    output += _value;
                    break;
            }
            return output;
        }

        public IEnumerator<Sexp> GetEnumerator()
        {
            if (_type == SexpType.LIST)
            {
                foreach (Sexp sexp in _sexps)
                {
                    yield return sexp;
                }
            }
            else
            {
                yield return this;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
