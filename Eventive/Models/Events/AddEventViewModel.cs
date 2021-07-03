﻿using Eventive.Models.Validations;
using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;
using static Eventive.ApplicationLogic.DataModel.EventOrganized;

namespace Eventive.Models.Events
{
    public class AddEventViewModel
    {
        public double CityLat { get; set; }
        public double CityLong { get; set; }

        [Required]
        [EnforceTrue]
        public bool IsLocationValid { get; set; } = false;

        [Required(ErrorMessage = "There needs to a title!")]
        [Display(Name = "Title")]
        public string Title { get; set; } = "";

        [Required(ErrorMessage = "A category must be specified")]
        [Display(Name = "Category")]
        public EventCategory Category { get; set; }

        [Required(ErrorMessage = "Deadline is required")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        [Display(Name = "Application Deadline")]
        public DateTime Deadline { get; set; }

        [Required(ErrorMessage = "Occurence date is required")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        [Display(Name = "Event Date")]
        public DateTime OccurenceDate { get; set; }

        [Required(ErrorMessage = "You don't want them stranded!")]
        [Display(Name = "Location")]
        public string Location { get; set; } = "";

        [Display(Name = "Image for the event")]
        public IFormFile EventImage { get; set; }

        [Display(Name = "Maximum number of participants")]
        public int MaximumParticipants { get; set; } = 0;

        [Display(Name = "Attendance fee")]
        public decimal ParticipationFee { get; set; } = 0;

        [Display(Name = "Event description")]
        public string EventDescription { get; set; } = "";

        [Display(Name = "Application Required?")]
        public bool ApplicationRequired { get; set; } = false;
    }
}
