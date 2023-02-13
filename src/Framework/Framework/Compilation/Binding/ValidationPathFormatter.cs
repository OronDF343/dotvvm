using System;
using System.Linq;
using System.Linq.Expressions;
using DotVVM.Framework.Binding;
using DotVVM.Framework.Compilation.ControlTree;
using DotVVM.Framework.Compilation.Javascript;
using DotVVM.Framework.Compilation.Javascript.Ast;
using DotVVM.Framework.Utils;
using DotVVM.Framework.ViewModel.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace DotVVM.Framework.Compilation.Binding
{
    public class ValidationPathFormatter
    {
        readonly IViewModelSerializationMapper mapper;
        readonly JavascriptTranslator javascriptTranslator;

        public ValidationPathFormatter(
            IViewModelSerializationMapper mapper,
            JavascriptTranslator javascriptTranslator
        )
        {
            this.mapper = mapper;
            this.javascriptTranslator = javascriptTranslator;
        }

        private bool isNull([NotNullWhen(false)] JsNode? expr) =>
            expr is null or JsLiteral { Value: null };

        public JsExpression GetValidationPath(
            Expression expr,
            DataContextStack dataContext,
            Func<Expression, JsExpression?>? baseFormatter = null)
        {
            // TODO: handle lambda arguments
            // TODO: propagate errors into block variables
            expr = ExpressionHelper.UnwrapPassthroughOperations(expr);

            var baseFmt = baseFormatter?.Invoke(expr);
            if (baseFmt is {})
                return baseFmt;

            if (expr.GetParameterAnnotation() is {} annotation)
            {
                if (annotation.ExtensionParameter is not null)
                    return null;

                var parentIndex = dataContext.EnumerableItems().ToList().IndexOf(annotation.DataContext.NotNull());

                if (parentIndex < 0)
                    throw new InvalidOperationException($"DataContext parameter is invalid. Current data context is {dataContext}, the parameter is not one of the ancestors: {annotation.DataContext}");

                if (parentIndex == 0)
                    return new JsLiteral(".");
                else
                    return new JsLiteral(string.Join("/", Enumerable.Repeat("..", parentIndex)));
            }

            switch (expr)
            {
                case MemberExpression m when m.Expression is {}: {
                    var targetPath = GetValidationPath(m.Expression, dataContext, baseFormatter);
                    if (isNull(targetPath))
                        return targetPath;

                    var typeMap = mapper.GetMap(m.Member.DeclaringType!);
                    var property = typeMap.Properties.FirstOrDefault(p => p.PropertyInfo == m.Member);

                    if (property is null)
                        return JsLiteral.Null.CommentBefore($"{m.Member.Name} is not mapped");

                    return appendPaths(targetPath, property.Name);
                }
                case ConditionalExpression conditional: {
                    var truePath = GetValidationPath(conditional.IfTrue, dataContext, baseFormatter);
                    var falsePath = GetValidationPath(conditional.IfFalse, dataContext, baseFormatter);

                    if (isNull(truePath) || isNull(falsePath))
                        return truePath ?? falsePath;

                    return new JsConditionalExpression(
                        this.javascriptTranslator.CompileToJavascript(conditional.Test, dataContext),
                        truePath,
                        falsePath
                    );
                }
                case IndexExpression index: {
                    var targetPath = GetValidationPath(index.Object, dataContext, baseFormatter);
                    if (isNull(targetPath))
                        return targetPath;
                    if (index.Arguments.Count != 1 || !index.Arguments.Single().Type.IsNumericType())
                        return JsLiteral.Null.CommentBefore("Unsupported Index");

                    var indexPath = this.javascriptTranslator.CompileToJavascript(index.Arguments.Single(), dataContext);
                    if (indexPath is JsLiteral { Value: not null } indexLiteral)
                        return appendPaths(targetPath, indexLiteral.Value!.ToString());
                    else if (targetPath is JsLiteral { Value: "." })
                        return indexPath;
                    else
                        return new JsBinaryExpression(stringAppend(targetPath, "/"), BinaryOperatorType.Plus, indexPath);
                }
                default:
                    return JsLiteral.Null.CommentBefore($"{expr} isn't supported");
            }
        }

        static JsExpression appendPaths(JsExpression left, string right)
        {
            if (left is JsLiteral l && ".".Equals(l.Value))
                return new JsLiteral(right);
            return stringAppend(left, "/" + right);
        }

        static JsExpression stringAppend(JsExpression left, string right)
        {
            if (left is JsLiteral l && l.Value is string s)
                return new JsLiteral(s + right);
            else if (left is JsBinaryExpression { OperatorString: "+", Right: JsLiteral { Value: string str2 } } binary)
                return new JsBinaryExpression(left, BinaryOperatorType.Plus, new JsLiteral(str2 + right));
            else
                return new JsBinaryExpression(left, BinaryOperatorType.Plus, new JsLiteral(right));
        }
    }
}
