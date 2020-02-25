namespace Endjin.Imm.Extensions
{
    using Endjin.Imm.Domain;
    using Endjin.Imm.Serialisation;
    using Newtonsoft.Json;

    public static class IpMaturityMatrixExtensions
    {
        public static string ToJson(this IpMaturityMatrixRuleset self) => JsonConvert.SerializeObject(self, Converter.Settings);
    }
}