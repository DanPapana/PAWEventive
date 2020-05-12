﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using PAWEventive.ApplicationLogic.DataModel;
using PAWEventive.ApplicationLogic.Services;
using PAWEventive.Models.Events;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace PAWEventive.Controllers
{
    public class EventController : Controller
    {
        private readonly EventService eventService;
        private readonly UserService userService;
        private readonly UserManager<IdentityUser> userManager;
        
        public EventController(UserManager<IdentityUser> userManager, EventService eventService, UserService userService)
        {
            this.eventService = eventService;
            this.userService = userService;
            this.userManager = userManager;
        }

        public IActionResult Index()
        {
            try
            {
                List<EventViewModel> eventViewModels = new List<EventViewModel>();

                foreach (Event oneEvent in eventService.GetCurrentEvents())
                {
                    Event theEvent = eventService.GetEventById(oneEvent.Id);
                    User hostingUser = userService.GetCreatorByGuid(theEvent.CreatorId);
                    string participationFee = theEvent.EventDetails.ParticipationFee.ToString("#.##");

                    if (participationFee.Length > 0)
                    {
                        participationFee = "Fee: $" + participationFee;
                    } else
                    {
                        participationFee = "Free admission";
                    }

                    eventViewModels.Add(new EventViewModel
                    {
                        Title = theEvent.Title,
                        ImageByteArray = theEvent.ImageByteArray,
                        UserName = $"{hostingUser.FirstName} {hostingUser.LastName}",
                        UserEmail = hostingUser.ContactDetails.Email,
                        UserPhoneNo = hostingUser.ContactDetails.PhoneNo,
                        Location = theEvent.EventDetails.Location,
                        ParticipationFee = participationFee,
                        EventDeadline = theEvent.EventDetails.Deadline.ToString("MM/dd/yyyy H:mm"),
                        EventDescription = theEvent.EventDetails.Description,
                        EventMaximumParticipants = theEvent.EventDetails.MaximumParticipantNo,
                        Category = theEvent.Category
                    });
                }

                EventListViewModel viewModel = new EventListViewModel()
                {
                    EventViewModelList = eventViewModels    
                };

                return View(viewModel);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }



        [HttpGet]
        public IActionResult NewEvent()
        {
            return PartialView("_AddEventPartial", new NewEventViewModel());
        }

        [HttpPost]
        public IActionResult NewEvent([FromForm]NewEventViewModel eventData)
        {

            if (!ModelState.IsValid || eventData == null)
                //return PartialView("_AddEventPartial", eventData);
                return RedirectToAction("Index");

            try
            {
                var userId = userManager.GetUserId(User);
                var creatingUser = userService.GetUserByUserId(userId);

                EventDetails details = new EventDetails(eventData.EventDescription,
                                        eventData.Location, 
                                        DateTime.Parse(eventData.Deadline), 
                                        eventData.MaximumParticipants, 
                                        eventData.ParticipationFee);

                    userService.AddEvent(creatingUser.Id,
                                           eventData.Title,
                                           eventData.Category,
                                           eventData.EventImage,
                                           details);

                //return PartialView("_AddEventPartial", eventData);
                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }
    }
}