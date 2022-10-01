//#define TESTING

using LegoAppToolsLib;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace LegoAppToolsWebApp.Controllers
{
    using LegoAppCodeListing = List<string>;
    using LegoAppStatsList = Dictionary<string, string>;

    /// <summary>
    /// Simple service to fix corrupt SPIKE/Robot Inventor/EV3 Classroom files
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ProcessFileController : ControllerBase
    {
        private readonly ILogger<ProcessFileController> _logger;
#if TESTING
        private readonly IWebHostEnvironment _webHostEnvironment;
#endif
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="logger"></param>
        public ProcessFileController(ILogger<ProcessFileController> logger, IWebHostEnvironment webHostEnvironment)
        {
#if TESTING
            _webHostEnvironment = webHostEnvironment;
#endif
            _logger = logger;
        }

        [HttpPost]
        public IActionResult ProcessFile([FromForm] IFormFile file, [FromForm] string selectedpart, [FromForm] string selectedtab)
        {
#if TESTING
#else
            //-- exit if there are no files
            if (file == null || file.Length == 0)
                return Content("file not selected");
#endif

            switch (selectedtab)
            {
                case "repair":
                    return RepairFile(file, selectedpart);
                case "screenshot":
                    return ScreenshotFile(file);
                case "preview":
                    return Preview(file);
                case "machinelearning_preview":
                    return MachineLearningPreview(file);
            }
            return BadRequest();
        }

        /// <summary>
        /// Preview Machine learning samples
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public IActionResult MachineLearningPreview(IFormFile file)
        {
            try
            {
                Stream stream1 = file?.OpenReadStream();
#if TESTING
                string webRootPath = _webHostEnvironment.WebRootPath;
                string contentRootPath = _webHostEnvironment.ContentRootPath;
                var filename = @"sample\MLearning1.lms";
                var afilename = Path.Combine(contentRootPath, filename);
                stream1 = System.IO.File.OpenRead(afilename);
#endif

                //dynamic eo = new ExpandoObject();
                JObject result = new JObject();
                var mlresults = LegoAppTools.GetFileMachineLearningImages(stream1);
                foreach (var kvp in mlresults)
                {
                    //eo[kvp.Key] = kvp.Value.Count.ToString();

                    var joa2 = new JArray();
                    kvp.Value.ForEach(mlitem =>
                    {
                        var jo2 = new JObject();
                        jo2["filename"] = mlitem.Filename;
                        jo2["size"] = mlitem.Size;
                        MemoryStream ms = new MemoryStream(); mlitem.Stream.Position = 0; mlitem.Stream.CopyTo(ms);
                        jo2["image"] = Convert.ToBase64String(ms.ToArray());
                        joa2.Add(jo2);
                    });
                    result[kvp.Key] = joa2;
                }
                return Content(result.ToString());
            }
            catch (LegoAppToolException ex)
            {
                return ValidationProblem(ex.Message);
            }
        }


        /// <summary>
        /// Service to returna  compined preview json
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public IActionResult Preview(IFormFile file)
        {
            //-- repair the file
            try
            {
                JObject result = new JObject();
                Stream stream1 = file?.OpenReadStream();

#if TESTING
                string webRootPath = _webHostEnvironment.WebRootPath;
                string contentRootPath = _webHostEnvironment.ContentRootPath;
                var filename = @"sample\MLearning1.lms";
                var afilename = Path.Combine(contentRootPath, filename);
                stream1 = System.IO.File.OpenRead(afilename);
#endif

                stream1.Position = 0;
                (LegoAppCodeListing code, LegoAppStatsList stats) = LegoAppTools.GetFileContents(stream1);

                //-- add stats
                result.Add("stats", JObject.FromObject(stats));

                //-- add code
                result.Add("code", string.Join("\r\n", code.ToArray()));

                //-- add svg
                stream1.Position = 0;
                using Stream svgstream = LegoAppTools.GetSVGStreamFromLegoFileStream(stream1);
                using StreamReader sr = new StreamReader(svgstream);
                string svg = sr.ReadToEnd();
                result.Add("svg", svg);

                return Content(result.ToString());
            }
            catch (LegoAppToolException ex)
            {
                return ValidationProblem(ex.Message);
            }
        }

        /// <summary>
        /// Returns a png screenshot
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public IActionResult ScreenshotFile([FromForm] IFormFile file)
        {
            try
            {
                var so_result = LegoAppTools.GeneratePngCanvas(file.OpenReadStream(), file.FileName);

                if (so_result.stream != null)
                    return File(so_result.stream, "image/png", so_result.name);
                else
                    return BadRequest();
            }
            catch (LegoAppToolException ex)
            {
                return ValidationProblem(ex.Message);
            }
        }

        /// <summary>
        /// Upload a corrupted LEGO file and download the repaired file
        /// </summary>
        /// <param name="file">Corrupt LEGO file</param>
        /// <param name="selectedpart">Choose whether 'First' or 'Second' part will be restored</param>
        /// <returns>Repaired LEGO file</returns>
        /// <response code="400">If the LEGO file is already valid or cannot be repaired</response>
        public IActionResult RepairFile([FromForm] IFormFile file, [FromForm] string selectedpart)
        {
            //-- repair the file
            try
            {
                var so_result = LegoAppTools.RepairFile(file.OpenReadStream(), file.FileName, selectedpart == "first");

                if (so_result.stream != null)
                    return File(so_result.stream, "application/zip", so_result.name);
                else
                    return BadRequest();
            }
            catch (LegoAppToolException ex)
            {
                return ValidationProblem(ex.Message);
            }
        }
    }
}
