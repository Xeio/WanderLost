﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WanderLost.Server.Controllers;

#nullable disable

namespace WanderLost.Server.Migrations
{
    [DbContext(typeof(MerchantsDbContext))]
    partial class MerchantsDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("WanderLost.Server.Data.Ban", b =>
                {
                    b.Property<string>("ClientId")
                        .HasMaxLength(60)
                        .HasColumnType("nvarchar(60)");

                    b.Property<DateTimeOffset>("ExpiresAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("UserId")
                        .HasMaxLength(60)
                        .HasColumnType("nvarchar(60)");

                    b.HasKey("ClientId", "ExpiresAt");

                    b.ToTable("Bans");
                });

            modelBuilder.Entity("WanderLost.Server.Data.SentPushNotification", b =>
                {
                    b.Property<Guid>("MerchantId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("SubscriptionId")
                        .HasColumnType("int");

                    b.HasKey("MerchantId", "SubscriptionId");

                    b.HasIndex("SubscriptionId");

                    b.ToTable("SentPushNotifications");
                });

            modelBuilder.Entity("WanderLost.Shared.Data.ActiveMerchant", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<int?>("ActiveMerchantGroupId")
                        .HasColumnType("int");

                    b.Property<bool>("Hidden")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<bool>("RequiresProcessing")
                        .HasColumnType("bit");

                    b.Property<string>("UploadedBy")
                        .IsRequired()
                        .HasMaxLength(60)
                        .HasColumnType("nvarchar(60)");

                    b.Property<string>("UploadedByUserId")
                        .HasMaxLength(60)
                        .HasColumnType("nvarchar(60)");

                    b.Property<int>("Votes")
                        .HasColumnType("int");

                    b.Property<string>("Zone")
                        .IsRequired()
                        .HasMaxLength(40)
                        .HasColumnType("nvarchar(40)");

                    b.HasKey("Id");

                    b.HasIndex("ActiveMerchantGroupId");

                    b.HasIndex("RequiresProcessing")
                        .HasFilter("[RequiresProcessing] = 1");

                    b.HasIndex("UploadedBy");

                    b.HasIndex("UploadedByUserId");

                    b.ToTable("ActiveMerchants");
                });

            modelBuilder.Entity("WanderLost.Shared.Data.ActiveMerchantGroup", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<DateTimeOffset>("AppearanceExpires")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("MerchantName")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<DateTimeOffset>("NextAppearance")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("Server")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.HasKey("Id");

                    b.HasAlternateKey("Server", "MerchantName", "AppearanceExpires");

                    b.ToTable("MerchantGroups");
                });

            modelBuilder.Entity("WanderLost.Shared.Data.PushSubscription", b =>
                {
                    b.Property<string>("Token")
                        .HasColumnType("nvarchar(450)");

                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<DateTimeOffset>("LastModified")
                        .HasColumnType("datetimeoffset");

                    b.Property<bool>("LegendaryRapportNotify")
                        .HasColumnType("bit");

                    b.Property<int>("RapportVoteThreshold")
                        .HasColumnType("int");

                    b.Property<bool>("SendTestNotification")
                        .HasColumnType("bit");

                    b.Property<string>("Server")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<bool>("WeiNotify")
                        .HasColumnType("bit");

                    b.Property<int>("WeiVoteThreshold")
                        .HasColumnType("int");

                    b.HasKey("Token");

                    b.HasIndex("SendTestNotification")
                        .HasFilter("[SendTestNotification] = 1");

                    b.HasIndex("Server");

                    b.ToTable("PushSubscriptions");
                });

            modelBuilder.Entity("WanderLost.Shared.Data.Vote", b =>
                {
                    b.Property<Guid>("ActiveMerchantId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("ClientId")
                        .HasMaxLength(60)
                        .HasColumnType("nvarchar(60)");

                    b.Property<string>("UserId")
                        .HasMaxLength(60)
                        .HasColumnType("nvarchar(60)");

                    b.Property<int>("VoteType")
                        .HasColumnType("int");

                    b.HasKey("ActiveMerchantId", "ClientId");

                    b.ToTable("Votes");
                });

            modelBuilder.Entity("WanderLost.Server.Data.SentPushNotification", b =>
                {
                    b.HasOne("WanderLost.Shared.Data.ActiveMerchant", "Merchant")
                        .WithMany()
                        .HasForeignKey("MerchantId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("WanderLost.Shared.Data.PushSubscription", null)
                        .WithMany()
                        .HasForeignKey("SubscriptionId")
                        .HasPrincipalKey("Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Merchant");
                });

            modelBuilder.Entity("WanderLost.Shared.Data.ActiveMerchant", b =>
                {
                    b.HasOne("WanderLost.Shared.Data.ActiveMerchantGroup", null)
                        .WithMany("ActiveMerchants")
                        .HasForeignKey("ActiveMerchantGroupId");

                    b.OwnsOne("WanderLost.Shared.Data.Item", "Card", b1 =>
                        {
                            b1.Property<Guid>("ActiveMerchantId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<string>("Name")
                                .IsRequired()
                                .HasMaxLength(40)
                                .HasColumnType("nvarchar(40)");

                            b1.Property<int>("Rarity")
                                .HasColumnType("int");

                            b1.HasKey("ActiveMerchantId");

                            b1.ToTable("ActiveMerchants");

                            b1.WithOwner()
                                .HasForeignKey("ActiveMerchantId");
                        });

                    b.OwnsOne("WanderLost.Shared.Data.Item", "Rapport", b1 =>
                        {
                            b1.Property<Guid>("ActiveMerchantId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<string>("Name")
                                .IsRequired()
                                .HasMaxLength(40)
                                .HasColumnType("nvarchar(40)");

                            b1.Property<int>("Rarity")
                                .HasColumnType("int");

                            b1.HasKey("ActiveMerchantId");

                            b1.ToTable("ActiveMerchants");

                            b1.WithOwner()
                                .HasForeignKey("ActiveMerchantId");
                        });

                    b.Navigation("Card")
                        .IsRequired();

                    b.Navigation("Rapport")
                        .IsRequired();
                });

            modelBuilder.Entity("WanderLost.Shared.Data.Vote", b =>
                {
                    b.HasOne("WanderLost.Shared.Data.ActiveMerchant", "ActiveMerchant")
                        .WithMany("ClientVotes")
                        .HasForeignKey("ActiveMerchantId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ActiveMerchant");
                });

            modelBuilder.Entity("WanderLost.Shared.Data.ActiveMerchant", b =>
                {
                    b.Navigation("ClientVotes");
                });

            modelBuilder.Entity("WanderLost.Shared.Data.ActiveMerchantGroup", b =>
                {
                    b.Navigation("ActiveMerchants");
                });
#pragma warning restore 612, 618
        }
    }
}
