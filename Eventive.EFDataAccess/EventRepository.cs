﻿using Microsoft.EntityFrameworkCore;
using Eventive.ApplicationLogic.Abstraction;
using Eventive.ApplicationLogic.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using static Eventive.ApplicationLogic.DataModel.EventOrganized;

namespace Eventive.EFDataAccess
{
    public class EventRepository : BaseRepository<EventOrganized>, IEventRepository
    {
        public EventRepository(EventManagerDbContext dbContext) : base(dbContext)
        {
        }
        
        public EventOrganized GetEventById(Guid eventId)
        {
            return dbContext.Events.Include(ev => ev.EventDetails)
                    .Include(evnt => evnt.Comments)
                    .Include(evnt => evnt.Applications)
                    .Include(evnt => evnt.Followings)
                    .Include(evnt => evnt.Clicks)
                    .Include(evnt => evnt.Ratings)
                    .Where(evnt => evnt.Id == eventId)
                    .FirstOrDefault();
        }

        public IEnumerable<EventOrganized> GetActiveEvents()
        {
            return dbContext.Events
                    .Include(evnt => evnt.EventDetails)
                    .Include(evnt => evnt.Comments)
                    .Where(evnt => evnt
                    .EventDetails.Deadline > DateTime.Now)
                    .OrderBy(evnt => evnt.EventDetails.Deadline)
                    .AsEnumerable();
        }

        public IEnumerable<EventOrganized> GetActiveEvents(Guid? participantId)
        {
            return dbContext.Events
                    .Include(evnt => evnt.EventDetails)
                    .Include(evnt => evnt.Comments)
                    .Where(evnt => evnt
                    .EventDetails.Deadline > DateTime.Now
                        && evnt.CreatorId != participantId)
                    .OrderBy(evnt => evnt.EventDetails.Deadline)
                    .AsEnumerable();
        }

        public IEnumerable<EventOrganized> GetActiveEvents(EventCategory eventCategory)
        {
            return dbContext.Events
                    .Include(evnt => evnt.EventDetails)
                    .Include(evnt => evnt.Comments)
                    .Where(evnt => evnt
                    .EventDetails.Deadline > DateTime.Now
                        && evnt.Category == eventCategory)
                    .OrderBy(evnt => evnt.EventDetails.Deadline)
                    .AsEnumerable();
        }

        public IEnumerable<EventOrganized> GetActiveEvents(EventCategory eventCategory, Guid? participantId)
        {
            return dbContext.Events
                    .Include(evnt => evnt.EventDetails)
                    .Include(evnt => evnt.Comments)
                    .Where(evnt => evnt
                    .EventDetails.Deadline > DateTime.Now
                        && evnt.CreatorId != participantId
                        && evnt.Category == eventCategory)
                    .OrderBy(evnt => evnt.EventDetails.Deadline)
                    .AsEnumerable();
        }

        public IEnumerable<EventOrganized> GetEventsToRecommend(Guid participantId)
        {
            List<Guid> eventIdsToIgnore = new List<Guid>();
            
            var userApplications = GetUserApplications(participantId);
            foreach(var application in userApplications)
            {
                eventIdsToIgnore.Add(application.EventOrganized.Id);
            }

            var userFollowings = GetUserFollowings(participantId);
            foreach(var following in userFollowings)
            {
                eventIdsToIgnore.Add(following.EventOrganized.Id);
            }

            return dbContext.Events
                    .Include(evnt => evnt.EventDetails)
                    .Include(evnt => evnt.Comments)
                    .Where(evnt => evnt
                    .EventDetails.Deadline > DateTime.Now
                        && evnt.CreatorId != participantId
                        && !eventIdsToIgnore.Contains(evnt.Id))
                    .OrderBy(evnt => evnt.EventDetails.Deadline)
                    .AsEnumerable();
        }

        public EventApplication GetApplication(Guid eventId, Guid participantId)
        {
            return dbContext.Applications
                    .Include(e => e.Participant)
                    .Include(e => e.EventOrganized)
                    .Where(ev => ev.Participant.Id == participantId
                        && ev.EventOrganized.Id == eventId)
                    .FirstOrDefault();
        }

        public EventApplication GetApplication(Guid applicationId)
        {
            return dbContext.Applications.Include(e => e.Participant)
                    .Include(e => e.EventOrganized)
                    .Where(app => app.Id.Equals(applicationId))
                    .FirstOrDefault();
        }

        public EventFollowing GetFollowing(Guid eventId, Guid participantId)
        {
            return dbContext.Followings
                    .Include(e => e.Participant)
                    .Include(e => e.EventOrganized)
                    .Where(ev => ev.Participant.Id == participantId
                        && ev.EventOrganized.Id == eventId)
                    .FirstOrDefault();
        }

