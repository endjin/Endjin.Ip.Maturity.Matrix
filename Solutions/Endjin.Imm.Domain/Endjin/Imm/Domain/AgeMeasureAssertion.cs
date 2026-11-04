using System.Diagnostics;

namespace Endjin.Imm.Domain
{
    [DebuggerDisplay("Date = {Date}")]
    public class AgeMeasureAssertion : MeasureAssertion
    {
        public string Date { get; set; } = "";
    }
}
