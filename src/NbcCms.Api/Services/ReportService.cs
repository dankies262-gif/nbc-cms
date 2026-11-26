using System.Globalization;
using Microsoft.EntityFrameworkCore;
using NbcCms.Api.Data;
using NbcCms.Api.Data.Entities;
using NbcCms.Api.Models;
using NbcCms.Api.Models.Response;
using NbcCms.Api.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Claim = NbcCms.Api.Models.Response.Claim;
using ClaimEntity = NbcCms.Api.Data.Entities.Claim;
using ReportDto = NbcCms.Api.Models.Response.Report;
using ReportEntity = NbcCms.Api.Data.Entities.Report;

namespace NbcCms.Api.Services;

public class ReportService(
    ApplicationDbContext db,
    AuditService audit,
    IWebHostEnvironment environment) : IReportService
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;
    private readonly string? logoPath = ResolveLogoPath(environment);

    private static string? ResolveLogoPath(IWebHostEnvironment environment)
    {
        var root = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var path = Path.Combine(root, "img", "nbc-logo.png");

        return File.Exists(path) ? path : null;
    }

    public async Task<List<ReportDto>> ListAsync(int take = 100)
    {
        var reports = await db.Reports
            .Include(r => r.Claim)
            .Include(r => r.GeneratedByUser)
            .OrderByDescending(r => r.GeneratedAt)
            .Take(take)
            .ToListAsync();

        return reports.Select(ReportDto.FromEntity).ToList();
    }

    public async Task<List<Claim>> ReadyAsync()
    {
        var claims = await db.Claims
            .Include(c => c.Claimant)
            .Include(c => c.Status)
            .Include(c => c.Items)
            .Where(c => c.Status.StatusName == ClaimStatusNames.Accepted && c.Report == null)
            .OrderByDescending(c => c.ReviewedAt ?? c.UpdatedAt ?? c.CreatedAt)
            .ToListAsync();

        return claims.Select(ClaimMapping.ToResponse).ToList();
    }

    public async Task<ReportDto> GenerateAsync(int claimId, int userId, bool regenerate = false)
    {
        var claim = await db.Claims
            .Include(c => c.Claimant).ThenInclude(u => u.BankingDetail)
            .Include(c => c.Status)
            .Include(c => c.Reviewer)
            .Include(c => c.Items)
            .Include(c => c.Corrections).ThenInclude(x => x.RequestedByUser)
            .Include(c => c.Documents)
            .Include(c => c.Report)
            .FirstOrDefaultAsync(c => c.ClaimId == claimId)
            ?? throw ApiProblemException.NotFound("Claim not found.");

        if (claim.Status.StatusName != ClaimStatusNames.Accepted)
        {
            throw ApiProblemException.Conflict("A report can only be generated once the claim has been accepted.");
        }

        if (claim.Report is not null && !regenerate)
        {
            return ReportDto.FromEntity(claim.Report);
        }

        var reportNumber = $"NBC-CMS-{Windhoek(DateTime.UtcNow).Year}-{claim.ClaimId:D5}";
        var bytes = BuildPdf(claim, logoPath);

        if (claim.Report is { } existing)
        {
            existing.ReportNumber = reportNumber;
            existing.FileName = $"Claim-Report-{claim.Reference}.pdf";
            existing.GeneratedBy = userId;
            existing.GeneratedAt = DateTime.UtcNow;
            existing.FileData = bytes;
            await db.SaveChangesAsync();
            await audit.RecordAsync(userId, "report", existing.ReportId, "ReportRegenerate");

            existing.Claim = claim;
            return ReportDto.FromEntity(existing);
        }

        var report = new ReportEntity
        {
            ClaimId = claim.ClaimId,
            GeneratedBy = userId,
            ReportNumber = reportNumber,
            FileName = $"Claim-Report-{claim.Reference}.pdf",
            GeneratedAt = DateTime.UtcNow,
            FileData = bytes
        };

        db.Reports.Add(report);
        await db.SaveChangesAsync();

        report.FileURL = $"/api/reports/{report.ReportId}/download";
        await db.SaveChangesAsync();
        await audit.RecordAsync(userId, "report", report.ReportId, "Report");

        report.Claim = claim;
        return ReportDto.FromEntity(report);
    }

    public async Task<(byte[] Bytes, string FileName)> DownloadAsync(int reportId, int userId, bool isStaff)
    {
        var report = await db.Reports
            .Include(r => r.Claim)
            .FirstOrDefaultAsync(r => r.ReportId == reportId)
            ?? throw ApiProblemException.NotFound("Report not found.");

        if (!isStaff && report.Claim.ClaimantId != userId)
        {
            throw ApiProblemException.NotFound("Report not found.");
        }

        if (report.FileData is null || report.FileData.Length == 0)
        {
            throw ApiProblemException.Conflict("The report file is not available. Generate it again.");
        }

        return (report.FileData, report.FileName ?? $"{report.ReportNumber}.pdf");
    }

    private static byte[] BuildPdf(ClaimEntity claim, string? logoPath) =>
        Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(36);
                page.MarginVertical(30);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(style => style
                    .FontFamily("Lato")
                    .FontSize(8.5f)
                    .FontColor(Colors.Black));

                page.Content().Element(container => ComposeForm(container, claim, logoPath));
            });
        }).GeneratePdf();

    private static void ComposeForm(IContainer container, ClaimEntity claim, string? logoPath)
    {
        var claimant = claim.Claimant;
        var banking = claimant.BankingDetail;
        var items = claim.Items
            .OrderBy(i => i.RecordingDate)
            .ThenBy(i => i.ClaimItemId)
            .ToList();
        var rowCount = Math.Max(20, items.Count);
        var total = items.Sum(i => i.FeesAmount);

        container.Column(column =>
        {
            if (logoPath is not null)
            {
                column.Item().AlignCenter().Height(44).Image(logoPath);
            }

            column.Item().PaddingTop(logoPath is null ? 0 : 4)
                .AlignCenter().Text("ARTISTS' FEES OR ROYALTIES").FontSize(14).Bold();

            column.Item().PaddingTop(6).Text(text =>
            {
                text.Span("Req. No. ").SemiBold().FontSize(9);
                text.Span(claim.Reference).FontSize(9);
            });

            column.Item().PaddingTop(6).Element(header => ComposeHeaderFields(header, claimant, banking));

            column.Item().PaddingTop(6).Element(content => ComposeItemsTable(content, claim, items, rowCount, total));

            column.Item().PaddingTop(3).Row(row =>
            {
                row.RelativeItem();
                row.ConstantItem(100).AlignRight().Text("CASH TOTAL").Bold().FontSize(9);
                row.ConstantItem(80).AlignRight().Text(Money(total)).Bold().FontSize(9);
            });

            column.Item().PaddingTop(5)
                .Text("Certified that the above artists took part in the programmes mentioned and that the amounts are payable.")
                .FontSize(8.5f);

            column.Item().PaddingTop(8).Element(ComposeSignatures);

            column.Item().PaddingTop(10).Text("Processed By Human Resources").FontSize(8.5f);

            column.Item().PaddingTop(8).Width(240).Column(processed =>
            {
                processed.Item().Text("Date:").FontSize(8.5f);
                processed.Item().PaddingTop(12).LineHorizontal(0.8f).LineColor(Colors.Black);
            });
        });
    }

    private static void ComposeHeaderFields(
        IContainer container,
        User claimant,
        ArtistBankingDetail? banking)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(62);
                columns.RelativeColumn();
                columns.ConstantColumn(72);
                columns.RelativeColumn();
            });

            table.Cell().Element(FieldLabel).Text("Surname:");
            table.Cell().Element(FieldValue).Text(claimant.LastName ?? string.Empty);
            table.Cell().Element(FieldHeading).Text("Banking Details:");
            table.Cell().Text(string.Empty);

            FieldRow(table, "Full Names:", claimant.FirstName, "Bank:", banking?.Bank);
            FieldRow(table, "ID Number:", claimant.IdNumber, "Branch:", banking?.Branch);
            FieldRow(table, "Address:", claimant.Address, "Br. Code:", banking?.BranchCode);
            FieldRow(table, "Tel Number:", claimant.ContactNumber, "Signature:", null);
        });
    }

    private static void FieldRow(
        TableDescriptor table,
        string leftLabel,
        string? leftValue,
        string rightLabel,
        string? rightValue)
    {
        table.Cell().Element(FieldLabel).Text(leftLabel);
        table.Cell().Element(FieldValue).Text(leftValue ?? string.Empty);
        table.Cell().Element(FieldLabel).Text(rightLabel);
        table.Cell().Element(FieldValue).Text(rightValue ?? string.Empty);
    }

    private static IContainer FieldLabel(IContainer container) =>
        container.PaddingRight(4).PaddingBottom(3).MinHeight(16).AlignBottom();

    private static IContainer FieldHeading(IContainer container) =>
        FieldLabel(container).DefaultTextStyle(style => style.SemiBold());

    private static IContainer FieldValue(IContainer container) =>
        container.PaddingHorizontal(3).PaddingBottom(2).MinHeight(16)
            .BorderBottom(0.7f).BorderColor(Colors.Black).AlignBottom();

    private static void ComposeItemsTable(
        IContainer container,
        ClaimEntity claim,
        List<ClaimItem> items,
        int rowCount,
        decimal total)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(30);
                columns.ConstantColumn(46);
                columns.ConstantColumn(66);
                columns.RelativeColumn();
                columns.ConstantColumn(62);
                columns.ConstantColumn(62);
                columns.ConstantColumn(56);
                columns.ConstantColumn(48);
            });

            table.Header(header =>
            {
                header.Cell().Element(HeadCell).Text("ITEM");
                header.Cell().Element(HeadCell).Text("ARTIST No.");
                header.Cell().Element(HeadCell).Text("RECORDING DATE");
                header.Cell().Element(HeadCell).AlignLeft().Text("PROGRAMME TITLE");
                header.Cell().Element(HeadCell).Text("FEES/AMOUNT");
                header.Cell().Element(HeadCell).Text("ALLOCATION");
                header.Cell().Element(HeadCell).Text("CHEQUE NO.");
                header.Cell().Element(HeadCell).Text("DATE");
            });

            for (var index = 0; index < rowCount; index++)
            {
                var item = index < items.Count ? items[index] : null;

                table.Cell().Element(CenterCell).Text((index + 1).ToString(Culture));
                table.Cell().Element(CenterCell)
                    .Text(item is null ? string.Empty : claim.ClaimantId.ToString(Culture));
                table.Cell().Element(CenterCell)
                    .Text(item is null ? string.Empty : item.RecordingDate.ToString("dd/MM/yyyy", Culture));
                table.Cell().Element(LeftCell).Text(item?.ProgrammeTitle ?? string.Empty);
                table.Cell().Element(RightCell).Text(item is null ? string.Empty : Money(item.FeesAmount));
                table.Cell().Element(CenterCell).Text(string.Empty);
                table.Cell().Element(CenterCell).Text(string.Empty);
                table.Cell().Element(CenterCell).Text(string.Empty);
            }

            table.Cell().ColumnSpan(4).Element(TotalLabelCell).AlignRight().Text("CASH TOTAL");
            table.Cell().Element(TotalValueCell).AlignRight().Text(Money(total));
            table.Cell().ColumnSpan(3).Element(BodyCell).Text(string.Empty);
        });
    }

    private static void ComposeSignatures(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
            });

            SignatureField(table, "Supervisor:");
            SignatureField(table, "Manager: IT");
            SignatureField(table, "HOD: N/A");
            SignatureField(table, "CTO: Technology");

            DateField(table, "Date:");
            DateField(table, "Date:");
            DateField(table, "Date:");
            DateField(table, "Date:");
        });
    }

    private static void SignatureField(TableDescriptor table, string label)
    {
        table.Cell().PaddingRight(14).PaddingTop(12).Column(column =>
        {
            column.Item().Text(label).FontSize(8);
            column.Item().PaddingTop(14).LineHorizontal(0.8f).LineColor(Colors.Black);
        });
    }

    private static void DateField(TableDescriptor table, string label)
    {
        table.Cell().PaddingRight(14).PaddingTop(10).Column(column =>
        {
            column.Item().Text(label).FontSize(8);
            column.Item().PaddingTop(8).LineHorizontal(0.8f).LineColor(Colors.Black);
        });
    }

    private static IContainer HeadCell(IContainer container) =>
        container.Border(0.7f).BorderColor(Colors.Black)
            .PaddingVertical(4).PaddingHorizontal(2)
            .AlignCenter().AlignMiddle()
            .DefaultTextStyle(style => style.FontSize(7.5f).Bold());

    private static IContainer BodyCell(IContainer container) =>
        container.Border(0.7f).BorderColor(Colors.Black)
            .PaddingVertical(2.5f).PaddingHorizontal(3)
            .MinHeight(13).AlignMiddle();

    private static IContainer CenterCell(IContainer container) => BodyCell(container).AlignCenter();

    private static IContainer LeftCell(IContainer container) => BodyCell(container).AlignLeft();

    private static IContainer RightCell(IContainer container) => BodyCell(container).AlignRight();

    private static IContainer TotalLabelCell(IContainer container) =>
        container.Border(0.7f).BorderColor(Colors.Black)
            .PaddingVertical(3).PaddingHorizontal(3)
            .AlignMiddle()
            .DefaultTextStyle(style => style.Bold());

    private static IContainer TotalValueCell(IContainer container) =>
        TotalLabelCell(container).AlignRight();

    private static string Money(decimal amount) => amount.ToString("N2", Culture);

    private static DateTime Windhoek(DateTime value) =>
        TimeZoneInfo.ConvertTimeFromUtc(
            value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc),
            ResolveWindhoek());

    private static TimeZoneInfo ResolveWindhoek()
    {
        foreach (var id in new[] { "Africa/Windhoek", "Namibia Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone("CAT", TimeSpan.FromHours(2), "Central Africa Time", "CAT");
    }
}
