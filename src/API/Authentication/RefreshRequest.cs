using System.ComponentModel.DataAnnotations;

namespace RippleSync.API.Authentication;

public sealed record RefreshRequest(
    [Required] string RefreshToken);
