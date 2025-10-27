using MobyPark.Models;
using MobyPark.Services.Results.Session;

namespace MobyPark.Services.Interfaces;

public interface ISessionService
{
    CreateJwtResult CreateSession(UserModel user);
}