namespace Endjin.Imm.Serialisation
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
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
    /// YAML node deserializer that reads a rule mapping straight into a <see cref="RuleDefinition"/>
    /// or a <see cref="RuleAssertion"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This replaces YamlDotNet's general-purpose object deserializer for rules. That deserializer
    /// finds the property for each key by enumerating the type's property descriptors, keeps sets
    /// of the keys and properties it has seen, and writes each value through reflection. It also
    /// reads every property value, and every measure, through YamlDotNet's value pipeline, which
    /// for each node tries each type resolver and each node deserializer in turn. This reads the
    /// few known properties directly, reads the common forms of scalar values with
    /// <see cref="ScalarValueReader"/>, and passes each ordinary measure mapping straight to
    /// <see cref="MeasureNodeDeserializer"/>, which together save a large share of the time and
    /// allocations made by loading a document.
    /// </para>
    /// <para>
    /// It reproduces the observable behaviour of that deserializer exactly, including for unusual
    /// documents (a test compares the two). In particular:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// Keys are matched case-insensitively (ordinal). Unrecognised keys are skipped without their
    /// values being read. A repeated key overwrites the earlier value.
    /// </item>
    /// <item>
    /// Values are read by the deserializer's own value pipeline (so anchors, aliases, tags, null
    /// forms and scalar conversion behave as for any other property) and then converted to the
    /// property type as YamlDotNet would, including deferred assignment when a value is an alias
    /// whose anchored node has not finished deserializing. The exceptions are untagged,
    /// unanchored nodes whose result through the pipeline is known: scalar values in the forms
    /// that <see cref="ScalarValueReader"/> reads, the <c>Measures</c> sequence, and each measure
    /// mapping in it. These are read directly, with the same result.
    /// </item>
    /// <item>
    /// Exceptions are reported as YamlDotNet reported them, and as soon as they occur: a property
    /// that cannot be read produces the same exception as the object deserializer would, and
    /// parser errors propagate unchanged.
    /// </item>
    /// </list>
    /// <para>
    /// Only mappings are handled here. Every other node where a rule is expected (null, scalar,
    /// sequence, or a node whose tag resolves to another type) is left to YamlDotNet, as is the
    /// root sequence of rules.
    /// </para>
    /// <para>
    /// This type is stateless, so a single instance can be used for concurrent deserialization.
    /// </para>
    /// </remarks>
    internal sealed class RuleNodeDeserializer : INodeDeserializer
    {
        // Used only for enum conversion of values that are not already of the property's type.
        private static readonly ITypeInspector TypeInspector = new ReadablePropertiesTypeInspector(new StaticTypeResolver());

        // Stateless, so it can be shared, and called directly for the measures of a rule.
        private static readonly MeasureNodeDeserializer Measures = new();

        private enum RuleProperty
        {
            None,
            Name,
            Id,
            DataType,
            CanOptOut,
            OptOut,
            Measures,
        }

        /// <inheritdoc/>
        public bool Deserialize(IParser reader, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value, ObjectDeserializer rootDeserializer)
        {
            if (expectedType == typeof(RuleDefinition))
            {
                if (reader.TryConsume<MappingStart>(out _))
                {
                    value = ReadDefinition(reader, nestedObjectDeserializer, rootDeserializer);
                    return true;
                }
            }
            else if (expectedType == typeof(RuleAssertion))
            {
                if (reader.TryConsume<MappingStart>(out _))
                {
                    value = ReadAssertion(reader, nestedObjectDeserializer, rootDeserializer);
                    return true;
                }
            }

            value = null;
            return false;
        }

        private static RuleDefinition ReadDefinition(IParser parser, Func<IParser, Type, object?> nestedObjectDeserializer, ObjectDeserializer rootDeserializer)
        {
            RuleDefinition rule = new();

            while (!parser.TryConsume<MappingEnd>(out _))
            {
                Scalar key = parser.Consume<Scalar>();
                RuleProperty property =
                    Is(key, nameof(RuleDefinition.Name)) ? RuleProperty.Name
                    : Is(key, nameof(RuleDefinition.Id)) ? RuleProperty.Id
                    : Is(key, nameof(RuleDefinition.DataType)) ? RuleProperty.DataType
                    : Is(key, nameof(RuleDefinition.CanOptOut)) ? RuleProperty.CanOptOut
                    : Is(key, nameof(RuleDefinition.Measures)) ? RuleProperty.Measures
                    : RuleProperty.None;

                ReadProperty(parser, key, rule, property, nestedObjectDeserializer, rootDeserializer);
            }

            return rule;
        }

        private static RuleAssertion ReadAssertion(IParser parser, Func<IParser, Type, object?> nestedObjectDeserializer, ObjectDeserializer rootDeserializer)
        {
            RuleAssertion rule = new();

            while (!parser.TryConsume<MappingEnd>(out _))
            {
                Scalar key = parser.Consume<Scalar>();
                RuleProperty property =
                    Is(key, nameof(RuleAssertion.Name)) ? RuleProperty.Name
                    : Is(key, nameof(RuleAssertion.Id)) ? RuleProperty.Id
                    : Is(key, nameof(RuleAssertion.OptOut)) ? RuleProperty.OptOut
                    : Is(key, nameof(RuleAssertion.Measures)) ? RuleProperty.Measures
                    : RuleProperty.None;

                ReadProperty(parser, key, rule, property, nestedObjectDeserializer, rootDeserializer);
            }

            return rule;
        }

        private static bool Is(Scalar key, string propertyName) => string.Equals(key.Value, propertyName, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Reads one property value into the rule, following YamlDotNet's <c>ObjectNodeDeserializer</c>.
        /// </summary>
        private static void ReadProperty(IParser parser, Scalar key, object rule, RuleProperty property, Func<IParser, Type, object?> nestedObjectDeserializer, ObjectDeserializer rootDeserializer)
        {
            try
            {
                if (property == RuleProperty.None)
                {
                    parser.SkipThisAndNestedEvents();
                    return;
                }

                Type propertyType = PropertyType(rule, property);
                object? value =
                    ScalarValueReader.TryRead(parser, propertyType, out object? scalar) ? scalar
                    : property == RuleProperty.Measures && parser.Current is SequenceStart sequence && sequence.Tag.IsEmpty && sequence.Anchor.IsEmpty
                    ? ReadMeasures(parser, rule, nestedObjectDeserializer, rootDeserializer)
                    : nestedObjectDeserializer(parser, propertyType);
                if (value is IValuePromise promise)
                {
                    Defer(promise, rule, property, propertyType);
                }
                else
                {
                    Write(rule, property, ChangeType(value, propertyType));
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

        /// <summary>
        /// Reads an untagged, unanchored measure sequence as YamlDotNet's value pipeline and
        /// <c>CollectionNodeDeserializer</c> would, without going through the pipeline for the
        /// sequence node itself (see <see cref="ReadMeasure"/> for each item).
        /// </summary>
        /// <remarks>
        /// For such a node, every type resolver and node deserializer ahead of the collection
        /// deserializer declines it, so the result is a <see cref="List{T}"/> filled in the same
        /// way, and an exception other than a <see cref="YamlException"/> is reported at the
        /// sequence as the pipeline would report it.
        /// </remarks>
        private static IList ReadMeasures(IParser parser, object rule, Func<IParser, Type, object?> nestedObjectDeserializer, ObjectDeserializer rootDeserializer)
        {
            SequenceStart sequence = (SequenceStart)parser.Current!;
            Type itemType;
            IList list;
            if (rule is RuleDefinition)
            {
                itemType = typeof(MeasureDefinition);
                list = new List<MeasureDefinition>();
            }
            else
            {
                itemType = typeof(MeasureAssertion);
                list = new List<MeasureAssertion>();
            }

            try
            {
                parser.MoveNext();
                while (!parser.TryConsume<SequenceEnd>(out _))
                {
                    object? item = ReadMeasure(parser, itemType, nestedObjectDeserializer, rootDeserializer);
                    if (item is IValuePromise promise)
                    {
                        DeferItem(promise, list, list.Add(null), itemType);
                    }
                    else
                    {
                        list.Add(ChangeType(item, itemType));
                    }
                }
            }
            catch (YamlException)
            {
                throw;
            }
            catch (Exception x)
            {
                throw new YamlException(sequence.Start, sequence.End, "Exception during deserialization", x);
            }

            return list;
        }

        /// <summary>
        /// Reads one measure. An untagged, unanchored mapping goes straight to the
        /// <see cref="MeasureNodeDeserializer"/>, which is the node deserializer YamlDotNet's value
        /// pipeline would choose for it, with any exception other than a
        /// <see cref="YamlException"/> reported at the mapping as the pipeline would report it.
        /// Every other node (null forms, aliases, anchored or tagged nodes, scalars, sequences) goes
        /// through the pipeline.
        /// </summary>
        private static object? ReadMeasure(IParser parser, Type itemType, Func<IParser, Type, object?> nestedObjectDeserializer, ObjectDeserializer rootDeserializer)
        {
            if (parser.Current is not MappingStart mapping || !mapping.Tag.IsEmpty || !mapping.Anchor.IsEmpty)
            {
                return nestedObjectDeserializer(parser, itemType);
            }

            try
            {
                Measures.Deserialize(parser, itemType, nestedObjectDeserializer, out object? measure, rootDeserializer);
                return measure;
            }
            catch (YamlException)
            {
                throw;
            }
            catch (Exception x)
            {
                throw new YamlException(mapping.Start, mapping.End, "Exception during deserialization", x);
            }
        }

        private static void DeferItem(IValuePromise promise, IList list, int index, Type itemType) =>
            promise.ValueAvailable += v => list[index] = ChangeType(v, itemType);

        // A separate method so that the closure is allocated only when a value is deferred.
        private static void Defer(IValuePromise promise, object rule, RuleProperty property, Type propertyType) =>
            promise.ValueAvailable += v => Write(rule, property, ChangeType(v, propertyType));

        private static Type PropertyType(object rule, RuleProperty property) => property switch
        {
            RuleProperty.Name => typeof(string),
            RuleProperty.Id => typeof(Guid),
            RuleProperty.DataType => typeof(DataType),
            RuleProperty.CanOptOut or RuleProperty.OptOut => typeof(bool),
            _ => rule is RuleDefinition ? typeof(IList<MeasureDefinition>) : typeof(IList<MeasureAssertion>),
        };

        private static object? ChangeType(object? value, Type propertyType) =>
            value is not null && value.GetType() == propertyType
                ? value
                : TypeConverter.ChangeType(value, propertyType, NullNamingConvention.Instance, TypeInspector);

        /// <summary>
        /// Writes a converted value to the rule's property. A value that is not exactly of the
        /// property's type (which the conversion can return, for example an integer for an enum)
        /// is written through reflection, as YamlDotNet writes every value, so that it is accepted
        /// or rejected in the same way.
        /// </summary>
        private static void Write(object rule, RuleProperty property, object? value)
        {
            switch (rule, property, value)
            {
                case (RuleDefinition r, RuleProperty.Name, string or null): r.Name = (string?)value; break;
                case (RuleDefinition r, RuleProperty.Id, Guid v): r.Id = v; break;
                case (RuleDefinition r, RuleProperty.DataType, DataType v): r.DataType = v; break;
                case (RuleDefinition r, RuleProperty.CanOptOut, bool v): r.CanOptOut = v; break;
                case (RuleDefinition r, RuleProperty.Measures, IList<MeasureDefinition> or null): r.Measures = (IList<MeasureDefinition>)value!; break;
                case (RuleAssertion r, RuleProperty.Name, string or null): r.Name = (string?)value; break;
                case (RuleAssertion r, RuleProperty.Id, Guid v): r.Id = v; break;
                case (RuleAssertion r, RuleProperty.OptOut, bool v): r.OptOut = v; break;
                case (RuleAssertion r, RuleProperty.Measures, IList<MeasureAssertion> or null): r.Measures = (IList<MeasureAssertion>)value!; break;
                default:
                    rule.GetType().GetProperty(property.ToString())!.SetValue(rule, value, null);
                    break;
            }
        }
    }
}
