using System.ComponentModel.DataAnnotations;

namespace NbcCms.Api.Models.Requests;

public class ArtistBankingDetailsRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Bank { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string Branch { get; set; } = string.Empty;

    [Required, StringLength(50, MinimumLength = 2)]
    public string BranchCode { get; set; } = string.Empty;
}
