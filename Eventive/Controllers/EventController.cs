﻿using Eventive.ApplicationLogic.DataModel;
using Eventive.ApplicationLogic.Services;
using Eventive.Models.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace Eventive.Controllers
{
    [Authorize]
    public class EventController : Controller
    {
        private readonly EventService eventService;
        private readonly UserService userService;
        private readonly UserManager<IdentityUser> userManager;

        public EventController(UserManager<IdentityUser> userManager, 
            EventService eventService, UserService userService)
        {
            this.eventService = eventService;
            this.userService = userService;
            this.userManager = userManager;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult MyEvents()
        {
            return View();
        }

        private EventListViewModel GetEventListViewModel(IEnumerable<EventOrganized> events)
        {
            List<EventViewModel> eventViewModels = new List<EventViewModel>();

            foreach (EventOrganized theEvent in events)
            {
                if (theEvent != null)
                {
                    eventViewModels.Add(GetEventViewModel(theEvent));
                }
            }

            IEnumerable<Guid> eventsFollowed = null;
            IEnumerable<Guid> eventsApplied = null;

            if (User.Identity.IsAuthenticated)
            {
                var currentParticipantId = GetCurrentParticipantId();

                eventsFollowed = eventService
                            .GetFollowedEventsGuidForUser(currentParticipantId);

                eventsApplied = eventService
                            .GetAppliedEventsGuidForUser(currentParticipantId);
            }

            EventListViewModel eventListViewModel = new EventListViewModel()
            {
                EventViewModelList = eventViewModels,
                EventsFollowed = eventsFollowed,
                EventsApplied = eventsApplied
            };

            return eventListViewModel;
        }

        [AllowAnonymous]
        public IActionResult EventContainer(string category)
        {
            try
            {
                IEnumerable<EventOrganized> events;
                if (User.Identity.IsAuthenticated)
                {
                    var currentParticipantId = GetCurrentParticipantId();
                    events = eventService.GetActiveEvents(category, currentParticipantId);
                } 
                else
                {
                    events = eventService.GetActiveEvents(category);
                }

                var viewModel = GetEventListViewModel(events);
                return PartialView("_ContainerPartial", viewModel);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }
        
        [AllowAnonymous]
        public IActionResult TrendingContainer(string category)
        {
            try
            {
                IEnumerable<EventOrganized> events;
                if (User.Identity.IsAuthenticated)
                {
                    var currentParticipantId = GetCurrentParticipantId();
                    events = eventService.GetTrendingEvents(category, currentParticipantId);
                }
                else
                {
                    events = eventService.GetTrendingEvents(category);
                }

                var viewModel = GetEventListViewModel(events);
                return PartialView("_ContainerPartial", viewModel);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        public IActionResult RecommendedContainer(string lat = null, string lng = null)
        {
            try
            {
                IEnumerable<EventOrganized> events;
                var currentParticipantId = GetCurrentParticipantId();
                events = eventService.GetRecommendedEvents(currentParticipantId, lat, lng);
                
                var viewModel = GetEventListViewModel(events);
                return PartialView("_ContainerPartial", viewModel);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        public IActionResult AppliedEvents()
        {
            try
            {
                var currentParticipantId = GetCurrentParticipantId();

                IEnumerable<EventOrganized> appliedEvents = eventService
                            .GetAppliedEventsForUser(currentParticipantId);

                EventListViewModel viewModel = GetEventListViewModel(appliedEvents);

                return PartialView("_ContainerPartial", viewModel);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        public IActionResult FollowingEvents()
        {
            try
            {
                var currentParticipantId = GetCurrentParticipantId();

                IEnumerable<EventOrganized> followingEvents = eventService
                            .GetFollowedEventsForUser(currentParticipantId);

                EventListViewModel viewModel = GetEventListViewModel(followingEvents);

                return PartialView("_ContainerPartial", viewModel);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        public IActionResult CreatedEvents()
        {
            try
            {
                var currentParticipantId = GetCurrentParticipantId();

                IEnumerable<EventOrganized> createdEvents = userService
                            .GetUserEvents(currentParticipantId);

                EventListViewModel viewModel = GetEventListViewModel(createdEvents);

                return PartialView("_ContainerPartial", viewModel);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        public IActionResult PastEvents()
        {
            try
            {
                var currentParticipantId = GetCurrentParticipantId();

                IEnumerable<EventOrganized> pastEvents = eventService
                            .GetPastEventsOfUser(currentParticipantId);

                EventListViewModel viewModel = GetEventListViewModel(pastEvents);

                return PartialView("_PastAttendedPartial", viewModel);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        public IActionResult Rate(string eventId, string score)
        {
            if (!ModelState.IsValid || eventId is null || score is null)
            {
                return PartialView("_PastAttendedPartial", new EventViewModel());
            }

            try
            {
                Guid.TryParse(eventId, out Guid eventGuid);
                var currentParticipantId = GetCurrentParticipantId();

                var oldRating = eventService.GetUserRating(eventGuid, currentParticipantId);

                if (oldRating != null)
                {
                    eventService.RemoveInteraction(oldRating);
                }

                eventService.AddRating(currentParticipantId, eventGuid, score);
                return PastEvents();
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetApplications([FromRoute] Guid Id)
        {
            string creatorId = string.Empty;
            if (User.Identity.IsAuthenticated)
            {
                var userId = userManager.GetUserId(User);
                var user = userService.GetParticipantByUserId(userId);
                creatorId = user.Id.ToString();
            }

            var viewModel = new GetApplicationsViewModel()
            {
                CreatorId = creatorId,
                Event = eventService.GetEventById(Id),
                Applications = eventService.GetEventApplications(Id)
            };

            return PartialView("_ApproveApplicationPartial", viewModel);
        }

        [HttpGet]
        public IActionResult NewEvent()
        {
            return PartialView("_AddEventPartial", new AddEventViewModel());
        }

        [HttpPost]
        public IActionResult NewEvent([FromForm]AddEventViewModel eventData)
        {
            if (!ModelState.IsValid || eventData is null)
            {
                return PartialView("_AddEventPartial", eventData);
            }

            try
            {
                string cityLongName = HelperService.GetCityFromAddress(eventData.Location).Result;

                string resultImage = string.Empty;
                if (eventData.EventImage != null)
                {
                    resultImage = HelperService.CompressImage(eventData.EventImage);
                }

                var currentParticipantId = GetCurrentParticipantId();

                EventDetails details = EventDetails.Create(eventData.EventDescription,
                                        eventData.Location,
                                        eventData.CityLat,
                                        eventData.CityLong,
                                        cityLongName,
                                        eventData.Deadline,
                                        eventData.OccurenceDate,
                                        eventData.MaximumParticipants,
                                        eventData.ParticipationFee,
                                        eventData.ApplicationRequired);

                var createdEvent = userService.AddEvent(currentParticipantId,
                                       eventData.Title,
                                       eventData.Category,
                                       resultImage,
                                       details);

                return PartialView("_AddEventPartial", eventData);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [AllowAnonymous]
        public IActionResult Details([FromRoute] string Id)
        {
            try
            {
                Guid.TryParse(Id, out Guid eventGuid);
                var eventToGo = eventService.GetEventById(eventGuid);
                var eventViewModel = GetEventViewModel(eventToGo);

                var comments = eventService.GetEventComments(eventToGo.Id);
                foreach (var comment in comments)
                {
                    comment.Timestamp = comment.Timestamp.ToLocalTime();
                }

                if (User.Identity.IsAuthenticated)
                {
                    var currentParticipantId = GetCurrentParticipantId();
                    eventService.RegisterClick(eventGuid, currentParticipantId);
                }
                
                var detailsViewModel = new DetailsViewModel()
                {
                    Id = eventViewModel.Id,
                    Title = eventViewModel.Title,
                    Image = eventViewModel.Image,
                    HostName = eventViewModel.HostName,
                    HostEmail = eventViewModel.HostEmail,
                    HostPhoneNo = eventViewModel.HostPhoneNo,
                    Location = eventViewModel.Location,
                    ParticipationFee = eventViewModel.ParticipationFee,
                    Deadline = eventViewModel.Deadline,
                    OccurenceDate = eventViewModel.OccurenceDate,
                    OccurenceTime = eventViewModel.OccurenceTime,
                    Description = eventViewModel.Description,
                    MaximumParticipants = eventViewModel.MaximumParticipants,
                    Category = eventViewModel.Category,
                    HostProfileImage = eventViewModel.HostProfileImage,
                    UserName = eventViewModel.UserName,
                    UserProfileImage = eventViewModel.UserProfileImage,
                    Comments = comments
                };

                return PartialView("_DetailsPartial", detailsViewModel);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        public IActionResult Follow(string id)
        {
            try
            {
                Guid.TryParse(id, out Guid eventGuid);
                var eventToFollow = eventService.GetEventById(eventGuid);

                var currentParticipantId = GetCurrentParticipantId();

                var previousFollowing = eventService.GetFollowing(eventToFollow.Id, currentParticipantId);

                if (previousFollowing is null)
                {
                    eventService.FollowEvent(eventToFollow.Id, currentParticipantId);
                }
                else
                {
                    eventService.RemoveInteraction(previousFollowing);
                }

                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [HttpGet]
        public IActionResult Apply([FromRoute]string id)
        {
            try
            {
                Guid.TryParse(id, out Guid eventGuid);

                var currentParticipantId = GetCurrentParticipantId();

                var eventAppliedTo = eventService.GetEventById(eventGuid);
                bool applied = false;

                var eventApplied = eventService.GetApplication(eventGuid, currentParticipantId);
                var applicationText = eventService.GetApplicationText(eventGuid, currentParticipantId);

                if (eventApplied != null)
                {
                    applied = true;
                }

                var applyViewModel = new ApplyToEventViewModel()
                {
                    EventId = id,
                    EventName = eventAppliedTo.Title,
                    AlreadyApplied = applied,
                    ApplicationRequired = eventAppliedTo.EventDetails.ApplicationRequired,
                    ApplicationText = applicationText
                };

                return PartialView("_ApplyToEventPartial", applyViewModel);
            } 
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPost]
        public IActionResult Apply(ApplyToEventViewModel applyViewModel)
        {
            try
            {
                Guid.TryParse(applyViewModel.EventId, out Guid eventGuid);
                var eventToApply = eventService.GetEventById(eventGuid);
                
                var currentParticipantId = GetCurrentParticipantId();

                var eventId = eventToApply.Id;

                var previousApplication = eventService.GetApplication(eventId, currentParticipantId);
                var previousFollowing = eventService.GetFollowing(eventId, currentParticipantId);

                if (previousFollowing is null && previousApplication is null)
                {
                    eventService.FollowEvent(eventId, currentParticipantId);
                }

                if (previousApplication is null)
                {
                    eventService.ApplyToEvent(eventId, currentParticipantId, applyViewModel.ApplicationText);
                }
                else
                {
                    eventService.RemoveInteraction(previousApplication);
                }

                return PartialView("_ApplyToEventPartial", applyViewModel);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [HttpGet]
        public IActionResult Edit([FromRoute]string id)
        {
            try
            {
                Guid.TryParse(id, out Guid eventGuid);
                var eventToUpdate = eventService.GetEventById(eventGuid);

                var editEventViewModel = new ModifyEventViewModel()
                {
                    Id = id,
                    Title = eventToUpdate.Title,
                    Category = eventToUpdate.Category,
                    Deadline = eventToUpdate.EventDetails.Deadline,
                    OccurenceDate = eventToUpdate.EventDetails.OccurenceDate,
                    Location = eventToUpdate.EventDetails.Location,
                    EventDescription = eventToUpdate.EventDetails.Description,
                    MaximumParticipants = eventToUpdate.EventDetails.MaximumParticipantNo,
                    ParticipationFee = eventToUpdate.EventDetails.ParticipationFee,
                    ApplicationRequired = eventToUpdate.EventDetails.ApplicationRequired,
                    CityLat = (double)eventToUpdate.EventDetails.Latitude,
                    CityLong = (double)eventToUpdate.EventDetails.Longitude
                };

                return PartialView("_ModifyEventPartial", editEventViewModel);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [HttpPost]
        public IActionResult Edit([FromForm] ModifyEventViewModel updatedData)
        {
            if (!ModelState.IsValid)
            {
                return PartialView("_ModifyEventPartial", new ModifyEventViewModel());
            }

            try
            {
                Guid.TryParse(updatedData.Id, out Guid eventGuid);
                var eventToUpdate = eventService.GetEventById(eventGuid);
                string cityLongName = HelperService.GetCityFromAddress(updatedData.Location).Result;

                string image = string.Empty;
                if (updatedData.EventImage != null)
                {
                    image = HelperService.CompressImage(updatedData.EventImage);
                }

                eventService.UpdateEvent(eventToUpdate.Id,
                                        updatedData.Title,
                                        updatedData.Category,
                                        updatedData.EventDescription,
                                        updatedData.Location,
                                        updatedData.CityLat,
                                        updatedData.CityLong,
                                        cityLongName,
                                        updatedData.Deadline,
                                        updatedData.OccurenceDate,
                                        image,
                                        updatedData.MaximumParticipants,
                                        updatedData.ParticipationFee,
                                        updatedData.ApplicationRequired);

                return PartialView("_ModifyEventPartial", updatedData);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        public IActionResult RemoveComment(string Id)
        {
            var comment = eventService.GetCommentById(Id);
            eventService.RemoveInteraction(comment);

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Remove([FromRoute]string Id)
        {
            Guid.TryParse(Id, out Guid eventGuid);
            var eventToDelete = eventService.GetEventById(eventGuid);

            RemoveEventViewModel removeViewModel = new RemoveEventViewModel()
            {
                Id = Id,
                EventName = eventToDelete.Title
            };

            return PartialView("_RemoveEventPartial", removeViewModel);
        }

        [HttpPost]
        public IActionResult Remove(RemoveEventViewModel removeData)
        {
            try
            {
                eventService.RemoveEvent(removeData.Id);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }

            return PartialView("_RemoveEventPartial", removeData);
        }
        
        [HttpPost]
        public IActionResult AddComment([FromForm] DetailsViewModel viewModel)
        {
            if (!ModelState.IsValid || viewModel is null || string.IsNullOrWhiteSpace(viewModel.NewCommentMessage))
            {
                return PartialView("_DetailsPartial", viewModel);
            }

            var currentParticipantId = GetCurrentParticipantId();
            eventService.AddComment(currentParticipantId, viewModel.NewCommentEventId, viewModel.NewCommentMessage);
            
            return PartialView("_DetailsPartial", viewModel);
        }

        public void AcceptApplication([FromRoute] Guid Id)
        {
            eventService.AcceptApplication(Id);
        }

        public void RejectApplication([FromRoute] Guid Id)
        {
            eventService.RejectApplication(Id);
        }

        private EventViewModel GetEventViewModel(EventOrganized organizedEvent)
        {
            Participant hostingUser = userService.GetCreatorById(organizedEvent.CreatorId);
            string participationFee = eventService.FormatParticipationFee(organizedEvent.EventDetails.ParticipationFee);
            string maximumParticipants = eventService.FormatMaximumParticipants(organizedEvent.EventDetails.MaximumParticipantNo);
            string currentUsername = null;
            string currentProfileImage = null;
            var applicationStatus = EventApplication.ApplicationStatus.Pending.ToString();
            bool isPendingApplication = false;
            int? userScore = null;

            if (User.Identity.IsAuthenticated)
            {
                var userId = userManager.GetUserId(User);
                var currentUser = userService.GetParticipantByUserId(userId);
                currentUsername = eventService.FormatUserName(currentUser);
                currentProfileImage = currentUser.ProfileImage;
                userScore = eventService.GetUserRating(organizedEvent.Id, currentUser.Id)?.Score;
                var application = eventService.GetApplication(organizedEvent.Id, currentUser.Id);
                if (application != null)
                {
                    applicationStatus = application.Status.ToString();
                }

                var applications = eventService.GetEventApplications(organizedEvent.Id);
                foreach(var app in applications)
                {
                    if (app.Status.Equals(EventApplication.ApplicationStatus.Pending))
                    {
                        isPendingApplication = true;
                        break;
                    }
                }
            }

            var eventViewModel = new EventViewModel
            {
                Id = organizedEvent.Id,
                Title = organizedEvent.Title,
                Image = organizedEvent.ImageByteArray,
                HostName = eventService.FormatUserName(hostingUser),
                CreatorId = hostingUser.Id.ToString(),
                HostEmail = hostingUser.ContactDetails.Email,
                HostPhoneNo = hostingUser.ContactDetails.PhoneNo,
                HostProfileImage = hostingUser.ProfileImage,
                Location = organizedEvent.EventDetails.Location,
                ParticipationFee = participationFee,
                City = organizedEvent.EventDetails.City,
                Deadline = eventService.FormatEventDate(organizedEvent.EventDetails.Deadline),
                OccurenceDate = eventService.FormatEventDate(organizedEvent.EventDetails.OccurenceDate),
                OccurenceTime = eventService.FormatEventTime(organizedEvent.EventDetails.OccurenceDate),
                Description = organizedEvent.EventDetails.Description,
                MaximumParticipants = maximumParticipants,
                Category = organizedEvent.Category,
                UserName = currentUsername,
                UserProfileImage = currentProfileImage,
                Rating = userScore,
                ApplicationStatus = applicationStatus,
                IsPendingApplication = isPendingApplication
            };
            
            return eventViewModel;
        }

        private Guid GetCurrentParticipantId()
        {
            var userId = userManager.GetUserId(User);
            return userService.GetParticipantByUserId(userId).Id;
        }
    }
}