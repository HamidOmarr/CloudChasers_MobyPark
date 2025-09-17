using Microsoft.AspNetCore.Mvc;
using MobyPark.Models;
using MobyPark.Services;

namespace MobyPark.Controllers
{
    [ApiController]
    public abstract class BaseController : ControllerBase
    {
        protected readonly SessionService SessionService;

        protected BaseController(SessionService sessionService)
        {
            SessionService = sessionService;
        }

        protected UserModel GetCurrentUser()
        {
            if (!Request.Headers.TryGetValue("Authorization", out var token))
                UnauthorizedResponse();

            var user = SessionService.GetSession(token);
            if (user == null)
                UnauthorizedResponse();

            return user;
        }

        private void UnauthorizedResponse()
        {
            Response.StatusCode = 401;
            Response.ContentType = "application/json";
            Response.WriteAsync("{\"error\": \"Invalid or missing session token\"}").Wait();
            throw new Exception("Unauthorized"); // Stop further execution
        }
    }
}