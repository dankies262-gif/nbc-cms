/* ============================================================================
   NBC Claims Management System (NBC-CMS)
   SQL Server schema for the claims workflow described in the SRS.

   Usage
   -----
   1. Run this script on a SQL Server instance (SQL Server 2019 or later).
   2. Set the DefaultConnection string in appsettings.json to the database.
   3. Start the web application; the first run seeds demo users and claims.

   The application also creates this schema automatically via EF Core
   (Database.EnsureCreated) when the database does not exist, so running this
   script is only required for environments that provision SQL by script.
   ============================================================================ */

IF DB_ID(N'NBC_CMS_Dev') IS NULL
BEGIN
    CREATE DATABASE NBC_CMS_Dev;
END
GO

USE NBC_CMS_Dev;
GO

/* ---------------------------------------------------------------- userRole */
IF OBJECT_ID(N'dbo.userRole', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.userRole
    (
        roleID      INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_userRole PRIMARY KEY,
        roleName    NVARCHAR(50)  NOT NULL,
        description NVARCHAR(255) NULL
    );

    CREATE UNIQUE INDEX IX_userRole_roleName ON dbo.userRole(roleName);
END
GO

/* ------------------------------------------------------------- claimStatus */
IF OBJECT_ID(N'dbo.claimStatus', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.claimStatus
    (
        statusID    INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_claimStatus PRIMARY KEY,
        statusName  NVARCHAR(100) NOT NULL,
        description NVARCHAR(255) NULL
    );

    CREATE UNIQUE INDEX IX_claimStatus_statusName ON dbo.claimStatus(statusName);
END
GO

/* ------------------------------------------------------------------- Users */
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        userID        INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Users PRIMARY KEY,
        username      NVARCHAR(150) NOT NULL,
        firstName     NVARCHAR(100) NOT NULL,
        lastName      NVARCHAR(100) NOT NULL,
        email         NVARCHAR(150) NOT NULL,
        contactNumber NVARCHAR(30)  NULL,
        roleID        INT           NOT NULL,
        isActive      BIT           NOT NULL CONSTRAINT DF_Users_isActive DEFAULT (1),
        createdAt     DATETIME2     NOT NULL CONSTRAINT DF_Users_createdAt DEFAULT (SYSDATETIME()),
        updatedAt     DATETIME2     NULL,
        updatedBy     INT           NULL,
        idNumber      NVARCHAR(50)  NULL,
        address       NVARCHAR(250) NULL,
        createdBy     INT           NULL,
        passwordHash  NVARCHAR(500) NULL,
        CONSTRAINT FK_Users_userRole_roleID
            FOREIGN KEY (roleID) REFERENCES dbo.userRole(roleID)
    );

    CREATE UNIQUE INDEX UX_Users_Username ON dbo.Users(username);
    CREATE UNIQUE INDEX IX_Users_email ON dbo.Users(email);
    CREATE INDEX IX_Users_roleID ON dbo.Users(roleID);
END
GO

/* --------------------------------------------------------------- programme */
IF OBJECT_ID(N'dbo.programme', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.programme
    (
        programmeID   INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_programme PRIMARY KEY,
        programmeName NVARCHAR(200) NOT NULL,
        description   NVARCHAR(500) NULL,
        isActive      BIT           NOT NULL CONSTRAINT DF_programme_isActive DEFAULT (1),
        createdAt     DATETIME2     NOT NULL CONSTRAINT DF_programme_createdAt DEFAULT (SYSDATETIME()),
        updatedAt     DATETIME2     NULL
    );

    CREATE UNIQUE INDEX IX_programme_programmeName ON dbo.programme(programmeName);
END
GO

/* ------------------------------------------------------------------- claim */
IF OBJECT_ID(N'dbo.claim', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.claim
    (
        claimID          INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_claim PRIMARY KEY,
        claimantID       INT           NOT NULL,
        statusID         INT           NOT NULL,
        claimDate        DATE          NOT NULL,
        submittedAt      DATETIME2     NULL,
        reviewedBy       INT           NULL,
        reviewedAt       DATETIME2     NULL,
        createdAt        DATETIME2     NOT NULL CONSTRAINT DF_claim_createdAt DEFAULT (SYSDATETIME()),
        updatedAt        DATETIME2     NULL,
        recordingDate    DATE          NULL,
        programmeTitle   NVARCHAR(500) NULL,
        feesAmount       DECIMAL(18,2) NULL,
        rejectionComment NVARCHAR(1000) NULL,
        CONSTRAINT FK_claim_Users_claimantID
            FOREIGN KEY (claimantID) REFERENCES dbo.Users(userID),
        CONSTRAINT FK_claim_Users_reviewedBy
            FOREIGN KEY (reviewedBy) REFERENCES dbo.Users(userID),
        CONSTRAINT FK_claim_claimStatus_statusID
            FOREIGN KEY (statusID) REFERENCES dbo.claimStatus(statusID)
    );

    CREATE INDEX IX_claim_claimantID ON dbo.claim(claimantID);
    CREATE INDEX IX_claim_statusID   ON dbo.claim(statusID);
    CREATE INDEX IX_claim_reviewedBy ON dbo.claim(reviewedBy);
END
GO

/* --------------------------------------------------------------- claimItem */
IF OBJECT_ID(N'dbo.claimItem', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.claimItem
    (
        claimItemID    INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_claimItem PRIMARY KEY,
        claimID        INT           NOT NULL,
        recordingDate  DATE          NOT NULL,
        programmeTitle NVARCHAR(250) NOT NULL,
        rate           DECIMAL(18,2) NOT NULL,
        feesAmount     DECIMAL(18,2) NOT NULL,
        createdAt      DATETIME2     NOT NULL CONSTRAINT DF_claimItem_createdAt DEFAULT (SYSDATETIME()),
        CONSTRAINT FK_claimItem_claim_claimID
            FOREIGN KEY (claimID) REFERENCES dbo.claim(claimID) ON DELETE CASCADE,
        CONSTRAINT CK_claimItem_feesAmount CHECK (feesAmount >= 0),
        CONSTRAINT CK_claimItem_rate CHECK (rate > 0)
    );

    CREATE INDEX IX_claimItem_claimID ON dbo.claimItem(claimID);
END
GO

/* --------------------------------------------------------- claimCorrection */
IF OBJECT_ID(N'dbo.claimCorrection', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.claimCorrection
    (
        correctionID      INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_claimCorrection PRIMARY KEY,
        claimID           INT            NOT NULL,
        requestedBy       INT            NOT NULL,
        correctionComment NVARCHAR(MAX)  NOT NULL,
        requestedAt       DATETIME2      NOT NULL CONSTRAINT DF_claimCorrection_requestedAt DEFAULT (SYSDATETIME()),
        correctedAt       DATETIME2      NULL,
        CONSTRAINT FK_claimCorrection_claim_claimID
            FOREIGN KEY (claimID) REFERENCES dbo.claim(claimID) ON DELETE CASCADE,
        CONSTRAINT FK_claimCorrection_Users_requestedBy
            FOREIGN KEY (requestedBy) REFERENCES dbo.Users(userID)
    );

    CREATE INDEX IX_claimCorrection_claimID     ON dbo.claimCorrection(claimID);
    CREATE INDEX IX_claimCorrection_requestedBy ON dbo.claimCorrection(requestedBy);
END
GO

/* ------------------------------------------------------- supportingDocument */
IF OBJECT_ID(N'dbo.supportingDocument', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.supportingDocument
    (
        documentID INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_supportingDocument PRIMARY KEY,
        claimID    INT           NOT NULL,
        uploadedBy INT           NOT NULL,
        fileName   NVARCHAR(255) NOT NULL,
        fileURL    NVARCHAR(500) NOT NULL,
        uploadedAt DATETIME2     NOT NULL CONSTRAINT DF_supportingDocument_uploadedAt DEFAULT (SYSDATETIME()),
        isActive   BIT           NOT NULL CONSTRAINT DF_supportingDocument_isActive DEFAULT (1),
        CONSTRAINT FK_supportingDocument_claim_claimID
            FOREIGN KEY (claimID) REFERENCES dbo.claim(claimID) ON DELETE CASCADE,
        CONSTRAINT FK_supportingDocument_Users_uploadedBy
            FOREIGN KEY (uploadedBy) REFERENCES dbo.Users(userID)
    );

    CREATE INDEX IX_supportingDocument_claimID    ON dbo.supportingDocument(claimID);
    CREATE INDEX IX_supportingDocument_uploadedBy ON dbo.supportingDocument(uploadedBy);
END
GO

/* ------------------------------------------------------------------ report */
IF OBJECT_ID(N'dbo.report', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.report
    (
        reportID     INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_report PRIMARY KEY,
        claimID      INT           NOT NULL,
        generatedBy  INT           NOT NULL,
        reportNumber NVARCHAR(50)  NOT NULL,
        fileName     NVARCHAR(255) NULL,
        fileURL      NVARCHAR(500) NULL,
        generatedAt  DATETIME2     NOT NULL CONSTRAINT DF_report_generatedAt DEFAULT (SYSDATETIME()),
        fileData     VARBINARY(MAX) NULL,
        CONSTRAINT FK_report_claim_claimID
            FOREIGN KEY (claimID) REFERENCES dbo.claim(claimID) ON DELETE CASCADE,
        CONSTRAINT FK_report_Users_generatedBy
            FOREIGN KEY (generatedBy) REFERENCES dbo.Users(userID)
    );

    CREATE UNIQUE INDEX IX_report_claimID      ON dbo.report(claimID);
    CREATE UNIQUE INDEX IX_report_reportNumber ON dbo.report(reportNumber);
    CREATE INDEX IX_report_generatedBy         ON dbo.report(generatedBy);
END
GO

/* --------------------------------------------------- artistBankingDetails */
IF OBJECT_ID(N'dbo.artistBankingDetails', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.artistBankingDetails
    (
        bankingID  INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_artistBankingDetails PRIMARY KEY,
        artistID   INT           NOT NULL,
        bank       NVARCHAR(100) NOT NULL,
        branch     NVARCHAR(100) NOT NULL,
        branchCode NVARCHAR(50)  NOT NULL,
        createdAt  DATETIME2     NOT NULL CONSTRAINT DF_artistBankingDetails_createdAt DEFAULT (SYSDATETIME()),
        updatedAt  DATETIME2     NOT NULL CONSTRAINT DF_artistBankingDetails_updatedAt DEFAULT (SYSDATETIME()),
        createdBy  INT           NOT NULL,
        updatedBy  INT           NOT NULL,
        CONSTRAINT FK_artistBankingDetails_Users_artistID
            FOREIGN KEY (artistID) REFERENCES dbo.Users(userID) ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX UX_artistBankingDetails_artistID ON dbo.artistBankingDetails(artistID);
END
GO

/* --------------------------------------------------------------- AuditLog */
IF OBJECT_ID(N'dbo.AuditLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLog
    (
        auditID    INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_AuditLog PRIMARY KEY,
        userID     INT           NULL,
        tableName  NVARCHAR(100) NOT NULL,
        recordID   INT           NULL,
        actionType NVARCHAR(20)  NOT NULL,
        actionDate DATETIME2     NOT NULL CONSTRAINT DF_AuditLog_actionDate DEFAULT (SYSDATETIME()),
        CONSTRAINT FK_AuditLog_Users_userID
            FOREIGN KEY (userID) REFERENCES dbo.Users(userID)
    );

    CREATE INDEX IX_AuditLog_userID ON dbo.AuditLog(userID);
END
GO

/* ---------------------------------------------------------- overallAmount */
IF OBJECT_ID(N'dbo.overallAmount', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.overallAmount
    (
        calculationID    INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_overallAmount PRIMARY KEY,
        claimID          INT           NOT NULL,
        rate             DECIMAL(18,2) NOT NULL,
        quantity         DECIMAL(10,2) NOT NULL,
        calculatedAmount DECIMAL(18,2) NOT NULL,
        calculatedBy     INT           NOT NULL,
        calculatedAt     DATETIME2     NOT NULL CONSTRAINT DF_overallAmount_calculatedAt DEFAULT (SYSDATETIME()),
        itemDescription  NVARCHAR(250) NOT NULL CONSTRAINT DF_overallAmount_itemDescription DEFAULT (N'Secretary allocation'),
        updatedBy        INT           NOT NULL,
        updatedAt        DATETIME2     NOT NULL CONSTRAINT DF_overallAmount_updatedAt DEFAULT (SYSDATETIME()),
        CONSTRAINT FK_overallAmount_claim_claimID
            FOREIGN KEY (claimID) REFERENCES dbo.claim(claimID) ON DELETE CASCADE
    );

    CREATE INDEX IX_overallAmount_claimID ON dbo.overallAmount(claimID);
END
GO

/* ------------------------------------------------------------ lookup data */
IF NOT EXISTS (SELECT 1 FROM dbo.userRole)
BEGIN
    INSERT INTO dbo.userRole (roleName, description) VALUES
        (N'Artist',    N'External artist or claimer who submits and corrects claims.'),
        (N'Secretary', N'Internal user who reviews, corrects and approves claims.'),
        (N'Admin',     N'System administrator with full access.');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.claimStatus)
BEGIN
    INSERT INTO dbo.claimStatus (statusName, description) VALUES
        (N'Draft',                         N'Claim is being prepared and has not been submitted.'),
        (N'Submitted',                     N'Claim has been submitted and is awaiting review.'),
        (N'Request Submission Correction', N'The Secretary identified an error or missing information and requested corrections.'),
        (N'Resubmitted',                   N'The Artist corrected the claim and submitted it again.'),
        (N'Accepted',                      N'The Secretary approved the claim.'),
        (N'Declined',                      N'The Secretary declined the claim.');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.programme)
BEGIN
    INSERT INTO dbo.programme (programmeName, description) VALUES
        (N'Prime News Tonight',       N'Flagship evening news bulletin.'),
        (N'The Morning Show',         N'Weekday morning magazine programme.'),
        (N'Namibia Today',            N'Daily current affairs programme.'),
        (N'Sports Desk',              N'Weekend sports wrap and live analysis.'),
        (N'One Africa',               N'Continental affairs and culture.'),
        (N'NBC Drama Series',         N'Locally produced drama productions.'),
        (N'Fresh FM Sessions',        N'Live music sessions recorded for radio.'),
        (N'National Events Coverage', N'Official ceremonies and national events.');
END
GO

/* ---------------------------------------------------------------------------
   Demo users and claims are seeded by the web application on first start so
   that passwords are hashed with the application's PBKDF2-SHA256 hasher.
   --------------------------------------------------------------------------- */
