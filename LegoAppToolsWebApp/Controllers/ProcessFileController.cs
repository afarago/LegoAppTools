using ICSharpCode.SharpZipLib.Zip;
using LegoAppToolsLib;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace LegoAppToolsWebApp.Controllers
{
    using LegoAppStatsList = Dictionary<string, string>;
    using LegoAppErrorList = List<string>;
    using LegoAppCodeListing = List<string>;

    /// <summary>
    /// Simple service to fix corrupt SPIKE/Robot Inventor/EV3 Classroom files
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ProcessFileController : ControllerBase
    {
        private readonly ILogger<ProcessFileController> _logger;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="logger"></param>
        public ProcessFileController(ILogger<ProcessFileController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public IActionResult ProcessFile([FromForm] IFormFile file, [FromForm] string selectedpart, [FromForm] string selectedtab)
        {
            //-- exit if there are no files
            if (file == null || file.Length == 0)
                return Content("file not selected");

            switch (selectedtab)
            {
                case "repair":
                    return RepairFile(file, selectedpart);
                case "screenshot":
                    return ScreenshotFile(file);
                case "preview":
                    return Preview(file);
            }
            return BadRequest();
        }

        public IActionResult Preview([FromForm] IFormFile file)
        {
            //-- repair the file
            try
            {
                JObject result = new JObject();

                (LegoAppCodeListing code, LegoAppStatsList stats) = LegoAppTools.GetFileContents(file.OpenReadStream());

                result.Add("stats", JObject.FromObject(stats));
                result.Add("code", string.Join("\r\n", code.ToArray()));

                using Stream svgstream = LegoAppTools.GetSVGStreamFromLegoFileStream(file.OpenReadStream());
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
