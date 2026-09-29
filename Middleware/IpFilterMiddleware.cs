namespace MVCSampleApp.Middleware
{
    // Simple IP allow-list. If the list in appsettings.json is empty, everyone is let through.
    public class IpFilterMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly List<string> _allowedIps;

        public IpFilterMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _allowedIps = configuration.GetSection("IpFilter:AllowedIPs").Get<List<string>>() ?? new List<string>();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var remoteIp = context.Connection.RemoteIpAddress?.ToString();

            // Treat localhost IPv6/IPv4 the same, useful when testing on your own machine
            if (remoteIp == "::1") remoteIp = "127.0.0.1";

            if (_allowedIps.Count > 0 && !_allowedIps.Contains(remoteIp))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Access denied: your IP address is not allowed.");
                return;
            }

            await _next(context);
        }
    }
}
