namespace Endjin.Imm.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// An entry from an <c>imm.yaml</c> file, in which a particular project is asserting its
    /// measures against a particular rule.
    /// </summary>
    [DebuggerDisplay("Name = {Name}")]
    public partial class RuleAssertion
    {
        public string? Name { get; set; }

        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the project has opted out of this particular
        /// rule.
        /// </summary>
        /// <remarks>
        /// This may be true only if the corresponding <see cref="RuleDefinition.CanOptOut"/>
        /// property is true.
        /// </remarks>
        public bool OptOut { get; set; }

        public IList<MeasureAssertion> Measures { get; set; } = new List<MeasureAssertion>();
    }
}