using Microsoft.AspNetCore.Mvc;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace LegoAppToolsWebApp.Controllers
{
    public class MultipartContent
    {
        public string ContentType { get; set; }

        public string FileName { get; set; }

        public Stream Stream { get; set; }
    }

    public class MultipartResult : Collection<MultipartContent>, IActionResult
    {
        private readonly System.Net.Http.MultipartContent content;

        public MultipartResult(string subtype = "byteranges", string boundary = null)
        {
            if (boundary == null)
            {
                this.content = new System.Net.Http.MultipartContent(subtype);
            }
            else
            {
                this.content = new System.Net.Http.MultipartContent(subtype, boundary);
            }
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            foreach (var item in this)
            {
                if (item.Stream != null)
                {
                    var content = new StreamContent(item.Stream);

                    if (item.ContentType != null)
                    {
                        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(item.ContentType);
                    }

                    if (item.FileName != null)
                    {
                        var contentDisposition = new ContentDispositionHeaderValue("attachment");
                        //!! contentDisposition.SetHttpFileName(item.FileName);
                        content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment");
                        content.Headers.ContentDisposition.FileName = contentDisposition.FileName;
                        content.Headers.ContentDisposition.FileNameStar = contentDisposition.FileNameStar;
                    }

                    this.content.Add(content);
                }
            }

            context.HttpContext.Response.ContentLength = content.Headers.ContentLength;
            context.HttpContext.Response.ContentType = content.Headers.ContentType.ToString();

            await content.CopyToAsync(context.HttpContext.Response.Body);
        }
    }
}
