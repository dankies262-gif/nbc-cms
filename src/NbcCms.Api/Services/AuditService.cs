using NbcCms.Api.Data;
using NbcCms.Api.Data.Entities;

namespace NbcCms.Api.Services;

public class AuditService(ApplicationDbContext db)
{
    public async Task RecordAsync(int? userId, string table, int? recordId, string action)
    {
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            TableName = table,
            RecordID = recordId,
            ActionType = action,
            ActionDate = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }
}
