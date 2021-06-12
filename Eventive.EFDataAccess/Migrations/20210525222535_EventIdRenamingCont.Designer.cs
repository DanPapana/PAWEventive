﻿// <auto-generated />
using System;
using Eventive.EFDataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Eventive.EFDataAccess.Migrations
{
    [DbContext(typeof(EventManagerDbContext))]
    [Migration("20210525222535_EventIdRenamingCont")]
    partial class EventIdRenamingCont
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.7")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Eventive.ApplicationLogic.DataModel.Application", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ApplicationText")
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("EventOrganizedId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ParticipantId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("EventOrganizedId");

                    b.ToTable("Applications");
                });

            modelBuilder.Entity("Eventive.ApplicationLogic.DataModel.Comment", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("CommenterId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("EventOrganizedId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Message")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("CommenterId");

                    b.HasIndex("EventOrganizedId");

                    b.ToTable("Comments");
                });

            modelBuilder.Entity("Eventive.ApplicationLogic.DataModel.ContactDetails", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Address")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("City")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Country")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Email")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("LinkToSocialM")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PhoneNo")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("ContactDetails");
                });

            modelBuilder.Entity("Eventive.ApplicationLogic.DataModel.EventDetails", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<bool>("ApplicationRequired")
                        .HasColumnType("bit");

                    b.Property<DateTime>("Deadline")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Location")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("MaximumParticipantNo")
                        .HasColumnType("int");

                    b.Property<DateTime>("OccurenceDate")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("ParticipationFee")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("Id");

                    b.ToTable("EventDetails");
                });

            modelBuilder.Entity("Eventive.ApplicationLogic.DataModel.EventOrganized", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Category")
                        .HasColumnType("int");

                    b.Property<Guid>("CreatorId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("EventDetailsId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ImageByteArray")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Title")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("EventDetailsId");

                    b.ToTable("Events");
                });

            modelBuilder.Entity("Eventive.ApplicationLogic.DataModel.Interaction", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("EventOrganizedId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ParticipantId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("datetime2");

                    b.Property<int>("UserParticipationType")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("EventOrganizedId");

                    b.ToTable("Interactions");
                });

            modelBuilder.Entity("Eventive.ApplicationLogic.DataModel.Participant", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("ContactDetailsId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("DateOfBirth")
                        .HasColumnType("datetime2");

                    b.Property<string>("FirstName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("LastName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ProfileImage")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SocialId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ContactDetailsId");

                    b.ToTable("Participants");
                });

            modelBuilder.Entity("Eventive.ApplicationLogic.DataModel.Rating", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("EventId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ParticipantId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Score")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("ParticipantId");

                    b.ToTable("Ratings");
                });

            modelBuilder.Entity("Eventive.ApplicationLogic.DataModel.Application", b =>
                {
                    b.HasOne("Eventive.ApplicationLogic.DataModel.EventOrganized", null)
                        .WithMany("Applications")
                        .HasForeignKey("EventOrganizedId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Eventive.ApplicationLogic.DataModel.Comment", b =>
                {
                    b.HasOne("Eventive.ApplicationLogic.DataModel.Participant", "Commenter")
                        .WithMany()
                        .HasForeignKey("CommenterId");

                    b.HasOne("Eventive.ApplicationLogic.DataModel.EventOrganized", null)
                        .WithMany("Comments")
                        .HasForeignKey("EventOrganizedId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Eventive.ApplicationLogic.DataModel.EventOrganized", b =>
                {
                    b.HasOne("Eventive.ApplicationLogic.DataModel.EventDetails", "EventDetails")
                        .WithMany()
                        .HasForeignKey("EventDetailsId");
                });

            modelBuilder.Entity("Eventive.ApplicationLogic.DataModel.Interaction", b =>
                {
                    b.HasOne("Eventive.ApplicationLogic.DataModel.EventOrganized", null)
                        .WithMany("Interactions")
                        .HasForeignKey("EventOrganizedId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Eventive.ApplicationLogic.DataModel.Participant", b =>
                {
                    b.HasOne("Eventive.ApplicationLogic.DataModel.ContactDetails", "ContactDetails")
                        .WithMany()
                        .HasForeignKey("ContactDetailsId");
                });

            modelBuilder.Entity("Eventive.ApplicationLogic.DataModel.Rating", b =>
                {
                    b.HasOne("Eventive.ApplicationLogic.DataModel.Participant", null)
                        .WithMany("Ratings")
                        .HasForeignKey("ParticipantId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}