﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DotLisp.Exceptions;
using DotLisp.Types;

namespace DotLisp.Parsing
{
    public static class Parser
    {
        public static string ToLisp(DotExpression exp)
        {
            if (exp is DotList l)
            {
                return "(" + string.Join(" ",
                               l.Expressions.Select(ToLisp))
                           + ")";
            }

            return exp.ToString();
        }

        public static List<string> Tokenize(string input)
        {
            return input
                .Replace("(", " ( ")
                .Replace(")", " ) ")
                .Split(" ")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }

        public static DotExpression Parse(string program)
        {
            return ReadFromTokens(Tokenize(program));
        }

        public static DotExpression ReadFromTokens(List<string> tokens)
        {
            if (tokens.Count == 0)
            {
                throw new ParserException("Unexpected EOF!");
            }

            var token = tokens[0];
            tokens.RemoveAt(0);

            switch (token)
            {
                case "(":
                {
                    if (tokens.Count == 0)
                    {
                        throw new ParserException("Missing ')'!");
                    }

                    var l = new LinkedList<DotExpression>();
                    while (tokens[0] != ")")
                    {
                        l.AddLast(ReadFromTokens(tokens));
                    }

                    tokens.RemoveAt(0);
                    return new DotList()
                    {
                        Expressions = l
                    };
                }
                case ")":
                    throw new ParserException("Unexpected ')'!");
                default:
                    return ParseAtom(token);
            }
        }

        public static DotAtom ParseAtom(string token)
        {
            if (token[0] == '"')
            {
                return new DotString
                {
                    Value =
                        token.Substring(1, token.Length - 2)
                };
            }

            if (token == "true" || token == "false")
            {
                return new DotBool()
                {
                    Value = bool.Parse(token)
                };
            }

            if (int.TryParse(token, out var integer))
            {
                return new DotNumber()
                {
                    Int = integer
                };
            }

            if (float.TryParse(token,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var floating))
            {
                return new DotNumber()
                {
                    Float = floating
                };
            }

            return new DotSymbol(token);
        }
    }

    public class InPort
    {
        private readonly Regex _tokenizer = new Regex(
            @"\s*(,@|[('`,)]|""(?:[\\].|[^\\""])*""|;.*|[^\s('""`,;)]*)(.*)"
        );

        private Dictionary<string, string> _quotes = new Dictionary<string, string>()
        {
            ["'"] = "quote",
            ["`"] = "quasiquote",
            [","] = "unquote",
            [",@"] = "unquotesplicing"
        };

        private StreamReader _inputStream;

        private string _line = "";

        public InPort()
        {
        }

        public InPort(StreamReader inputStream)
        {
            _inputStream = inputStream;
        }

        public InPort(string input) : this(
            new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(input))))
        {
        }

        public string NextToken()
        {
            while (true)
            {
                if (string.IsNullOrEmpty(_line))
                {
                    _line = _inputStream.ReadLine();
                }

                if (string.IsNullOrEmpty(_line) && _inputStream.EndOfStream)
                {
                    return null;
                }

                var match = _tokenizer.Match(_line);
                var token = match.Groups[1].Value.Trim();
                _line = _line.ReplaceFirst(token, "").Trim();

                if (token != "" && !token.StartsWith(";"))
                {
                    return token;
                }
            }
        }

        public static string ReadChar(InPort inPort)
        {
            if (inPort._line != "")
            {
                var ch = "" + inPort._line[0];
                inPort._line = inPort._line.Substring(1);
                return ch;
            }
            else
            {
                return "" + Convert.ToChar(inPort._inputStream.Read());
            }
        }

        private DotExpression ReadAhead(string token)
        {
            switch (token)
            {
                case "(":
                    var l = new DotList()
                    {
                        Expressions = new LinkedList<DotExpression>()
                    };
                    while (true)
                    {
                        token = NextToken();
                        if (token == ")")
                        {
                            return l;
                        }

                        l.Expressions.AddLast(ReadAhead(token));
                    }

                case ")":
                    throw new ParserException("Unexpected ')'!");
            }

            if (_quotes.ContainsKey(token))
            {
                // convert to real expression
                var keyword = _quotes[token];
                var exps = new LinkedList<DotExpression>();

                exps.AddLast(new DotSymbol(keyword));
                exps.AddLast(Read());

                return new DotList
                {
                    Expressions = exps
                };
            }

            return Parser.ParseAtom(token);
        }

        public DotExpression Read()
        {
            var token1 = NextToken();
            return token1 == null ? null : ReadAhead(token1);
        }

        public DotExpression Read(string input)
        {
            _inputStream =
                new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(input)));
            return Read();
        }

        public DotExpression Read(FileStream fileStream)
        {
            _inputStream = new StreamReader(fileStream);
            return Read();
        }
    }
}