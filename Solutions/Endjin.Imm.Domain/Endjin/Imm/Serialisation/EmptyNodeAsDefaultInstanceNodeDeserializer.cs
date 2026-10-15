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
    /// explicit <c>tag:yaml.org,2002:null</c> (<c>!!null</c>) tag, or a plain scalar whose value
    /// is empty, <c>~</c>, <c>null</c>, <c>Null</c> or <c>NULL</c>.
    /// </para>
    /// <para>
    /// YamlDotNet has no tag mapping for <c>!!null</c>, so its <c>PreventUnknownTagsNodeTypeResolver</c>
    /// rejects such a node as an unresolved tag before any node deserializer sees it. So that the
    /// tag can be handled, this type is also a node type resolver, and an instance must be
    /// registered as one (ahead of that resolver) as well as as a node deserializer. As a resolver
    /// it accepts only a <c>!!null</c> scalar where a <typeparamref name="T"/> is expected, so
    /// <c>!!null</c> remains an unresolved tag everywhere else.
    /// </para>
    /// </remarks>
    internal sealed class EmptyNodeAsDefaultInstanceNodeDeserializer<T> : INodeDeserializer, INodeTypeResolver
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

        /// <inheritdoc/>
        public bool Resolve(NodeEvent? nodeEvent, ref Type currentType) =>
            currentType == typeof(T) && nodeEvent is Scalar scalar && HasNullTag(scalar);

        private static bool IsNull(Scalar scalar)
        {
            if (HasNullTag(scalar))
            {
                return true;
            }

            return scalar.Style == ScalarStyle.Plain
                && scalar.Value is "" or "~" or "null" or "Null" or "NULL";
        }

        private static bool HasNullTag(Scalar scalar) => !scalar.Tag.IsEmpty && scalar.Tag.Value == NullTag;
    }
}
