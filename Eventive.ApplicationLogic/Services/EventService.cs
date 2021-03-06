﻿using Eventive.ApplicationLogic.Abstraction;
using Eventive.ApplicationLogic.Common;
using Eventive.ApplicationLogic.DataModel;
using Eventive.ApplicationLogic.Dtos;
using Eventive.ApplicationLogic.Exceptions;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using static Eventive.ApplicationLogic.DataModel.EventApplication;
using static Eventive.ApplicationLogic.DataModel.EventOrganized;

namespace Eventive.ApplicationLogic.Services
{
    public class EventService
    {
        private readonly IEventRepository eventRepository;
        private readonly IUserRepository userRepository;

        public EventService(IEventRepository eventRepository, IUserRepository userRepository)
        {
            this.eventRepository = eventRepository;
            this.userRepository = userRepository;
        }

        public IEnumerable<EventOrganized> GetActiveEvents(Guid? participantId = null)
        {
            if (participantId is null)
            {
                return eventRepository.GetActiveEvents();
            }

            return eventRepository.GetActiveEvents(participantId);
        }

        public IEnumerable<EventOrganized> GetActiveEvents(string category, Guid? participantId = null)
        {
            if (string.IsNullOrEmpty(category))
            {
                if (participantId is null) {
                    return eventRepository.GetActiveEvents();
                }

                return eventRepository.GetActiveEvents(participantId);
            }

            if (participantId is null)
            {
                return eventRepository.GetActiveEvents((EventCategory)Enum.Parse(typeof(EventCategory), category));
            }

            return eventRepository.GetActiveEvents((EventCategory)Enum.Parse(typeof(EventCategory), category), participantId);
        }

        public IEnumerable<EventOrganized> GetTrendingEvents(string category, Guid? participantId = null)
        {
            IEnumerable<EventOrganized> events = GetActiveEvents(category, participantId);
            if (events.Count() == 0)
            {
                return events;
            }

            return GetEventsOrderedByScore(events);
        }

        private Dictionary<EventOrganized, double> GetTotalRecommendationScore(Guid participantId, Dictionary<EventOrganized, double> proximityScores,
            Dictionary<EventOrganized, double> categoryScores)
        {
            Dictionary<EventOrganized, double> totalScores = new Dictionary<EventOrganized, double>();
            var events = eventRepository.GetEventsToRecommend(participantId);
            foreach (var organizedEvent in events)
            {
                totalScores.Add(organizedEvent, 0);
                if (proximityScores.ContainsKey(organizedEvent))
                {
                    totalScores[organizedEvent] += proximityScores[organizedEvent] * Constants.RecommendationProximityWeight;
                }

                if (categoryScores.ContainsKey(organizedEvent))
                {
                    totalScores[organizedEvent] += categoryScores[organizedEvent] * Constants.RecommendationCategoryWeight;
                }
            }

            return totalScores;
        }

        private Dictionary<EventCategory, double> GetSimilarityScoreForCategories(Guid participantId)
        {
            //Get all the other participants
            var participants = userRepository.GetAll().Where(p => p.Id != participantId);

            //Get the category scores for all the other users
            Dictionary<Guid, SortedDictionary<EventCategory, double>> categoryScores = 
                new Dictionary<Guid, SortedDictionary<EventCategory, double>>();
            foreach (var participant in participants)
            {
                categoryScores.Add(participant.Id, GetTotalCategoryScoreForUser(participant.Id));
            }

            //Get the category score for the current user
            var currentUserCategoryScore = GetTotalCategoryScoreForUser(participantId);

            //Calculate cosine similarity for every other user
            Dictionary<Guid, double> cosineSimilarities = new Dictionary<Guid, double>();
            foreach (var participant in participants)
            {
                cosineSimilarities.Add(participant.Id, GetCosineSimilarity(currentUserCategoryScore.Values.ToArray(),
                    categoryScores[participant.Id].Values.ToArray()));
            }

            //Order the other users by their similarity to the current user
            var orderedParticipantIds = cosineSimilarities
                .OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

            return GetTotalCategoryScoreBasedOnSimilarity(orderedParticipantIds, 
                currentUserCategoryScore, categoryScores);
        }

