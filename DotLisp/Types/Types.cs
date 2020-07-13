﻿using System;
using System.Collections.Generic;
using System.Linq;
using DotLisp.Environments;
using DotLisp.Exceptions;
using DotLisp.Parsing;
using Environment = DotLisp.Environments.Environment;

namespace DotLisp.Types
{
    public abstract class Expression
    {
        public new abstract string ToString();
    }

    public abstract class Atom : Expression
    {
    }

    public class Symbol : Atom
    {
        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public class Func : Expression
    {
        public delegate Expression DoSomething(Expression args);

        public DoSomething Action { get; set; }

        public override string ToString()
        {
            return $"builtin function '{Action.Method.Name}'";
        }
    }

    public class List : Expression
    {
        public List<Expression> Expressions { get; set; }

        public override string ToString()
        {
            return "(" +
                   string.Join(" ", Expressions.Select(e => e.ToString()).ToList()) +
                   ")";
        }
    }

    public class Number : Atom
    {
        public int? Int { get; set; }
        public float? Float { get; set; }

        public bool IsFloat() => this.Float != null;

        public float GetValue() =>
            IsFloat() ? this.Float!.Value : this.Int!.Value;

        public override string ToString()
        {
            return IsFloat() ? Float!.ToString() : Int!.ToString();
        }
    }

    public class Bool : Atom
    {
        public bool Value { get; set; }

        public Bool(bool value)
        {
            Value = value;
        }

        public Bool()
        {
        }

        public static Bool True()
        {
            return new Bool()
            {
                Value = true
            };
        }

        public static Bool False()
        {
            return new Bool()
            {
                Value = false
            };
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    // TODO: Merge with Func?
    public class Procedure : Expression
    {
        private readonly List<string> _parameters;

        private readonly Expression _body;

        private readonly Environment _env;

        public Procedure(Expression parameters, Expression body,
            Environment env)
        {
            if (!(parameters is List l))
            {
                throw new EvaluatorException("Parameter list expected!");
            }

            Console.WriteLine(l.PrettyPrint());

            var names = l.Expressions.Cast<Symbol>().Select(s => s.Name)
                .ToList();

            _parameters = names;
            _body = body;
            _env = env;
        }

        public Expression Call(Expression args)
        {
            List<Expression> exps;
            if (!(args is List l))
            {
                exps = new List<Expression>() {args};
            }
            else
            {
                exps = l.Expressions;
            }

            return Evaluator.Eval(_body,
                new Environment(_parameters, exps, _env));
        }

        public override string ToString()
        {
            return $"procedure with {_parameters.Count} parameters";
        }
    }
}