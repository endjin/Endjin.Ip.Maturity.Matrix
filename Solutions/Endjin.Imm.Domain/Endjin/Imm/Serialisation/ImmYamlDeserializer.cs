namespace Endjin.Imm.Serialisation
{
    using System;
    using System.Collections.Generic;
    using Endjin.Imm.Domain;
    using YamlDotNet.Serialization;

    /// <summary>
    /// Builds the YAML deserializer used to load both rule set definitions and project IMM files.
    /// </summary>
    internal static class ImmYamlDeserializer
    {
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
        /// with none of these keys deserialize as the base type. Key matching is case-insensitive.
        /// </para>
        /// <para>
        /// Property matching is case-insensitive, and unrecognised properties are ignored. A bare
        /// <c>-</c> measure entry becomes a default instance of the base measure type (see
        /// <see cref="EmptyNodeAsDefaultInstanceNodeDeserializer{T}"/>).
        /// </para>
        /// </remarks>
        public static IDeserializer Create() =>
            new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .WithCaseInsensitivePropertyMatching()
                .WithNodeDeserializer(new EmptyNodeAsDefaultInstanceNodeDeserializer<MeasureDefinition>())
                .WithNodeDeserializer(new EmptyNodeAsDefaultInstanceNodeDeserializer<MeasureAssertion>())

                // The type-discriminating deserializer must be configured last.
                .WithTypeDiscriminatingNodeDeserializer(options =>
                {
                    options.AddUniqueKeyTypeDiscriminator<MeasureDefinition>(
                        new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
                        {
                            [nameof(FrameworkMeasureDefinition.Framework)] = typeof(FrameworkMeasureDefinition),
                            [nameof(AgeMeasureDefinition.Age)] = typeof(AgeMeasureDefinition),
                        });
                    options.AddUniqueKeyTypeDiscriminator<MeasureAssertion>(
                        new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
                        {
                            [nameof(FrameworkMeasureAssertion.Framework)] = typeof(FrameworkMeasureAssertion),
                            [nameof(AgeMeasureAssertion.Date)] = typeof(AgeMeasureAssertion),
                        });
                })
                .Build();
    }
}
