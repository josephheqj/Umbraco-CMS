using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Serialization;

namespace Umbraco.Cms.Core.PropertyEditors;

public class TagPropertyIndexValueFactory : JsonPropertyIndexValueFactoryBase<string[]>, ITagPropertyIndexValueFactory
{
    public TagPropertyIndexValueFactory(IJsonSerializer jsonSerializer) : base(jsonSerializer)
    {
    }

    protected override IEnumerable<KeyValuePair<string, IEnumerable<object?>>> Handle(
        string[] deserializedPropertyValue,
        IProperty property,
        string? culture,
        string? segment,
        bool published,
        IEnumerable<string> availableCultures)
    {
        yield return new KeyValuePair<string, IEnumerable<object?>>(property.Alias, deserializedPropertyValue);
    }

    [Obsolete("Use the overload that specifies availableCultures, scheduled for removal in v14")]
    protected override IEnumerable<KeyValuePair<string, IEnumerable<object?>>> Handle(
        string[] deserializedPropertyValue,
        IProperty property,
        string? culture,
        string? segment,
        bool published)
    {
        yield return new KeyValuePair<string, IEnumerable<object?>>(property.Alias, deserializedPropertyValue);
    }
}
