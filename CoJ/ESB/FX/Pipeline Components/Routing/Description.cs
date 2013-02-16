namespace CoJ.ESB.FX.PIP.ASM.Routing
{
    #region Using Directives
    using System;
    using System.ComponentModel;
    using System.Collections;
    using System.Resources;
    using System.Reflection;
    #endregion

    /// <summary>
    /// Implements an atribute for the custom property name
    /// </summary>
    [AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = false)]
    public sealed class ItineraryPropertyNameAttribute : Attribute
    {
        private string propertyName;
        public ItineraryPropertyNameAttribute(string propertyName)
        {
            this.propertyName = propertyName;
        }
        public string PropertyName
        {
            get
            {
                return propertyName;
            }
        }
    }

    /// <summary>
    /// Implements an attribute for the custom property description
    /// </summary>
    [AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = false)]
    public sealed class ItineraryDescriptionAttribute : Attribute
    {
        private string description;
        public ItineraryDescriptionAttribute(string description)
        {
            this.description = description;
        }
        public string Description
        {
            get
            {
                return description;
            }
        }
    }

    #region DiscardPropertyDescriptor
    /// <summary>
    /// Cutom property descriptor
    /// </summary>
    public class ItineraryPropertyDescriptor : PropertyDescriptor
    {
        private PropertyDescriptor descriptor;
        private ResourceManager resManager;

        public ResourceManager ResourceManager
        {
            get
            {
                return resManager;
            }
        }

        public ItineraryPropertyDescriptor(PropertyDescriptor descriptor, ResourceManager resourceManager)
            : base(descriptor)
        {
            this.descriptor = descriptor;
            this.resManager = resourceManager;
        }

        public override AttributeCollection Attributes
        {
            get
            {
                return descriptor.Attributes;
            }
        }
        public override object GetEditor(Type editorBaseType)
        {
            return descriptor.GetEditor(editorBaseType);
        }

        public override string Category
        {
            get
            {
                return descriptor.Category;
            }
        }
        public override Type ComponentType
        {
            get
            {
                return descriptor.ComponentType;
            }
        }
        public override TypeConverter Converter
        {
            get
            {
                return descriptor.Converter;
            }
        }
        public override string Description
        {
            get
            {
                AttributeCollection attributes = descriptor.Attributes;

                ItineraryDescriptionAttribute descriptionAttribute =
                    (ItineraryDescriptionAttribute)attributes[typeof(ItineraryDescriptionAttribute)];

                if (descriptionAttribute == null)
                    return descriptor.Description;

                string strId = descriptionAttribute.Description;
                if (resManager == null)
                    return strId;

                return resManager.GetString(strId);
            }
        }
        public override bool DesignTimeOnly
        {
            get
            {
                return descriptor.DesignTimeOnly;
            }
        }
        public override bool IsBrowsable
        {
            get
            {
                return descriptor.IsBrowsable;
            }
        }
        public override bool IsLocalizable
        {
            get
            {
                return descriptor.IsLocalizable;
            }
        }
        public override bool IsReadOnly
        {
            get
            {
                return descriptor.IsReadOnly;
            }
        }
        public override Type PropertyType
        {
            get
            {
                return descriptor.PropertyType;
            }
        }
        public override bool ShouldSerializeValue(object o)
        {
            return descriptor.ShouldSerializeValue(o);
        }

        public override void AddValueChanged(object o, EventHandler handler)
        {
            descriptor.AddValueChanged(o, handler);
        }

        public override string DisplayName
        {
            get
            {
                AttributeCollection attributes = descriptor.Attributes;

                ItineraryPropertyNameAttribute nameAttribute =
                    (ItineraryPropertyNameAttribute)attributes[typeof(ItineraryPropertyNameAttribute)];

                if (nameAttribute == null)
                    return descriptor.DisplayName;

                string strId = nameAttribute.PropertyName;
                if (resManager == null)
                    return strId;

                return resManager.GetString(strId);
            }
        }
        public override string Name
        {
            get
            {
                return descriptor.Name;
            }
        }
        public override Object GetValue(object o)
        {
            return descriptor.GetValue(o);
        }
        public override void ResetValue(object o)
        {
            descriptor.ResetValue(o);
        }
        public override bool CanResetValue(object o)
        {
            return descriptor.CanResetValue(o);
        }
        public override void SetValue(object obj1, object obj2)
        {
            descriptor.SetValue(obj1, obj2);
        }
    }

    #endregion

    #region BaseCustomTypeDescriptor
    /// <summary>
    /// Custom type description for pipeline component properties
    /// </summary>
    public class BaseCustomTypeDescriptor : ICustomTypeDescriptor
    {
        private ResourceManager resourceManager;

        public BaseCustomTypeDescriptor(ResourceManager resourceManager)
        {
            this.resourceManager = resourceManager;
        }

        public AttributeCollection GetAttributes()
        {
            return new AttributeCollection(null);
        }
        public virtual string GetClassName()
        {
            return null;
        }
        public virtual string GetComponentName()
        {
            return null;
        }
        public TypeConverter GetConverter()
        {
            return null;
        }
        public EventDescriptor GetDefaultEvent()
        {
            return null;
        }
        public PropertyDescriptor GetDefaultProperty()
        {
            return null;
        }
        public object GetEditor(Type editorBaseType)
        {
            return null;
        }
        public EventDescriptorCollection GetEvents()
        {
            return EventDescriptorCollection.Empty;
        }
        public EventDescriptorCollection GetEvents(Attribute[] filter)
        {
            return EventDescriptorCollection.Empty;
        }

        public virtual PropertyDescriptorCollection GetProperties()
        {
            PropertyDescriptorCollection srcProperties = TypeDescriptor.GetProperties(this.GetType());
            ItineraryPropertyDescriptor[] bteProperties = new ItineraryPropertyDescriptor[srcProperties.Count];

            int i = 0;
            foreach (PropertyDescriptor srcDescriptor in srcProperties)
            {
                ItineraryPropertyDescriptor destDescriptor = new ItineraryPropertyDescriptor(srcDescriptor, resourceManager);
                AttributeCollection attributes = srcDescriptor.Attributes;
                bteProperties[i++] = destDescriptor;
            }
            PropertyDescriptorCollection destProperties = new PropertyDescriptorCollection(bteProperties);
            return destProperties;
        }

        public virtual PropertyDescriptorCollection GetProperties(Attribute[] filter)
        {
            PropertyDescriptorCollection baseProps = TypeDescriptor.GetProperties(this);
            return baseProps;
        }

        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }
    }
    #endregion
}
