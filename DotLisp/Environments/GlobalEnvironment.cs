﻿using System;
using System.Collections.Generic;
using System.Linq;
using DotLisp.Exceptions;
using DotLisp.Types;

namespace DotLisp.Environments
{
    public class GlobalEnvironment : Environment
    {
        public GlobalEnvironment() :
            base(_initData.Keys, _initData.Values, null)
        {
        }

        private static readonly Dictionary<string, Expression> _initData = new Dictionary<string, Expression>()
        {
            ["begin"] = new Func()
            {
                Action = (expr) =>
                {
                    if (expr is List l)
                    {
                        return new List()
                        {
                            Expressions = l.Expressions.Skip(1).ToList()
                        };
                    }

                    throw new EvaluatorException("'begin' needs at least one parameter");
                }
            },

            ["+"] = Apply((acc, x) => acc + x),
            ["-"] = Apply((acc, x) => acc - x),
            ["*"] = Apply((acc, x) => acc * x),
            ["/"] = Apply((acc, x) => acc / x),

            [">"] = BoolApply((a, b) => a > b),
            [">="] = BoolApply((a, b) => a >= b),
            ["<"] = BoolApply((a, b) => a < b),
            ["<="] = BoolApply((a, b) => a <= b),
            ["=="] = Equals(),

            ["PI"] = new Number() {Float = (float) Math.PI},
            ["E"] = new Number() {Float = (float) Math.E}
        };

        public static Expression Apply(Func<float, float, float> reducer)
        {
            return new Func()
            {
                Action = args =>
                {
                    switch (args)
                    {
                        case Number n:
                            return n;
                        case List l:
                        {
                            var sum = l.Expressions.Cast<Number>()
                                .Select(s => s.GetValue())
                                .Aggregate(reducer);

                            return new Number()
                            {
                                Float = sum
                            };
                        }
                        default:
                            throw new EvaluatorException("Illegal arguments for +");
                    }
                }
            };
        }

        public static Expression BoolApply(Func<float, float, bool> predicate)
        {
            return new Func()
            {
                Action = args =>
                {
                    switch (args)
                    {
                        case Bool b:
                            return b;
                        case List l:
                        {
                            var numbers = l.Expressions.Cast<Number>()
                                .Select(s => s.GetValue()).ToList();

                            var (_, result) = numbers.Skip(1).Aggregate(
                                (numbers[0], true), (tuple, b) =>
                                {
                                    var (a, resultSoFar) = tuple;

                                    if (!resultSoFar)
                                    {
                                        return (b, false);
                                    }

                                    var predicateStillTrue = predicate(a, b);

                                    return (b, predicateStillTrue);
                                });


                            return new Bool(result);
                        }
                        default:
                            throw new Exception("Illegal arguments for BoolApply.");
                    }
                }
            };
        }

        public static Func Equals()
        {
            return new Func()
            {
                Action = args =>
                {
                    switch (args)
                    {
                        case Symbol _:
                            return Bool.True();
                        case List l:
                        {
                            // TODO: extend equality to objects?

                            var numbers = l.Expressions.Cast<Number>().ToList();

                            var first = numbers[0];

                            var (_, result) = numbers.Skip(1).Aggregate(
                                seed: (first, true), func: (tuple, b) =>
                                {
                                    var (a, resultSoFar) = tuple;

                                    if (!resultSoFar)
                                    {
                                        return (b, false);
                                    }

                                    var predicateStillTrue = a.GetValue() == b.GetValue();

                                    return (b, predicateStillTrue);
                                });
                            return new Bool(result);
                        }
                    }

                    return Bool.False();
                }
            };
        }
    }
}