        private Dictionary<EventCategory, double> GetTotalCategoryScoreBasedOnSimilarity(Dictionary<Guid, double> orderedParticipantIds, 
            SortedDictionary<EventCategory, double> currentUserCategoryScore, 
            Dictionary<Guid, SortedDictionary<EventCategory, double>> categoryScores)
        {
            Dictionary<EventCategory, double> categoryTotalScore = new Dictionary<EventCategory, double>();

            //The more similar a user is, the higher its weight
            foreach (var participant in orderedParticipantIds)
            {
                foreach (var category in currentUserCategoryScore.Keys)
                {
                    //Adding the similar user's category score of the current user and the most similar user
                    double score = currentUserCategoryScore[category] + 
                        categoryScores[participant.Key][category] * orderedParticipantIds[participant.Key];

                    if (categoryTotalScore.ContainsKey(category))
                    {
                        categoryTotalScore[category] = score;
                    }
                    else
                    {
                        categoryTotalScore.Add(category, score);
                    }
                }
            }

            return categoryTotalScore;
        }

        public IEnumerable<EventOrganized> GetRecommendedEvents(Guid participantId, string lat = null, string lng = null) 
        {
            var events = eventRepository.GetEventsToRecommend(participantId);
            if (events.Count() == 0)
            {
                return events;
            }

            var proximityScores = GetEventProximityScoreForUserAsync(participantId, lat, lng).Result;

            //Add the resulting score to the categories from the users most similar to other users
            var categoryTotalScore = GetSimilarityScoreForCategories(participantId);
            
            //Put all the category scores into the events with such categories
            var eventScore = GetEventScoreFromCategoryScore(participantId, categoryTotalScore);

            //Add the scores to the events according to their weight
            var totalEventScores = GetTotalRecommendationScore(participantId, proximityScores, eventScore);

            //Order events by the score
            var orderedScores = totalEventScores
                .OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

            return GetRecommendedEvents(orderedScores);
        }

        private List<EventOrganized> GetRecommendedEvents(Dictionary<EventOrganized, double> orderedScores)
        {
            //Return the top x events
            var maximumNumberOfRecommendedEvents = Constants.NumberOfRecommendedEventsShown;
            if (maximumNumberOfRecommendedEvents > orderedScores.Count())
            {
                maximumNumberOfRecommendedEvents = orderedScores.Count();
            }

            List<EventOrganized> recommendedEvents = new List<EventOrganized>();
            for (int i = 0; i < maximumNumberOfRecommendedEvents; i++)
            {
                recommendedEvents.Add(orderedScores.ElementAt(i).Key);
            }

            return recommendedEvents;
        }

        //Cosine similarity(A, B) = Sum(A * B)/Sqrt(Sum(A^2)) * Sqrt(Sum(B^2))
        private double GetCosineSimilarity(double[] currentUser, double[] targetUser)
        {
            double numerator = 0;
            double A = 0;
            double B = 0;

            for (int i = 0; i < currentUser.Length; i++)
            {
                numerator += currentUser[i] * targetUser[i];
                A += currentUser[i] * currentUser[i];
                B += targetUser[i] * targetUser[i];
            }

            double denominator = Math.Sqrt(A) * Math.Sqrt(B);
            return (numerator == 0) ? 0 : numerator / denominator;
        }
        

        public EventOrganized GetEventById(Guid eventId)
        {            
            var eventOrganized = eventRepository.GetEventById(eventId);

            if (eventOrganized is null)
            {
                throw new EntityNotFoundException(eventId);
            }

            return eventOrganized;
        }

        public EventFollowing FollowEvent(Guid eventId, Guid participantId)
        {
            var eventOrganized = eventRepository.GetEventById(eventId);
            var participant = userRepository.GetParticipantByGuid(participantId);

            EventFollowing following = EventFollowing.Create(eventOrganized, participant);
            eventRepository.AddInteraction(following);
            return following;
        }

