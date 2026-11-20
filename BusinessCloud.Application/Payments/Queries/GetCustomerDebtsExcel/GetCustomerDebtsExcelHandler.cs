using BusinessCloud.Application.Common.Interfaces;
using BusinessCloud.Application.Payments.Queries.GetCustomerDebts;
using BusinessCloud.Domain.Payments.Entities;
using ClosedXML.Excel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessCloud.Application.Payments.Queries.GetCustomerDebtsExcel;

public sealed class GetCustomerDebtsExcelHandler(IPaymentsDbContext db)
    : IRequestHandler<GetCustomerDebtsExcelQuery, CustomerDebtsExcelResult>
{
    public async Task<CustomerDebtsExcelResult> Handle(
        GetCustomerDebtsExcelQuery request, CancellationToken cancellationToken)
    {
        var sales = await ActiveDebtSales.Query(db)
            .Include(s => s.Customer).ThenInclude(c => c.Seller)
            .Include(s => s.Seller)
            .Include(s => s.Payment.Where(p => p.PaymentTypeId == 2))
            .AsSingleQuery()
            .OrderBy(s => s.Customer.Name).ThenBy(s => s.Customer.LastName)
            .ThenBy(s => s.CustomerId).ThenBy(s => s.Date).ThenBy(s => s.Id)
            .ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var summary = workbook.Worksheets.Add("Deudas activas");
        var purchases = workbook.Worksheets.Add("Compras activas");
        var payments = workbook.Worksheets.Add("Abonos");
        WriteHeaders(summary, ["Cliente ID", "Nombre", "Vendedor", "Monto total", "Total abonado",
            "Deuda activa", "Ultimo abono", "Fecha ultimo abono", "Compras activas"]);
        WriteHeaders(purchases, ["Compra ID", "Cliente ID", "Nombre", "Vendedor de la compra", "Fecha compra",
            "Descripcion", "Monto total", "Total abonado", "Saldo pendiente", "Ultimo abono", "Fecha ultimo abono"]);
        WriteHeaders(payments, ["Abono ID", "Compra ID", "Cliente ID", "Nombre", "Vendedor de la compra",
            "Monto", "Fecha abono", "Metodo de pago", "Referencia"]);

        var row = 2;
        foreach (var group in sales.GroupBy(s => s.CustomerId))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var customer = group.First().Customer;
            var last = LastPayment(group.SelectMany(s => s.Payment));
            summary.Cell(row, 1).Value = customer.Id;
            summary.Cell(row, 2).Value = CustomerName(customer);
            summary.Cell(row, 3).Value = SellerName(customer.Seller);
            summary.Cell(row, 4).Value = group.Sum(s => s.TotalAmount);
            summary.Cell(row, 5).Value = group.Sum(s => s.Payment.Sum(p => p.Amount));
            summary.Cell(row, 6).Value = group.Sum(s => s.TotalAmount - s.Payment.Sum(p => p.Amount));
            if (last is not null)
            {
                summary.Cell(row, 7).Value = last.Amount;
                summary.Cell(row, 8).Value = last.PaymentDate;
            }
            summary.Cell(row, 9).Value = group.Count();
            row++;
        }

        row = 2;
        var paymentRow = 2;
        foreach (var sale in sales)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var last = LastPayment(sale.Payment);
            var paid = sale.Payment.Sum(p => p.Amount);
            purchases.Cell(row, 1).Value = sale.Id;
            purchases.Cell(row, 2).Value = sale.CustomerId;
            purchases.Cell(row, 3).Value = CustomerName(sale.Customer);
            purchases.Cell(row, 4).Value = SellerName(sale.Seller);
            purchases.Cell(row, 5).Value = sale.Date;
            purchases.Cell(row, 6).Value = sale.ProductDescription;
            purchases.Cell(row, 7).Value = sale.TotalAmount;
            purchases.Cell(row, 8).Value = paid;
            purchases.Cell(row, 9).Value = sale.TotalAmount - paid;
            if (last is not null)
            {
                purchases.Cell(row, 10).Value = last.Amount;
                purchases.Cell(row, 11).Value = last.PaymentDate;
            }
            foreach (var payment in sale.Payment.OrderBy(p => p.PaymentDate).ThenBy(p => p.Id))
            {
                payments.Cell(paymentRow, 1).Value = payment.Id;
                payments.Cell(paymentRow, 2).Value = sale.Id;
                payments.Cell(paymentRow, 3).Value = sale.CustomerId;
                payments.Cell(paymentRow, 4).Value = CustomerName(sale.Customer);
                payments.Cell(paymentRow, 5).Value = SellerName(sale.Seller);
                payments.Cell(paymentRow, 6).Value = payment.Amount;
                payments.Cell(paymentRow, 7).Value = payment.PaymentDate;
                payments.Cell(paymentRow, 8).Value = payment.PaymentMethod switch
                {
                    "Cash" => "Efectivo",
                    "Card" => "Tarjeta",
                    "Transfer" => "Transferencia",
                    _ => payment.PaymentMethod
                };
                payments.Cell(paymentRow, 9).Value = payment.Reference;
                paymentRow++;
            }
            row++;
        }

        summary.Columns(4, 7).Style.NumberFormat.Format = "$#,##0.00";
        summary.Column(8).Style.DateFormat.Format = "dd/MM/yyyy";
        purchases.Columns(7, 10).Style.NumberFormat.Format = "$#,##0.00";
        purchases.Column(5).Style.DateFormat.Format = "dd/MM/yyyy";
        purchases.Column(11).Style.DateFormat.Format = "dd/MM/yyyy";
        payments.Column(6).Style.NumberFormat.Format = "$#,##0.00";
        payments.Column(7).Style.DateFormat.Format = "dd/MM/yyyy";
        foreach (var sheet in workbook.Worksheets)
        {
            sheet.SheetView.FreezeRows(1);
            sheet.RangeUsed()!.SetAutoFilter();
            sheet.ColumnsUsed().AdjustToContents(1, Math.Min(sheet.LastRowUsed()!.RowNumber(), 200));
            foreach (var column in sheet.ColumnsUsed())
                column.Width = Math.Clamp(column.Width, 12, 60);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return new CustomerDebtsExcelResult(stream.ToArray(), $"DeudasActivas_{DateTime.UtcNow:yyyyMMdd}.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    private static Payment? LastPayment(IEnumerable<Payment> payments) =>
        payments.OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.Date).ThenByDescending(p => p.Id).FirstOrDefault();

    private static string CustomerName(Customer customer) => $"{customer.Name} {customer.LastName}".Trim();
    private static string SellerName(Seller? seller) => seller is null ? "Sin asignar" : $"{seller.Name} {seller.LastName}".Trim();

    private static void WriteHeaders(IXLWorksheet sheet, string[] headers)
    {
        for (var index = 0; index < headers.Length; index++)
            sheet.Cell(1, index + 1).Value = headers[index];
        sheet.Row(1).Style.Font.Bold = true;
        sheet.Row(1).Style.Fill.BackgroundColor = XLColor.LightBlue;
    }
}
