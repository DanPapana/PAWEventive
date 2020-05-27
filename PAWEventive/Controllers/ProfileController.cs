﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PAWEventive.ApplicationLogic.DataModel;
using PAWEventive.ApplicationLogic.Services;
using PAWEventive.Models.Events;
using PAWEventive.Models.Users;
using System;
using System.Collections.Generic;
using System.IO;

namespace PAWEventive.Controllers
{
    public class ProfileController : Controller
    {
        private readonly UserService userService;
        private readonly UserManager<IdentityUser> userManager;

        public ProfileController(UserManager<IdentityUser> userManager, UserService userService)
        {
            this.userManager = userManager;
            this.userService = userService;
        }


        public IActionResult Index()
        {
            return View();
        }

        public IActionResult GetProfile()
        {
            try
            {
                var thisUserId = userManager.GetUserId(User);
                User user = userService.GetUserByUserId(thisUserId);

                UserProfileViewModel viewModel = new UserProfileViewModel()
                {
                    Id = user.Id.ToString(),
                    DateOfBirth = $"{user.DateOfBirth: dd MMMM yyyy} 🎂",
                    FullName = $"{user.FirstName} {user.LastName}",
                    ProfileImage = user.ProfileImage,
                    Email = user.ContactDetails.Email,
                    CityCountry = $"{user.ContactDetails.City}, {user.ContactDetails.Country}",
                    PhoneNo = user.ContactDetails.PhoneNo,
                    LinkToSocialM = user.ContactDetails.LinkToSocialM
                };

                return PartialView("_ProfilePartial", viewModel);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }


        [HttpGet]
        public IActionResult EditProfile()
        {
            try
            {
                var thisUserId = userManager.GetUserId(User);
                User user = userService.GetUserByUserId(thisUserId);

                var editProfileViewModel = new EditProfileViewModel()
                {
                    Id = user.Id.ToString(),
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    City = user.ContactDetails.City,
                    Country = user.ContactDetails.Country,
                    Email = user.ContactDetails.Email,
                    PhoneNo = user.ContactDetails.PhoneNo,
                    LinkToSocialM = user.ContactDetails.LinkToSocialM
                };

                return PartialView("_EditProfilePartial", editProfileViewModel);

            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [HttpPost]
        public IActionResult EditProfile([FromForm] EditProfileViewModel updatedData)
        {
            if (!ModelState.IsValid)
            {
                return PartialView("_EditProfilePartial", new EditProfileViewModel());
            }

            try
            {
                string image = "";
                var thisUserId = userManager.GetUserId(User);
                User userToUpdate = userService.GetUserByUserId(thisUserId);

                if (updatedData.ProfileImage != null)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        updatedData.ProfileImage.CopyTo(memoryStream);
                        image = Convert.ToBase64String(memoryStream.ToArray());
                    }
                }

                userService.UpdateUser(userToUpdate.Id,
                                                updatedData.FirstName,
                                                updatedData.LastName,
                                                image,
                                                updatedData.Address,
                                                updatedData.City,
                                                updatedData.Country,
                                                updatedData.PhoneNo,
                                                updatedData.Email,
                                                updatedData.LinkToSocialM);

                return PartialView("_EditProfilePartial", updatedData);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }
    }
}