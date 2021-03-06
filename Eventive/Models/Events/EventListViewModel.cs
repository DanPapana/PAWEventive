﻿using System;
using System.Collections.Generic;

namespace Eventive.Models.Events
{
    public class EventListViewModel
    {
        public List<EventViewModel> EventViewModelList { get; set; }
        public IEnumerable<Guid> EventsFollowed { get; set; }
        public IEnumerable<Guid> EventsApplied { get; set; }
    }
}
