﻿using Microsoft.EntityFrameworkCore;
using RamaExpress.Areas.Admin.Data.Service;
using RamaExpress.Areas.Admin.Data;
using RamaExpress.Areas.Admin.Models;

namespace RamaExpress.Areas.Admin.Data
{
    public class RamaExpressAppContext : DbContext
    {
        public RamaExpressAppContext(DbContextOptions<RamaExpressAppContext> options) : base(options) { }

        public DbSet<User> User { get; set; }
        public DbSet<Posisi> Posisi { get; set; }
        public DbSet<Pelatihan> Pelatihan { get; set; }
        public DbSet<PelatihanPosisi> PelatihanPosisi { get; set; }
        public DbSet<PelatihanMateri> PelatihanMateri { get; set; }
        public DbSet<PelatihanSoal> PelatihanSoal { get; set; }
        public DbSet<PelatihanProgress> PelatihanProgress { get; set; }
        public DbSet<PelatihanHasil> PelatihanHasil { get; set; }
        public DbSet<PelatihanSertifikat> PelatihanSertifikat { get; set; }
        public DbSet<Sertifikat> Sertifikat { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PelatihanPosisi>()
                .HasKey(pp => new { pp.PelatihanId, pp.PosisiId });

            modelBuilder.Entity<PelatihanPosisi>()
                .HasOne(pp => pp.Pelatihan)
                .WithMany(p => p.PelatihanPosisis)
                .HasForeignKey(pp => pp.PelatihanId);

            modelBuilder.Entity<PelatihanPosisi>()
                .HasOne(pp => pp.Posisi)
                .WithMany(p => p.PelatihanPosisis)
                .HasForeignKey(pp => pp.PosisiId);

            modelBuilder.Entity<PelatihanMateri>()
                .HasOne(pm => pm.Pelatihan)
                .WithMany(p => p.PelatihanMateris)
                .HasForeignKey(pm => pm.PelatihanId);

            modelBuilder.Entity<PelatihanSoal>()
                .HasOne(ps => ps.Pelatihan)
                .WithMany(p => p.PelatihanSoals)
                .HasForeignKey(ps => ps.PelatihanId);

            modelBuilder.Entity<PelatihanProgress>()
                .HasOne(pp => pp.Pelatihan)
                .WithMany(p => p.PelatihanProgresses)
                .HasForeignKey(pp => pp.PelatihanId);

            modelBuilder.Entity<PelatihanProgress>()
                .HasOne(pp => pp.User)
                .WithMany()
                .HasForeignKey(pp => pp.UserId);

            modelBuilder.Entity<PelatihanHasil>()
                .HasOne(ph => ph.Pelatihan)
                .WithMany(p => p.PelatihanHasils)
                .HasForeignKey(ph => ph.PelatihanId);

            modelBuilder.Entity<PelatihanHasil>()
                .HasOne(ph => ph.User)
                .WithMany()
                .HasForeignKey(ph => ph.UserId);
        }
    }
}