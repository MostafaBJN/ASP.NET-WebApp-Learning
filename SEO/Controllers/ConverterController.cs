using Microsoft.AspNetCore.Mvc;
using SEO.Logic;
using SEO.Models;

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
        public IActionResult Upload(AppFile model)
        {
            var f = model.UploadFile;

            if (f == null)
                return View("Converter");

            string html = f.ReadAsString();//Added Extention Methode

            SaveLoad.Save(html, "SelectedHTML.html");
            new ConvertHTML(html).Start();

            return View("Converter");//RedirectToAction("Edit", "Converter");
        }

        [HttpGet]
        public IActionResult Download(string filePath, string fileName)
        {
            string fullName = System.IO.Path.Combine(/*GetBaseDir(), */filePath, fileName);

            byte[] fileBytes = GetFile(fullName);
            //Response.ContentType = MimeMapping.GetMimeMapping(filePath);
            return File(
                fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);

            //https://www.c-sharpcorner.com/article/file-upload-and-download-using-asp-net-mvc-5-for-beginners/
            //https://learn.microsoft.com/en-us/previous-versions/aspnet/dd492897(v=vs.98)?redirectedfrom=MSDN
            //https://stackoverflow.com/questions/3604562/download-file-of-any-type-in-asp-net-mvc-using-fileresult
            //return File(virtualFilePath, System.Net.Mime.MediaTypeNames.Application.Octet, Path.GetFileName(virtualFilePath));

        }

        byte[] GetFile(string s)
        {
            byte[] data = System.IO.File.ReadAllBytes(s);

            //System.IO.FileStream fs = System.IO.File.OpenRead(s);
            //byte[] data = new byte[fs.Length];
            //int br = fs.Read(data, 0, data.Length);
            //if (br != fs.Length)
            //    throw new System.IO.IOException(s);
            return data;
        }


    }
}
