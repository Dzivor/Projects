using BankStatementAPI.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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
            string postalLine = string.IsNullOrWhiteSpace(statement.PostalAddress)
                ? "No postal address available"
                : statement.PostalAddress;

            string streetLine = string.IsNullOrWhiteSpace(statement.StreetAddress)
                ? (string.IsNullOrWhiteSpace(statement.ResidentialAddress)
                    ? "No house address available"
                    : statement.ResidentialAddress)
                : statement.StreetAddress;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    // ── HEADER ──
                    page.Header().Column(col =>
                    {
                        col.Item().Column(c =>
                        {
                            c.Item().Text(statement.AccountName).Bold();
                            c.Item().Text(postalLine);
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
                    page.Footer().Column(col =>
                    {
                        col.Item().Text(chargingResult.Message)
                            .FontColor(
                                chargingResult.Status == ChargeStatus.Failed
                                    ? "#FF0000" : "#333333"
                            );
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
                                header.Cell().Text("Booking Date").Bold();
                                header.Cell().Text("Narrative").Bold();
                                header.Cell().Text("Value Date").Bold();
                                header.Cell().Text("Debit").Bold();
                                header.Cell().Text("Credit").Bold();
                                header.Cell().Text("Balance").Bold();
                            });

                            // Opening balance row
                            table.Cell().ColumnSpan(5)
                                .Text("Balance Brought Forward:");
                            table.Cell().AlignRight()
                                .Text($"{statement.OpeningBalance:N2}");

                            // Transaction rows — using updated field names
                            foreach (var t in statement.Transactions)
                            {
                                table.Cell().Text(
                                    t.BookingDate        // ← updated from Date
                                    .ToString("dd MMM yyyy")
                                    .ToUpper()
                                );
                                table.Cell().Text(t.Narrative);  // ← updated from Description
                                table.Cell().Text(
                                    t.ValueDate          // ← new field
                                    .ToString("dd MMM yyyy")
                                    .ToUpper()
                                );
                                table.Cell().AlignRight()
                                    .Text(t.Debit > 0 ? $"{t.Debit:N2}" : "");
                                table.Cell().AlignRight()
                                    .Text(t.Credit > 0 ? $"{t.Credit:N2}" : "");
                                table.Cell().AlignRight()
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
    }
}