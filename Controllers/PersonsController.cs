using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProceraAssessment.Controllers;

[Authorize]
public class PersonsController : Controller
{
    // CRUD functionality will be implemented here later
    public IActionResult Index()
    {
        return View();
    }
}