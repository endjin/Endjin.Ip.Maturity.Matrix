namespace Endjin.Imm.Serialisation
{
    using System;
    using Endjin.Imm.Domain;
    using YamlDotNet.Core;
    using YamlDotNet.Core.Events;

    /// <summary>
    /// Reads the common forms of a scalar property value straight from the parser, when the
    /// result is certain to be the one YamlDotNet's value pipeline would produce for it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every value that a deserializer reads through the pipeline passes through its alias
    /// handling, every type resolver and, in turn, each node deserializer until one accepts it.
    /// For the scalars that make up most of a rule set or IMM, that costs far more than the
    /// conversion itself. Reading them here instead does not change the result, because only
    /// values for which the pipeline's result is known are read:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// Only an untagged, unanchored scalar qualifies, so no type resolver applies and no alias can
    /// refer to it. (An alias is an <see cref="AnchorAlias"/> event, not a scalar.)
    /// </item>
    /// <item>
    /// <see cref="string"/>: any value except a plain null form (empty, <c>~</c>, <c>null</c>,
    /// <c>Null</c> or <c>NULL</c>), which YamlDotNet's null deserializer turns into null.
    /// Otherwise its scalar deserializer returns the text unchanged.
    /// </item>
    /// <item>
    /// <see cref="bool"/>: exactly <c>true</c> or <c>false</c>, ignoring case, which are among the
    /// forms that its scalar deserializer accepts.
    /// </item>
    /// <item>
    /// <see cref="long"/>: <c>0</c>, or 1 to 18 decimal digits with no leading zero, which its
    /// scalar deserializer reads as the same decimal number. (Any other form, such as a sign,
    /// leading zero, underscore, base prefix or base-60 separator, is left to it.)
    /// </item>
    /// <item>
    /// <see cref="Guid"/>: a value that <see cref="Guid.TryParse(string?, out Guid)"/> accepts,
    /// which YamlDotNet's <c>GuidConverter</c> parses identically with <c>new Guid(string)</c>.
    /// </item>
    /// <item>
    /// <see cref="DataType"/>: exactly one of its names, ignoring case, which its scalar
    /// deserializer parses with <see cref="Enum.Parse(Type, string, bool)"/> ignoring case.
    /// </item>
    /// </list>
    /// <para>
    /// None of these values is a null form, so YamlDotNet's null deserializer, which comes before
    /// its scalar deserializer, would not have accepted any of them. The tests that compare loads
    /// with YamlDotNet's own handling cover each of these rules.
    /// </para>
    /// </remarks>
    internal static class ScalarValueReader
    {
        /// <summary>
        /// Reads the current scalar as a value of the given type if it is in one of the forms
        /// described in the remarks for <see cref="ScalarValueReader"/>.
        /// </summary>
        /// <param name="parser">The parser, positioned at the value.</param>
        /// <param name="type">The type of the property being read.</param>
        /// <param name="value">The value, if it was read.</param>
        /// <returns>
        /// <see langword="true"/> if the value was read (and consumed); <see langword="false"/> if
        /// nothing was consumed and the value must be read through the pipeline.
        /// </returns>
        public static bool TryRead(IParser parser, Type type, out object? value)
        {
            value = null;
            if (parser.Current is not Scalar scalar || !scalar.Tag.IsEmpty || !scalar.Anchor.IsEmpty)
            {
                return false;
            }

            string text = scalar.Value;
            if (type == typeof(string))
            {
                if (scalar.Style == ScalarStyle.Plain && text is "" or "~" or "null" or "Null" or "NULL")
                {
                    return false;
                }

                value = text;
            }
            else if (type == typeof(long))
            {
                if (!TryParseDecimal(text, out long number))
                {
                    return false;
                }

                value = number;
            }
            else if (type == typeof(bool))
            {
                if (text.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    value = true;
                }
                else if (text.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    value = false;
                }
                else
                {
                    return false;
                }
            }
            else if (type == typeof(Guid))
            {
                if (!Guid.TryParse(text, out Guid id))
                {
                    return false;
                }

                value = id;
            }
            else if (type == typeof(DataType))
            {
                DataType? dataType =
                    text.Equals(nameof(DataType.Continuous), StringComparison.OrdinalIgnoreCase) ? DataType.Continuous
                    : text.Equals(nameof(DataType.Discrete), StringComparison.OrdinalIgnoreCase) ? DataType.Discrete
                    : text.Equals(nameof(DataType.Age), StringComparison.OrdinalIgnoreCase) ? DataType.Age
                    : text.Equals(nameof(DataType.Framework), StringComparison.OrdinalIgnoreCase) ? DataType.Framework
                    : null;
                if (dataType is null)
                {
                    return false;
                }

                value = dataType.Value;
            }
            else
            {
                return false;
            }

            parser.MoveNext();
            return true;
        }

        private static bool TryParseDecimal(string text, out long number)
        {
            number = 0;
            if (text.Length is 0 or > 18 || (text[0] == '0' && text.Length > 1))
            {
                return false;
            }

            foreach (char c in text)
            {
                if (c is < '0' or > '9')
                {
                    return false;
                }

                number = (number * 10) + (c - '0');
            }

            return true;
        }
    }
}
