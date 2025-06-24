using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RamaExpress.Migrations
{
    /// <inheritdoc />
    public partial class AddPelatihanTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pelatihan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Judul = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Deskripsi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DurasiMenit = table.Column<int>(type: "int", nullable: false),
                    SkorMinimal = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pelatihan", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PelatihanHasil",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PelatihanId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Skor = table.Column<int>(type: "int", nullable: false),
                    IsLulus = table.Column<bool>(type: "bit", nullable: false),
                    TanggalSelesai = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PelatihanHasil", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PelatihanHasil_Pelatihan_PelatihanId",
                        column: x => x.PelatihanId,
                        principalTable: "Pelatihan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PelatihanHasil_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PelatihanMateri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PelatihanId = table.Column<int>(type: "int", nullable: false),
                    Judul = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TipeKonten = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Konten = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Urutan = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PelatihanMateri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PelatihanMateri_Pelatihan_PelatihanId",
                        column: x => x.PelatihanId,
                        principalTable: "Pelatihan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PelatihanPosisi",
                columns: table => new
                {
                    PelatihanId = table.Column<int>(type: "int", nullable: false),
                    PosisiId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PelatihanPosisi", x => new { x.PelatihanId, x.PosisiId });
                    table.ForeignKey(
                        name: "FK_PelatihanPosisi_Pelatihan_PelatihanId",
                        column: x => x.PelatihanId,
                        principalTable: "Pelatihan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PelatihanPosisi_Posisi_PosisiId",
                        column: x => x.PosisiId,
                        principalTable: "Posisi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PelatihanProgress",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PelatihanId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    MateriTerakhirId = table.Column<int>(type: "int", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PelatihanProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PelatihanProgress_Pelatihan_PelatihanId",
                        column: x => x.PelatihanId,
                        principalTable: "Pelatihan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PelatihanProgress_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PelatihanSoal",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PelatihanId = table.Column<int>(type: "int", nullable: false),
                    Pertanyaan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OpsiA = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OpsiB = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OpsiC = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OpsiD = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JawabanBenar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Urutan = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PelatihanSoal", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PelatihanSoal_Pelatihan_PelatihanId",
                        column: x => x.PelatihanId,
                        principalTable: "Pelatihan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sertifikat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PelatihanId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    NomorSertifikat = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TanggalTerbit = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TanggalKadaluarsa = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sertifikat", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sertifikat_Pelatihan_PelatihanId",
                        column: x => x.PelatihanId,
                        principalTable: "Pelatihan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sertifikat_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PelatihanHasil_PelatihanId",
                table: "PelatihanHasil",
                column: "PelatihanId");

            migrationBuilder.CreateIndex(
                name: "IX_PelatihanHasil_UserId",
                table: "PelatihanHasil",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PelatihanMateri_PelatihanId",
                table: "PelatihanMateri",
                column: "PelatihanId");

            migrationBuilder.CreateIndex(
                name: "IX_PelatihanPosisi_PosisiId",
                table: "PelatihanPosisi",
                column: "PosisiId");

            migrationBuilder.CreateIndex(
                name: "IX_PelatihanProgress_PelatihanId",
                table: "PelatihanProgress",
                column: "PelatihanId");

            migrationBuilder.CreateIndex(
                name: "IX_PelatihanProgress_UserId",
                table: "PelatihanProgress",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PelatihanSoal_PelatihanId",
                table: "PelatihanSoal",
                column: "PelatihanId");

            migrationBuilder.CreateIndex(
                name: "IX_Sertifikat_PelatihanId",
                table: "Sertifikat",
                column: "PelatihanId");

            migrationBuilder.CreateIndex(
                name: "IX_Sertifikat_UserId",
                table: "Sertifikat",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PelatihanHasil");

            migrationBuilder.DropTable(
                name: "PelatihanMateri");

            migrationBuilder.DropTable(
                name: "PelatihanPosisi");

            migrationBuilder.DropTable(
                name: "PelatihanProgress");

            migrationBuilder.DropTable(
                name: "PelatihanSoal");

            migrationBuilder.DropTable(
                name: "Sertifikat");

            migrationBuilder.DropTable(
                name: "Pelatihan");
        }
    }
}
