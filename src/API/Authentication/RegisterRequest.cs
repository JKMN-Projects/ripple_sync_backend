using System.ComponentModel.DataAnnotations;

namespace RippleSync.API.Authentication;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, StringLength(32, MinimumLength = 8)] string Password);