using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
// Debug - required for TypeName()
// using Microsoft.VisualBasic;

using SimaticLib;


namespace S7Lib
{
    /// <summary>
    /// Handles the release of unmanaged COM objects from SimaticLib
    /// </summary>
    /// <remarks>
    /// As seen in https://stackoverflow.com/a/2191570.
    /// Convenient wrapper to register accesses to objects in SimaticLib that
    /// allows them to be released with `Marshal.ReleaseComObject()` as soon as
    /// the wrapper is disposed of.
    /// Skips releasing the top-level Simatic object, mostly so we can avoid
    /// releasing it more than once, while keeping a convenient syntax.
    /// </remarks>
    /// <example>
    /// using (var wrapper = new ReleaseWrapper())
    /// {
    ///     var projects = wrapper.Add(() => Simatic.Projects)
    /// }
    /// </example>
    class ReleaseWrapper : IDisposable
    {
        readonly List<object> objects = new List<object>();

        public T Add<T>(Expression<Func<T>> func)
        {
            return (T)Walk(func.Body);
        }

        object Walk(Expression expr)
        {
            object obj = WalkImpl(expr);
            if (obj != null && Marshal.IsComObject(obj) && !objects.Contains(obj))
            {
                objects.Add(obj);
            }
            return obj;
        }

        object[] Walk(IEnumerable<Expression> args)
        {
            if (args == null) return null;
            return args.Select(arg => Walk(arg)).ToArray();
        }

        object WalkImpl(Expression expr)
        {
            if (expr == null)
                return null;

            switch (expr.NodeType)
            {
                case ExpressionType.Constant:
                    return ((ConstantExpression)expr).Value;
                case ExpressionType.New:
                    NewExpression ne = (NewExpression)expr;
                    return ne.Constructor.Invoke(Walk(ne.Arguments));
                case ExpressionType.MemberAccess:
                    MemberExpression me = (MemberExpression)expr;
                    object target = Walk(me.Expression);
                    switch (me.Member.MemberType)
                    {
                        case MemberTypes.Field:
                            return ((FieldInfo)me.Member).GetValue(target);
                        case MemberTypes.Property:
                            return ((PropertyInfo)me.Member).GetValue(target, null);
                        default:
                            throw new NotSupportedException();

                    }
                case ExpressionType.Call:
                    MethodCallExpression mce = (MethodCallExpression)expr;
                    try
                    {
                        return mce.Method.Invoke(Walk(mce.Object), Walk(mce.Arguments));
                    }
                    catch (TargetInvocationException exc)
                    {
                        // Exceptions are wrapped in a TargetInvocationException
                        var capturedException = ExceptionDispatchInfo.Capture(exc);
                        throw capturedException.SourceException.InnerException;
                    }
                default:
                    throw new NotSupportedException();
            }
        }

        public void Dispose()
        {
            foreach (object obj in objects)
            {
                // Skip top-level Simatic object since it's released explictly in S7Handle.Dispose()
                if (obj is Simatic)
                    continue;

                //Console.WriteLine($"Releasing {Information.TypeName(obj)}");
                Marshal.ReleaseComObject(obj);
            }
            objects.Clear();
        }
    }
}
