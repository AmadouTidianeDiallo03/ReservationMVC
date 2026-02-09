using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReservationMVC.Data;
using ReservationMVC.Models;

namespace ReservationMVC.Controllers
{
    [Authorize]
    public class InvoicesController : Controller
    {
        private readonly ApplicationDbContext _db;
        public InvoicesController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Details(int id)
        {
            var invoice = await _db.Invoices
                .Include(i => i.Order)
                    .ThenInclude(o => o.Lines)
                        .ThenInclude(l => l.Offer)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice is null) return NotFound();

            var buyer = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == invoice.BuyerUserId);

            var vm = new InvoiceDetailsVm
            {
                Invoice = invoice,
                Order = invoice.Order,
                Lines = invoice.Order?.Lines?.ToList() ?? new List<OrderLine>(),
                BuyerDisplay = buyer?.FullName ?? buyer?.Email ?? invoice.BuyerUserId
            };

            return View(vm);
        }

        public async Task<IActionResult> DownloadPdf(int id)
        {
            var invoice = await _db.Invoices
                .Include(i => i.Order)
                    .ThenInclude(o => o.Lines)
                        .ThenInclude(l => l.Offer)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice is null) return NotFound();

            var buyer = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == invoice.BuyerUserId);

            var buyerDisplay = buyer?.FullName ?? buyer?.Email ?? invoice.BuyerUserId;

            using var ms = new MemoryStream();

            var doc = new Document(PageSize.A4, 40, 40, 40, 40);
            PdfWriter.GetInstance(doc, ms);
            doc.Open();

            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
            var subFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
            var bold = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);

            doc.Add(new Paragraph("Facture", titleFont) { Alignment = Element.ALIGN_CENTER });
            doc.Add(new Paragraph($"Numéro : {invoice.Number}", subFont));
            doc.Add(new Paragraph($"Date d’émission : {invoice.IssuedUtc.ToLocalTime():dd/MM/yyyy HH:mm}", subFont));
            doc.Add(new Paragraph($"Client : {buyerDisplay}", subFont));
            doc.Add(new Paragraph(" "));

            doc.Add(new Paragraph("Détails de la commande :", bold));
            doc.Add(new Paragraph(" "));

            var table = new PdfPTable(3) { WidthPercentage = 100f };
            table.AddCell("Offre");
            table.AddCell("Qté");
            table.AddCell("Prix");

            foreach (var line in invoice.Order?.Lines ?? new List<OrderLine>())
            {
                table.AddCell(line.Offer?.Title ?? "-");
                table.AddCell(line.Quantity.ToString());
                table.AddCell($"{line.UnitPrice:C}");
            }

            doc.Add(table);
            doc.Add(new Paragraph(" "));
            doc.Add(new Paragraph($"Montant total : {invoice.Amount:C}", bold));
            doc.Add(new Paragraph(" "));
            doc.Add(new Paragraph("Merci pour votre confiance !", subFont));

            doc.Close();

            return File(ms.ToArray(), "application/pdf", $"Facture_{invoice.Number}.pdf");
        }
    }
}
