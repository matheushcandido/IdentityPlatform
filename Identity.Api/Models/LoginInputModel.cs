namespace Identity.Api.Models;

public class LoginInputModel
{
    public string? Email { get; set; }

    public string? Password { get; set; }

    public string? ReturnUrl { get; set; }
}
