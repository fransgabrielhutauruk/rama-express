using Microsoft.AspNetCore.Mvc;
using RamaExpress.Models;

namespace RamaExpress.Controllers
{
    public class AboutController : Controller
    {
        public IActionResult Index()
        {
            var model = new AboutViewModel
            {
                CompanyName = "PT Rama Express",
                CompanyDescription = "PT Rama Express merupakan perusahaan jasa outsourcing yang berfokus di bidang promosi kegiatan pemasaran, penjualan, periklanan, yang didukung dengan tenaga kerja yang ahli dan berpengalaman dibidangnya sehingga mampu melakukan seluruh kegiatan pemasaran dan penjualan dengan hasil yang berkualitas.",
                CompanyVision = "Menjadi perusahaan outsourcing terdepan dan terpercaya di Indonesia",
                CompanyMission = "Memberikan solusi outsourcing terbaik dengan tenaga kerja profesional dan berkualitas",
                Services = new List<ServiceModel>
                {
                    new ServiceModel { ServiceName = "Resepsionist", Description = "Layanan resepsionis profesional", IconClass = "fas fa-user-tie" },
                    new ServiceModel { ServiceName = "Warehouse staff", Description = "Tenaga kerja gudang berpengalaman", IconClass = "fas fa-warehouse" },
                    new ServiceModel { ServiceName = "Admin", Description = "Staff administrasi yang kompeten", IconClass = "fas fa-file-alt" },
                    new ServiceModel { ServiceName = "Trading & Supplying", Description = "Layanan trading dan supply chain", IconClass = "fas fa-truck" },
                    new ServiceModel { ServiceName = "Driver", Description = "Driver profesional dan berpengalaman", IconClass = "fas fa-car" },
                    new ServiceModel { ServiceName = "Cleaning service", Description = "Layanan kebersihan terpercaya", IconClass = "fas fa-broom" },
                    new ServiceModel { ServiceName = "Salesman", Description = "Sales profesional dengan target oriented", IconClass = "fas fa-handshake" },
                    new ServiceModel { ServiceName = "Team promosi (SPG/SPB)", Description = "Tim promosi untuk event dan campaign", IconClass = "fas fa-bullhorn" },
                    new ServiceModel { ServiceName = "Merchandiser", Description = "Merchandiser untuk retail dan display", IconClass = "fas fa-shopping-cart" },
                    new ServiceModel { ServiceName = "Telemarketing", Description = "Layanan telemarketing profesional", IconClass = "fas fa-phone" }
                },
                Values = new List<CompanyValue>
                {
                    new CompanyValue
                    {
                        Title = "Profesionalisme",
                        Description = "Memberikan layanan terbaik dengan standar profesional yang tinggi",
                        IconClass = "fas fa-award"
                    },
                    new CompanyValue
                    {
                        Title = "Integritas",
                        Description = "Menjaga kepercayaan dan komitmen dalam setiap layanan yang diberikan",
                        IconClass = "fas fa-handshake"
                    },
                    new CompanyValue
                    {
                        Title = "Inovasi",
                        Description = "Selalu berinovasi untuk memberikan solusi terbaik bagi mitra bisnis",
                        IconClass = "fas fa-lightbulb"
                    },
                    new CompanyValue
                    {
                        Title = "Kualitas",
                        Description = "Mengutamakan kualitas dalam setiap hasil kerja dan layanan",
                        IconClass = "fas fa-star"
                    }
                }
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult SubmitContact(ContactFormModel model)
        {
            // Simulasi pengiriman berhasil (tanpa database)
            TempData["SuccessMessage"] = "Terima kasih! Pesan Anda telah berhasil dikirim. Tim kami akan segera menghubungi Anda.";
            return RedirectToAction("Index");
        }
    }
}