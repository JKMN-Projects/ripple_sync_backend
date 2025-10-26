using System.ComponentModel.DataAnnotations;

namespace RippleSync.API.Authentication;

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);
