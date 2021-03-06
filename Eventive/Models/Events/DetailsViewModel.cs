﻿using Eventive.ApplicationLogic.DataModel;
using System;
using System.Collections.Generic;
using static Eventive.ApplicationLogic.DataModel.EventOrganized;

namespace Eventive.Models.Events
{
    public class DetailsViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Image { get; set; }
        public EventCategory Category { get; set; }
        public string Deadline { get; set; }
        public string OccurenceDate { get; set; }
        public string OccurenceTime { get; set; }
        public string MaximumParticipants { get; set; }
        public string Location { get; set; }
        public string ParticipationFee { get; set; }
        public string Description { get; set; }
        public string HostName { get; set; }
        public string HostPhoneNo { get; set; }
        public string HostEmail { get; set; }
        public string UserName { get; set; }
        public string UserProfileImage { get; set; }
        public string HostProfileImage { get; set; }
        public IEnumerable<Comment> Comments { get; set; }
        public Guid NewCommentEventId { get; set; }
        public string NewCommentMessage { get; set; }
    }
}
