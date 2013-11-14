﻿// Copyright © 2013 Open Octopus Ltd.
// 
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License along
// with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
// 
// Website: http://www.opencbs.com
// Contact: contact@opencbs.com

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenCBS.Model
{
    public class Role : EntityBase
    {
        private static readonly IDictionary<string, HashSet<string>> Map = new Dictionary<string, HashSet<string>>();

        static Role()
        {
            // There are two levels of permissions:
            // a) Screen permissions -- define what is accessible to the user 
            // from the UI perspective (menu items, forms, buttons, etc.)
            // b) Service permissions -- define what is accessible at the Service layer.
            //
            // In general, one screen function requires several service permissions. For example,
            // to be able to add a user you have to list all the roles in the system.
            // So, along with IUserService.Add you need IRoleService.FindAll
            //
            // The Map below defines all such entries: screen permission -> list of service permissions
            Map.Add("Role.Add", new HashSet<string>
            {
                "IRoleService.FindAll",
                "IRoleService.Validate",
                "IRoleService.Add"
            });
            Map.Add("Role.Edit", new HashSet<string>
            {
                "IRoleService.FindAll",
                "IRoleService.Validate",
                "IRoleService.FindById",
                "IRoleService.Update"
            });
            Map.Add("Role.Delete", new HashSet<string>
            {
                "IRoleService.FindAll",
                "IRoleService.Delete"
            });
        }

        public string Name { get; set; }
        public IList<string> Permissions { get; set; }

        public bool Can(string permission)
        {
            if (Permissions == null)
                throw new ArgumentException(string.Format("No permissions for role {0}", Name));
            return Permissions.Contains(permission);
        }

        public bool HasServicePermission(string servicePermission)
        {
            if (Permissions == null)
                throw new ArgumentException(string.Format("No permissions for role {0}", Name));
            var list = Permissions.Select(p => Map[p]);
            var set = new HashSet<string>();
            foreach (var item in list) set.UnionWith(item);
            return set.Contains(servicePermission);
        }
    }
}
