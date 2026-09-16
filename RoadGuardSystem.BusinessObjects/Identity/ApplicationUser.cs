using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace RoadGuardSystem.aBusinessObjects.Identity
{
    /// <summary>
    /// Represents an application user, extending ASP.NET Core Identity.
    /// Mapped to the USER concept in the ER diagram.
    /// </summary>

    public class ApplicationUser : IdentityUser<Guid>
    {

    }
}