        public EventApplication ApplyToEvent(Guid eventId, Guid participantId, string applicationText = null)
        {
            var eventOrganized = eventRepository.GetEventById(eventId);
            var participant = userRepository.GetParticipantByGuid(participantId);

            EventApplication application = Create(eventOrganized, participant, applicationText);
            //if an application is not required, users are accepted by default
            if (!eventOrganized.EventDetails.ApplicationRequired)
            {
                application.Status = ApplicationStatus.Approved;
            }

            eventRepository.AddInteraction(application);
            return application;
        }

        public EventApplication GetApplication(Guid eventId, Guid participantId)
        {
            return eventRepository.GetApplication(eventId, participantId);
        }

        public EventApplication GetApplication(Guid applicationId)
        {
            return eventRepository.GetApplication(applicationId);
        }

        public EventFollowing GetFollowing(Guid eventId, Guid participantId)
        {
            return eventRepository.GetFollowing(eventId, participantId);
        }

        public string GetApplicationText(Guid eventId, Guid participantId)
        {
            return eventRepository.GetUserApplicationText(eventId, participantId);
        }

        public IEnumerable<EventOrganized> GetAppliedEventsForUser(Guid participantId)
        {
            return eventRepository.GetAppliedEventsForUser(participantId);
        }

        public IEnumerable<EventOrganized> GetFollowedEventsForUser(Guid participantId)
        {
            return eventRepository.GetFollowedEventsForUser(participantId);
        }

        public IEnumerable<EventOrganized> GetPastEventsOfUser(Guid participantId)
        {
            return eventRepository.GetPastAppliedEvents(participantId);
        }

        public IEnumerable<Guid> GetAppliedEventsGuidForUser(Guid participantId)
        {
            return eventRepository.GetAppliedEventsGuidForUser(participantId);
        }

        public IEnumerable<Guid> GetFollowedEventsGuidForUser(Guid participantId)
        {
            return eventRepository.GetFollowedEventsGuidForUser(participantId);
        }

        public IEnumerable<Comment> GetEventComments(Guid eventId)
        {
            return eventRepository.GetEventComments(eventId);
        }

        public IEnumerable<EventClick> GetEventClicks(Guid eventId, Guid participantId)
        {
            return eventRepository.GetClicks(eventId, participantId);
        }

        public IEnumerable<EventRating> GetEventRatings(Guid eventId)
        {
            return eventRepository.GetEventRatings(eventId);
        }

        public IEnumerable<EventRating> GetUserRatings(Guid participantId)
        {
            return eventRepository.GetUserRatings(participantId);
        }


        public IEnumerable<EventApplication> GetEventApplications(Guid eventId)
        {
            return eventRepository.GetEventApplications(eventId);
        }
        public IEnumerable<EventFollowing> GetEventFollowings(Guid eventId)
        {
            return eventRepository.GetEventFollowings(eventId);
        }

        public IEnumerable<EventClick> GetEventClicks(Guid eventId)
        {
            return eventRepository.GetEventClicks(eventId);
        }

        public EventRating GetUserRating(Guid eventId, Guid participantId)
        {
            return eventRepository.GetUserRating(eventId, participantId);
        }

        private double GetInteractionDecay(IEnumerable<IEventInteraction> interactions)
        {
            double gravity = Constants.TrendingGravity;
            double decay = 0;
            foreach (var interaction in interactions)
            {
                double hours = (DateTime.UtcNow - interaction.Timestamp).TotalHours;
                decay += hours * gravity;
            }
            return decay;
        }

        private double GetInteractionScore(IEnumerable<IEventInteraction> interactions, double interactionWeight)
        {
            double interactionScore = 0;
            var interactionDecay = GetInteractionDecay(interactions);
            var interactionCount = interactions.ToList().Count;

            if (interactions != null && interactionCount > 0)
            {
                interactionScore = interactionCount * interactionWeight / (interactionDecay / (interactionCount + 1));
            }
            return interactionScore;
        }

