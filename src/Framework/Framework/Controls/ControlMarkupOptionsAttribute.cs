using System;
using System.Collections.Generic;
using System.Linq;

namespace DotVVM.Framework.Controls
{
    /// <summary> Controls various aspects of how this control behaves in dothtml markup files. </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ControlMarkupOptionsAttribute : Attribute
    {
        /// <summary> When false, adding children to this control in a markup file will be an error. </summary>
        public bool AllowContent { get; set; } = true;

        /// <summary> Name of the DotvvmProperty where all child nodes will be placed. When null <see cref="DotvvmControl.Children" /> collection is used. If the property is not a collection type, only one child control will be allowed. </summary>
        public string? DefaultContentProperty { get; set; }

        /// <summary> When set, the control will be evaluated only once during view compilation, instead of execting it for every request. It only work for <see cref="CompositeControl" />s. </summary>
        public ControlPrecompilationMode Precompile { get; set; } = ControlPrecompilationMode.Never;

        /// <summary>
        /// If set, the control will be referenced by this name in the markup and the primary name will appear in the Visual Studio IntelliSense.
        /// If not set, the control class name will be used as a primary name.
        /// </summary>
        public string? PrimaryName { get; set; } = null;

        /// <summary>
        /// Represents a set of alternative names that are possible to use in the markup.
        /// </summary>
        public string[]? AlternativeNames { get; set; } = null;
    }
}
