using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CWBSampleLibrary.Controllers
{
    public class Mp3SamplesController : Controller
    {
        // GET: Mp3Samples
        public ActionResult Index()
        {
            SamplesController s = new SamplesController();
            var samples = s.Get();
            


            return View(samples.ToList());
        }

        //// GET: Mp3Samples/Details/5
        //public ActionResult Details(int id)
        //{
        //    return View();
        //}

        //// GET: Mp3Samples/Create
        //public ActionResult Create()
        //{
        //    return View();
        //}

        //// POST: Mp3Samples/Create
        //[HttpPost]
        //public ActionResult Create(FormCollection collection)
        //{
        //    try
        //    {
        //        // TODO: Add insert logic here

        //        return RedirectToAction("Index");
        //    }
        //    catch
        //    {
        //        return View();
        //    }
        //}

        //// GET: Mp3Samples/Edit/5
        //public ActionResult Edit(int id)
        //{
        //    return View();
        //}

        //// POST: Mp3Samples/Edit/5
        //[HttpPost]
        //public ActionResult Edit(int id, FormCollection collection)
        //{
        //    try
        //    {
        //        // TODO: Add update logic here

        //        return RedirectToAction("Index");
        //    }
        //    catch
        //    {
        //        return View();
        //    }
        //}

        //// GET: Mp3Samples/Delete/5
        //public ActionResult Delete(int id)
        //{
        //    return View();
        //}

        //// POST: Mp3Samples/Delete/5
        //[HttpPost]
        //public ActionResult Delete(int id, FormCollection collection)
        //{
        //    try
        //    {
        //        // TODO: Add delete logic here

        //        return RedirectToAction("Index");
        //    }
        //    catch
        //    {
        //        return View();
        //    }
        //}
    }
}
