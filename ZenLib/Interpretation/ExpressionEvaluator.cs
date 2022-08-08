﻿// <copyright file="ExpressionEvaluator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace ZenLib.Interpretation
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Numerics;
    using System.Reflection;
    using ZenLib.SymbolicExecution;
    using static ZenLib.Zen;

    /// <summary>
    /// Interpret a Zen expression.
    /// </summary>
    internal sealed class ExpressionEvaluator : IZenExprVisitor<ExpressionEvaluatorEnvironment, object>
    {
        /// <summary>
        /// Evaluate method reference.
        /// </summary>
        private static MethodInfo evaluateMethod = typeof(ExpressionEvaluator).GetMethod("Evaluate");

        /// <summary>
        /// Whether to track covered branches.
        /// </summary>
        private bool trackBranches;

        /// <summary>
        /// Path constraint for the execution.
        /// </summary>
        public PathConstraint PathConstraint { get; set; }

        /// <summary>
        /// Track the symbolic assignment to arguments when collecting path constraints.
        /// </summary>
        public Dictionary<long, object> PathConstraintSymbolicEnvironment { get; set; }

        /// <summary>
        /// Cache of inputs and results.
        /// </summary>
        private Dictionary<object, object> cache = new Dictionary<object, object>();

        /// <summary>
        /// Create a new instance of the <see cref="ExpressionEvaluator"/> class.
        /// </summary>
        /// <param name="trackBranches">Whether to track branches during execution.</param>
        public ExpressionEvaluator(bool trackBranches)
        {
            this.trackBranches = trackBranches;

            if (this.trackBranches)
            {
                this.PathConstraint = new PathConstraint();
                this.PathConstraintSymbolicEnvironment = new Dictionary<long, object>();
            }
        }

        /// <summary>
        /// Evaluate an expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The evaluation environment.</param>
        /// <returns>The resulting C# object.</returns>
        public object Evaluate<T>(Zen<T> expression, ExpressionEvaluatorEnvironment parameter)
        {
            if (this.cache.TryGetValue(expression, out var value))
            {
                return value;
            }

            var result = expression.Accept(this, parameter);
            this.cache[expression] = result;
            return result;
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit<T>(ZenArbitraryExpr<T> expression, ExpressionEvaluatorEnvironment parameter)
        {
            if (parameter.ArbitraryAssignment == null)
                return ReflectionUtilities.GetDefaultValue<T>();
            if (!parameter.ArbitraryAssignment.TryGetValue(expression, out var value))
                return ReflectionUtilities.GetDefaultValue<T>();
            return value;
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit(ZenLogicalBinopExpr expression, ExpressionEvaluatorEnvironment parameter)
        {
            var e1 = (bool)Evaluate(expression.Expr1, parameter);
            var e2 = (bool)Evaluate(expression.Expr2, parameter);

            switch (expression.Operation)
            {
                case ZenLogicalBinopExpr.LogicalOp.And:
                    return e1 && e2;
                default:
                    Contract.Assert(expression.Operation == ZenLogicalBinopExpr.LogicalOp.Or);
                    return e1 || e2;
            }
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit<T>(ZenArgumentExpr<T> expression, ExpressionEvaluatorEnvironment parameter)
        {
            return parameter.ArgumentAssignment[expression.ArgumentId];
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit<T>(ZenArithBinopExpr<T> expression, ExpressionEvaluatorEnvironment parameter)
        {
            var e1 = Evaluate(expression.Expr1, parameter);
            var e2 = Evaluate(expression.Expr2, parameter);
            var type = typeof(T);

            switch (expression.Operation)
            {
                case ArithmeticOp.Addition:
                    if (type == ReflectionUtilities.ByteType)
                        return (byte)((byte)e1 + (byte)e2);
                    else if (type == ReflectionUtilities.ShortType)
                        return (short)((short)e1 + (short)e2);
                    else if (type == ReflectionUtilities.UshortType)
                        return (ushort)((ushort)e1 + (ushort)e2);
                    else if (type == ReflectionUtilities.IntType)
                        return (int)e1 + (int)e2;
                    else if (type == ReflectionUtilities.UintType)
                        return (uint)e1 + (uint)e2;
                    else if (type == ReflectionUtilities.LongType)
                        return (long)e1 + (long)e2;
                    else if (type == ReflectionUtilities.UlongType)
                        return (ulong)e1 + (ulong)e2;
                    else if (type == ReflectionUtilities.BigIntType)
                        return (BigInteger)e1 + (BigInteger)e2;
                    else if (type == ReflectionUtilities.RealType)
                        return (Real)e1 + (Real)e2;
                    else
                    {
                        Contract.Assert(ReflectionUtilities.IsFixedIntegerType(type));
                        return ((dynamic)e1).Add((dynamic)e2);
                    }

                case ArithmeticOp.Subtraction:
                    if (type == ReflectionUtilities.ByteType)
                        return (byte)((byte)e1 - (byte)e2);
                    else if (type == ReflectionUtilities.ShortType)
                        return (short)((short)e1 - (short)e2);
                    else if (type == ReflectionUtilities.UshortType)
                        return (ushort)((ushort)e1 - (ushort)e2);
                    else if (type == ReflectionUtilities.IntType)
                        return (int)e1 - (int)e2;
                    else if (type == ReflectionUtilities.UintType)
                        return (uint)e1 - (uint)e2;
                    else if (type == ReflectionUtilities.LongType)
                        return (long)e1 - (long)e2;
                    else if (type == ReflectionUtilities.UlongType)
                        return (ulong)e1 - (ulong)e2;
                    else if (type == ReflectionUtilities.BigIntType)
                        return (BigInteger)e1 - (BigInteger)e2;
                    else if (type == ReflectionUtilities.RealType)
                        return (Real)e1 - (Real)e2;
                    else
                    {
                        Contract.Assert(ReflectionUtilities.IsFixedIntegerType(type));
                        return ((dynamic)e1).Subtract((dynamic)e2);
                    }

                default:
                    Contract.Assert(expression.Operation == ArithmeticOp.Multiplication);
                    if (type == ReflectionUtilities.ByteType)
                        return (byte)((byte)e1 * (byte)e2);
                    else if (type == ReflectionUtilities.ShortType)
                        return (short)((short)e1 * (short)e2);
                    else if (type == ReflectionUtilities.UshortType)
                        return (ushort)((ushort)e1 * (ushort)e2);
                    else if (type == ReflectionUtilities.IntType)
                        return (int)e1 * (int)e2;
                    else if (type == ReflectionUtilities.UintType)
                        return (uint)e1 * (uint)e2;
                    else if (type == ReflectionUtilities.LongType)
                        return (long)e1 * (long)e2;
                    else if (type == ReflectionUtilities.UlongType)
                        return (ulong)e1 * (ulong)e2;
                    else if (type == ReflectionUtilities.RealType)
                        return (Real)e1 * (Real)e2;
                    else
                    {
                        Contract.Assert(ReflectionUtilities.IsBigIntegerType(type));
                        return (BigInteger)e1 * (BigInteger)e2;
                    }
            }
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit<T>(ZenBitwiseBinopExpr<T> expression, ExpressionEvaluatorEnvironment parameter)
        {
            var e1 = Evaluate(expression.Expr1, parameter);
            var e2 = Evaluate(expression.Expr2, parameter);
            var type = typeof(T);

            switch (expression.Operation)
            {
                case BitwiseOp.BitwiseAnd:
                    if (ReflectionUtilities.IsFixedIntegerType(type))
                        return ((dynamic)e1).BitwiseAnd((dynamic)e2);
                    else
                        return ReflectionUtilities.FromLong<T>(ReflectionUtilities.ToLong(e1) & ReflectionUtilities.ToLong(e2));

                case BitwiseOp.BitwiseOr:
                    if (ReflectionUtilities.IsFixedIntegerType(type))
                        return ((dynamic)e1).BitwiseOr((dynamic)e2);
                    else
                        return ReflectionUtilities.FromLong<T>(ReflectionUtilities.ToLong(e1) | ReflectionUtilities.ToLong(e2));

                default:
                    Contract.Assert(expression.Operation == BitwiseOp.BitwiseXor);
                    if (ReflectionUtilities.IsFixedIntegerType(type))
                        return ((dynamic)e1).BitwiseXor((dynamic)e2);
                    else
                        return ReflectionUtilities.FromLong<T>(ReflectionUtilities.ToLong(e1) ^ ReflectionUtilities.ToLong(e2));
            }
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit<T>(ZenBitwiseNotExpr<T> expression, ExpressionEvaluatorEnvironment parameter)
        {
            var x = ReflectionUtilities.ToLong(Evaluate(expression.Expr, parameter));
            return ReflectionUtilities.FromLong<T>(~x);
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit<T>(ZenConstantExpr<T> expression, ExpressionEvaluatorEnvironment parameter)
        {
            return expression.Value;
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit<TObject>(ZenCreateObjectExpr<TObject> expression, ExpressionEvaluatorEnvironment parameter)
        {
            var fieldNames = new List<string>();
            var parameters = new List<object>();
            foreach (var fieldValuePair in expression.Fields)
            {
                var type = fieldValuePair.Value.GetType();
                var innerType = type.BaseType.GetGenericArgumentsCached()[0];
                var field = fieldValuePair.Key;
                var method = evaluateMethod.MakeGenericMethod(innerType);
                var valueResult = method.Invoke(this, new object[] { fieldValuePair.Value, parameter });
                fieldNames.Add(field);
                parameters.Add(valueResult);
            }

            return ReflectionUtilities.CreateInstance<TObject>(fieldNames.ToArray(), parameters.ToArray());
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit<T1, T2>(ZenGetFieldExpr<T1, T2> expression, ExpressionEvaluatorEnvironment parameter)
        {
            var e = (T1)Evaluate(expression.Expr, parameter);
            return ReflectionUtilities.GetFieldOrProperty<T1, T2>(e, expression.FieldName);
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit<T>(ZenIfExpr<T> expression, ExpressionEvaluatorEnvironment parameter)
        {
            var e1 = (bool)Evaluate(expression.GuardExpr, parameter);

            if (e1)
            {
                if (this.trackBranches)
                {
                    this.PathConstraint = this.PathConstraint.Add(expression.GuardExpr);
                }

                return (T)Evaluate(expression.TrueExpr, parameter);
            }
            else
            {
                if (this.trackBranches)
                {
                    this.PathConstraint = this.PathConstraint.Add(ZenNotExpr.Create(expression.GuardExpr));
                }

                return (T)Evaluate(expression.FalseExpr, parameter);
            }
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit<T>(ZenEqualityExpr<T> expression, ExpressionEvaluatorEnvironment parameter)
        {
            var e1 = Evaluate(expression.Expr1, parameter);
            var e2 = Evaluate(expression.Expr2, parameter);
            return ((T)e1).Equals((T)e2);
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit<T>(ZenArithComparisonExpr<T> expression, ExpressionEvaluatorEnvironment parameter)
        {
            var e1 = Evaluate(expression.Expr1, parameter);
            var e2 = Evaluate(expression.Expr2, parameter);
            var type = typeof(T);

            switch (expression.ComparisonType)
            {
                case ComparisonType.Geq:
                    if (type == ReflectionUtilities.ByteType)
                        return (byte)e1 >= (byte)e2;
                    else if (type == ReflectionUtilities.ShortType)
                        return (short)e1 >= (short)e2;
                    else if (type == ReflectionUtilities.UshortType)
                        return (ushort)e1 >= (ushort)e2;
                    else if (type == ReflectionUtilities.IntType)
                        return (int)e1 >= (int)e2;
                    else if (type == ReflectionUtilities.UintType)
                        return (uint)e1 >= (uint)e2;
                    else if (type == ReflectionUtilities.LongType)
                        return (long)e1 >= (long)e2;
                    else if (type == ReflectionUtilities.UlongType)
                        return (ulong)e1 >= (ulong)e2;
                    else if (type == ReflectionUtilities.BigIntType)
                        return (BigInteger)e1 >= (BigInteger)e2;
                    else if (type == ReflectionUtilities.RealType)
                        return (Real)e1 >= (Real)e2;
                    else
                    {
                        Contract.Assert(ReflectionUtilities.IsFixedIntegerType(type));
                        return ((dynamic)e1) >= ((dynamic)e2);
                    }

                case ComparisonType.Gt:
                    if (type == ReflectionUtilities.ByteType)
                        return (byte)e1 > (byte)e2;
                    else if (type == ReflectionUtilities.ShortType)
                        return (short)e1 > (short)e2;
                    else if (type == ReflectionUtilities.UshortType)
                        return (ushort)e1 > (ushort)e2;
                    else if (type == ReflectionUtilities.IntType)
                        return (int)e1 > (int)e2;
                    else if (type == ReflectionUtilities.UintType)
                        return (uint)e1 > (uint)e2;
                    else if (type == ReflectionUtilities.LongType)
                        return (long)e1 > (long)e2;
                    else if (type == ReflectionUtilities.UlongType)
                        return (ulong)e1 > (ulong)e2;
                    else if (type == ReflectionUtilities.BigIntType)
                        return (BigInteger)e1 > (BigInteger)e2;
                    else if (type == ReflectionUtilities.RealType)
                        return (Real)e1 > (Real)e2;
                    else
                    {
                        Contract.Assert(ReflectionUtilities.IsFixedIntegerType(type));
                        return ((dynamic)e1) > ((dynamic)e2);
                    }

                case ComparisonType.Lt:
                    if (type == ReflectionUtilities.ByteType)
                        return (byte)e1 < (byte)e2;
                    else if (type == ReflectionUtilities.ShortType)
                        return (short)e1 < (short)e2;
                    else if (type == ReflectionUtilities.UshortType)
                        return (ushort)e1 < (ushort)e2;
                    else if (type == ReflectionUtilities.IntType)
                        return (int)e1 < (int)e2;
                    else if (type == ReflectionUtilities.UintType)
                        return (uint)e1 < (uint)e2;
                    else if (type == ReflectionUtilities.LongType)
                        return (long)e1 < (long)e2;
                    else if (type == ReflectionUtilities.UlongType)
                        return (ulong)e1 < (ulong)e2;
                    else if (type == ReflectionUtilities.BigIntType)
                        return (BigInteger)e1 < (BigInteger)e2;
                    else if (type == ReflectionUtilities.RealType)
                        return (Real)e1 < (Real)e2;
                    else
                    {
                        Contract.Assert(ReflectionUtilities.IsFixedIntegerType(type));
                        return ((dynamic)e1) < ((dynamic)e2);
                    }

                default:
                    Contract.Assert(expression.ComparisonType == ComparisonType.Leq);
                    if (type == ReflectionUtilities.ByteType)
                        return (byte)e1 <= (byte)e2;
                    else if (type == ReflectionUtilities.ShortType)
                        return (short)e1 <= (short)e2;
                    else if (type == ReflectionUtilities.UshortType)
                        return (ushort)e1 <= (ushort)e2;
                    else if (type == ReflectionUtilities.IntType)
                        return (int)e1 <= (int)e2;
                    else if (type == ReflectionUtilities.UintType)
                        return (uint)e1 <= (uint)e2;
                    else if (type == ReflectionUtilities.LongType)
                        return (long)e1 <= (long)e2;
                    else if (type == ReflectionUtilities.UlongType)
                        return (ulong)e1 <= (ulong)e2;
                    else if (type == ReflectionUtilities.BigIntType)
                        return (BigInteger)e1 <= (BigInteger)e2;
                    else if (type == ReflectionUtilities.RealType)
                        return (Real)e1 <= (Real)e2;
                    else
                    {
                        Contract.Assert(ReflectionUtilities.IsFixedIntegerType(type));
                        return ((dynamic)e1) <= ((dynamic)e2);
                    }
            }
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit<T>(ZenListEmptyExpr<T> expression, ExpressionEvaluatorEnvironment parameter)
        {
            return new FSeq<T>();
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit<T>(ZenListAddFrontExpr<T> expression, ExpressionEvaluatorEnvironment parameter)
        {
            var e1 = (FSeq<T>)Evaluate(expression.Expr, parameter);
            var e2 = (T)Evaluate(expression.ElementExpr, parameter);
            return e1.AddFront(e2);
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit<T, TResult>(ZenListCaseExpr<T, TResult> expression, ExpressionEvaluatorEnvironment parameter)
        {
            var e = (FSeq<T>)Evaluate(expression.ListExpr, parameter);

            if (e.Count() == 0)
            {
                if (this.trackBranches)
                {
                    this.PathConstraint.Add(expression.ListExpr.IsEmpty());
                }

                return Evaluate(expression.EmptyExpr, parameter);
            }
            else
            {
                var (hd, tl) = CommonUtilities.SplitHead(e);
                var argHd = new ZenArgumentExpr<T>();
                var argTl = new ZenArgumentExpr<FSeq<T>>();
                parameter.ArgumentAssignment[argHd.ArgumentId] = hd;
                parameter.ArgumentAssignment[argTl.ArgumentId] = tl;

                if (this.trackBranches)
                {
                    this.PathConstraint.Add(Zen.Not(expression.ListExpr.IsEmpty()));
                    this.PathConstraintSymbolicEnvironment[argHd.ArgumentId] = expression.ListExpr.Head();
                    this.PathConstraintSymbolicEnvironment[argTl.ArgumentId] = expression.ListExpr.Tail();
                }

                var c = expression.ConsCase.Invoke(argHd, argTl);
                return (TResult)Evaluate(c, parameter);
            }
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit(ZenNotExpr expression, ExpressionEvaluatorEnvironment parameter)
        {
            return !(bool)Evaluate(expression.Expr, parameter);
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit<T1, T2>(ZenWithFieldExpr<T1, T2> expression, ExpressionEvaluatorEnvironment parameter)
        {
            var e1 = (T1)Evaluate(expression.Expr, parameter);
            var e2 = (T2)Evaluate(expression.FieldExpr, parameter);
            return ReflectionUtilities.WithField<T1>(e1, expression.FieldName, e2);
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit<TKey, TValue>(ZenMapEmptyExpr<TKey, TValue> expression, ExpressionEvaluatorEnvironment parameter)
        {
            return new Map<TKey, TValue>();
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit<TKey, TValue>(ZenMapSetExpr<TKey, TValue> expression, ExpressionEvaluatorEnvironment parameter)
        {
            var e1 = (Map<TKey, TValue>)Evaluate(expression.MapExpr, parameter);
            var e2 = (TKey)Evaluate(expression.KeyExpr, parameter);
            var e3 = (TValue)Evaluate(expression.ValueExpr, parameter);
            return e1.Set(e2, e3);
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit<TKey, TValue>(ZenMapDeleteExpr<TKey, TValue> expression, ExpressionEvaluatorEnvironment parameter)
        {
            var e1 = (Map<TKey, TValue>)Evaluate(expression.MapExpr, parameter);
            var e2 = (TKey)Evaluate(expression.KeyExpr, parameter);
            return e1.Delete(e2);
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit<TKey, TValue>(ZenMapGetExpr<TKey, TValue> expression, ExpressionEvaluatorEnvironment parameter)
        {
            var e1 = (Map<TKey, TValue>)Evaluate(expression.MapExpr, parameter);
            var e2 = (TKey)Evaluate(expression.KeyExpr, parameter);
            return e1.Get(e2);
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit<TKey>(ZenMapCombineExpr<TKey> expression, ExpressionEvaluatorEnvironment parameter)
        {
            var e1 = (Map<TKey, SetUnit>)Evaluate(expression.MapExpr1, parameter);
            var e2 = (Map<TKey, SetUnit>)Evaluate(expression.MapExpr2, parameter);

            switch (expression.CombinationType)
            {
                case ZenMapCombineExpr<TKey>.CombineType.Intersect:
                    return CommonUtilities.DictionaryIntersect(e1, e2);
                case ZenMapCombineExpr<TKey>.CombineType.Union:
                    return CommonUtilities.DictionaryUnion(e1, e2);
                default:
                    Contract.Assert(expression.CombinationType == ZenMapCombineExpr<TKey>.CombineType.Difference);
                    return CommonUtilities.DictionaryDifference(e1, e2);
            }
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit<TKey, TValue>(ZenConstMapSetExpr<TKey, TValue> expression, ExpressionEvaluatorEnvironment parameter)
        {
            var e1 = (CMap<TKey, TValue>)Evaluate(expression.MapExpr, parameter);
            var e2 = (TValue)Evaluate(expression.ValueExpr, parameter);
            return e1.Set(expression.Key, e2);
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit<TKey, TValue>(ZenConstMapGetExpr<TKey, TValue> expression, ExpressionEvaluatorEnvironment parameter)
        {
            var e1 = (CMap<TKey, TValue>)Evaluate(expression.MapExpr, parameter);
            return e1.Get(expression.Key);
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit<T>(ZenSeqEmptyExpr<T> expression, ExpressionEvaluatorEnvironment parameter)
        {
            return new Seq<T>();
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit<T>(ZenSeqConcatExpr<T> expression, ExpressionEvaluatorEnvironment parameter)
        {
            var e1 = (Seq<T>)Evaluate(expression.SeqExpr1, parameter);
            var e2 = (Seq<T>)Evaluate(expression.SeqExpr2, parameter);
            return e1.Concat(e2);
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit<T>(ZenSeqUnitExpr<T> expression, ExpressionEvaluatorEnvironment parameter)
        {
            var e1 = (T)Evaluate(expression.ValueExpr, parameter);
            return new Seq<T>(e1);
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit<T>(ZenSeqLengthExpr<T> expression, ExpressionEvaluatorEnvironment parameter)
        {
            var e = (Seq<T>)Evaluate(expression.SeqExpr, parameter);
            return new BigInteger(e.Length());
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit<T>(ZenSeqAtExpr<T> expression, ExpressionEvaluatorEnvironment parameter)
        {
            var e1 = (Seq<T>)Evaluate(expression.SeqExpr, parameter);
            var e2 = (BigInteger)Evaluate(expression.IndexExpr, parameter);
            return e1.AtBigInteger(e2);
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit<T>(ZenSeqContainsExpr<T> expression, ExpressionEvaluatorEnvironment parameter)
        {
            var e1 = (Seq<T>)Evaluate(expression.SeqExpr, parameter);
            var e2 = (Seq<T>)Evaluate(expression.SubseqExpr, parameter);

            switch (expression.ContainmentType)
            {
                case SeqContainmentType.HasPrefix:
                    return e1.HasPrefix(e2);
                case SeqContainmentType.HasSuffix:
                    return e1.HasSuffix(e2);
                default:
                    Contract.Assert(expression.ContainmentType == SeqContainmentType.Contains);
                    return e1.Contains(e2);
            }
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit<T>(ZenSeqIndexOfExpr<T> expression, ExpressionEvaluatorEnvironment parameter)
        {
            var e1 = (Seq<T>)Evaluate(expression.SeqExpr, parameter);
            var e2 = (Seq<T>)Evaluate(expression.SubseqExpr, parameter);
            var e3 = (BigInteger)Evaluate(expression.OffsetExpr, parameter);
            return e1.IndexOfBigInteger(e2, e3);
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit<T>(ZenSeqSliceExpr<T> expression, ExpressionEvaluatorEnvironment parameter)
        {
            var e1 = (Seq<T>)Evaluate(expression.SeqExpr, parameter);
            var e2 = (BigInteger)Evaluate(expression.OffsetExpr, parameter);
            var e3 = (BigInteger)Evaluate(expression.LengthExpr, parameter);
            return e1.SliceBigInteger(e2, e3);
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit<T>(ZenSeqReplaceFirstExpr<T> expression, ExpressionEvaluatorEnvironment parameter)
        {
            var e1 = (Seq<T>)Evaluate(expression.SeqExpr, parameter);
            var e2 = (Seq<T>)Evaluate(expression.SubseqExpr, parameter);
            var e3 = (Seq<T>)Evaluate(expression.ReplaceExpr, parameter);
            return e1.ReplaceFirst(e2, e3);
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit<TKey, TValue>(ZenCastExpr<TKey, TValue> expression, ExpressionEvaluatorEnvironment parameter)
        {
            var e = Evaluate(expression.SourceExpr, parameter);

            if (typeof(TKey) == ReflectionUtilities.StringType)
            {
                return Seq.FromString((string)e);
            }
            else if (typeof(TKey) == ReflectionUtilities.UnicodeSequenceType)
            {
                Contract.Assert(typeof(TKey) == ReflectionUtilities.UnicodeSequenceType);
                return Seq.AsString((Seq<char>)e);
            }
            else
            {
                Contract.Assert(ReflectionUtilities.IsFiniteIntegerType(typeof(TKey)));
                Contract.Assert(ReflectionUtilities.IsFiniteIntegerType(typeof(TValue)));
                return IntN.CastFiniteInteger<TKey, TValue>((TKey)e);
            }
        }

        /// <summary>
        /// Visit a Zen expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="parameter">The environment.</param>
        /// <returns>The C# object.</returns>
        public object Visit<T>(ZenSeqRegexExpr<T> expression, ExpressionEvaluatorEnvironment parameter)
        {
            var e = (Seq<T>)Evaluate(expression.SeqExpr, parameter);
            return e.MatchesRegex(expression.Regex);
        }
    }
}
