﻿using Coolector.Common.Nancy;

namespace Coolector.Services.Users.Modules
{
    public abstract class ModuleBase : ApiModuleBase
    {
        protected ModuleBase(string modulePath = "") : base(modulePath)
        {
        }
    }
}