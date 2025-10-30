using System.ComponentModel.DataAnnotations;

namespace ASPNETCoreStatusCodes.Models;
// ReSharper disable All
public class UserPatchDto
{
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string? Email { get; set; }

    [StringLength(100, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 100 characters")]
    public string? FirstName { get; set; }

    [StringLength(100, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 100 characters")]
    public string? LastName { get; set; }

    [Range(18, 120, ErrorMessage = "Age must be between 18 and 120")]
    public int? Age { get; set; }

    public bool? IsActive { get; set; }
}