        public IEnumerable<EventApplication> GetUserApplications(Guid participantId)
        {
            return dbContext.Applications
                    .Include(e => e.Participant)
                    .Include(e => e.EventOrganized)
                    .Where(app => app.Participant.Id == participantId)
                    .OrderBy(evnt => evnt.EventOrganized.EventDetails.Deadline)
                    .AsEnumerable();
        }

        public IEnumerable<EventFollowing> GetUserFollowings(Guid participantId)
        {
            return dbContext.Followings
                    .Include(e => e.Participant)
                    .Include(e => e.EventOrganized)
                    .Where(follow => follow.Participant.Id == participantId)
                    .OrderBy(evnt => evnt.EventOrganized.EventDetails.Deadline)
                    .AsEnumerable();
        }

        public IEnumerable<Comment> GetEventComments(Guid eventId)
        {
            return dbContext.Comments
                    .Include(e => e.Participant)
                    .ThenInclude(e => e.ContactDetails)
                    .Include(e => e.EventOrganized)
                    .Where(e => e.EventOrganized.Id.Equals(eventId))
                    .OrderBy(e => e.Timestamp)
                    .AsEnumerable();
        }

        public IEnumerable<EventClick> GetClicks(Guid eventId, Guid participantId)
        {
            return dbContext.Clicks
                    .Include(e => e.Participant)
                    .Include(e => e.EventOrganized)
                    .Where(e => e.EventOrganized.Id.Equals(eventId)
                        && e.Participant.Id.Equals(participantId))
                    .AsEnumerable();                
        }

        public IEnumerable<EventRating> GetEventRatings(Guid eventId)
        {
            return dbContext.Ratings
                    .Include(e => e.Participant)
                    .Include(e => e.EventOrganized)
                    .Where(e => e.EventOrganized.Id.Equals(eventId))
                    .AsEnumerable();
        }

        public IEnumerable<EventRating> GetUserRatings(Guid participantId)
        {
            return dbContext.Ratings
                    .Include(e => e.Participant)
                    .Include(e => e.EventOrganized)
                    .Where(e => e.Participant.Id.Equals(participantId))
                    .AsEnumerable();
        }

        public int GetClicksPerCategoryForUser(Guid participantId, EventCategory category)
        {
            return dbContext.Clicks
                .Where(ev => ev.EventOrganized.Category.Equals(category)
                    && ev.Participant.Id.Equals(participantId)).Count();
        }

        public int GetTotalNumberOfClicksForUser(Guid participantId)
        {
            return dbContext.Clicks
                .Where(ev => ev.Participant.Id.Equals(participantId)).Count();
        }

        public UserBehaviour GetUserBehaviour(Guid participantId)
        {
            return dbContext.UserBehaviours
                .Where(ub => ub.Participant.Id.Equals(participantId))
                .FirstOrDefault();
        }

        public IEnumerable<EventOrganized> GetAppliedEventsForUser(Guid participantId)
        {
            List<EventOrganized> appliedEvents = new List<EventOrganized>();

            var applications = GetUserApplications(participantId);

            foreach (EventApplication application in applications)
            {
                var eventApplied = GetEventById(application.EventOrganized.Id);
                appliedEvents.Add(eventApplied);
            }

            return appliedEvents.AsEnumerable();
        }

        public IEnumerable<EventOrganized> GetFollowedEventsForUser(Guid participantId)
        {
            List<EventOrganized> followedEvents = new List<EventOrganized>();

            var followings = GetUserFollowings(participantId);

            foreach (EventFollowing follow in followings)
            {
                var eventFollowed = GetEventById(follow.EventOrganized.Id);
                followedEvents.Add(eventFollowed);
            }

            return followedEvents.AsEnumerable();
        }

        public IEnumerable<Guid> GetAppliedEventsGuidForUser(Guid participantId)
        {
            List<Guid> appliedEventIds = new List<Guid>();

            var applications = GetUserApplications(participantId);

            foreach (IEventInteraction application in applications)
            {
                appliedEventIds.Add(application.EventOrganized.Id);
            }

            return appliedEventIds.AsEnumerable();
        }

        public IEnumerable<Guid> GetFollowedEventsGuidForUser(Guid participantId)
        {
            List<Guid> followedEventIds = new List<Guid>();

            var followings = GetUserFollowings(participantId);

            foreach (IEventInteraction follow in followings)
            {
                followedEventIds.Add(follow.EventOrganized.Id);
            }

            return followedEventIds.AsEnumerable();
        }

        public IEnumerable<EventOrganized> GetPastAppliedEvents(Guid participantId)
        {
            var allPastEvents = dbContext.Events
                    .Include(evnt => evnt.EventDetails)
                    .Include(evnt => evnt.Comments)
                    .Include(evnt => evnt.Followings)
                    .Include(evnt => evnt.Applications)
                    .ThenInclude(app => app.Participant)
                    .Where(evnt => evnt
                        .EventDetails.OccurenceDate < DateTime.UtcNow
                        && evnt.Applications.Count > 0)
                    .OrderBy(evnt => evnt.EventDetails.OccurenceDate)
                    .AsEnumerable();

            List<EventOrganized> pastEventsList = new List<EventOrganized>();

            foreach (var pastEvent in allPastEvents)
            {
                foreach (var application in pastEvent.Applications)
                {
                    if (application.Participant.Id.Equals(participantId) 
                        && application.Status.Equals(EventApplication.ApplicationStatus.Approved))
                    {
                        pastEventsList.Add(pastEvent);
                    }
                }
            }

            return pastEventsList;
        }

