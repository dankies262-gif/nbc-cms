using System.Globalization;
using NbcCms.Web.Data.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NbcCms.Web.Services;

public class PdfReportService(IWebHostEnvironment environment)
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;
    private readonly string? logoPath = ResolveLogoPath(environment);

    private static string? ResolveLogoPath(IWebHostEnvironment environment)
    {
        var root = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var path = Path.Combine(root, "img", "nbc-logo.png");

        return File.Exists(path) ? path : null;
    }

    public byte[] Build(Claim claim) =>
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

    private static void ComposeForm(IContainer container, Claim claim, string? logoPath)
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
        Claim claim,
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

}
