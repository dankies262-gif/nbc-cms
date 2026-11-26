using System.ComponentModel.DataAnnotations;

namespace NbcCms.Api.Models.Requests;

public class UpdateUserStatusRequest
{
    public bool IsActive { get; set; }
}
