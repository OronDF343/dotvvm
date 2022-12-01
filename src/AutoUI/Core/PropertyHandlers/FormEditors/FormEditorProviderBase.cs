﻿using DotVVM.AutoUI.Controls;
using DotVVM.AutoUI.Metadata;
using DotVVM.Framework.Controls;

namespace DotVVM.AutoUI.PropertyHandlers.FormEditors
{
    public abstract class FormEditorProviderBase : AutoUIPropertyHandlerBase, IFormEditorProvider
    {
        public string? ControlCssClass { get; set; }

        public abstract DotvvmControl CreateControl(PropertyDisplayMetadata property, AutoEditor.Props props, AutoUIContext context);
    }
}
