using System;

namespace Crisp
{
    public enum TokenType
    {
        OPEN_PAREN,
        CLOSE_PAREN,
        COMMENT,
        IDENT,
        INTEGER_LITERAL,
        FLOAT_LITERAL,
        STRING_LITERAL,
        CHAR_LITERAL,
        BOOL_LITERAL,
        INVALID,
        EOF
    }

    public class Token
    {
        public TokenType Type
        {
            get;
        }

        public String Value
        {
            get;
        }

        public int Line
        {
            get;
        }

        public Token(TokenType type, String value, int line)
        {
            Type = type;
            Value = value;
            Line = line;
        }

        private static Token _eof = new Token(TokenType.EOF, "", -1);

        public static Token EOF()
        {
            return _eof;
        }
    }
}
