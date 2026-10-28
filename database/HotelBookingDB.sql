/* =====================================================================
   RoyalStay - Hotel Room Booking System
   Database : HotelRoomBookingDB   (SQL Server / SQL Server Express)
   Intended instance : DESKTOP-VVQM1KOU\SQLEXPRESS

   Tables : dbo.Users  (1) --< (N) dbo.Bookings (N) >-- (1) dbo.Rooms
   Contents: schema, primary/foreign keys, unique + CHECK constraints,
             indexes, 3 sample rooms, 1 sample guest and 2 sample bookings.

   HOW TO RUN: open this file in SSMS, connect to your SQL Server, press F5.
   * Safe to run more than once: it never drops or renames anything.
     It creates the database/tables only if missing, adds a column only if
     it is missing, and inserts sample rows only if they do not exist.
   * No admin password is stored here. The admin account is created by the
     application on first start (see README.md, "Test login").
   * The column names come from the SQL used by the application code
     (the original project shipped without a SQL file).
   ===================================================================== */

IF DB_ID(N'HotelRoomBookingDB') IS NULL
    CREATE DATABASE HotelRoomBookingDB;
GO
USE HotelRoomBookingDB;
GO

/* ---------------------------- USERS ---------------------------------- */
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        UserId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
        FullName     NVARCHAR(200) NOT NULL,
        Email        NVARCHAR(150) NOT NULL,
        PasswordHash NVARCHAR(256) NOT NULL,
        Role         NVARCHAR(50)  NOT NULL CONSTRAINT DF_Users_Role DEFAULT (N'Guest'),
        CreatedAt    DATETIME2(0)  NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME())
    );
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Users_Role')
    ALTER TABLE dbo.Users ADD CONSTRAINT CK_Users_Role CHECK (Role IN (N'Admin', N'Guest'));
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Users_Email' AND object_id = OBJECT_ID(N'dbo.Users'))
    CREATE UNIQUE INDEX UX_Users_Email ON dbo.Users (Email);
GO

