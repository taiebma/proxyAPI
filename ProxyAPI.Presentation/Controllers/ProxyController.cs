namespace ProxyAPI.Presentation.Controllers;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/proxy/")]
public class ProxyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ProxyController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    [HttpPost]
    [HttpPut]
    [HttpDelete]
    [HttpPatch]
    public async Task<IActionResult> ProxyRequest()
    {
        if (!HttpContext.Items.TryGetValue("ClientContext", out _))
            return Unauthorized(new { error = "Not authenticated" });

        string uri = Request.Query["uri"].ToString();

        if (string.IsNullOrWhiteSpace(uri))
            return BadRequest(new { error = "Missing 'uri' query parameter" });

        var upstreamUrl = $"{uri}";

        if (Request.QueryString.HasValue)
        {
            var queryItems = System.Web.HttpUtility.ParseQueryString(Request.QueryString.Value);
            queryItems.Remove("uri");
            
            if (queryItems.Count > 0)
            {
                string remainingQuery = queryItems.ToString(); // Génère automatiquement la chaîne formatée k1=v1&k2=v2
                if (!string.IsNullOrEmpty(remainingQuery))
                {
                    upstreamUrl += (upstreamUrl.Contains("?") ? "&" : "?") + remainingQuery;
                }
            }
        }

        var client = _httpClientFactory.CreateClient();

        try
        {
            var method = new HttpMethod(Request.Method);
            var request = new HttpRequestMessage(method, upstreamUrl);

            if (Request.ContentLength.HasValue && Request.ContentLength > 0)
            {
                /*
                var body = await new StreamReader(Request.Body).ReadToEndAsync();
                request.Content = new StringContent(body, System.Text.Encoding.UTF8, Request.ContentType ?? "application/json");
                */
                request.Content = new StreamContent(Request.Body);
                if (!string.IsNullOrEmpty(Request.ContentType))
                {
                    request.Content.Headers.TryAddWithoutValidation("Content-Type", Request.ContentType);
                }
            }

            CopyHeaders(request);

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            var result = new ContentResult
            {
                Content = content,
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json",
                StatusCode = (int)response.StatusCode
            };

            return result;
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Proxy request failed", details = ex.Message });
        }
    }

    private void CopyHeaders(HttpRequestMessage request)
    {
        foreach (var header in Request.Headers)
        {
            if (!header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) &&
                !header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase) &&
                !header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
            {
//                if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
//                    continue;

                request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }
    }
}
