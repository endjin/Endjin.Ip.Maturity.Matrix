using System.Net.Http;
using System.Threading.Tasks;

namespace Endjin.Imm.Caching
{
    /// <summary>
    /// Used by <see cref="HttpDeserializingCache{T}"/> to produce the deserialized result to be
    /// held in the cache.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <remarks>
    /// <para>
    /// Typically, an implementation of this for each target serialized type will be registered.
    /// In IMM apps, the DI initialization adds two: one for reading the rule definitions, and
    /// one for reading per-project IMMs.
    /// </para>
    /// </remarks>
    internal interface IHttpDeserializer<T>
    {
        Task<T> DeserializeAsync(HttpResponseMessage response);
    }
}
