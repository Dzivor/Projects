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
            const float letterheadReservedHeight = 95f;
            const float transactionRowPadding = 5f;

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
                    page.DefaultTextStyle(x => x.FontSize(16.0f).FontFamily(Fonts.TimesRoman));

                    // ── HEADER ──
                    page.Header().Column(col =>
                    {
                        // Keep a clear area for the bank letterhead/logo.
                        col.Item().Height(letterheadReservedHeight);

                        col.Item().Column(c =>
                        {
                            c.Item().Text(statement.AccountName).Bold();
                            c.Item().Text(postalLine);
                            // If street line is available, show it below the postal line. Otherwise, show residential address if available
                            c.Item().Text(statement.ResidentialAddress);

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
                    page.Footer().AlignRight().Text(x =>
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
                                columns.RelativeColumn(5.5f); // Narrative (wraps earlier to keep clear gutter)
                                columns.RelativeColumn(2.5f); // Value Date
                                columns.RelativeColumn(2); // Debit
                                columns.RelativeColumn(2); // Credit
                                columns.RelativeColumn(2); // Balance
                            });

                            // Table header row
                            table.Header(header =>
                            {
                                header.Cell().BorderTop(1).BorderBottom(1).PaddingVertical(4)
                                    .Text("Booking Date").Bold();
                                header.Cell().BorderTop(1).BorderBottom(1).PaddingVertical(4).PaddingRight(10)
                                    .Text("Narrative").Bold();
                                header.Cell().BorderTop(1).BorderBottom(1).PaddingVertical(4).PaddingLeft(8)
                                    .Text("Value Date").Bold();
                                header.Cell().BorderTop(1).BorderBottom(1).PaddingVertical(4).AlignRight()
                                    .Text("Debit").Bold();
                                header.Cell().BorderTop(1).BorderBottom(1).PaddingVertical(4).AlignRight()
                                    .Text("Credit").Bold();
                                header.Cell().BorderTop(1).BorderBottom(1).PaddingVertical(4).AlignRight()
                                    .Text("Balance").Bold();
                            });

                            // Opening balance row
                            table.Cell().ColumnSpan(5).PaddingVertical(5)
                                .Text("Balance Brought Forward:");
                            table.Cell().PaddingVertical(5).AlignRight()
                                .Text($"{statement.OpeningBalance:N2}");

                            // Transaction rows — using updated field names
                            foreach (var t in statement.Transactions)
                            {
                                table.Cell().PaddingVertical(transactionRowPadding).Text(
                                    t.BookingDate        // ← updated from Date
                                    .ToString("dd MMM yyyy")
                                    .ToUpper()
                                );
                                table.Cell().PaddingVertical(transactionRowPadding).PaddingRight(10)
                                    .Text(string.IsNullOrWhiteSpace(t.Narrative) ? "-" : TrimTrailingFullStop(t.Narrative))
                                    .LineHeight(1.2f);  // Wrap long narration over multiple lines
                                table.Cell().PaddingVertical(transactionRowPadding).PaddingLeft(8).Text(
                                    t.ValueDate          // ← new field
                                    .ToString("dd MMM yyyy")
                                    .ToUpper()
                                );
                                table.Cell().PaddingVertical(transactionRowPadding).AlignRight()
                                    .Text(t.Debit > 0 ? $"{t.Debit:N2}" : "");
                                table.Cell().PaddingVertical(transactionRowPadding).AlignRight()
                                    .Text(t.Credit > 0 ? $"{t.Credit:N2}" : "");
                                table.Cell().PaddingVertical(transactionRowPadding).AlignRight()
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

        private static string TrimTrailingFullStop(string value)
        {
            return value.TrimEnd().TrimEnd('.').TrimEnd();
        }
    }
}