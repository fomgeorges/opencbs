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
using OpenCBS.Interface;
using OpenCBS.Interface.Presenter;
using OpenCBS.Interface.View;

namespace OpenCBS.Presenter
{
    public class ExoticSchedulePresenter : IExoticSchedulePresenter, IExoticSchedulePresenterCallbacks
    {
        private readonly IExoticScheduleView _view;
        private readonly IApplicationController _appController;

        private CommandResult _commandResult = CommandResult.Cancel;

        public ExoticSchedulePresenter(IExoticScheduleView view, IApplicationController appController)
        {
            _view = view;
            _appController = appController;
        }

        public Result<ExoticScheduleDto> Get(ExoticScheduleDto dto)
        {
            _view.Attach(this);
            dto = dto ?? new ExoticScheduleDto();
            _view.InjectFrom(dto);
            _view.ExoticScheduleName = dto.Name;
            _view.Run();
            return new Result<ExoticScheduleDto>(CommandResult.Cancel, null);
        }

        public void Ok()
        {
        }

        public void Cancel()
        {
            _commandResult = CommandResult.Cancel;
            _view.Stop();
        }

        public void Close()
        {
            _appController.Unsubscribe(this);
        }

        public void ChangeSelectedItem()
        {
            var item = _view.SelectedItem;
            if (item == null)
            {
                _view.CanMoveUp = _view.CanMoveDown = _view.CanDelete = false;
                return;
            }
            _view.CanMoveUp = item.Number > 1;
            _view.CanMoveDown = item.Number < _view.Items.Count;
            _view.CanDelete = true;
        }

        public void MoveUp()
        {
            var item = _view.SelectedItem;
            if (item == null || item.Number == 1) return;
            var previousItem = _view.Items.SingleOrDefault(x => x.Number == item.Number - 1);
            if (previousItem == null) return;

            var temp = item.Number;
            item.Number = previousItem.Number;
            previousItem.Number = temp;

            _view.Items = _view.Items.OrderBy(x => x.Number).ToList().AsReadOnly();
            _view.FocusItems();
        }

        public void MoveDown()
        {
            var item = _view.SelectedItem;
            if (item == null) return;
            var nextItem = _view.Items.SingleOrDefault(x => x.Number == item.Number + 1);
            if (nextItem == null) return;

            var temp = item.Number;
            item.Number = nextItem.Number;
            nextItem.Number = temp;

            _view.Items = _view.Items.OrderBy(x => x.Number).ToList().AsReadOnly();
            _view.FocusItems();
        }

        public void Add()
        {
            var number = _view.Items.Count + 1;
            var item = new ExoticScheduleItemDto
            {
                Id = number,
                Number = number,
                PrincipalPercentage = 0,
                InterestPercentage = 0
            };
            var items = new List<ExoticScheduleItemDto>();
            items.AddRange(_view.Items);
            items.Add(item);
            _view.Items = items.AsReadOnly();
            _view.SelectedItem = item;
            _view.FocusItems();
        }

        public void Delete()
        {
            var item = _view.SelectedItem;
            if (item == null) return;
            var items = new List<ExoticScheduleItemDto>();
            items.AddRange(_view.Items);
            items.Remove(item);

            var number = 1;
            foreach (var i in items)
            {
                i.Number = number;
                number++;
            }
            _view.Items = items.AsReadOnly();
        }

        public object View
        {
            get { return _view; }
        }
    }
}
