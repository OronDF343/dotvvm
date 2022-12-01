using System;
using DotVVM.AutoUI.Metadata;
using DotVVM.Framework.Binding;
using DotVVM.Framework.Controls;

namespace DotVVM.AutoUI.Controls
{
    [ControlMarkupOptions(PrimaryName = "Form", Precompile = ControlPrecompilationMode.InServerSideStyles)]
    public class AutoForm : AutoFormBase
    {
        public AutoForm(IServiceProvider services) : base(services)
        {
        }

        public string? LabelCellCssClass
        {
            get { return (string?)GetValue(LabelCellCssClassProperty); }
            set { SetValue(LabelCellCssClassProperty, value); }
        }
        public static readonly DotvvmProperty LabelCellCssClassProperty =
            DotvvmProperty.Register<string, AutoForm>(nameof(LabelCellCssClass));

        public string? EditorCellCssClass
        {
            get { return (string?)GetValue(EditorCellCssClassProperty); }
            set { SetValue(EditorCellCssClassProperty, value); }
        }
        public static readonly DotvvmProperty EditorCellCssClassProperty =
            DotvvmProperty.Register<string, AutoForm>(nameof(EditorCellCssClass));

        public DotvvmControl GetContents(FieldProps props)
        {
            var context = CreateAutoUiContext();

            // create the table
            var table = InitializeTable(context);

            // create the rows
            foreach (var property in GetPropertiesToDisplay(context, props.FieldSelector))
            {
                if (TryGetFieldTemplate(property, props) is { } field)
                {
                    table.AppendChildren(field);
                    continue;
                }
                // create the row
                var row = InitializeTableRow(property, context, out var labelCell, out var editorCell);

                // create the label
                labelCell.AppendChildren(InitializeControlLabel(property, context, props));

                // create the editorProvider
                editorCell.AppendChildren(CreateEditor(property, context, props));

                // create the validator
                InitializeValidation(row, labelCell, property, context);

                SetFieldVisibility(row, property, props, context);
                table.Children.Add(row);
            }
            return table;
        }

        /// <summary>
        /// Creates the table element for the form.
        /// </summary>
        protected virtual HtmlGenericControl InitializeTable(AutoUIContext autoUiContext) =>
            new HtmlGenericControl("table")
                .AddCssClass("autoui-form-table");


        /// <summary>
        /// Creates the table row for the specified property.
        /// </summary>
        protected virtual HtmlGenericControl InitializeTableRow(PropertyDisplayMetadata property, AutoUIContext autoUiContext, out HtmlGenericControl labelCell, out HtmlGenericControl editorCell)
        {
            labelCell = new HtmlGenericControl("td")
                .AddCssClasses("autoui-label", LabelCellCssClass);

            editorCell = new HtmlGenericControl("td")
                .AddCssClasses("autoui-editor", EditorCellCssClass, property.Styles?.FormControlContainerCssClass);

            return new HtmlGenericControl("tr")
                .AddCssClass(property.Styles?.FormRowCssClass)
                .AppendChildren(labelCell, editorCell);
        }
    }
}
