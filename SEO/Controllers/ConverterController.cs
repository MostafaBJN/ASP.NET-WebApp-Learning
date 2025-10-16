using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System;
using NuGet.Protocol;
using SEO.Models;
using System.Text;
using HtmlAgilityPack;

namespace SEO.Controllers
{
    public class ConverterController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Edit()
        {
            return View("converter");
        }


        [HttpPost]
        public IActionResult upload(AppFile model)
        {
            var f = model.UploadFile;

            string html = f.ReadAsString();
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            var htmlBody = htmlDoc.DocumentNode.SelectSingleNode("//body");

            Console.WriteLine(htmlBody.OuterHtml);
            System.IO.File.WriteAllText("SelectedHTML.html", html);


            return RedirectToAction("", "Convertor");
        }
  


    }
}
