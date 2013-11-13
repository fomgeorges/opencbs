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

using System.Collections.Generic;
using System.Linq;
using Omu.ValueInjecter;
using OpenCBS.DataContract;
using OpenCBS.Interface.Repository;
using OpenCBS.Interface.Service;
using OpenCBS.Interface.Validator;
using OpenCBS.Model;

namespace OpenCBS.Service
{
    public class UserService : Service, IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IUserValidator _userValidator;

        public UserService(IUserRepository userRepository, IRoleRepository roleRepository, IUserValidator userValidator)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _userValidator = userValidator;
        }

        public IList<UserDto> FindAll()
        {
            return _userRepository.FindAll().Select(Map).ToList().AsReadOnly();
        }

        public UserDto FindById(int id)
        {
            var user = _userRepository.FindById(id);
            return user == null ? null : Map(user);
        }

        public void Validate(UserDto dto)
        {
            _userValidator.Validate(dto);
        }

        public int Add(UserDto dto)
        {
            _userValidator.Validate(dto);
            ThrowIfInvalid(dto);

            var user = new User();
            user.InjectFrom(dto);
            user.Roles = _roleRepository.FindByIds(dto.RoleIds);
            return _userRepository.Add(user);
        }

        public void Update(UserDto dto)
        {
            _userValidator.Validate(dto);
            ThrowIfInvalid(dto);

            var user = new User();
            user.InjectFrom(dto);
            user.Roles = _roleRepository.FindByIds(dto.RoleIds);
            _userRepository.Update(user);
        }

        public void Remove(int id)
        {
            _userRepository.Remove(id);
        }

        private static UserDto Map(User user)
        {
            var result = new UserDto();
            result.InjectFrom<FlatLoopValueInjection>(user);
            result.RoleIds = user.Roles.Select(r => r.Id).ToList().AsReadOnly();
            return result;
        }
    }
}
