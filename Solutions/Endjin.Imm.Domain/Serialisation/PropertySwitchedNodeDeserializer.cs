namespace Endjin.Imm.Serialisation
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;
    using YamlDotNet.Core;
    using YamlDotNet.Serialization;

    /// <summary>
    /// Custom YAML node deserializer that works out whether a specialized derived type is needed
    /// based on which properties are present.
    /// </summary>
    internal class PropertySwitchedNodeDeserializer<TBase> : INodeDeserializer
    {
        private readonly (string propertyName, Type type)[] mappings;

        public PropertySwitchedNodeDeserializer(params (string propertyName, Type type)[] mappings)
        {
            this.mappings = mappings;
            Type baseType = typeof(TBase);
            var (badPropertyName, badType) = Array.Find(mappings, m => !baseType.IsAssignableFrom(m.type));
            if (badType != null)
            {
                throw new ArgumentException($"Property {badPropertyName} maps to Type {badType}, but this is not assignable to {baseType}");
            }
        }

        public bool Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object> nestedObjectDeserializer, out object? value)
        {
            value = null;

            if (expectedType != typeof(TBase))
            {
                return false;
            }

            // This is messy because the YamlDotNet parser does not support backtracking or arbitrary lookahead.
            // What we would like to do is peek at all the properties to work out whether any of the ones
            // signifying a special type are present (e.g. Age, or Framework) to determine which type we're
            // deserializing into, and then defer to YamlDotNet's default handling. Unfortunately, the
            // act of inspecting the properties is destructive: by the time we've read enough data from
            // the parser to discover what properties are there, we've moved the parser state beyond the
            // point where YamlDotNet's default handling would have been able to pick up. So instead we
            // end up having to do our own deserialization.
            // Really, the way this should work is that we should supply an INodeTypeResolver. YamlDotNet
            // invokes those to find out what type to use before it even begins to deserialize a node.
            // Unfortunately, the NodeEvent type that these resolvers get passed do not make it possible
            // to extract the information we need to determine the correct node type (and again, it's
            // the inability to peek forward that sinks us).
            //
            // We still get YamlDotNet to do some of the work. We ask it to deserialize everything into
            // a dictionary for us. We then use that to work out which target type we need, and finally,
            // we use the slightly shonky technique of going via a JObject to deserialize that dictionary
            // back into the output object.
            // This all only works because the objects in question are flat. If we needed nested properties,
            // this would all fall apart.

            Type targetType = typeof(TBase);
            var properties = (Dictionary<string, object>)nestedObjectDeserializer(reader, typeof(Dictionary<string, object>));
            if (properties != null)
            {
                foreach ((string propertyName, Type t) in this.mappings)
                {
                    if (properties.ContainsKey(propertyName))
                    {
                        targetType = t;
                        break;
                    }
                }
                var jo = JObject.FromObject(properties);
                value = jo.ToObject(targetType);
            }

            return true;
        }
    }
}