        private double GetTotalTrendingScoreForAnEvent(Guid eventId)
        {
            double applicationWeight = Constants.TrendingApplicationWeight;
            double followWeight = Constants.TrendingFollowWeight;
            double commentWeight = Constants.TrendingCommentWeight;
            double clickWeight = Constants.TrendingClickWeight;

            var applicationScore = GetInteractionScore(GetEventApplications(eventId), applicationWeight);
            var followingScore = GetInteractionScore(GetEventFollowings(eventId), followWeight);
            var commentScore = GetInteractionScore(GetEventComments(eventId), commentWeight);
            var clickScore = GetInteractionScore(GetEventClicks(eventId), clickWeight);

            return applicationScore + followingScore + commentScore + clickScore;
        }

        private IEnumerable<EventOrganized> GetEventsOrderedByScore(IEnumerable<EventOrganized> eventsToScore)
        {
            Dictionary<EventOrganized, double> eventScore = new Dictionary<EventOrganized, double>();

            foreach (EventOrganized eventOrganized in eventsToScore)
            {
                double trendingScore = GetTotalTrendingScoreForAnEvent(eventOrganized.Id);
                eventScore.Add(eventOrganized, trendingScore);
            }

            var orderedScores = eventScore
                .OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

            var maximumNumberOfTrendingEvents = Constants.NumberOfTrendingEventsShown;
            if (maximumNumberOfTrendingEvents > orderedScores.Count())
            {
                maximumNumberOfTrendingEvents = orderedScores.Count();
            }

            List<EventOrganized> trendingEvents = new List<EventOrganized>();
            for (int i = 0; i < maximumNumberOfTrendingEvents; i++)
            {
                trendingEvents.Add(orderedScores.ElementAt(i).Key);
            }

            return trendingEvents.AsEnumerable();
        }

        public SortedDictionary<EventCategory, double> GetTotalCategoryScoreForUser(Guid participantId)
        {
            SortedDictionary<EventCategory, double> categoryTotalScore = new SortedDictionary<EventCategory, double>();
            Dictionary<EventCategory, double> categoryRatingScore = GetCategoryRatingScoreForUser(participantId);
            Dictionary<EventCategory, double> categoryInteractionScore = GetCategoryInteractionScoreForUser(participantId);

            foreach (EventCategory category in Enum.GetValues(typeof(EventCategory)))
            {
                int numberOfScores = 0;
                double totalWeightedScore = 0;
                if (categoryRatingScore != null && categoryRatingScore.ContainsKey(category))
                {
                    totalWeightedScore += categoryRatingScore[category] * Constants.RecommendationRatingWeight;
                    numberOfScores++;
                }

                if (categoryInteractionScore.ContainsKey(category))
                {
                    totalWeightedScore += categoryInteractionScore[category];
                    numberOfScores++;
                }

                if (numberOfScores > 0)
                {
                    categoryTotalScore.Add(category, totalWeightedScore / numberOfScores);
                } else {
                    categoryTotalScore.Add(category, 0);
                }
            }
            return categoryTotalScore;
        }

        private Dictionary<EventOrganized, double> GetEventScoreFromCategoryScore(Guid participantId, 
            Dictionary<EventCategory, double> categoryTotalScore)
        {
            Dictionary<EventOrganized, double> categoryScoreForEvents = new Dictionary<EventOrganized, double>();
            var events = eventRepository.GetEventsToRecommend(participantId);

            foreach (var eventOrganized in events)
            {
                if (categoryTotalScore.ContainsKey(eventOrganized.Category))
                {
                    categoryScoreForEvents.Add(eventOrganized, categoryTotalScore[eventOrganized.Category]);
                }
            }

            return categoryScoreForEvents;
        }

