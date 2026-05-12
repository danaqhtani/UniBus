using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using UniBusApp.Models;

namespace UniBusApp.Data;

public partial class UniBusDbContext : DbContext
{
    public UniBusDbContext()
    {
    }

    public UniBusDbContext(DbContextOptions<UniBusDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Booking> Booking { get; set; }

    public virtual DbSet<Building> Building { get; set; }

    public virtual DbSet<Driver> Driver { get; set; }

    public virtual DbSet<EmailVerificationCode> EmailVerificationCode { get; set; }

    public virtual DbSet<Location> Location { get; set; }

    public virtual DbSet<MetroStation> MetroStation { get; set; }

    public virtual DbSet<OptimizedRoute> OptimizedRoute { get; set; }

    public virtual DbSet<PasswordReset> PasswordReset { get; set; }

    public virtual DbSet<ShuttleTrip> ShuttleTrip { get; set; }

    public virtual DbSet<Student> Student { get; set; }

    public virtual DbSet<TripDirection> Tripdirection { get; set; }

    public virtual DbSet<TripStop> TripStop { get; set; }

    public virtual DbSet<VwTripSummary> VwTripSummary { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.booking_id).HasName("PK__Booking__5DE3A5B1412F4750");

            entity.ToTable("Booking", tb => tb.HasTrigger("trg_CheckSeats"));

            entity.HasIndex(e => new { e.student_id, e.trip_id }, "UQ_Booking_Student_Trip").IsUnique();

            entity.Property(e => e.booking_id).HasColumnName("booking_id");
            entity.Property(e => e.booking_status)
                .HasMaxLength(30)
                .HasDefaultValue("Confirmed")
                .HasColumnName("booking_status");
            entity.Property(e => e.booking_time)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("booking_time");
            entity.Property(e => e.student_id).HasColumnName("student_id");
            entity.Property(e => e.trip_id).HasColumnName("trip_id");

            entity.HasOne(d => d.Student).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.student_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Booking_Student");

            entity.HasOne(d => d.Trip).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.trip_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Booking_Trip");
        });

        modelBuilder.Entity<Building>(entity =>
        {
            entity.HasKey(e => e.building_id).HasName("PK__Building__9C9FBF7F46F190CF");

            entity.ToTable("Building");

            entity.Property(e => e.building_id).HasColumnName("building_id");
            entity.Property(e => e.building_name)
                .HasMaxLength(100)
                .HasColumnName("building_name");
            entity.Property(e => e.latitude)
                .HasColumnType("decimal(9, 6)")
                .HasColumnName("latitude");
            entity.Property(e => e.longitude)
                .HasColumnType("decimal(9, 6)")
                .HasColumnName("longitude");
        });

        modelBuilder.Entity<Driver>(entity =>
        {
            entity.HasKey(e => e.driver_id).HasName("PK__Driver__A411C5BD7FCDE7C8");

            entity.ToTable("Driver");

            entity.HasIndex(e => e.login_id, "UQ_Driver_login_id").IsUnique();

            entity.Property(e => e.driver_id).HasColumnName("driver_id");
            entity.Property(e => e.bus_color)
                .HasMaxLength(30)
                .HasColumnName("bus_color");
            entity.Property(e => e.bus_plate)
                .HasMaxLength(20)
                .HasColumnName("bus_plate");
            entity.Property(e => e.driver_name)
                .HasMaxLength(100)
                .HasColumnName("driver_name");
            entity.Property(e => e.is_active)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.login_id).HasColumnName("login_id");
            entity.Property(e => e.phone_number)
                .HasMaxLength(15)
                .HasColumnName("phone_number");
        });

        modelBuilder.Entity<EmailVerificationCode>(entity =>
        {
            entity.HasKey(e => e.verification_id).HasName("PK__EmailVer__24F17969E788F582");

            entity.ToTable("EmailVerificationCode");

            entity.Property(e => e.verification_id).HasColumnName("verification_id");
            entity.Property(e => e.attempts).HasColumnName("attempts");
            entity.Property(e => e.code_hash)
                .HasMaxLength(200)
                .HasColumnName("code_hash");
            entity.Property(e => e.created_at)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.expires_at)
                .HasColumnType("datetime")
                .HasColumnName("expires_at");
            entity.Property(e => e.is_used).HasColumnName("is_used");
            entity.Property(e => e.student_id).HasColumnName("student_id");

            entity.HasOne(d => d.Student).WithMany(p => p.EmailVerificationCodes)
                .HasForeignKey(d => d.student_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmailVerificationCode_Student");
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasKey(e => e.location_id).HasName("PK__Location__771831EA978A897B");

            entity.ToTable("Location");

            entity.Property(e => e.location_id).HasColumnName("location_id");
            entity.Property(e => e.driver_id).HasColumnName("driver_id");
            entity.Property(e => e.latitude)
                .HasColumnType("decimal(9, 6)")
                .HasColumnName("latitude");
            entity.Property(e => e.longitude)
                .HasColumnType("decimal(9, 6)")
                .HasColumnName("longitude");
            entity.Property(e => e.timestamp)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("timestamp");
            entity.Property(e => e.trip_id).HasColumnName("trip_id");

            entity.HasOne(d => d.Driver).WithMany(p => p.Locations)
                .HasForeignKey(d => d.driver_id)
                .HasConstraintName("FK_Location_Driver");

            entity.HasOne(d => d.Trip).WithMany(p => p.Locations)
                .HasForeignKey(d => d.trip_id)
                .HasConstraintName("FK_Location_Trip");
        });

        modelBuilder.Entity<MetroStation>(entity =>
        {
            entity.HasKey(e => e.metro_id).HasName("PK__MetroSta__81674296A4424F33");

            entity.ToTable("MetroStation");

            entity.Property(e => e.metro_id).HasColumnName("metro_id");
            entity.Property(e => e.latitude)
                .HasColumnType("decimal(9, 6)")
                .HasColumnName("latitude");
            entity.Property(e => e.longitude)
                .HasColumnType("decimal(9, 6)")
                .HasColumnName("longitude");
            entity.Property(e => e.station_name)
                .HasMaxLength(100)
                .HasColumnName("station_name");
        });

        modelBuilder.Entity<OptimizedRoute>(entity =>
        {
            entity.HasKey(e => e.route_id).HasName("PK__Optimize__28F706FE6C8AE491");

            entity.ToTable("OptimizedRoute");

            entity.HasIndex(e => e.trip_id, "UQ_OptimizedRoute_Trip").IsUnique();

            entity.Property(e => e.route_id).HasColumnName("route_id");
            entity.Property(e => e.created_at)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.route_order).HasColumnName("route_order");
            entity.Property(e => e.total_distance).HasColumnName("total_distance");
            entity.Property(e => e.trip_id).HasColumnName("trip_id");

            entity.HasOne(d => d.Trip).WithOne(p => p.OptimizedRoute)
                .HasForeignKey<OptimizedRoute>(d => d.trip_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OptimizedRoute_Trip");
        });

        modelBuilder.Entity<PasswordReset>(entity =>
        {
            entity.HasKey(e => e.reset_id).HasName("PK__Password__40FB052084580E77");

            entity.ToTable("PasswordReset");

            entity.Property(e => e.reset_id).HasColumnName("reset_id");
            entity.Property(e => e.created_at)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.expires_at)
                .HasColumnType("datetime")
                .HasColumnName("expires_at");
            entity.Property(e => e.is_used).HasColumnName("is_used");
            entity.Property(e => e.student_id).HasColumnName("student_id");
            entity.Property(e => e.token_hash)
                .HasMaxLength(200)
                .HasColumnName("token_hash");

            entity.HasOne(d => d.Student).WithMany(p => p.PasswordResets)
                .HasForeignKey(d => d.student_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PasswordReset_Student");
        });

        modelBuilder.Entity<ShuttleTrip>(entity =>
        {
            entity.HasKey(e => e.trip_id).HasName("PK__ShuttleT__302A5D9E0227F6FF");

            entity.ToTable("ShuttleTrip");

            entity.Property(e => e.trip_id).HasColumnName("trip_id");
            entity.Property(e => e.arrival_time).HasColumnName("arrival_time");
            entity.Property(e => e.available_seats).HasColumnName("available_seats");
            entity.Property(e => e.departure_time).HasColumnName("departure_time");
            entity.Property(e => e.direction_id).HasColumnName("direction_id");
            entity.Property(e => e.driver_id).HasColumnName("driver_id");
            entity.Property(e => e.ended_at)
                .HasColumnType("datetime")
                .HasColumnName("ended_at");
            entity.Property(e => e.metro_id).HasColumnName("metro_id");
            entity.Property(e => e.started_at)
                .HasColumnType("datetime")
                .HasColumnName("started_at");
            entity.Property(e => e.status)
                .HasMaxLength(20)
                .HasDefaultValue("Scheduled")
                .HasColumnName("status");
            entity.Property(e => e.total_seats).HasColumnName("total_seats");
            entity.Property(e => e.trip_date).HasColumnName("trip_date");

            entity.HasOne(d => d.Direction).WithMany(p => p.ShuttleTrips)
                .HasForeignKey(d => d.direction_id)
                .HasConstraintName("FK_Trip_direction");

            entity.HasOne(d => d.Driver).WithMany(p => p.ShuttleTrips)
                .HasForeignKey(d => d.driver_id)
                .HasConstraintName("FK_ShuttleTrip_Driver");

            entity.HasOne(d => d.Metro).WithMany(p => p.ShuttleTrips)
                .HasForeignKey(d => d.metro_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShuttleTrip_Metro");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.student_id).HasName("PK__Student__2A33069A068C9C59");

            entity.ToTable("Student");

            entity.HasIndex(e => e.university_email, "UQ__Student__8446240E646F01C9").IsUnique();

            entity.Property(e => e.student_id).HasColumnName("student_id");
            entity.Property(e => e.building_id).HasColumnName("building_id");
            entity.Property(e => e.email_verified)
                .HasDefaultValue(false)
                .HasColumnName("email_verified");
            entity.Property(e => e.email_verified_at)
                .HasColumnType("datetime")
                .HasColumnName("email_verified_at");
            entity.Property(e => e.name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.password_hash)
                .HasMaxLength(64)
                .HasColumnName("password_hash");
            entity.Property(e => e.password_salt)
                .HasMaxLength(32)
                .HasColumnName("password_salt");
            entity.Property(e => e.phone_number)
                .HasMaxLength(15)
                .HasColumnName("phone_number");
            entity.Property(e => e.university_email)
                .HasMaxLength(100)
                .HasColumnName("university_email");

            entity.HasOne(d => d.Building).WithMany(p => p.Students)
                .HasForeignKey(d => d.building_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Student_Building");
        });

        modelBuilder.Entity<TripDirection>(entity =>
        {
            entity.HasKey(e => e.direction_id).HasName("PK__TripDire__FAAC47DB86288EAA");

            entity.ToTable("Tripdirection");

            entity.Property(e => e.direction_id)
                .ValueGeneratedNever()
                .HasColumnName("direction_id");
            entity.Property(e => e.direction_name)
                .HasMaxLength(50)
                .HasColumnName("direction_name");
        });

        modelBuilder.Entity<TripStop>(entity =>
        {
            entity.HasKey(e => e.stop_id).HasName("PK__TripStop__86FBE1820654D60A");

            entity.ToTable("TripStop");

            entity.HasIndex(e => new { e.trip_id, e.building_id }, "UQ_TripStop_Trip_Building").IsUnique();

            entity.HasIndex(e => new { e.trip_id, e.stop_order }, "UQ_TripStop_Trip_Order").IsUnique();

            entity.Property(e => e.stop_id).HasColumnName("stop_id");
            entity.Property(e => e.building_id).HasColumnName("building_id");
            entity.Property(e => e.stop_order).HasColumnName("stop_order");
            entity.Property(e => e.trip_id).HasColumnName("trip_id");

            entity.HasOne(d => d.Building).WithMany(p => p.TripStops)
                .HasForeignKey(d => d.building_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TripStop_Building");

            entity.HasOne(d => d.Trip).WithMany(p => p.TripStops)
                .HasForeignKey(d => d.trip_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TripStop_Trip");
        });

        modelBuilder.Entity<VwTripSummary>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_TripSummary");

            entity.Property(e => e.available_seats).HasColumnName("available_seats");
            entity.Property(e => e.booked_seats).HasColumnName("booked_seats");
            entity.Property(e => e.departure_time).HasColumnName("departure_time");

            entity.Property(e => e.direction_id).HasColumnName("direction_id"); entity.Property(e => e.driver_id).HasColumnName("driver_id");
            entity.Property(e => e.metro_id).HasColumnName("metro_id");
            entity.Property(e => e.status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.total_seats).HasColumnName("total_seats");
            entity.Property(e => e.trip_date).HasColumnName("trip_date");
            entity.Property(e => e.trip_id)
                .ValueGeneratedOnAdd()
                .HasColumnName("trip_id");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}