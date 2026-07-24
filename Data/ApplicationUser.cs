using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace AeroResponse.Data;


public class ApplicationUser : IdentityUser
{

    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;


    [MaxLength(100)]
    public string Surname { get; set; } = string.Empty;

 
    [MaxLength(100)]
    public string? ReferenceCode { get; set; }

    public string FullName =>
        string.Join(
            " ",
            new[] { FirstName, Surname }
                .Where(value =>
                    !string.IsNullOrWhiteSpace(value)));
}