
using BankStatementAPI.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

 namespace BankStatementAPI.Services{
    public class PdfService{
        public PdfService()
        {
            //Required by QuestPDF for non-commercial use
            QuestPDF.Settings.License = LicenseType.Community;
        }
public byte[] GenerateStatement(Statement statement, ChargingResult chargingResult)
{
    var document = Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(9));

            // ── FOOTER — account details at bottom like your sample ──
            page.Footer().Column(col =>
            {
                col.Item().BorderTop(1).PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text(statement.AccountName).Bold();
                        c.Item().Text("No postal address available");
                        c.Item().Text(statement.BranchAddress);
                    });
                });

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem()
                        .Text($"Branch: {statement.Branch}   " +
                              $"Account Type: {statement.AccountType}   " +
                              $"Account No: {statement.AccountNumber}");
                });

                col.Item().Row(row =>
                {
                    row.RelativeItem()
                        .Text($"Printed On: {DateTime.Now:dd MMM yyyy}   " +
                              $"From: {statement.StartDate:dd MMM yyyy}   " +
                              $"To: {statement.EndDate:dd MMM yyyy}   " +
                              $"CCY: GHANA CEDIS");
                });

                // Charge info
                col.Item().PaddingTop(5).Text(chargingResult.Message)
                    .FontColor(chargingResult.Status == ChargeStatus.Failed
                        ? "#FF0000" : "#333333");
            });

            // ── CONTENT ──
            page.Content().Column(col =>
            {
                // Transaction table
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);  // Booking Date
                        columns.RelativeColumn(4);  // Narrative
                        columns.RelativeColumn(2);  // Value Date
                        columns.RelativeColumn(2);  // Debit
                        columns.RelativeColumn(2);  // Credit
                        columns.RelativeColumn(2);  // Balance
                    });

                    // Header row
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
                    table.Cell().ColumnSpan(5).Text("Balance Brought Forward:");
                    table.Cell().AlignRight()
                        .Text($"{statement.OpeningBalance:N2}");

                    // Transaction rows
                    foreach (var t in statement.Transactions)
                    {
                        table.Cell().Text(t.BookingDate.ToString("dd MMM yyyy").ToUpper());
                        table.Cell().Text(t.Narrative);
                        table.Cell().Text(t.ValueDate.ToString("dd MMM yyyy").ToUpper());
                        table.Cell().AlignRight()
                            .Text(t.Debit > 0 ? $"{t.Debit:N2}" : "");
                        table.Cell().AlignRight()
                            .Text(t.Credit > 0 ? $"{t.Credit:N2}" : "");
                        table.Cell().AlignRight().Text($"{t.Balance:N2}");
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

    return document.GeneratePdf();
}
 }
 }