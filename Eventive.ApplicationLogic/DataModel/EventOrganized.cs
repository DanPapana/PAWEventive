﻿using System;
using System.Collections.Generic;

namespace Eventive.ApplicationLogic.DataModel
{
    public class EventOrganized
    {
        public enum EventCategory
        {
            Art,
            Causes,
            Comedy,
            Crafts,
            Dance,
            Drinks,
            Film,
            Sightseeing,
            Food,
            Games,
            Health,
            Literature,
            Music,
            Networking,
            Party,
            Sports,
            Other
        }

        public Guid Id { get; set; }
        public Guid CreatorId { get; set; }
        public string Title { get; set; }
        public string ImageByteArray { get; set; }
        public EventCategory Category { get; set; }
        public EventDetails EventDetails { get; set; }
        public List<Comment> Comments { get; set; }
        public List<EventClick> Clicks { get; set; }
        public List<EventApplication> Applications { get; set; }
        public List<EventFollowing> Followings { get; set; }
        public List<EventRating> Ratings { get; set; }

        public static EventOrganized Create(Guid creatorId,
                    string title,
                    string image,
                    EventCategory category,
                    EventDetails details)
        {
            var newEvent = new EventOrganized()
            {
                Id = Guid.NewGuid(),
                CreatorId = creatorId,
                Title = title,
                Category = category,
                EventDetails = details,
                Comments = new List<Comment>(),
                Clicks = new List<EventClick>(),
                Followings = new List<EventFollowing>(),
                Applications = new List<EventApplication>(),
                Ratings = new List<EventRating>()
            };

            if (!string.IsNullOrEmpty(image))
            {
                newEvent.ImageByteArray = image;
            }

            return newEvent;
        }

        public EventOrganized UpdateEvent(string title, 
                    EventCategory category, 
                    string description, 
                    string location,
                    double? latitude,
                    double? longitude,
                    string cityName,
                    DateTime deadline, 
                    DateTime occurenceDate, 
                    string image,
                    int maximumParticipants,
                    decimal fee,
                    bool applicationRequired)
        {
            if (!string.IsNullOrEmpty(image)) {
                ImageByteArray = image;
            }

            Title = title;
            Category = category;
            EventDetails.UpdateDetails(description, location, latitude, longitude, cityName, 
                deadline, occurenceDate, maximumParticipants, fee, applicationRequired);

            return this;
        }
    }

}
