using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Juno.Payload.Service.ActionResult
{
    internal class ContentResultWithHeader : ContentResult
    {
        private readonly IDictionary<string, string> _headers;

        public ContentResultWithHeader(IDictionary<string, string> headers) : base()
        {
            _headers = headers;
        }

        public override void ExecuteResult(ActionContext context)
        {
            base.ExecuteResult(context);

            if (_headers != null)
            {
                foreach (var header in _headers)
                {
                    context.HttpContext.Response.Headers.Add(header.Key, header.Value);
                }
            }
        }
    }
}
