﻿using System;
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
    /// Enables a simpler logic structure while still clearing resources.
    /// Otherwise one would have to write several nested try/catch/finally blocks.
    /// Skips releasing instances of the Simatic class, mostly so we can avoid
    /// releasing it more than once, while keeping a convenient syntax.
    ///
    /// ```
    /// using (var wrapper = new ReleaseWrapper())
    /// {
    ///     simatic = new Simatic();
    ///
    ///     // Releases the temporary variable simatic.Projects
    ///     // Does not attempt to release simatic, since it's explicitly ignored for convenience
    ///     var projects = wrapper.Add(() => simatic.Projects);
    ///
    ///     // Releases project variable, but not simatic.Projects (or simatic)
    ///     // This is useful when one wants to only release a resource at a higher abstraction level, e.g.
    ///     // pass low-level COM object as an argument to function, but only release it outside the function.
    ///     // Otherwise it could result in a double release.
    ///     var project = simatic.Projects[0];
    ///     wrapper.Add(() => project);
    ///
    ///     // Releases Projects, Projects[0], Programs and program (= Programs[0])
    ///     var program = wrapper.Add(() => simatic.Projects[0].Programs[0]);
    /// }
    /// ```
    /// Finally, implementing a run-time list of ignored types is non-trivial, since
    /// the `is` operator can only be used for constant (build-time) types.
    /// </remarks>
    class ReleaseWrapper : IDisposable
    {
        readonly List<object> objects = new List<object>();

        /// <summary>
        /// Registers COM objects in expression so they can be released
        /// when wrapper is disposed of.
        /// </summary>
        /// <remarks>
        /// The expression is walked recursively, and all of the COM objects
        /// encountered along the way will be registered for release
        /// (unless explicitly ignored e.g. instances of PServerPro).
        /// The following expression types are supported:
        /// ```
        /// // Local variables
        /// wrapper.Add(() => local);
        /// // Object constructors
        /// wrapper.Add(() => new Class());
        /// // Member access
        /// wrapper.Add(() => map[key]);
        /// // Function calls that return a COM object
        /// wrapper.Add(() => Function(arg));
        /// ```
        /// It's also possible to nest expressions:
        /// ```
        /// wrapper.Add(() => Function(map[key].ComputeValue());
        /// ```
        /// </remarks>
        /// <typeparam name="T">Return type of expression</typeparam>
        /// <param name="func">Target expression</param>
        /// <returns>Object returned by expression</returns>
        public T Add<T>(Expression<Func<T>> func)
        {
            return (T)Walk(func.Body);
        }

        object Walk(Expression expr)
        {
            object obj = WalkImpl(expr);
            if (obj != null && Marshal.IsComObject(obj) && !objects.Contains(obj) && !(obj is Simatic))
            {
                //Console.WriteLine($"Registering {Information.TypeName(obj)}");
                objects.Add(obj);
            }
            return obj;
        }

        object[] Walk(IEnumerable<Expression> args)
        {
            if (args == null)
                return null;
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
            // Release in reverse order
            for (int i = objects.Count - 1; i >= 0; i--)
            {
                //Console.WriteLine($"Releasing {Information.TypeName(obj)}");
                Marshal.ReleaseComObject(objects[i]);
                objects[i] = null;
            }
            objects.Clear();
        }
    }
}
