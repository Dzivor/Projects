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
            // Required by QuestPDF for non-commercial use
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GenerateStatement(
            Statement statement,
            ChargingResult chargingResult)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // ── HEADER ──
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text("UMB BANK")
                                .FontSize(20)
                                .Bold()
                                .FontColor("#E6A817");

                            row.ConstantItem(150).AlignRight()
                                .Text($"Statement of Account")
                                .FontSize(12)
                                .Bold();
                        });

                        col.Item().BorderBottom(1).BorderColor("#E6A817").PaddingBottom(5);

                        // Account details
                        col.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Account Name: {statement.AccountName}").Bold();
                                c.Item().Text($"Account Number: {statement.AccountNumber}");
                                c.Item().Text($"Channel: {statement.Channel}");
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Period: {statement.StartDate:dd/MM/yyyy} - {statement.EndDate:dd/MM/yyyy}");
                                c.Item().Text($"Opening Balance: GHS {statement.OpeningBalance:N2}");
                                c.Item().Text($"Closing Balance: GHS {statement.ClosingBalance:N2}");
                            });
                        });
                    });

                    // ── CONTENT — TRANSACTIONS TABLE ──
                    page.Content().PaddingTop(20).Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            // Define columns
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);   // Date
                                columns.RelativeColumn(4);   // Description
                                columns.RelativeColumn(2);   // Debit
                                columns.RelativeColumn(2);   // Credit
                                columns.RelativeColumn(2);   // Balance
                            });

                            // Table header
                            table.Header(header =>
                            {
                                header.Cell().Background("#E6A817")
                                    .Padding(5).Text("Date").Bold();
                                header.Cell().Background("#E6A817")
                                    .Padding(5).Text("Description").Bold();
                                header.Cell().Background("#E6A817")
                                    .Padding(5).Text("Debit").Bold();
                                header.Cell().Background("#E6A817")
                                    .Padding(5).Text("Credit").Bold();
                                header.Cell().Background("#E6A817")
                                    .Padding(5).Text("Balance").Bold();
                            });

                            // Transaction rows
                            foreach (var transaction in statement.Transactions)
                            {
                                table.Cell().Padding(5)
                                    .Text(transaction.Date.ToString("dd/MM/yyyy"));
                                table.Cell().Padding(5)
                                    .Text(transaction.Narrative);
                                table.Cell().Padding(5)
                                    .Text(transaction.Debit > 0
                                        ? $"GHS {transaction.Debit:N2}" : "-");
                                table.Cell().Padding(5)
                                    .Text(transaction.Credit > 0
                                        ? $"GHS {transaction.Credit:N2}" : "-");
                                table.Cell().Padding(5)
                                    .Text($"GHS {transaction.Balance:N2}");
                            }
                        });
                    });

                    // ── FOOTER ──
                    page.Footer().Column(col =>
                    {
                        col.Item().BorderTop(1).BorderColor("#E6A817").PaddingTop(5);

                        // Charge information
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text(chargingResult.Message)
                                .FontSize(9)
                                .FontColor(
                                    chargingResult.Status == ChargeStatus.Failed
                                        ? "#FF0000" : "#333333"
                                );

                            // Page numbers
                            row.ConstantItem(100).AlignRight().Text(text =>
                            {
                                text.Span("Page ");
                                text.CurrentPageNumber();
                                text.Span(" of ");
                                text.TotalPages();
                            });
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }

        // Calculates how many pages the PDF will have
        // before actually generating it — used for charge preview
        public int CalculateNumberOfPages(Statement statement)
        {
            // Approximately 30 transactions fit per page
            const int transactionsPerPage = 30;
            int pages = (int)Math.Ceiling(
                (double)statement.Transactions.Count / transactionsPerPage
            );
            return Math.Max(1, pages);  // minimum 1 page
        }
    }
}