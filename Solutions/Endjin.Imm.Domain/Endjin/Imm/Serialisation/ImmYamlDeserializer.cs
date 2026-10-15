namespace Endjin.Imm.Serialisation
{
    using Endjin.Imm.Domain;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.NodeDeserializers;

    /// <summary>
    /// Builds the YAML deserializer used to load both rule set definitions and project IMM files.
    /// </summary>
    internal static class ImmYamlDeserializer
    {
        /// <summary>
        /// Gets a deserializer, configured as by <see cref="Create"/>, that is shared by every load.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Building a deserializer is expensive (both in its own right and because each new instance
        /// starts with empty type metadata caches that the first load has to populate), so the
        /// factory methods use this single instance rather than building one per call.
        /// </para>
        /// <para>
        /// Sharing it, including concurrently (rule sets and IMMs are fetched and loaded in
        /// parallel), is safe because a YamlDotNet <see cref="Deserializer"/> holds no per-document
        /// state: each <c>Deserialize</c> call creates its own parser and serializer state, and the
        /// configured components are immutable after <see cref="DeserializerBuilder.Build"/> or keep
        /// only thread-safe caches. Specifically, the type inspector and object factory caches are
        /// concurrent dictionaries keyed by type, and <see cref="MeasureNodeDeserializer"/> and
        /// <see cref="EmptyNodeAsDefaultInstanceNodeDeserializer{T}"/> are stateless. A load that
        /// fails part way through therefore leaves nothing behind that could affect later loads.
        /// </para>
        /// <para>
        /// Any new node deserializer or type resolver added to <see cref="Create"/> must keep this
        /// property: it must not hold mutable per-document state.
        /// </para>
        /// </remarks>
        public static IDeserializer Shared { get; } = Create();

        /// <summary>
        /// Creates a deserializer configured for IMM YAML documents.
        /// </summary>
        /// <returns>The deserializer.</returns>
        /// <remarks>
        /// <para>
        /// Measure entries are polymorphic, but the YAML carries no explicit type tags. Instead, the
        /// presence of a particular key determines the concrete type: a measure definition with a
        /// <c>Framework</c> key becomes a <see cref="FrameworkMeasureDefinition"/>, and one with an
        /// <c>Age</c> key becomes an <see cref="AgeMeasureDefinition"/>. Likewise, a measure
        /// assertion with a <c>Framework</c> key becomes a <see cref="FrameworkMeasureAssertion"/>,
        /// and one with a <c>Date</c> key becomes an <see cref="AgeMeasureAssertion"/>. Entries
        /// with none of these keys deserialize as the base type. If an entry has both, the first
        /// one determines the type. Key matching is case-insensitive. This is implemented by
        /// <see cref="MeasureNodeDeserializer"/>.
        /// </para>
        /// <para>
        /// Property matching is case-insensitive, and unrecognised properties are ignored. A bare
        /// <c>-</c> measure entry, or one that is null in any other form including <c>!!null</c>,
        /// becomes a default instance of the base measure type (see
        /// <see cref="EmptyNodeAsDefaultInstanceNodeDeserializer{T}"/>).
        /// </para>
        /// </remarks>
        public static IDeserializer Create()
        {
            var emptyMeasureDefinition = new EmptyNodeAsDefaultInstanceNodeDeserializer<MeasureDefinition>();
            var emptyMeasureAssertion = new EmptyNodeAsDefaultInstanceNodeDeserializer<MeasureAssertion>();

            return new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .WithCaseInsensitivePropertyMatching()

                // Also registered as type resolvers (on top, so ahead of the one that rejects
                // unknown tags), so that a !!null measure entry reaches them as deserializers.
                .WithNodeTypeResolver(emptyMeasureDefinition)
                .WithNodeTypeResolver(emptyMeasureAssertion)
                .WithNodeDeserializer(emptyMeasureDefinition)
                .WithNodeDeserializer(emptyMeasureAssertion)

                // In the position YamlDotNet uses for its own type-discriminating deserializer.
                .WithNodeDeserializer(new MeasureNodeDeserializer(), s => s.Before<DictionaryNodeDeserializer>())
                .Build();
        }
    }
}
