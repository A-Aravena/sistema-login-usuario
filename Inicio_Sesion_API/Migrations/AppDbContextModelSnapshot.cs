﻿// <auto-generated />
using System;
using Inicio_Sesion_API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Inicio_Sesion_API.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("Inicio_Sesion_API.Models.Usuario", b =>
                {
                    b.Property<int>("usuario_id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("created_at")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("updated_at")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("usuario_apellidos")
                        .HasColumnType("longtext");

                    b.Property<string>("usuario_email")
                        .HasColumnType("longtext");

                    b.Property<string>("usuario_fono")
                        .HasColumnType("longtext");

                    b.Property<string>("usuario_nombres")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("usuario_password")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("usuario_rut")
                        .HasColumnType("longtext");

                    b.Property<string>("usuario_url")
                        .HasColumnType("longtext");

                    b.Property<string>("usuario_username")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("usuario_id");

                    b.ToTable("Usuario");
                });
#pragma warning restore 612, 618
        }
    }
}
