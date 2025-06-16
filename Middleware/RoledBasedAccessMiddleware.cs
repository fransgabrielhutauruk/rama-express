using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using System.Threading.Tasks;

namespace RamaExpress.Middleware
{
    public class RoledBasedAccessMiddleware
    {
        private readonly RequestDelegate _next;

        public RoledBasedAccessMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower();
            var userRole = context.Session.GetString("UserRole")?.ToLower();
            var userId = context.Session.GetInt32("UserId");

            if (path.StartsWith("/admin"))
            {
                if (userId == null)
                {
                    context.Response.Redirect("/Login");
                    return;
                }

                if (userRole != "admin")
                {
                    context.Response.Redirect("/Error/AccessDenied");
                    return;
                }
            }

            else if (path.StartsWith("/karyawan"))
            {
                if (userId == null)
                {
                    context.Response.Redirect("/Login");
                    return;
                }

                if (userRole != "karyawan")
                {
                    context.Response.Redirect("/Error/AccessDenied");
                    return;
                }
            }
            await _next(context);
        }
    }
}
