using System.ComponentModel;

namespace OlAform
{
    internal sealed class ScriptActionPropertyGridAdapter : ICustomTypeDescriptor
    {
        private readonly ScriptAction _action;

        public ScriptActionPropertyGridAdapter(ScriptAction action)
        {
            _action = action;
        }

        public ScriptAction Action => _action;

        public AttributeCollection GetAttributes() => TypeDescriptor.GetAttributes(_action, true);

        public string? GetClassName() => TypeDescriptor.GetClassName(_action, true);

        public string? GetComponentName() => TypeDescriptor.GetComponentName(_action, true);

        public TypeConverter GetConverter() => TypeDescriptor.GetConverter(_action, true);

        public EventDescriptor? GetDefaultEvent() => TypeDescriptor.GetDefaultEvent(_action, true);

        public PropertyDescriptor? GetDefaultProperty() => null;

        public object? GetEditor(Type editorBaseType) => TypeDescriptor.GetEditor(_action, editorBaseType, true);

        public EventDescriptorCollection GetEvents(Attribute[]? attributes) => TypeDescriptor.GetEvents(_action, attributes, true);

        public EventDescriptorCollection GetEvents() => TypeDescriptor.GetEvents(_action, true);

        public PropertyDescriptorCollection GetProperties(Attribute[]? attributes)
        {
            var visibleProperties = ScriptActionPropertyVisibility.GetVisibleProperties(_action);
            var sourceProperties = TypeDescriptor.GetProperties(typeof(ScriptAction), attributes ?? Array.Empty<Attribute>());
            var filtered = sourceProperties.Cast<PropertyDescriptor>()
                .Where(property => visibleProperties.Contains(property.Name))
                .Select(property => new ScriptActionPropertyDescriptor(property, _action))
                .ToArray();

            return new PropertyDescriptorCollection(filtered, true);
        }

        public PropertyDescriptorCollection GetProperties()
        {
            return GetProperties(Array.Empty<Attribute>());
        }

        public object? GetPropertyOwner(PropertyDescriptor? pd) => _action;
    }

    internal sealed class ScriptActionPropertyDescriptor : PropertyDescriptor
    {
        private readonly PropertyDescriptor _inner;
        private readonly ScriptAction _action;

        public ScriptActionPropertyDescriptor(PropertyDescriptor inner, ScriptAction action)
            : base(inner)
        {
            _inner = inner;
            _action = action;
        }

        public override bool CanResetValue(object component) => _inner.CanResetValue(_action);

        public override Type ComponentType => _inner.ComponentType;

        public override object? GetValue(object? component) => _inner.GetValue(_action);

        public override bool IsReadOnly => _inner.IsReadOnly;

        public override Type PropertyType => _inner.PropertyType;

        public override void ResetValue(object component) => _inner.ResetValue(_action);

        public override void SetValue(object? component, object? value) => _inner.SetValue(_action, value);

        public override bool ShouldSerializeValue(object component) => _inner.ShouldSerializeValue(_action);

        public override string DisplayName => _inner.DisplayName;

        public override string Description => _inner.Description;

        public override string Category => _inner.Category;
    }
}
