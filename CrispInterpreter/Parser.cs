using System;
using System.Collections.Generic;

namespace Crisp
{
    public class Parser
    {
        /// <summary>
        /// This function takes a string of data of presumed valid crisp code.
        /// </summary>
        /// <param name="input">string containing code</param>
        /// <returns>A list of all Sexp values to be passed to the evaluator</returns>
        public Sexp Parse(String input)
        {
            int tokensRead = 0;
            int numberOfParens = 0;
            List<Token> tokens = new List<Token>(new Lexer(input));
            return Parse(tokens, ref tokensRead, ref numberOfParens);
        }

        /// <summary>
        /// This function turns a list of tokens into a list of Sexps. It recursively creates Sexps
        /// of type list, every top-level function call are actually stored in an Sexp
        /// </summary>
        /// <param name="tokens">The list of tokens to read</param>
        /// <param name="i">A shared index to keep track of next token throughout the recursive calls</param>
        /// <param name="numberOfParens">A shared interger that marks how many open parenthesises</param>
        /// <returns>A list of all Sexp values to be passed to the evaluator</returns>
        private Sexp Parse(List<Token> tokens, ref int i, ref int numberOfParens)
        {
            Sexp curr = new Sexp();
            for (; i < tokens.Count; ++i)
            {
                Token t = tokens[i];
                switch (t.Type)
                {
                    case TokenType.OPEN_PAREN:
                        ++i; // needs to be incremented here to stop an infinite recursion error
                        ++numberOfParens;
                        curr.Type = SexpType.LIST;
                        curr.Add(Parse(tokens, ref i, ref numberOfParens));
                        break;
                    case TokenType.CLOSE_PAREN:
                        --numberOfParens;
                        // tests if there is a surplus ')' token
                        if (numberOfParens < 0)
                        {
                            throw new CrispParserExpcetion("Mismatched parenthesises", t.Line);
                        }
                        return curr;
                    case TokenType.INTEGER_LITERAL:
                        curr.Add(new Sexp(SexpType.INT, t.Value, t.Line));
                        break;
                    case TokenType.FLOAT_LITERAL:
                        curr.Add(new Sexp(SexpType.FLOAT, t.Value, t.Line));
                        break;
                    case TokenType.CHAR_LITERAL:
                        curr.Add(new Sexp(SexpType.CHAR, t.Value, t.Line));
                        break;
                    case TokenType.IDENT:
                        curr.Add(new Sexp(SexpType.IDENT, t.Value, t.Line));
                        break;
                    case TokenType.STRING_LITERAL:
                        curr.Add(new Sexp(SexpType.STRING, t.Value, t.Line));
                        break;
                    case TokenType.BOOL_LITERAL:
                        curr.Add(new Sexp(SexpType.BOOL, t.Value, t.Line));
                        break;
                    case TokenType.EOF:
                        // In a valid program there should be no unmatched '(' character
                        if (numberOfParens != 0)
                        {
                            throw new CrispParserExpcetion("Mismatched parenthesises", tokens[i-1].Line);
                        }
                        break;
                    default:
                        break;
                }
            }
            return curr;
        }
    }
}