/* ---------------------------- ROOMS ---------------------------------- */
IF OBJECT_ID(N'dbo.Rooms', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Rooms
    (
        RoomId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Rooms PRIMARY KEY,
        RoomNumber  NVARCHAR(20)   NOT NULL,
        RoomType    NVARCHAR(100)  NOT NULL,
        Price       DECIMAL(10,2)  NOT NULL CONSTRAINT CK_Rooms_Price CHECK (Price > 0),
        Capacity    INT            NOT NULL CONSTRAINT DF_Rooms_Capacity DEFAULT (2),
        Description NVARCHAR(1000) NULL,
        ImageUrl    NVARCHAR(500)  NULL,
        IsAvailable BIT            NOT NULL CONSTRAINT DF_Rooms_IsAvailable DEFAULT (1)
    );
END
GO
-- Add any column the application needs but an older table might not have.
IF COL_LENGTH('dbo.Rooms', 'RoomType')    IS NULL ALTER TABLE dbo.Rooms ADD RoomType    NVARCHAR(100)  NOT NULL CONSTRAINT DF_Rooms_RoomType DEFAULT (N'Standard Room');
IF COL_LENGTH('dbo.Rooms', 'Capacity')    IS NULL ALTER TABLE dbo.Rooms ADD Capacity    INT            NOT NULL CONSTRAINT DF_Rooms_Capacity2 DEFAULT (2);
IF COL_LENGTH('dbo.Rooms', 'Description') IS NULL ALTER TABLE dbo.Rooms ADD Description NVARCHAR(1000) NULL;
IF COL_LENGTH('dbo.Rooms', 'ImageUrl')    IS NULL ALTER TABLE dbo.Rooms ADD ImageUrl    NVARCHAR(500)  NULL;
IF COL_LENGTH('dbo.Rooms', 'IsAvailable') IS NULL ALTER TABLE dbo.Rooms ADD IsAvailable BIT            NOT NULL CONSTRAINT DF_Rooms_IsAvailable2 DEFAULT (1);
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Rooms_Capacity')
    ALTER TABLE dbo.Rooms ADD CONSTRAINT CK_Rooms_Capacity CHECK (Capacity BETWEEN 1 AND 10);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Rooms_RoomNumber' AND object_id = OBJECT_ID(N'dbo.Rooms'))
    CREATE UNIQUE INDEX UX_Rooms_RoomNumber ON dbo.Rooms (RoomNumber);
GO

/* --------------------------- BOOKINGS -------------------------------- */
IF OBJECT_ID(N'dbo.Bookings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Bookings
    (
        BookingId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Bookings PRIMARY KEY,
        UserId      INT           NOT NULL CONSTRAINT FK_Bookings_Users REFERENCES dbo.Users (UserId),
        RoomId      INT           NOT NULL CONSTRAINT FK_Bookings_Rooms REFERENCES dbo.Rooms (RoomId),  -- no cascade: rooms with bookings cannot be deleted
        GuestName   NVARCHAR(200) NOT NULL,
        GuestEmail  NVARCHAR(150) NOT NULL,
        GuestPhone  NVARCHAR(20)  NULL,
        CheckIn     DATE          NOT NULL,
        CheckOut    DATE          NOT NULL,
        Guests      INT           NOT NULL,
        TotalAmount DECIMAL(18,2) NOT NULL,
        Status      NVARCHAR(30)  NOT NULL CONSTRAINT DF_Bookings_Status DEFAULT (N'Pending'),
        CreatedAt   DATETIME2(0)  NOT NULL CONSTRAINT DF_Bookings_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT CK_Bookings_Dates  CHECK (CheckOut > CheckIn),
        CONSTRAINT CK_Bookings_Guests CHECK (Guests >= 1),
        CONSTRAINT CK_Bookings_Status CHECK (Status IN (N'Pending', N'Confirmed', N'Cancelled', N'Completed'))
    );
END
GO
IF COL_LENGTH('dbo.Bookings', 'GuestPhone') IS NULL ALTER TABLE dbo.Bookings ADD GuestPhone NVARCHAR(20) NULL;
IF COL_LENGTH('dbo.Bookings', 'CreatedAt') IS NULL ALTER TABLE dbo.Bookings ADD CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Bookings_CreatedAt2 DEFAULT (SYSUTCDATETIME());
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Bookings_Room_Dates' AND object_id = OBJECT_ID(N'dbo.Bookings'))
    CREATE INDEX IX_Bookings_Room_Dates ON dbo.Bookings (RoomId, CheckIn, CheckOut) INCLUDE (Status);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Bookings_GuestEmail' AND object_id = OBJECT_ID(N'dbo.Bookings'))
    CREATE INDEX IX_Bookings_GuestEmail ON dbo.Bookings (GuestEmail);
GO

/* ------------------------- SAMPLE ROOMS ------------------------------ */
-- Three sample rooms (same as the original UI starter).
IF NOT EXISTS (SELECT 1 FROM dbo.Rooms WHERE RoomNumber = N'101')
    INSERT dbo.Rooms (RoomNumber, RoomType, Price, Capacity, Description, ImageUrl, IsAvailable)
    VALUES (N'101', N'Deluxe Room', 3000, 2, N'Spacious room with modern amenities and city views.',
            N'https://images.unsplash.com/photo-1611892440504-42a792e24d32?auto=format&fit=crop&w=1100&q=85', 1);
IF NOT EXISTS (SELECT 1 FROM dbo.Rooms WHERE RoomNumber = N'201')
    INSERT dbo.Rooms (RoomNumber, RoomType, Price, Capacity, Description, ImageUrl, IsAvailable)
    VALUES (N'201', N'Executive Room', 4000, 2, N'Extra space and work-friendly facilities for business travellers.',
            N'https://images.unsplash.com/photo-1566665797739-1674de7a421a?auto=format&fit=crop&w=1100&q=85', 1);
IF NOT EXISTS (SELECT 1 FROM dbo.Rooms WHERE RoomNumber = N'301')
    INSERT dbo.Rooms (RoomNumber, RoomType, Price, Capacity, Description, ImageUrl, IsAvailable)
    VALUES (N'301', N'Suite Room', 6000, 4, N'A luxurious suite with a separate living area and premium comforts.',
            N'https://images.unsplash.com/photo-1590490360182-c33d57733427?auto=format&fit=crop&w=1100&q=85', 1);
GO

/* ------------------- SAMPLE GUEST + SAMPLE BOOKINGS -------------------- */
-- The guest row uses a placeholder hash that can never be used to log in.
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = N'sample.guest@royalstay.test')
    INSERT dbo.Users (FullName, Email, PasswordHash, Role)
    VALUES (N'Sample Guest', N'sample.guest@royalstay.test', N'BOOKING_ONLY_NO_LOGIN', N'Guest');
GO
-- Dates are relative to today so the sample bookings are always in the future.
DECLARE @GuestId INT = (SELECT UserId FROM dbo.Users WHERE Email = N'sample.guest@royalstay.test');
DECLARE @Today DATE = CAST(SYSDATETIME() AS DATE);

IF NOT EXISTS (SELECT 1 FROM dbo.Bookings WHERE GuestEmail = N'sample.guest@royalstay.test' AND RoomId = (SELECT RoomId FROM dbo.Rooms WHERE RoomNumber = N'101'))
    INSERT dbo.Bookings (UserId, RoomId, GuestName, GuestEmail, GuestPhone, CheckIn, CheckOut, Guests, TotalAmount, Status)
    SELECT @GuestId, r.RoomId, N'Sample Guest', N'sample.guest@royalstay.test', N'9876543210',
           DATEADD(DAY, 7, @Today), DATEADD(DAY, 9, @Today), 2, 2 * r.Price, N'Confirmed'
    FROM dbo.Rooms r WHERE r.RoomNumber = N'101';

IF NOT EXISTS (SELECT 1 FROM dbo.Bookings WHERE GuestEmail = N'sample.guest@royalstay.test' AND RoomId = (SELECT RoomId FROM dbo.Rooms WHERE RoomNumber = N'201'))
    INSERT dbo.Bookings (UserId, RoomId, GuestName, GuestEmail, GuestPhone, CheckIn, CheckOut, Guests, TotalAmount, Status)
    SELECT @GuestId, r.RoomId, N'Sample Guest', N'sample.guest@royalstay.test', N'9876543210',
           DATEADD(DAY, 14, @Today), DATEADD(DAY, 17, @Today), 2, 3 * r.Price, N'Pending'
    FROM dbo.Rooms r WHERE r.RoomNumber = N'201';
GO

/* The Admin user is NOT inserted here (no password in SQL files).
   It is created by the web app on first start from the AdminSeed settings. */
PRINT 'HotelRoomBookingDB is ready.';
GO
