using System.ComponentModel.DataAnnotations;
namespace Roomr.Auth;

public class User
{
    [Key]
    [Required]
    public string Username { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}