        private Dictionary<EventCategory, double> GetCategoryInteractionScoreForUser(Guid participantId)
        {
            Dictionary<EventCategory, double> categoryInteractionScoreForUser = new Dictionary<EventCategory, double>();
            var clickScorePerCategory = GetCategoryClickScoreForUser(participantId);
            var followingScorePerCategory = GetFollowingScoreForUser(participantId);
            var applicationScorePerCategory = GetApplicationScoreForUser(participantId);

            foreach(EventCategory category in Enum.GetValues(typeof(EventCategory)))
            {
                int numberOfScores = 0;
                double weightedTotalScore = 0;
                if (clickScorePerCategory != null && clickScorePerCategory.ContainsKey(category))
                {
                    weightedTotalScore += clickScorePerCategory[category] * Constants.RecommendationClickWeight;
                    numberOfScores++;
                }

                if (followingScorePerCategory != null && followingScorePerCategory.ContainsKey(category))
                {
                    weightedTotalScore += followingScorePerCategory[category] * Constants.RecommendationFollowWeight;
                    numberOfScores++;
                }

                if (applicationScorePerCategory != null && applicationScorePerCategory.ContainsKey(category))
                {
                    weightedTotalScore += applicationScorePerCategory[category] * Constants.RecommendationApplicationWeight;
                    numberOfScores++;
                }

                if (numberOfScores > 0)
                {
                    categoryInteractionScoreForUser.Add(category, weightedTotalScore / numberOfScores);
                }
            }

            return categoryInteractionScoreForUser;
        }

        private Dictionary<EventCategory, double> GetApplicationScoreForUser(Guid participantId)
        {
            List<EventApplication> eventApplications = eventRepository.GetUserApplications(participantId).ToList();
            if (eventApplications.Count == 0)
            {
                return null;
            }

            Dictionary<EventCategory, int> applicationCategories = new Dictionary<EventCategory, int>();
            foreach (var application in eventApplications)
            {
                if (applicationCategories.ContainsKey(application.EventOrganized.Category))
                {
                    applicationCategories[application.EventOrganized.Category]++;
                }
                else
                {
                    applicationCategories.Add(application.EventOrganized.Category, 1);
                }
            }

            //Normalize applications
            Dictionary<EventCategory, double> applicationScorePerCategory = new Dictionary<EventCategory, double>();
            foreach (EventCategory category in applicationCategories.Keys)
            {
                applicationScorePerCategory.Add(category, applicationCategories[category] / eventApplications.Count);
            }

            return applicationScorePerCategory;
        }

        private Dictionary<EventCategory, double> GetFollowingScoreForUser(Guid participantId)
        {
            List<EventFollowing> eventFollowings = eventRepository.GetUserFollowings(participantId).ToList();
            if (eventFollowings.Count == 0)
            {
                return null;
            }

            Dictionary<EventCategory, int> followingCategories = new Dictionary<EventCategory, int>();
            foreach(var following in eventFollowings)
            {
                if (followingCategories.ContainsKey(following.EventOrganized.Category))
                {
                    followingCategories[following.EventOrganized.Category]++;
                } 
                else
                {
                    followingCategories.Add(following.EventOrganized.Category, 1);
                }
            }

            //Normalize followings
            Dictionary<EventCategory, double> followingScorePerCategory = new Dictionary<EventCategory, double>();
            foreach (EventCategory category in followingCategories.Keys)
            {
                followingScorePerCategory.Add(category, followingCategories[category] / eventFollowings.Count);
            }

            return followingScorePerCategory;
        }

        private Dictionary<EventCategory, double> GetCategoryRatingScoreForUser(Guid participantId)
        {
            List<EventRating> userRatings = GetUserRatings(participantId).ToList();
            if (userRatings.Count == 0)
            {
                return null;
            }

            Dictionary<EventCategory, List<int>> categoryRatingScores = new Dictionary<EventCategory, List<int>>();
            foreach (var userRating in userRatings)
            {
                if (categoryRatingScores.ContainsKey(userRating.EventOrganized.Category))
                {
                    categoryRatingScores[userRating.EventOrganized.Category].Add(userRating.Score);
                }
                else
                {
                    categoryRatingScores.Add(userRating.EventOrganized.Category, new List<int>(userRating.Score));
                }
            }

            //Normalize ratings
            return GetNormalizedEventRatingScoreForUser(categoryRatingScores);
        }

