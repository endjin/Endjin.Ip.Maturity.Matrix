namespace Endjin.Imm.Serialisation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization;
    using Endjin.Imm.Domain;
    using YamlDotNet.Core;
    using YamlDotNet.Core.Events;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.NamingConventions;
    using YamlDotNet.Serialization.TypeInspectors;
    using YamlDotNet.Serialization.TypeResolvers;
    using YamlDotNet.Serialization.Utilities;

    /// <summary>
    /// YAML node deserializer that reads a measure mapping straight into the right concrete
    /// <see cref="MeasureDefinition"/> or <see cref="MeasureAssertion"/> type, choosing the type
    /// from the keys present (see <see cref="ImmYamlDeserializer.Create"/>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This replaces YamlDotNet's type-discriminating node deserializer (configured with unique key
    /// discriminators) followed by its general-purpose object deserializer, which between them
    /// copied every measure's events into a buffer, scanned it for a discriminating key, and then
    /// replayed it through reflection-based property matching. Measures are the most numerous
    /// nodes in both rule sets and IMMs, so doing this in a single pass over the parser saves a
    /// significant share of the allocations made by loading a document.
    /// </para>
    /// <para>
    /// It reproduces the observable behaviour of that configuration exactly, including for unusual
    /// documents (a test compares the two). In particular:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// The type is chosen by the first key, in document order, that names a discriminating
    /// property (<c>Framework</c> or <c>Age</c> for a definition, <c>Framework</c> or <c>Date</c>
    /// for an assertion). Any later key for the other discriminating property is not a property of
    /// the chosen type, so like any other unrecognised key its value is skipped without being read.
    /// Because every non-discriminating property belongs to the base type, keys that precede the
    /// discriminating one can be read before the type is known.
    /// </item>
    /// <item>
    /// Keys are matched case-insensitively (ordinal). A repeated key overwrites the earlier value.
    /// Values are read by the deserializer's own value pipeline (so anchors, aliases, tags, null
    /// forms and scalar conversion behave as for any other property) and then converted to the
    /// property type as YamlDotNet would, including deferred assignment when a value is an alias
    /// whose anchored node has not finished deserializing.
    /// </item>
    /// <item>
    /// Exceptions are reported as YamlDotNet reported them. A property that cannot be read
    /// produces the same exception as its object deserializer would. Because the previous
    /// configuration read the whole mapping into a buffer before deserializing anything, an error
    /// from the parser anywhere within the mapping (or in the event just after it) took precedence
    /// and was reported as a plain <see cref="YamlException"/>; this reads the rest of the mapping
    /// before reporting a property error, and reports parser errors in the same way.
    /// </item>
    /// </list>
    /// <para>
    /// This type is stateless, so a single instance can be used for concurrent deserialization.
    /// </para>
    /// </remarks>
    internal sealed class MeasureNodeDeserializer : INodeDeserializer
    {
        // Used only for enum conversion, which never applies to measure properties.
        private static readonly ITypeInspector TypeInspector = new ReadablePropertiesTypeInspector(new StaticTypeResolver());

        /// <inheritdoc/>
        public bool Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value, ObjectDeserializer rootDeserializer)
        {
            bool isDefinition = expectedType == typeof(MeasureDefinition);
            if ((!isDefinition && expectedType != typeof(MeasureAssertion)) || !reader.Accept<MappingStart>(out _))
            {
                value = null;
                return false;
            }

            MappingParser mapping = new(reader);
            try
            {
                value = isDefinition
                    ? ReadDefinition(mapping, nestedObjectDeserializer)
                    : ReadAssertion(mapping, nestedObjectDeserializer);
                return true;
            }
            catch (Exception) when (mapping.Fault is null)
            {
                // Not a parser error (those propagate unchanged, because YamlDotNet's value
                // deserializers rethrow a YamlException as it is). A parser error later in the
                // mapping takes precedence over this one.
                mapping.SkipRemainder();
                throw;
            }
        }

        private static MeasureDefinition ReadDefinition(MappingParser parser, Func<IParser, Type, object?> nestedObjectDeserializer)
        {
            MeasureFields fields = default;
            Discriminated discriminated = Discriminated.None;

            while (!parser.TryConsume<MappingEnd>(out _))
            {
                Scalar key = parser.Consume<Scalar>();
                MeasureProperty property =
                    Is(key, nameof(MeasureDefinition.Score)) ? MeasureProperty.Score
                    : Is(key, nameof(MeasureDefinition.Description)) ? MeasureProperty.Description
                    : Is(key, nameof(MeasureDefinition.CanOptOut)) ? MeasureProperty.OptOut
                    : Is(key, nameof(FrameworkMeasureDefinition.Framework)) ? Discriminate(ref discriminated, Discriminated.Framework)
                    : Is(key, nameof(AgeMeasureDefinition.Age)) ? Discriminate(ref discriminated, Discriminated.AgeOrDate)
                    : MeasureProperty.None;

                ReadProperty(parser, key, property, nestedObjectDeserializer, ref fields);
            }

            MeasureDefinition measure = discriminated switch
            {
                Discriminated.Framework => new FrameworkMeasureDefinition(),
                Discriminated.AgeOrDate => new AgeMeasureDefinition(),
                _ => new MeasureDefinition(),
            };

            fields.ApplyTo(measure, static (target, property, value) =>
            {
                MeasureDefinition m = (MeasureDefinition)target;
                switch (property)
                {
                    case MeasureProperty.Score: m.Score = (long)value!; break;
                    case MeasureProperty.Description: m.Description = (string)value!; break;
                    case MeasureProperty.OptOut: m.CanOptOut = (bool)value!; break;
                    case MeasureProperty.Discriminator when m is FrameworkMeasureDefinition f: f.Framework = (string)value!; break;
                    case MeasureProperty.Discriminator when m is AgeMeasureDefinition a: a.Age = (string)value!; break;
                }
            });

            return measure;
        }

        private static MeasureAssertion ReadAssertion(MappingParser parser, Func<IParser, Type, object?> nestedObjectDeserializer)
        {
            MeasureFields fields = default;
            Discriminated discriminated = Discriminated.None;

            while (!parser.TryConsume<MappingEnd>(out _))
            {
                Scalar key = parser.Consume<Scalar>();
                MeasureProperty property =
                    Is(key, nameof(MeasureAssertion.Score)) ? MeasureProperty.Score
                    : Is(key, nameof(MeasureAssertion.Description)) ? MeasureProperty.Description
                    : Is(key, nameof(MeasureAssertion.OptOut)) ? MeasureProperty.OptOut
                    : Is(key, nameof(FrameworkMeasureAssertion.Framework)) ? Discriminate(ref discriminated, Discriminated.Framework)
                    : Is(key, nameof(AgeMeasureAssertion.Date)) ? Discriminate(ref discriminated, Discriminated.AgeOrDate)
                    : MeasureProperty.None;

                ReadProperty(parser, key, property, nestedObjectDeserializer, ref fields);
            }

            MeasureAssertion measure = discriminated switch
            {
                Discriminated.Framework => new FrameworkMeasureAssertion(),
                Discriminated.AgeOrDate => new AgeMeasureAssertion(),
                _ => new MeasureAssertion(),
            };

            fields.ApplyTo(measure, static (target, property, value) =>
            {
                MeasureAssertion m = (MeasureAssertion)target;
                switch (property)
                {
                    case MeasureProperty.Score: m.Score = (long)value!; break;
                    case MeasureProperty.Description: m.Description = (string)value!; break;
                    case MeasureProperty.OptOut: m.OptOut = (bool)value!; break;
                    case MeasureProperty.Discriminator when m is FrameworkMeasureAssertion f: f.Framework = (string)value!; break;
                    case MeasureProperty.Discriminator when m is AgeMeasureAssertion a: a.Date = (string)value!; break;
                }
            });

            return measure;
        }

        private static bool Is(Scalar key, string propertyName) => string.Equals(key.Value, propertyName, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Handles a key naming a discriminating property: the first such key chooses the type, and
        /// its property is the only discriminating property the chosen type has.
        /// </summary>
        private static MeasureProperty Discriminate(ref Discriminated discriminated, Discriminated keyType)
        {
            if (discriminated == Discriminated.None)
            {
                discriminated = keyType;
            }

            return discriminated == keyType ? MeasureProperty.Discriminator : MeasureProperty.None;
        }

        /// <summary>
        /// Reads one property value, following YamlDotNet's <c>ObjectNodeDeserializer</c>.
        /// </summary>
        private static void ReadProperty(IParser parser, Scalar key, MeasureProperty property, Func<IParser, Type, object?> nestedObjectDeserializer, ref MeasureFields fields)
        {
            try
            {
                if (property == MeasureProperty.None)
                {
                    parser.SkipThisAndNestedEvents();
                    return;
                }

                Type propertyType = property switch
                {
                    MeasureProperty.Score => typeof(long),
                    MeasureProperty.OptOut => typeof(bool),
                    _ => typeof(string),
                };

                object? value = nestedObjectDeserializer(parser, propertyType);
                if (value is IValuePromise promise)
                {
                    fields.Defer(property, propertyType, promise);
                }
                else
                {
                    fields.Set(property, ChangeType(value, propertyType));
                }
            }
            catch (SerializationException x)
            {
                throw new YamlException(key.Start, key.End, x.Message);
            }
            catch (YamlException)
            {
                throw;
            }
            catch (Exception x)
            {
                throw new YamlException(key.Start, key.End, "Exception during deserialization", x);
            }
        }

        private static object? ChangeType(object? value, Type propertyType) =>
            value is not null && value.GetType() == propertyType
                ? value
                : TypeConverter.ChangeType(value, propertyType, NullNamingConvention.Instance, TypeInspector);

        private enum MeasureProperty
        {
            None,
            Score,
            Description,
            OptOut,
            Discriminator,
        }

        private enum Discriminated
        {
            None,
            Framework,
            AgeOrDate,
        }

        /// <summary>
        /// The property values read so far, held until the type is known.
        /// </summary>
        private struct MeasureFields
        {
            private object? score;
            private object? description;
            private object? optOut;
            private object? discriminator;
            private PropertySet set;
            private List<(MeasureProperty Property, Type Type, IValuePromise Promise)>? deferred;

            [Flags]
            private enum PropertySet
            {
                None = 0,
                Score = 1,
                Description = 2,
                OptOut = 4,
                Discriminator = 8,
            }

            public void Set(MeasureProperty property, object? value)
            {
                switch (property)
                {
                    case MeasureProperty.Score: this.score = value; this.set |= PropertySet.Score; break;
                    case MeasureProperty.Description: this.description = value; this.set |= PropertySet.Description; break;
                    case MeasureProperty.OptOut: this.optOut = value; this.set |= PropertySet.OptOut; break;
                    case MeasureProperty.Discriminator: this.discriminator = value; this.set |= PropertySet.Discriminator; break;
                }
            }

            public void Defer(MeasureProperty property, Type type, IValuePromise promise) =>
                (this.deferred ??= []).Add((property, type, promise));

            public readonly void ApplyTo(object target, Action<object, MeasureProperty, object?> write)
            {
                if ((this.set & PropertySet.Score) != 0)
                {
                    write(target, MeasureProperty.Score, this.score);
                }

                if ((this.set & PropertySet.Description) != 0)
                {
                    write(target, MeasureProperty.Description, this.description);
                }

                if ((this.set & PropertySet.OptOut) != 0)
                {
                    write(target, MeasureProperty.OptOut, this.optOut);
                }

                if ((this.set & PropertySet.Discriminator) != 0)
                {
                    write(target, MeasureProperty.Discriminator, this.discriminator);
                }

                if (this.deferred is not null)
                {
                    foreach ((MeasureProperty property, Type type, IValuePromise promise) in this.deferred)
                    {
                        promise.ValueAvailable += v => write(target, property, ChangeType(v, type));
                    }
                }
            }
        }

        /// <summary>
        /// Passes through the events of one mapping, tracking how far through it the reader is so
        /// that the rest can be read after an error, and reporting parser errors as YamlDotNet's
        /// buffering type-discriminating deserializer did.
        /// </summary>
        private sealed class MappingParser : IParser
        {
            private const string BufferingFailedMessage = "Failed to buffer yaml node";

            private readonly IParser inner;
            private readonly Mark start;

            // Nesting depth relative to the inside of the mapping; -1 once its end has been consumed.
            private int depth = -1;

            public MappingParser(IParser inner)
            {
                this.inner = inner;
                this.start = inner.Current!.Start;
                this.MoveNext();
            }

            public ParsingEvent? Current => this.inner.Current;

            public YamlException? Fault { get; private set; }

            public bool MoveNext()
            {
                ParsingEvent? consumed = this.inner.Current;
                try
                {
                    bool moved = this.inner.MoveNext();
                    this.depth += consumed?.NestingIncrease ?? 0;
                    return moved;
                }
                catch (Exception x)
                {
                    this.Fault = new YamlException(this.start, this.inner.Current?.End ?? this.start, BufferingFailedMessage, x);
                    throw this.Fault;
                }
            }

            public void SkipRemainder()
            {
                while (this.depth >= 0)
                {
                    if (this.Current is null)
                    {
                        this.Fault = new YamlException(this.start, this.start, BufferingFailedMessage, new EndOfStreamException());
                        throw this.Fault;
                    }

                    this.MoveNext();
                }
            }
        }
    }
}
