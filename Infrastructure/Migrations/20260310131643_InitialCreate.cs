using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExchangeRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    USD = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    AED = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    AFN = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    ALL = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    AMD = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    ANG = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    AOA = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    ARS = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    AUD = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    AWG = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    AZN = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    BAM = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    BBD = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    BDT = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    BGN = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    BHD = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    BIF = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    BMD = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    BND = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    BOB = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    BRL = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    BSD = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    BTN = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    BWP = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    BYN = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    BZD = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    CAD = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    CDF = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    CHF = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    CLF = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    CLP = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    CNH = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    CNY = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    COP = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    CRC = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    CUP = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    CVE = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    CZK = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    DJF = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    DKK = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    DOP = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    DZD = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    EGP = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    ERN = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    ETB = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    EUR = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    FJD = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    FKP = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    FOK = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    GBP = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    GEL = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    GGP = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    GHS = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    GIP = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    GMD = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    GNF = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    GTQ = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    GYD = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    HKD = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    HNL = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    HRK = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    HTG = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    HUF = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    IDR = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    ILS = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    IMP = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    INR = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    IQD = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    IRR = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    ISK = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    JEP = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    JMD = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    JOD = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    JPY = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    KES = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    KGS = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    KHR = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    KID = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    KMF = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    KRW = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    KWD = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    KYD = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    KZT = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    LAK = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    LBP = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    LKR = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    LRD = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    LSL = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    LYD = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    MAD = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    MDL = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    MGA = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    MKD = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    MMK = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    MNT = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    MOP = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    MRU = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    MUR = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    MVR = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    MWK = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    MXN = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    MYR = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    MZN = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    NAD = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    NGN = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    NIO = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    NOK = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    NPR = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    NZD = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    OMR = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    PAB = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    PEN = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    PGK = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    PHP = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    PKR = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    PLN = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    PYG = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    QAR = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    RON = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    RSD = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    RUB = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    RWF = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    SAR = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    SBD = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    SCR = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    SDG = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    SEK = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    SGD = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    SHP = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    SLE = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    SLL = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    SOS = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    SRD = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    SSP = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    STN = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    SYP = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    SZL = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    THB = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    TJS = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    TMT = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    TND = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    TOP = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    TRY = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    TTD = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    TVD = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    TWD = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    TZS = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    UAH = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    UGX = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    UYU = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    UZS = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    VES = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    VND = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    VUV = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    WST = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    XAF = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    XCD = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    XCG = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    XDR = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    XOF = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    XPF = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    YER = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    ZAR = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    ZMW = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    ZWG = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    ZWL = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TechSolutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechSolutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefreshTokenExpiry = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArabicName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    EnglishName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Website = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SafaKey = table.Column<int>(type: "int", nullable: true),
                    AccountType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ContractAttachment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApplicationForm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Stage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LeadSource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LeadStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExpectedRevenue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Companies_Users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ImportLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    SuccessRows = table.Column<int>(type: "int", nullable: false),
                    ErrorDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportLogs_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Activities_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Activities_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanyContacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    JobTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyContacts_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SaleOrderType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    OriginalCurrency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginalAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UsdRateAtTime = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    UsdAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Attachment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOrders_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesOrders_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesOrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SolutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesOrderItems_SalesOrders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "SalesOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesOrderItems_TechSolutions_SolutionId",
                        column: x => x.SolutionId,
                        principalTable: "TechSolutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_CompanyId",
                table: "Activities",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Activities_CreatedByUserId",
                table: "Activities",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_AssignedToUserId",
                table: "Companies",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyContacts_CompanyId",
                table: "CompanyContacts",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_Date",
                table: "ExchangeRates",
                column: "Date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportLogs_UploadedByUserId",
                table: "ImportLogs",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderItems_SalesOrderId",
                table: "SalesOrderItems",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderItems_SolutionId",
                table: "SalesOrderItems",
                column: "SolutionId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CompanyId",
                table: "SalesOrders",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CreatedByUserId",
                table: "SalesOrders",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_InvoiceNumber",
                table: "SalesOrders",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Activities");

            migrationBuilder.DropTable(
                name: "CompanyContacts");

            migrationBuilder.DropTable(
                name: "ExchangeRates");

            migrationBuilder.DropTable(
                name: "ImportLogs");

            migrationBuilder.DropTable(
                name: "SalesOrderItems");

            migrationBuilder.DropTable(
                name: "SalesOrders");

            migrationBuilder.DropTable(
                name: "TechSolutions");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