        private Dictionary<EventCategory, double> GetCategoryClickScoreForUser(Guid participantId)
        {
            Dictionary<EventCategory, int> clicksPerCategory = new Dictionary<EventCategory, int>();
            foreach (EventCategory category in Enum.GetValues(typeof(EventCategory)))
            {
                var numberOfClicks = eventRepository.GetClicksPerCategoryForUser(participantId, category);
                clicksPerCategory.Add(category, numberOfClicks);
            }

            //Normalize clicks
            var totalClicks = eventRepository.GetTotalNumberOfClicksForUser(participantId);
            if (totalClicks == 0)
            {
                return null;
            }

            Dictionary<EventCategory, double> clickScorePerCategory = new Dictionary<EventCategory, double>();
            foreach (EventCategory category in Enum.GetValues(typeof(EventCategory)))
            {
                if (clicksPerCategory.ContainsKey(category))
                {
                    clickScorePerCategory.Add(category, clicksPerCategory[category] / totalClicks);
                }
            }

            return clickScorePerCategory;
        }

        private Dictionary<EventCategory, double> GetNormalizedEventRatingScoreForUser(Dictionary<EventCategory, 
            List<int>> categoryRatingList)
        {
            Dictionary<EventCategory, double> categoryRatingScore = new Dictionary<EventCategory, double>();

            foreach(var category in categoryRatingList.Keys)
            {
                var ratingList = categoryRatingList[category];
                if (ratingList.Count > 0)
                {
                    double normalizedRating = ratingList.Sum() / ratingList.Count;
                    categoryRatingScore.Add(category, normalizedRating);
                }
            }

            return categoryRatingScore;
        }

        /*
         * Calculate a score from 0.0 to 1.0
         * 1.0 meaning the distance from the user's current location is 0
         * 0.0 meaning the distance is >= 1000 km (Specified as ProximityScoreMaximumDistanceInMeters in Constants)
         */
        public async Task<Dictionary<EventOrganized, double>> GetEventProximityScoreForUserAsync(Guid participantId, 
            string latitude, string longitude)
        {
            Dictionary<EventOrganized, double> proximityScoreForEvents = new Dictionary<EventOrganized, double>();
            if (string.IsNullOrEmpty(latitude) || string.IsNullOrEmpty(longitude))
            {
                return proximityScoreForEvents;
            }

            List<EventOrganized> events = eventRepository.GetEventsToRecommend(participantId).ToList();

            string destinations = GetDestinationsString(events);
            if (!string.IsNullOrEmpty(destinations))
            {
                var requestUri = GetResponseUriForDistanceMatrix(destinations, latitude, longitude);
                HttpClient client = new HttpClient();
                var request = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead);

                if (request.IsSuccessStatusCode && request.Content.Headers.ContentType.MediaType == "application/json")
                {
                    var responseObject = request.Content.ReadAsAsync<DistanceResponse>();
                    proximityScoreForEvents = GetProximityScoresFromResponse(responseObject.Result?.Rows[0]?.Elements, events);
                }
            }
            
            return proximityScoreForEvents;
        }

        private string GetResponseUriForDistanceMatrix(string destinations, string latitude, string longitude)
        {
            double userLatitude = Convert.ToDouble(latitude);
            double userLongitude = Convert.ToDouble(longitude);
            string apiKey = ConfigurationManager.AppSettings.Get("GOOGLE_API_KEY");
            string requestUri = string.Format("https://maps.googleapis.com/maps/api/distancematrix/json?origins={0},{1}&destinations={2}&key={3}",
                userLatitude, userLongitude, destinations, apiKey);

            return requestUri;
        }

        private string GetDestinationsString(IEnumerable<EventOrganized> events)
        {
            string destinations = string.Empty;
            foreach (var eventOrganized in events)
            {
                destinations += string.Format("{0},{1}|", eventOrganized.EventDetails.Latitude, eventOrganized.EventDetails.Longitude);
            }

            return destinations;
        }

        private Dictionary<EventOrganized, double> GetProximityScoresFromResponse(List<Element> elements, 
            IEnumerable<EventOrganized> events)
        {
            double maximumDistanceToScore = Constants.ProximityScoreMaximumDistanceInMeters;
            Dictionary<EventOrganized, double> proximityScoreForEvents = new Dictionary<EventOrganized, double>();

            int currentIndex = 0;
            foreach (var element in elements)
            {
                if (element.Status.Equals("OK"))
                {
                    double proximityScore = 1 - element.Distance.Value / maximumDistanceToScore;
                    if (proximityScore < 0)
                    {
                        proximityScore = 0;
                    }

                    proximityScoreForEvents.Add(events.ElementAt(currentIndex), proximityScore);
                }

                currentIndex++;
            }

            return proximityScoreForEvents;
        }

