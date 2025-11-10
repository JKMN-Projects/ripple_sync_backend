using System.Text.Json.Serialization;

namespace RippleSync.Infrastructure.SoMePlatforms.LinkedIn;


internal class PublishPayload()
{
    [JsonPropertyName("author")]
    public string Author { get; set; }
    [JsonPropertyName("commentary")]
    public string Commentary { get; set; }
    [JsonPropertyName("visibility")]
    public string Visibility { get; set; } = "PUBLIC";
    [JsonPropertyName("lifecycleState")]
    public string LifecycleState { get; set; } = "PUBLISHED";
    [JsonPropertyName("isReshareDisabledByAuthor")]
    public bool IsReshareDisabledByAuthor { get; set; } = false;
    [JsonPropertyName("distribution")]
    public Distribution Distribution { get; set; } = new Distribution();
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("content")]
    public Content? Content { get; set; }

}
internal class Distribution
{
    [JsonPropertyName("feedDistribution")]
    public string FeedDistribution { get; set; } = "MAIN_FEED";
    [JsonPropertyName("targetEntities")]
    public List<string> TargetEntities { get; set; } = new List<string>();
    [JsonPropertyName("thirdPartyDistributionChannels")]
    public List<string> ThirdPartyDistributionChannels { get; set; } = new List<string>();
}

internal class Content
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("multiImage")]
    public MultiImage? MultiImage { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("media")]
    public Media? Media { get; set; }
}

internal class MultiImage
{
    [JsonPropertyName("images")]
    public List<Media> Images { get; set; }
}
internal class Media
{
    [JsonPropertyName("altText")]
    public string AltText { get; set; }
    [JsonPropertyName("id")]
    public string Id { get; set; }
}