        public string GetUserApplicationText(Guid eventId, Guid participantId)
        {
            var appliedEvent = GetEventById(eventId);
            var application = dbContext.Applications
                    .Include(e => e.Participant)
                    .Include(e => e.EventOrganized)
                    .Where(ap => ap.EventOrganized.Id == eventId
                        && ap.Participant.Id == participantId)
                    .FirstOrDefault();

            if (!(application is null))
            {
                return application.ApplicationText;
            }

            return null;
        }

        public EventRating GetUserRating(Guid eventId, Guid participantId)
        {
            return dbContext.Ratings
                    .Include(e => e.EventOrganized)
                    .Include(e => e.Participant)
                    .Where(e => e.EventOrganized.Id.Equals(eventId)
                        && e.Participant.Id.Equals(participantId))
                    .FirstOrDefault();
        }

        public Comment GetCommentById(Guid commentId)
        {
            return dbContext.Comments
                    .Include(com => com.Participant)
                    .Include(com => com.EventOrganized)
                    .Where(com => com.Id.Equals(commentId))
                    .FirstOrDefault();
        }

        public EventFollowing AddInteraction(EventFollowing followInteraction)
        {
            dbContext.Followings.Add(followInteraction);
            SaveChanges();
            return followInteraction;
        }

        public EventApplication AddInteraction(EventApplication applyInteraction)
        {
            dbContext.Applications.Add(applyInteraction);
            SaveChanges();
            return applyInteraction;
        }

        public EventClick AddInteraction(EventClick clickInteraction)
        {
            dbContext.Clicks.Add(clickInteraction);
            SaveChanges();
            return clickInteraction;
        }

        public EventRating AddInteraction(EventRating eventRating)
        {
            dbContext.Ratings.Add(eventRating);
            SaveChanges();
            return eventRating;
        }

        public Comment AddInteraction(Comment comment)
        {
            dbContext.Comments.Add(comment);
            SaveChanges();
            return comment;
        }

        public UserBehaviour AddUserBehaviour(UserBehaviour userBehaviour)
        {
            dbContext.UserBehaviours.Add(userBehaviour);
            SaveChanges();
            return userBehaviour;
        }

        public UserBehaviour Update(UserBehaviour userBehaviour)
        {
            dbContext.UserBehaviours.Update(userBehaviour);
            SaveChanges();
            return userBehaviour;
        }

        public EventApplication Update(EventApplication eventApplication)
        {
            dbContext.Applications.Update(eventApplication);
            SaveChanges();
            return eventApplication;
        }

        public bool RemoveInteraction(IEventInteraction interactionToRemove)
        {
            if (interactionToRemove is null)
            {
                return false;
            }
            
            dbContext.Remove(interactionToRemove);
            SaveChanges();
            return true;
        }

        public bool RemoveEvent(Guid eventId)
        {
            var eventToRemove = GetEventById(eventId);
            if (eventToRemove != null)
            {
                dbContext.RemoveRange(eventToRemove?.Comments);
                dbContext.RemoveRange(eventToRemove?.Clicks);
                dbContext.RemoveRange(eventToRemove?.Applications);
                dbContext.RemoveRange(eventToRemove?.Followings);
                dbContext.RemoveRange(eventToRemove?.Ratings);
                dbContext.Remove(eventToRemove?.EventDetails);
                dbContext.Remove(eventToRemove);
                SaveChanges();
                
                return true;
            }

            return false;
        }

        public void SaveChanges()
        {
            dbContext.SaveChanges();
        }

        public IEnumerable<EventApplication> GetEventApplications(Guid eventId)
        {
            return dbContext.Applications
                    .Include(e => e.Participant)
                    .ThenInclude(e => e.ContactDetails)
                    .Include(e => e.EventOrganized)
                    .Where(e => e.EventOrganized.Id.Equals(eventId))
                    .AsEnumerable();
        }

        public IEnumerable<EventFollowing> GetEventFollowings(Guid eventId)
        {
            return dbContext.Followings
                    .Include(e => e.Participant)
                    .Include(e => e.EventOrganized)
                    .Where(e => e.EventOrganized.Id.Equals(eventId))
                    .AsEnumerable();
        }

        public IEnumerable<EventClick> GetEventClicks(Guid eventId)
        {
            return dbContext.Clicks
                    .Include(e => e.Participant)
                    .Include(e => e.EventOrganized)
                    .Where(e => e.EventOrganized.Id.Equals(eventId))
                    .AsEnumerable();
        }
    }
}
