namespace RippleSync.Infrastructure.SoMePlatforms.LinkedIn;
internal class LinkedInPostBuilder
{
    private string _author;
    private string _commentary;
    private string _visibility = "PUBLIC";
    private string _lifecycleState = "PUBLISHED";
    private bool _isReshareDisabledByAuthor = false;
    private readonly List<Media> _mediaItems = new();
    private Distribution _distribution = new Distribution();

    public LinkedInPostBuilder SetAuthor(string authorUrn)
    {
        _author = authorUrn;
        return this;
    }

    public LinkedInPostBuilder SetCommentary(string commentary)
    {
        _commentary = commentary;
        return this;
    }

    public LinkedInPostBuilder SetVisibility(string visibility)
    {
        _visibility = visibility;
        return this;
    }

    public LinkedInPostBuilder SetLifecycleState(string lifecycleState)
    {
        _lifecycleState = lifecycleState;
        return this;
    }

    public LinkedInPostBuilder SetReshareDisabled(bool disabled)
    {
        _isReshareDisabledByAuthor = disabled;
        return this;
    }

    public LinkedInPostBuilder AddImage(string imageUrn, string altText = "Uploaded Image")
    {
        _mediaItems.Add(new Media { Id = imageUrn, AltText = altText });
        return this;
    }

    public LinkedInPostBuilder AddImages(IEnumerable<string> imageUrns, string altText = "Uploaded Image")
    {
        foreach (var urn in imageUrns)
        {
            _mediaItems.Add(new Media { Id = urn, AltText = altText });
        }
        return this;
    }

    public LinkedInPostBuilder SetDistribution(Distribution distribution)
    {
        _distribution = distribution;
        return this;
    }

    public PublishPayload Build()
    {
        if (string.IsNullOrEmpty(_author))
            throw new InvalidOperationException("Author URN is required");

        var payload = new PublishPayload
        {
            Author = _author,
            Commentary = _commentary,
            Visibility = _visibility,
            LifecycleState = _lifecycleState,
            IsReshareDisabledByAuthor = _isReshareDisabledByAuthor,
            Distribution = _distribution
        };

        // Handle media based on count
        if (_mediaItems.Count == 1)
        {
            payload.Content = new Content
            {
                Media = new Media
                {
                    Id = _mediaItems[0].Id,
                    AltText = _mediaItems[0].AltText
                }
            };
        }
        else if (_mediaItems.Count > 1)
        {
            payload.Content = new Content
            {
                MultiImage = new MultiImage
                {
                    Images = _mediaItems.Select(m => new Media
                    {
                        Id = m.Id,
                        AltText = m.AltText
                    }).ToList()
                }
            };
        }

        return payload;
    }
}
