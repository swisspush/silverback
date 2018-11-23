﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SilverbackShop.Baskets.Infrastructure;

namespace SilverbackShop.Baskets.Infrastructure.Migrations
{
    [DbContext(typeof(BasketsDbContext))]
    [Migration("20181112070712_InitialCreate")]
    partial class InitialCreate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.4-rtm-31024")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("SilverbackShop.Baskets.Domain.Model.Basket", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime?>("CheckoutDate");

                    b.Property<DateTime>("Created");

                    b.Property<Guid>("UserId");

                    b.HasKey("Id");

                    b.ToTable("Baskets");
                });

            modelBuilder.Entity("SilverbackShop.Baskets.Domain.Model.BasketItem", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int?>("BasketId");

                    b.Property<string>("Name");

                    b.Property<int>("Quantity");

                    b.Property<string>("SKU");

                    b.Property<decimal>("UnitPrice");

                    b.HasKey("Id");

                    b.HasIndex("BasketId");

                    b.ToTable("BasketItems");
                });

            modelBuilder.Entity("SilverbackShop.Baskets.Domain.Model.InventoryItem", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("SKU");

                    b.Property<int>("StockQuantity");

                    b.HasKey("Id");

                    b.HasIndex("SKU")
                        .IsUnique()
                        .HasFilter("[SKU] IS NOT NULL");

                    b.ToTable("InventoryItems");
                });

            modelBuilder.Entity("SilverbackShop.Baskets.Domain.Model.Product", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("DisplayName")
                        .IsRequired()
                        .HasMaxLength(300);

                    b.Property<string>("SKU")
                        .IsRequired()
                        .HasMaxLength(100);

                    b.Property<decimal>("UnitPrice");

                    b.HasKey("Id");

                    b.ToTable("Products");
                });

            modelBuilder.Entity("SilverbackShop.Baskets.Domain.Model.BasketItem", b =>
                {
                    b.HasOne("SilverbackShop.Baskets.Domain.Model.Basket")
                        .WithMany("Items")
                        .HasForeignKey("BasketId");
                });
#pragma warning restore 612, 618
        }
    }
}
