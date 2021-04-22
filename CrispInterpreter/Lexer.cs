using System;
using System.Collections;
using System.Collections.Generic;

namespace Crisp
{
    public class Lexer : IEnumerable<Token>
    {
        // valid symbols in an identifer
        private static string _listOfValidSymbols = "+-*/<>=!?£$€%^&*@";
        private String _input;

        public Lexer(String input)
        {
            _input = input;
        }

        /// <summary>
        /// Check if a char is a valid indentifier. If it is the first char in an ident
        /// then it can only be a alphabetic character of from the subset _listOfValidSymbols.
        /// But if it isn't it can also be a number.
        /// </summary>
        /// <param name="c">Char to check</param>
        /// <param name="first">If it's the first char in a token</param>
        /// <returns>bool</returns>
        private bool isValidIdentifer(char c, bool first = false)
        {
            if (!first)
            {
                return Char.IsLetterOrDigit(c) || _listOfValidSymbols.Contains(c);
            }
            return Char.IsLetter(c) || _listOfValidSymbols.Contains(c);
        }

        /// <summary>
        /// Generator method that yields tokens as they are found. Can be collected by a container object
        /// or through a foreach loop
        /// </summary>
        /// <returns>Yields tokens as they are found</returns>
        public IEnumerator<Token> GetEnumerator()
        {
            String currToken = String.Empty;
            TokenType type = TokenType.INVALID;
            bool stringEscapeSequence = false;
            bool charEscapeSequence = false;
            int line = 1;

            foreach (char c in _input)
            {
                // INVALID is used a placeholder for a currently unknown token type
                // this if statement is a way to determine an inital type when no data has been found
                // for the current token
                if (type == TokenType.INVALID)
                {
                    currToken = "";
                    currToken += c;
                    if (Char.IsDigit(c)) // if is numeric then it is likely an int
                    {
                        type = TokenType.INTEGER_LITERAL;
                    }
                    else if (c == '(') // can be added directly
                    {
                        yield return new Token(TokenType.OPEN_PAREN, currToken, line);
                    }
                    else if (c == ')') // can be added directly
                    {
                        yield return new Token(TokenType.CLOSE_PAREN, currToken, line);
                    }
                    else if (isValidIdentifer(c, true)) // start of a ident
                    {
                        type = TokenType.IDENT;
                    }
                    else if (c == '"')
                    {
                        type = TokenType.STRING_LITERAL;
                        currToken = String.Empty;
                    }
                    else if (c == '\'')
                    {
                        type = TokenType.CHAR_LITERAL;
                        currToken = String.Empty;
                    }
                    else if (c == '#') // a bool literal is #F or #T
                    {
                        type = TokenType.BOOL_LITERAL;
                        currToken = String.Empty;
                        continue; // jump to next character
                    }
                    else if (Char.IsWhiteSpace(c)) // whitespace can be ignore in this stage
                    {
                        if (c == '\n')
                        {
                            ++line;
                        }
                        continue;
                    }
                    else if (c == ';')
                    {
                        type = TokenType.COMMENT;
                        continue;
                    } 
                    else
                    {
                        break;
                    }
                    continue;
                }
                // only branches if a type has been determined
                switch (type)
                {
                    case TokenType.IDENT:
                        if (isValidIdentifer(c)) // continue to buffer if this char is a valid character for an ident
                        {
                            currToken += c;
                        }
                        else
                        {
                            yield return new Token(TokenType.IDENT, currToken, line);
                            currToken = String.Empty;
                            type = TokenType.INVALID;
                            // this section is here so a paren is not missed as they are not valid char for an ident so
                            // could cause an error if missed
                            if (c == '(')
                            {
                                currToken += c;
                                type = TokenType.OPEN_PAREN;
                                yield return new Token(type, currToken, line);
                                currToken = String.Empty;
                                type = TokenType.INVALID;
                            }
                            else if (c == ')')
                            {
                                currToken += c;
                                type = TokenType.CLOSE_PAREN;
                                yield return new Token(type, currToken, line);
                                currToken = String.Empty;
                                type = TokenType.INVALID;
                            }
                        }
                        if (c == '\n')
                        {
                            ++line;
                        }
                        break;
                    case TokenType.BOOL_LITERAL:
                        if (c == 'T' || c == 'F') // T and F are only valid values for a bool
                        {
                            yield return new Token(type, c.ToString(), line);
                            type = TokenType.INVALID;
                        }
                        else
                        {
                            throw new CrispException(String.Format("#{0} is an invalid symbol for a boolean literal, use #T or #F", c), line);
                        }
                        break;
                    case TokenType.STRING_LITERAL:
                        if (stringEscapeSequence) // if a '\' character has been found
                        {
                            if (c == '"')
                            {
                                currToken += c;
                            }
                            else if (c == '\\')
                            {
                                currToken += c;
                            }
                            else if (c == 'n')
                            {
                                currToken += '\n';
                            }
                            else if (c == 'r')
                            {
                                currToken += '\r';
                            }
                            else if (c == 't')
                            {
                                currToken += '\t';
                            }
                            stringEscapeSequence = false;
                        }
                        else if (c == '\\')
                        {
                            stringEscapeSequence = true;
                        }
                        else if (c == '"') // end of string as no '\' was found before it
                        {
                            yield return new Token(type, currToken, line);
                            type = TokenType.INVALID;
                            currToken = String.Empty;
                        }
                        else
                        {
                            currToken += c;
                        }
                        break;
                    case TokenType.CHAR_LITERAL:
                        if (charEscapeSequence)
                        {
                            if (c == '"')
                            {
                                currToken += c;
                            }
                            else if (c == '\\')
                            {
                                currToken += c;
                            }
                            else if (c == 'n')
                            {
                                currToken += '\n';
                            }
                            else if (c == 'r')
                            {
                                currToken += '\r';
                            }
                            else if (c == 't')
                            {
                                currToken += '\t';
                            }
                            charEscapeSequence = false;
                        }
                        else if (c == '\\')
                        {
                            charEscapeSequence = true;
                        }
                        else if (c == '\'')
                        {
                            yield return new Token(type, currToken, line);
                            type = TokenType.INVALID;
                            currToken = String.Empty;
                        }
                        else
                        {
                            currToken += c;
                        }
                        break;
                    case TokenType.INTEGER_LITERAL:
                        if (Char.IsDigit(c) || c == '.') // valid int value or beginning of float
                        {
                            if (c == '.') // actually is a float literla
                            {
                                type = TokenType.FLOAT_LITERAL;
                            }
                            currToken += c;
                        }
                        else
                        {
                            yield return new Token(type, currToken, line);
                            type = TokenType.INVALID;
                            currToken = String.Empty;
                            // this section is here so a paren is not missed as they are not valid char for an int so
                            // could cause an error if missed
                            if (c == '(')
                            {
                                currToken += c;
                                type = TokenType.OPEN_PAREN;
                                yield return new Token(type, currToken, line);
                                currToken = String.Empty;
                                type = TokenType.INVALID;
                            }
                            else if (c == ')')
                            {
                                currToken += c;
                                type = TokenType.CLOSE_PAREN;
                                yield return new Token(type, currToken, line);
                                currToken = String.Empty;
                                type = TokenType.INVALID;
                            }
                        }
                        if (c == '\n')
                        {
                            ++line;
                        }
                        break;
                    case TokenType.FLOAT_LITERAL:
                        if (Char.IsDigit(c))
                        {
                            currToken += c;
                        }
                        else if (c == '.')
                        {
                            throw new CrispParserExpcetion("A second '.' character in float is illegal", line);
                        }
                        else
                        {
                            yield return new Token(type, currToken, line);
                            type = TokenType.INVALID;
                            currToken = String.Empty;
                            if (c == '(')
                            {
                                currToken += c;
                                type = TokenType.OPEN_PAREN;
                                yield return new Token(type, currToken, line);
                                currToken = String.Empty;
                                type = TokenType.INVALID;
                            }
                            else if (c == ')')
                            {
                                currToken += c;
                                type = TokenType.CLOSE_PAREN;
                                yield return new Token(type, currToken, line);
                                currToken = String.Empty;
                                type = TokenType.INVALID;
                            }
                        }
                        if (c == '\n')
                        {
                            ++line;
                        }
                        break;
                    case TokenType.COMMENT:
                        if (c == '\n')
                        {
                            ++line;
                            type = TokenType.INVALID;
                        }
                        break;
                    default:
                        break;
                }
            }
            if (currToken.Length > 0)
            {
                if (type != TokenType.COMMENT)
                {
                    yield return new Token(type, currToken, line);
                }
            }
            yield return Token.EOF();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
