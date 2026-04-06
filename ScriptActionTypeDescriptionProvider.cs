using System.ComponentModel;

namespace OlAform
{
    internal sealed class ScriptActionTypeDescriptionProvider : TypeDescriptionProvider
    {
        private static readonly TypeDescriptionProvider DefaultProvider = TypeDescriptor.GetProvider(typeof(object));

        public ScriptActionTypeDescriptionProvider()
            : base(DefaultProvider)
        {
        }

        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object? instance)
        {
            var baseDescriptor = base.GetTypeDescriptor(objectType, instance);
            return new ScriptActionTypeDescriptor(baseDescriptor, instance as ScriptAction);
        }
    }

    internal sealed class ScriptActionTypeDescriptor : CustomTypeDescriptor
    {
        private readonly ScriptAction? _action;

        public ScriptActionTypeDescriptor(ICustomTypeDescriptor parent, ScriptAction? action)
            : base(parent)
        {
            _action = action;
        }

        public override PropertyDescriptorCollection GetProperties()
        {
            return GetProperties(Array.Empty<Attribute>());
        }

        public override PropertyDescriptorCollection GetProperties(Attribute[]? attributes)
        {
            var baseProperties = base.GetProperties(attributes);
            if (_action is null)
            {
                return baseProperties;
            }

            var visibleProperties = ScriptActionPropertyVisibility.GetVisibleProperties(_action);
            var filtered = baseProperties.Cast<PropertyDescriptor>()
                .Where(property => visibleProperties.Contains(property.Name))
                .ToArray();

            return new PropertyDescriptorCollection(filtered, true);
        }
    }
}