        public Comment AddComment(Guid creatorId, Guid eventId, string message)
        {
            var participant = userRepository.GetParticipantByGuid(creatorId);
            var eventOrganized = eventRepository.GetEventById(eventId);

            if (participant is null || eventOrganized is null)
            {
                return null;
            }

            var comment = Comment.Create(participant, eventOrganized, message);
            eventRepository.AddInteraction(comment);
            return comment;
        }

        public EventRating AddRating(Guid participantId, Guid eventId, string score)
        {
            int ratingScore = int.Parse(score);
            var participant = userRepository.GetParticipantByGuid(participantId);
            var eventOrganized = eventRepository.GetEventById(eventId);

            if (participant is null || eventOrganized is null)
            {
                return null;
            }

            var rating = EventRating.Create(eventOrganized, participant, ratingScore);
            eventRepository.AddInteraction(rating);
            return rating;
        }

        public EventOrganized UpdateEvent(Guid eventId, string title,
                    EventCategory category,
                    string description,
                    string location,
                    double? latitude,
                    double? longitude,
                    string city,
                    DateTime deadline,
                    DateTime occurenceDate,
                    string image,
                    int maximumParticipants,
                    decimal fee,
                    bool applicationRequired)
        {

            var eventToUpdate = GetEventById(eventId);

            eventToUpdate.UpdateEvent(title, category, description, location, latitude, longitude, city, deadline, occurenceDate,
                                                image, maximumParticipants, fee, applicationRequired);
            eventRepository.Update(eventToUpdate);

            return eventToUpdate;
        }

        public bool RemoveInteraction(IEventInteraction eventInteraction)
        {
            return eventRepository.RemoveInteraction(eventInteraction);
        }

        public bool RemoveEvent(string eventId)
        {
            Guid.TryParse(eventId, out Guid eventGuid);
            return eventRepository.RemoveEvent(eventGuid);
        }

        public Comment GetCommentById(string commentId)
        {
            Guid.TryParse(commentId, out Guid commentGuid);
            return eventRepository.GetCommentById(commentGuid);
        }

        public EventClick RegisterClick(Guid eventId, Guid participantId)
        {
            var commenter = userRepository.GetParticipantByGuid(participantId);
            var eventToRegisterFor = eventRepository.GetEventById(eventId);

            if (eventToRegisterFor.CreatorId.Equals(participantId) 
                || commenter is null || eventToRegisterFor is null)
            {
                return null;
            }

            var newClick = EventClick.Create(eventToRegisterFor, commenter);
            eventRepository.AddInteraction(newClick);
            return newClick;
        }

        public void AcceptApplication(Guid applicationId)
        {
            var application = eventRepository.GetApplication(applicationId);
            application.JudgeApplication(ApplicationStatus.Approved);
            eventRepository.Update(application);
        }

        public void RejectApplication(Guid applicationId)
        {
            var application = eventRepository.GetApplication(applicationId);
            application.JudgeApplication(ApplicationStatus.Rejected);
            eventRepository.Update(application);
        }

        public string FormatParticipationFee(decimal participationFee)
        {
            string formattedFee = participationFee.ToString("#.##");

            if (formattedFee.Length > 0)
            {
                return $"${formattedFee}";
            }

            return "None";
        }

        public string FormatMaximumParticipants(int maximumParticipants)
        {
            if (maximumParticipants > 0)
            {
                return $"{maximumParticipants}";
            }

            return "None";
        }

        public string FormatEventDate(DateTime eventDate)
        {
            return $"{eventDate:dd/MM/yyyy}";
        }

        public string FormatEventTime(DateTime eventTime)
        {
            return $"{eventTime:H:mm}";
        }

        public string FormatUserName(Participant hostingUser)
        {
            return $"{hostingUser.FirstName} {hostingUser.LastName}";
        }
    }
}
