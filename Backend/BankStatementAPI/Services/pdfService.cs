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

        public (byte[] PdfBytes, int PageCount) GenerateStatement(
            Statement statement,
            ChargingResult chargingResult)
        {
            const float letterheadReservedHeight = 95f;
            const float transactionRowPadding = 7f;

            string postalLine = !string.IsNullOrWhiteSpace(statement.PostalAddress)
                ? statement.PostalAddress
                : "No postal address available";

            string? streetLine = !string.IsNullOrWhiteSpace(statement.StreetAddress)
                ? statement.StreetAddress
                : null;

            string? residentialLine = !string.IsNullOrWhiteSpace(statement.ResidentialAddress)
                ? statement.ResidentialAddress
                : null;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(8.30f).FontFamily(Fonts.Verdana));

                    // ── HEADER ──
                    page.Header().Column(col =>
                    {
                        col.Item().Height(letterheadReservedHeight);

                        col.Item().Column(c =>
                        {
                            c.Item().Text(statement.AccountName).Bold();
                            c.Item().Text(postalLine);

                            if (streetLine != null)
                                c.Item().Text(streetLine);

                            if (residentialLine != null)
                                c.Item().Text(residentialLine);
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
                                columns.RelativeColumn(2);      // Booking Date
                                columns.RelativeColumn(5.4f);   // Narrative
                                columns.RelativeColumn(2);      // Value Date
                                columns.RelativeColumn(2.16f);  // Debit
                                columns.RelativeColumn(2.16f);  // Credit
                                columns.RelativeColumn(2.216f); // Balance
                            });

                            // Table header row
                            table.Header(header =>
                            {
                                header.Cell().BorderTop(1).BorderBottom(1).PaddingVertical(5)
                                    .Text("Booking Date").Bold();
                                header.Cell().BorderTop(1).BorderBottom(1).PaddingVertical(5).PaddingRight(10)
                                    .Text("Narrative").Bold();
                                header.Cell().BorderTop(1).BorderBottom(1).PaddingVertical(5).PaddingLeft(8)
                                    .Text("Value Date").Bold();
                                header.Cell().BorderTop(1).BorderBottom(1).PaddingVertical(5).AlignCenter()
                                    .Text("Debit").Bold();
                                header.Cell().BorderTop(1).BorderBottom(1).PaddingVertical(5).AlignLeft()
                                    .Text("Credit").Bold();
                                header.Cell().BorderTop(1).BorderBottom(1).PaddingVertical(5).AlignRight()
                                    .Text("Balance").Bold();
                            });

                            // Opening balance row
                            table.Cell().ColumnSpan(5).PaddingVertical(5)
                                .Text("Balance Brought Forward:");
                            table.Cell().PaddingVertical(5).AlignRight()
                                .Text($"{statement.OpeningBalance:N2}");

                            // Transaction rows
                            foreach (var t in statement.Transactions)
                            {
                                table.Cell().PaddingVertical(transactionRowPadding)
                                    .Text(t.BookingDate.ToString("dd MMM yyyy").ToUpper());
                                table.Cell().PaddingVertical(transactionRowPadding).PaddingRight(10)
                                    .Text(string.IsNullOrWhiteSpace(t.Narrative) ? "-" : TrimTrailingFullStop(t.Narrative))
                                    .LineHeight(1.2f);
                                table.Cell().PaddingVertical(transactionRowPadding).PaddingLeft(8)
                                    .Text(t.ValueDate.ToString("dd MMM yyyy").ToUpper());
                                table.Cell().PaddingVertical(transactionRowPadding).AlignCenter()
                                    .Text(t.Debit > 0 ? $"{t.Debit:N2}" : "");
                                table.Cell().PaddingVertical(transactionRowPadding).AlignLeft()
                                    .Text(t.Credit > 0 ? $"{t.Credit:N2}" : "");
                                table.Cell().PaddingVertical(transactionRowPadding).AlignRight()
                                    .Text($"{t.Balance:N2}");
                            }
                        });

                        // Summary totals
                        col.Item().PaddingTop(10).Column(summary =>
                        {
                            summary.Item().Text($"Book Balance: {statement.BookBalance:N2}");
                            summary.Item().Text($"Clear Balance: {statement.ClearBalance:N2}");
                            summary.Item().Text($"Total Debit Value: {statement.TotalDebitValue:N2}");
                            summary.Item().Text($"Total Credit Value: {statement.TotalCreditValue:N2}");
                            summary.Item().Text($"Total Debit Number: {statement.TotalDebitCount}");
                            summary.Item().Text($"Total Credit Number: {statement.TotalCreditCount}");
                        });
                    });
                });
            });

            byte[] pdfBytes = document.GeneratePdf();
            int pageCount = CountPages(pdfBytes);

            return (pdfBytes, pageCount);
        }

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