namespace Endjin.Imm.Serialisation
{
    using System;
    using YamlDotNet.Core;
    using YamlDotNet.Core.Events;
    using YamlDotNet.Serialization;

    /// <summary>
    /// YAML node deserializer that turns a null node into a default-constructed instance of
    /// <typeparamref name="T"/>, instead of a null reference.
    /// </summary>
    /// <typeparam name="T">The type for which null nodes should become default instances.</typeparam>
    /// <remarks>
    /// <para>
    /// This preserves the long-standing behaviour for a measure list entry consisting of a bare
    /// <c>-</c> with no properties (as found in <c>imm-default.yaml</c>): such entries produce an
    /// empty measure object rather than a null, which the rule calculators do not expect.
    /// </para>
    /// <para>
    /// The null detection mirrors YamlDotNet's own <c>NullNodeDeserializer</c>: a scalar with an
    /// explicit <c>tag:yaml.org,2002:null</c> tag, or a plain scalar whose value is empty,
    /// <c>~</c>, <c>null</c>, <c>Null</c> or <c>NULL</c>.
    /// </para>
    /// </remarks>
    internal sealed class EmptyNodeAsDefaultInstanceNodeDeserializer<T> : INodeDeserializer
        where T : class, new()
    {
        private const string NullTag = "tag:yaml.org,2002:null";

        /// <inheritdoc/>
        public bool Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value, ObjectDeserializer rootDeserializer)
        {
            value = null;

            if (expectedType != typeof(T) || !reader.Accept<Scalar>(out Scalar? scalar) || !IsNull(scalar))
            {
                return false;
            }

            reader.MoveNext();
            value = new T();
            return true;
        }

        private static bool IsNull(Scalar scalar)
        {
            if (!scalar.Tag.IsEmpty && scalar.Tag.Value == NullTag)
            {
                return true;
            }

            return scalar.Style == ScalarStyle.Plain
                && scalar.Value is "" or "~" or "null" or "Null" or "NULL";
        }
    }
}
