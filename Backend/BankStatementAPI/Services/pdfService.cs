using BankStatementAPI.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using UglyToad.PdfPig;

namespace BankStatementAPI.Services
{
    public class PdfService
    {
        public PdfService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // Calculates pages based on transaction count
        public int CalculateNumberOfPages(Statement statement)
        {
            const int transactionsPerPage = 30;
            int pages = (int)Math.Ceiling(
                (double)statement.Transactions.Count / transactionsPerPage
            );
            return Math.Max(1, pages);
        }

        public byte[] GenerateStatement(
            Statement statement,
            ChargingResult chargingResult)
        {
            bool hasPostalAddress = !string.IsNullOrWhiteSpace(statement.PostalAddress);

            string postalLine = hasPostalAddress
                ? statement.PostalAddress
                : "No postal address available";

            string? streetLine = hasPostalAddress
                ? (string.IsNullOrWhiteSpace(statement.StreetAddress)
                    ? (string.IsNullOrWhiteSpace(statement.ResidentialAddress)
                        ? null
                        : statement.ResidentialAddress)
                    : statement.StreetAddress)
                : null;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily(Fonts.TimesNewRoman));

                    // ── HEADER ──
                    page.Header().Column(col =>
                    {
                        col.Item().Column(c =>
                        {
                            c.Item().Text(statement.AccountName).Bold();
                            c.Item().Text(postalLine);

                            if (!string.IsNullOrWhiteSpace(streetLine))
                                c.Item().Text(streetLine);
                        });

                        col.Item().PaddingTop(8).Row(row =>
                        {
                            row.RelativeItem().Text($"Branch: {statement.Branch}");
                            row.RelativeItem().AlignCenter()
                                .Text($"Account Type: {statement.AccountType}");
                            row.RelativeItem().AlignRight()
                                .Text($"Account No: {statement.AccountNumber}");
                        });

                        col.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Text($"Printed On: {DateTime.Now:dd MMM yyyy}");
                            row.RelativeItem().AlignCenter()
                                .Text($"From: {statement.StartDate:dd MMM yyyy} To: {statement.EndDate:dd MMM yyyy}")
                                .Bold();
                            row.RelativeItem().AlignRight().Text("CCY: GHANA CEDIS");
                        });

                        col.Item().PaddingTop(8).BorderBottom(1);
                    });

                    // ── FOOTER ──
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                    // ── CONTENT ──
                    page.Content().Column(col =>
                    {
                        col.Item().PaddingTop(10);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2); // Booking Date
                                columns.RelativeColumn(4); // Narrative
                                columns.RelativeColumn(2); // Value Date
                                columns.RelativeColumn(2); // Debit
                                columns.RelativeColumn(2); // Credit
                                columns.RelativeColumn(2); // Balance
                            });

                            // Table header row
                            table.Header(header =>
                            {
                                header.Cell().BorderTop(1).BorderBottom(1).PaddingVertical(4)
                                    .Text("Booking Date").Bold();
                                header.Cell().BorderTop(1).BorderBottom(1).PaddingVertical(4)
                                    .Text("Narrative").Bold();
                                header.Cell().BorderTop(1).BorderBottom(1).PaddingVertical(4)
                                    .Text("Value Date").Bold();
                                header.Cell().BorderTop(1).BorderBottom(1).PaddingVertical(4).AlignRight()
                                    .Text("Debit").Bold();
                                header.Cell().BorderTop(1).BorderBottom(1).PaddingVertical(4).AlignRight()
                                    .Text("Credit").Bold();
                                header.Cell().BorderTop(1).BorderBottom(1).PaddingVertical(4).AlignRight()
                                    .Text("Balance").Bold();
                            });

                            // Opening balance row
                            table.Cell().ColumnSpan(5).PaddingVertical(3)
                                .Text("Balance Brought Forward:");
                            table.Cell().PaddingVertical(3).AlignRight()
                                .Text($"{statement.OpeningBalance:N2}");

                            // Transaction rows — using updated field names
                            foreach (var t in statement.Transactions)
                            {
                                table.Cell().PaddingVertical(3).Text(
                                    t.BookingDate        // ← updated from Date
                                    .ToString("dd MMM yyyy")
                                    .ToUpper()
                                );
                                table.Cell().PaddingVertical(3)
                                    .Text(t.Narrative);  // ← updated from Description
                                table.Cell().PaddingVertical(3).Text(
                                    t.ValueDate          // ← new field
                                    .ToString("dd MMM yyyy")
                                    .ToUpper()
                                );
                                table.Cell().PaddingVertical(3).AlignRight()
                                    .Text(t.Debit > 0 ? $"{t.Debit:N2}" : "");
                                table.Cell().PaddingVertical(3).AlignRight()
                                    .Text(t.Credit > 0 ? $"{t.Credit:N2}" : "");
                                table.Cell().PaddingVertical(3).AlignRight()
                                    .Text($"{t.Balance:N2}");
                            }
                        });

                        // Summary totals — using new Statement fields
                        col.Item().PaddingTop(10).Column(summary =>
                        {
                            summary.Item()
                                .Text($"Book Balance: {statement.BookBalance:N2}");
                            summary.Item()
                                .Text($"Clear Balance: {statement.ClearBalance:N2}");
                            summary.Item()
                                .Text($"Total Debit Value: {statement.TotalDebitValue:N2}");
                            summary.Item()
                                .Text($"Total Credit Value: {statement.TotalCreditValue:N2}");
                            summary.Item()
                                .Text($"Total Debit Number: {statement.TotalDebitCount}");
                            summary.Item()
                                .Text($"Total Credit Number: {statement.TotalCreditCount}");
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }

        // Counts pages from the rendered PDF bytes to match actual layout pagination.
        public int CountPages(byte[] pdfBytes)
        {
            using var stream = new MemoryStream(pdfBytes);
            using var document = UglyToad.PdfPig.PdfDocument.Open(stream);
            return document.NumberOfPages;
        }
    }
}