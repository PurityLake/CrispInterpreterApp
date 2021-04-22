using System;

namespace Crisp
{
    /// <summary>
    /// Thrown if there is a general error in the program
    /// </summary>
    public class CrispException : Exception
    {
        public int Line
        {
            get;
            private set;
        }
        public CrispException()
        {
        }

        public CrispException(string message, int line)
            : base(message)
        {
            Line = line;
        }

        public CrispException(string message, int line, Exception inner)
            : base(message, inner)
        {
            Line = line;
        }
    }

    /// <summary>
    /// Thrown if a identifer can't be found in a namespace
    /// </summary>
    public class CrispNotExistsException : CrispException
    {
        public CrispNotExistsException()
        {
        }

        public CrispNotExistsException(string message, int line)
            : base(message, line)
        {
        }

        public CrispNotExistsException(string message, int line, Exception inner)
            : base(message, line, inner)
        { 
        }
    }

    /// <summary>
    /// Thrown if there is an error throughout the parse process (includes lexer)
    /// </summary>
    public class CrispParserExpcetion : CrispException
    {
        public CrispParserExpcetion()
        {
        }

        public CrispParserExpcetion(string message, int line)
            : base(message, line)
        {
        }

        public CrispParserExpcetion(string message, int line, Exception inner)
            : base(message, line, inner)
        {
        }
    }

    /// <summary>
    /// Thrown if there is an error regarding arguments in a function both user and builtin
    /// </summary>
    public class CrispArgumentException : CrispException
    {
        public CrispArgumentException()
        {
        }

        public CrispArgumentException(string message, int line)
            : base(message, line)
        {
        }

        public CrispArgumentException(string message, int line, Exception inner)
            : base(message, line, inner)
        {
        }
    }
}
