using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SEO.Data;
using SEO.Models;

namespace SEO.Controllers
{
    public class SEOTextsController : Controller
    {
        private readonly AppDbContext _db;

        public SEOTextsController(AppDbContext db)
        {
            _db = db;
        }

        // GET: SEOTexts
        public IActionResult Index()
        {
            return View(_db.SEOTexts.ToList());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var seoText = await _db.SEOTexts
                .FirstOrDefaultAsync(m => m.Id == id);
            if (seoText == null)
            {
                return NotFound();
            }

            return View(seoText);
        }

        // GET: SEOTexts/Create
        public IActionResult Create() => View();

        // POST: SEOTexts1/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(SEOText seoText)//[Bind("Id,Title,HtmlContent,BlockContent")] SEOText seoText
        {
            if (ModelState.IsValid)
            {
                _db.SEOTexts.Add(seoText);
                _db.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(seoText);
        }

        // GET: SEOTexts/Edit/id
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var seoText = await _db.SEOTexts.FindAsync(id);
            if (seoText == null)
            {
                return NotFound();
            }
            return View(seoText);
        }

        // POST: SEOTexts/Edit/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,HtmlContent,BlockContent")] SEOText seoText)//?? BIND
        {
            if (id != seoText.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _db.Update(seoText);
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SEOTextExists(seoText.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(seoText);
        }

        // GET: SEOTexts/Delete/id
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var seoText = await _db.SEOTexts
                .FirstOrDefaultAsync(m => m.Id == id);
            if (seoText == null)
            {
                return NotFound();
            }

            return View(seoText);
        }

        // POST: SEOTexts/Delete/id
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var seoText = await _db.SEOTexts.FindAsync(id);
            if (seoText != null)
            {
                _db.SEOTexts.Remove(seoText);
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SEOTextExists(int id)
        {
            return _db.SEOTexts.Any(e => e.Id == id);
        }
    }
}
