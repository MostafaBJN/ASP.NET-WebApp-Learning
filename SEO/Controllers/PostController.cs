using Microsoft.AspNetCore.Mvc;

namespace SEO.Controllers
{
    using System;
    using SEO.Logic;
    using SEO.Models;

    public class PostController : Controller
    {
        // GET: Post/Edit/5
        public ActionResult Edit(int id = 0)
        {
            // Load from DB; here example with placeholder
            Post model;
            if (id == 0)
            {
                model = new Post { Id = 0, Title = "New", EditorDataJson = null };
            }
            else
            {
                // Replace with real DB fetch
                model = /* fetch post by id */ null;
            }

            return View(model);
        }


        public List<Post> savedList = new();
        // POST: Post/Save
        [HttpPost]
        //[ValidateInput(false)]
        public JsonResult Save(int id, string title, string editorJson)
        {
            try
            {
                // Validate editorJson if desired; store to DB
                Post post;
                if (id == 0)
                {
                    post = new Post { Title = title, EditorDataJson = editorJson };
                    // insert into DB
                    savedList.Add(post);
                }
                else
                {

                    // fetch existing
                    post = savedList.Find(p => p.Id == id);
                    post.Title = title;
                    post.EditorDataJson = editorJson;
                    // update DB
                }

                // Return success
                return Json(new { success = true, message = "Saved" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Optional: convert HTML and return EditorJS JSON
        [HttpPost]
        //[ValidateInput(false)]
        public JsonResult ConvertHtmlToEditorJson(string html)
        {
            var converter = new HtmlToEditorJsConverter();
            var editorJson = converter.Convert(html);
            return Json(new { success = true, editorJson });
        }
    }
}
