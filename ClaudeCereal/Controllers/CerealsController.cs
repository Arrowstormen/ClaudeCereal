using ClaudeCereal.Models;
using ClaudeCereal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace ClaudeCereal.Controllers;

public class CerealsController(ICerealService service) : ODataController
{
    [EnableQuery(PageSize = 100)]
    public IQueryable<Cereal> Get() => service.GetQueryable();
}
