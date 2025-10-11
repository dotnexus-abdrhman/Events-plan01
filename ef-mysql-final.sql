CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;
DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    ALTER DATABASE CHARACTER SET utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `AppModules` (
        `ModuleId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `Description` longtext CHARACTER SET utf8mb4 NOT NULL,
        `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
        `RequiredFeatures` longtext CHARACTER SET utf8mb4 NOT NULL,
        `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        CONSTRAINT `PK_AppModules` PRIMARY KEY (`ModuleId`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `AuditLogs` (
        `AuditLogId` char(36) COLLATE ascii_general_ci NOT NULL,
        `ActorId` char(36) COLLATE ascii_general_ci NULL,
        `Action` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `Entity` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `EntityId` char(36) COLLATE ascii_general_ci NULL,
        `At` datetime(6) NOT NULL,
        `MetaJson` longtext CHARACTER SET utf8mb4 NOT NULL,
        CONSTRAINT `PK_AuditLogs` PRIMARY KEY (`AuditLogId`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `Organizations` (
        `OrganizationId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Name` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `NameEn` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `Type` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Logo` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `PrimaryColor` varchar(7) CHARACTER SET utf8mb4 NOT NULL,
        `SecondaryColor` varchar(7) CHARACTER SET utf8mb4 NOT NULL,
        `Settings` longtext CHARACTER SET utf8mb4 NOT NULL,
        `LicenseKey` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `LicenseExpiry` datetime(6) NOT NULL,
        `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
        CONSTRAINT `PK_Organizations` PRIMARY KEY (`OrganizationId`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `PlatformAdmins` (
        `Id` char(36) COLLATE ascii_general_ci NOT NULL,
        `Email` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
        `FullName` varchar(150) CHARACTER SET utf8mb4 NULL,
        `Phone` varchar(30) CHARACTER SET utf8mb4 NULL,
        `ProfilePicture` varchar(500) CHARACTER SET utf8mb4 NULL,
        `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
        `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        `LastLogin` datetime(6) NULL,
        CONSTRAINT `PK_PlatformAdmins` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `Localizations` (
        `LocalizationId` char(36) COLLATE ascii_general_ci NOT NULL,
        `OrganizationId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Key` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `Value` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Language` varchar(5) CHARACTER SET utf8mb4 NOT NULL,
        CONSTRAINT `PK_Localizations` PRIMARY KEY (`LocalizationId`),
        CONSTRAINT `FK_Localizations_Organizations_OrganizationId` FOREIGN KEY (`OrganizationId`) REFERENCES `Organizations` (`OrganizationId`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `Users` (
        `UserId` char(36) COLLATE ascii_general_ci NOT NULL,
        `OrganizationId` char(36) COLLATE ascii_general_ci NOT NULL,
        `FullName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `Email` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
        `Phone` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
        `Role` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `ProfilePicture` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `IsActive` tinyint(1) NOT NULL DEFAULT TRUE,
        `LastLogin` datetime(6) NULL,
        `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        CONSTRAINT `PK_Users` PRIMARY KEY (`UserId`),
        CONSTRAINT `FK_Users_Organizations_OrganizationId` FOREIGN KEY (`OrganizationId`) REFERENCES `Organizations` (`OrganizationId`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `Events` (
        `EventId` char(36) COLLATE ascii_general_ci NOT NULL,
        `OrganizationId` char(36) COLLATE ascii_general_ci NOT NULL,
        `CreatedById` char(36) COLLATE ascii_general_ci NOT NULL,
        `Title` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `Description` longtext CHARACTER SET utf8mb4 NOT NULL,
        `StartAt` datetime(6) NOT NULL,
        `EndAt` datetime(6) NOT NULL,
        `Status` varchar(50) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Draft',
        `RequireSignature` tinyint(1) NOT NULL DEFAULT FALSE,
        `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        `UpdatedAt` datetime(6) NULL,
        `AppModuleModuleId` char(36) COLLATE ascii_general_ci NULL,
        CONSTRAINT `PK_Events` PRIMARY KEY (`EventId`),
        CONSTRAINT `FK_Events_AppModules_AppModuleModuleId` FOREIGN KEY (`AppModuleModuleId`) REFERENCES `AppModules` (`ModuleId`),
        CONSTRAINT `FK_Events_Organizations_OrganizationId` FOREIGN KEY (`OrganizationId`) REFERENCES `Organizations` (`OrganizationId`) ON DELETE RESTRICT,
        CONSTRAINT `FK_Events_Users_CreatedById` FOREIGN KEY (`CreatedById`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `AgendaItems` (
        `AgendaItemId` char(36) COLLATE ascii_general_ci NOT NULL,
        `EventId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Title` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `Description` longtext CHARACTER SET utf8mb4 NOT NULL,
        `OrderIndex` int NOT NULL,
        `EstimatedDuration` int NOT NULL,
        `Type` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `RequiresVoting` tinyint(1) NOT NULL,
        `PresenterId` char(36) COLLATE ascii_general_ci NULL,
        `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        CONSTRAINT `PK_AgendaItems` PRIMARY KEY (`AgendaItemId`),
        CONSTRAINT `FK_AgendaItems_Events_EventId` FOREIGN KEY (`EventId`) REFERENCES `Events` (`EventId`) ON DELETE RESTRICT,
        CONSTRAINT `FK_AgendaItems_Users_PresenterId` FOREIGN KEY (`PresenterId`) REFERENCES `Users` (`UserId`) ON DELETE SET NULL
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `AttendanceLogs` (
        `AttendanceId` char(36) COLLATE ascii_general_ci NOT NULL,
        `EventId` char(36) COLLATE ascii_general_ci NOT NULL,
        `UserId` char(36) COLLATE ascii_general_ci NOT NULL,
        `JoinTime` datetime(6) NOT NULL,
        `LeaveTime` datetime(6) NULL,
        `AttendanceType` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        CONSTRAINT `PK_AttendanceLogs` PRIMARY KEY (`AttendanceId`),
        CONSTRAINT `FK_AttendanceLogs_Events_EventId` FOREIGN KEY (`EventId`) REFERENCES `Events` (`EventId`) ON DELETE CASCADE,
        CONSTRAINT `FK_AttendanceLogs_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `DiscussionPosts` (
        `DiscussionPostId` char(36) COLLATE ascii_general_ci NOT NULL,
        `EventId` char(36) COLLATE ascii_general_ci NOT NULL,
        `ParentId` char(36) COLLATE ascii_general_ci NULL,
        `UserId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Body` varchar(1000) CHARACTER SET utf8mb4 NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_DiscussionPosts` PRIMARY KEY (`DiscussionPostId`),
        CONSTRAINT `FK_DiscussionPosts_DiscussionPosts_ParentId` FOREIGN KEY (`ParentId`) REFERENCES `DiscussionPosts` (`DiscussionPostId`) ON DELETE RESTRICT,
        CONSTRAINT `FK_DiscussionPosts_Events_EventId` FOREIGN KEY (`EventId`) REFERENCES `Events` (`EventId`) ON DELETE CASCADE,
        CONSTRAINT `FK_DiscussionPosts_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `EventParticipants` (
        `EventParticipantId` char(36) COLLATE ascii_general_ci NOT NULL,
        `EventId` char(36) COLLATE ascii_general_ci NOT NULL,
        `UserId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Role` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `InvitedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        `JoinedAt` datetime(6) NULL,
        `Status` varchar(50) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Invited',
        CONSTRAINT `PK_EventParticipants` PRIMARY KEY (`EventParticipantId`),
        CONSTRAINT `FK_EventParticipants_Events_EventId` FOREIGN KEY (`EventId`) REFERENCES `Events` (`EventId`) ON DELETE RESTRICT,
        CONSTRAINT `FK_EventParticipants_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`UserId`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `EventPublicLinks` (
        `EventPublicLinkId` char(36) COLLATE ascii_general_ci NOT NULL,
        `EventId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Token` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `IsEnabled` tinyint(1) NOT NULL,
        `ExpiresAt` datetime(6) NULL,
        `CreatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_EventPublicLinks` PRIMARY KEY (`EventPublicLinkId`),
        CONSTRAINT `FK_EventPublicLinks_Events_EventId` FOREIGN KEY (`EventId`) REFERENCES `Events` (`EventId`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `Notifications` (
        `NotificationId` char(36) COLLATE ascii_general_ci NOT NULL,
        `UserId` char(36) COLLATE ascii_general_ci NOT NULL,
        `EventId` char(36) COLLATE ascii_general_ci NULL,
        `Title` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `Message` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Type` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `IsRead` tinyint(1) NOT NULL,
        `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        `ReadAt` datetime(6) NULL,
        CONSTRAINT `PK_Notifications` PRIMARY KEY (`NotificationId`),
        CONSTRAINT `FK_Notifications_Events_EventId` FOREIGN KEY (`EventId`) REFERENCES `Events` (`EventId`) ON DELETE SET NULL,
        CONSTRAINT `FK_Notifications_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`UserId`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `Proposals` (
        `ProposalId` char(36) COLLATE ascii_general_ci NOT NULL,
        `EventId` char(36) COLLATE ascii_general_ci NOT NULL,
        `UserId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Title` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `Body` varchar(2000) CHARACTER SET utf8mb4 NOT NULL,
        `Upvotes` int NOT NULL,
        `Status` varchar(20) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Pending',
        `CreatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_Proposals` PRIMARY KEY (`ProposalId`),
        CONSTRAINT `FK_Proposals_Events_EventId` FOREIGN KEY (`EventId`) REFERENCES `Events` (`EventId`) ON DELETE CASCADE,
        CONSTRAINT `FK_Proposals_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `PublicEventGuests` (
        `GuestId` char(36) COLLATE ascii_general_ci NOT NULL,
        `EventId` char(36) COLLATE ascii_general_ci NOT NULL,
        `FullName` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
        `Email` varchar(150) CHARACTER SET utf8mb4 NULL,
        `Phone` varchar(20) CHARACTER SET utf8mb4 NULL,
        `UniqueToken` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `IsGuest` tinyint(1) NOT NULL,
        `UserId` char(36) COLLATE ascii_general_ci NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_PublicEventGuests` PRIMARY KEY (`GuestId`),
        CONSTRAINT `FK_PublicEventGuests_Events_EventId` FOREIGN KEY (`EventId`) REFERENCES `Events` (`EventId`) ON DELETE CASCADE,
        CONSTRAINT `FK_PublicEventGuests_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`UserId`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `Sections` (
        `SectionId` char(36) COLLATE ascii_general_ci NOT NULL,
        `EventId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Title` varchar(300) CHARACTER SET utf8mb4 NOT NULL,
        `Body` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Order` int NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_Sections` PRIMARY KEY (`SectionId`),
        CONSTRAINT `FK_Sections_Events_EventId` FOREIGN KEY (`EventId`) REFERENCES `Events` (`EventId`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `UserHiddenEvents` (
        `UserId` char(36) COLLATE ascii_general_ci NOT NULL,
        `EventId` char(36) COLLATE ascii_general_ci NOT NULL,
        `HiddenAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        CONSTRAINT `PK_UserHiddenEvents` PRIMARY KEY (`UserId`, `EventId`),
        CONSTRAINT `FK_UserHiddenEvents_Events_EventId` FOREIGN KEY (`EventId`) REFERENCES `Events` (`EventId`) ON DELETE CASCADE,
        CONSTRAINT `FK_UserHiddenEvents_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`UserId`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `UserSignatures` (
        `UserSignatureId` char(36) COLLATE ascii_general_ci NOT NULL,
        `EventId` char(36) COLLATE ascii_general_ci NOT NULL,
        `UserId` char(36) COLLATE ascii_general_ci NOT NULL,
        `ImagePath` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `Data` longtext CHARACTER SET utf8mb4 NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_UserSignatures` PRIMARY KEY (`UserSignatureId`),
        CONSTRAINT `FK_UserSignatures_Events_EventId` FOREIGN KEY (`EventId`) REFERENCES `Events` (`EventId`) ON DELETE CASCADE,
        CONSTRAINT `FK_UserSignatures_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`UserId`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `Documents` (
        `DocumentId` char(36) COLLATE ascii_general_ci NOT NULL,
        `EventId` char(36) COLLATE ascii_general_ci NOT NULL,
        `AgendaItemId` char(36) COLLATE ascii_general_ci NULL,
        `FileName` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `FilePath` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `FileSize` bigint NOT NULL,
        `FileType` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `UploadedById` char(36) COLLATE ascii_general_ci NOT NULL,
        `UploadedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        CONSTRAINT `PK_Documents` PRIMARY KEY (`DocumentId`),
        CONSTRAINT `FK_Documents_AgendaItems_AgendaItemId` FOREIGN KEY (`AgendaItemId`) REFERENCES `AgendaItems` (`AgendaItemId`) ON DELETE SET NULL,
        CONSTRAINT `FK_Documents_Events_EventId` FOREIGN KEY (`EventId`) REFERENCES `Events` (`EventId`) ON DELETE RESTRICT,
        CONSTRAINT `FK_Documents_Users_UploadedById` FOREIGN KEY (`UploadedById`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `VotingSessions` (
        `VotingSessionId` char(36) COLLATE ascii_general_ci NOT NULL,
        `EventId` char(36) COLLATE ascii_general_ci NOT NULL,
        `AgendaItemId` char(36) COLLATE ascii_general_ci NULL,
        `Title` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `Question` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Type` longtext CHARACTER SET utf8mb4 NOT NULL,
        `StartTime` datetime(6) NOT NULL,
        `EndTime` datetime(6) NULL,
        `IsAnonymous` tinyint(1) NOT NULL,
        `Status` varchar(50) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Pending',
        `Settings` longtext CHARACTER SET utf8mb4 NOT NULL,
        `CreatedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        CONSTRAINT `PK_VotingSessions` PRIMARY KEY (`VotingSessionId`),
        CONSTRAINT `FK_VotingSessions_AgendaItems_AgendaItemId` FOREIGN KEY (`AgendaItemId`) REFERENCES `AgendaItems` (`AgendaItemId`) ON DELETE SET NULL,
        CONSTRAINT `FK_VotingSessions_Events_EventId` FOREIGN KEY (`EventId`) REFERENCES `Events` (`EventId`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `ProposalUpvotes` (
        `ProposalUpvoteId` char(36) COLLATE ascii_general_ci NOT NULL,
        `ProposalId` char(36) COLLATE ascii_general_ci NOT NULL,
        `UserId` char(36) COLLATE ascii_general_ci NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_ProposalUpvotes` PRIMARY KEY (`ProposalUpvoteId`),
        CONSTRAINT `FK_ProposalUpvotes_Proposals_ProposalId` FOREIGN KEY (`ProposalId`) REFERENCES `Proposals` (`ProposalId`) ON DELETE CASCADE,
        CONSTRAINT `FK_ProposalUpvotes_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `Attachments` (
        `AttachmentId` char(36) COLLATE ascii_general_ci NOT NULL,
        `EventId` char(36) COLLATE ascii_general_ci NOT NULL,
        `SectionId` char(36) COLLATE ascii_general_ci NULL,
        `Type` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `FileName` varchar(300) CHARACTER SET utf8mb4 NOT NULL,
        `Path` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `Size` bigint NOT NULL,
        `MetadataJson` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Order` int NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_Attachments` PRIMARY KEY (`AttachmentId`),
        CONSTRAINT `FK_Attachments_Events_EventId` FOREIGN KEY (`EventId`) REFERENCES `Events` (`EventId`) ON DELETE CASCADE,
        CONSTRAINT `FK_Attachments_Sections_SectionId` FOREIGN KEY (`SectionId`) REFERENCES `Sections` (`SectionId`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `Decisions` (
        `DecisionId` char(36) COLLATE ascii_general_ci NOT NULL,
        `SectionId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Title` varchar(300) CHARACTER SET utf8mb4 NOT NULL,
        `Order` int NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_Decisions` PRIMARY KEY (`DecisionId`),
        CONSTRAINT `FK_Decisions_Sections_SectionId` FOREIGN KEY (`SectionId`) REFERENCES `Sections` (`SectionId`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `Discussions` (
        `DiscussionId` char(36) COLLATE ascii_general_ci NOT NULL,
        `EventId` char(36) COLLATE ascii_general_ci NOT NULL,
        `SectionId` char(36) COLLATE ascii_general_ci NULL,
        `Title` varchar(300) CHARACTER SET utf8mb4 NOT NULL,
        `Purpose` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Order` int NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_Discussions` PRIMARY KEY (`DiscussionId`),
        CONSTRAINT `FK_Discussions_Events_EventId` FOREIGN KEY (`EventId`) REFERENCES `Events` (`EventId`) ON DELETE CASCADE,
        CONSTRAINT `FK_Discussions_Sections_SectionId` FOREIGN KEY (`SectionId`) REFERENCES `Sections` (`SectionId`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `Surveys` (
        `SurveyId` char(36) COLLATE ascii_general_ci NOT NULL,
        `EventId` char(36) COLLATE ascii_general_ci NOT NULL,
        `SectionId` char(36) COLLATE ascii_general_ci NULL,
        `Title` varchar(300) CHARACTER SET utf8mb4 NOT NULL,
        `Order` int NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_Surveys` PRIMARY KEY (`SurveyId`),
        CONSTRAINT `FK_Surveys_Events_EventId` FOREIGN KEY (`EventId`) REFERENCES `Events` (`EventId`) ON DELETE CASCADE,
        CONSTRAINT `FK_Surveys_Sections_SectionId` FOREIGN KEY (`SectionId`) REFERENCES `Sections` (`SectionId`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `TableBlocks` (
        `TableBlockId` char(36) COLLATE ascii_general_ci NOT NULL,
        `EventId` char(36) COLLATE ascii_general_ci NOT NULL,
        `SectionId` char(36) COLLATE ascii_general_ci NULL,
        `Title` varchar(300) CHARACTER SET utf8mb4 NOT NULL,
        `Description` varchar(1000) CHARACTER SET utf8mb4 NOT NULL,
        `HasHeader` tinyint(1) NOT NULL,
        `RowsJson` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Order` int NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_TableBlocks` PRIMARY KEY (`TableBlockId`),
        CONSTRAINT `FK_TableBlocks_Events_EventId` FOREIGN KEY (`EventId`) REFERENCES `Events` (`EventId`) ON DELETE CASCADE,
        CONSTRAINT `FK_TableBlocks_Sections_SectionId` FOREIGN KEY (`SectionId`) REFERENCES `Sections` (`SectionId`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `VotingOptions` (
        `VotingOptionId` char(36) COLLATE ascii_general_ci NOT NULL,
        `VotingSessionId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Text` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `OrderIndex` int NOT NULL,
        CONSTRAINT `PK_VotingOptions` PRIMARY KEY (`VotingOptionId`),
        CONSTRAINT `FK_VotingOptions_VotingSessions_VotingSessionId` FOREIGN KEY (`VotingSessionId`) REFERENCES `VotingSessions` (`VotingSessionId`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `DecisionItems` (
        `DecisionItemId` char(36) COLLATE ascii_general_ci NOT NULL,
        `DecisionId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Text` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Order` int NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_DecisionItems` PRIMARY KEY (`DecisionItemId`),
        CONSTRAINT `FK_DecisionItems_Decisions_DecisionId` FOREIGN KEY (`DecisionId`) REFERENCES `Decisions` (`DecisionId`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `DiscussionReplies` (
        `DiscussionReplyId` char(36) COLLATE ascii_general_ci NOT NULL,
        `DiscussionId` char(36) COLLATE ascii_general_ci NOT NULL,
        `UserId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Body` longtext CHARACTER SET utf8mb4 NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_DiscussionReplies` PRIMARY KEY (`DiscussionReplyId`),
        CONSTRAINT `FK_DiscussionReplies_Discussions_DiscussionId` FOREIGN KEY (`DiscussionId`) REFERENCES `Discussions` (`DiscussionId`) ON DELETE CASCADE,
        CONSTRAINT `FK_DiscussionReplies_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`UserId`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `SurveyQuestions` (
        `SurveyQuestionId` char(36) COLLATE ascii_general_ci NOT NULL,
        `SurveyId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Text` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Type` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Order` int NOT NULL,
        `IsRequired` tinyint(1) NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_SurveyQuestions` PRIMARY KEY (`SurveyQuestionId`),
        CONSTRAINT `FK_SurveyQuestions_Surveys_SurveyId` FOREIGN KEY (`SurveyId`) REFERENCES `Surveys` (`SurveyId`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `Votes` (
        `VoteId` char(36) COLLATE ascii_general_ci NOT NULL,
        `VotingSessionId` char(36) COLLATE ascii_general_ci NOT NULL,
        `UserId` char(36) COLLATE ascii_general_ci NOT NULL,
        `VotingOptionId` char(36) COLLATE ascii_general_ci NULL,
        `CustomAnswer` varchar(1000) CHARACTER SET utf8mb4 NOT NULL,
        `VotedAt` datetime(6) NOT NULL DEFAULT UTC_TIMESTAMP(),
        CONSTRAINT `PK_Votes` PRIMARY KEY (`VoteId`),
        CONSTRAINT `FK_Votes_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`UserId`) ON DELETE RESTRICT,
        CONSTRAINT `FK_Votes_VotingOptions_VotingOptionId` FOREIGN KEY (`VotingOptionId`) REFERENCES `VotingOptions` (`VotingOptionId`) ON DELETE SET NULL,
        CONSTRAINT `FK_Votes_VotingSessions_VotingSessionId` FOREIGN KEY (`VotingSessionId`) REFERENCES `VotingSessions` (`VotingSessionId`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `SurveyAnswers` (
        `SurveyAnswerId` char(36) COLLATE ascii_general_ci NOT NULL,
        `EventId` char(36) COLLATE ascii_general_ci NOT NULL,
        `QuestionId` char(36) COLLATE ascii_general_ci NOT NULL,
        `UserId` char(36) COLLATE ascii_general_ci NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_SurveyAnswers` PRIMARY KEY (`SurveyAnswerId`),
        CONSTRAINT `FK_SurveyAnswers_Events_EventId` FOREIGN KEY (`EventId`) REFERENCES `Events` (`EventId`) ON DELETE CASCADE,
        CONSTRAINT `FK_SurveyAnswers_SurveyQuestions_QuestionId` FOREIGN KEY (`QuestionId`) REFERENCES `SurveyQuestions` (`SurveyQuestionId`),
        CONSTRAINT `FK_SurveyAnswers_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`UserId`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `SurveyOptions` (
        `SurveyOptionId` char(36) COLLATE ascii_general_ci NOT NULL,
        `QuestionId` char(36) COLLATE ascii_general_ci NOT NULL,
        `Text` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
        `Order` int NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_SurveyOptions` PRIMARY KEY (`SurveyOptionId`),
        CONSTRAINT `FK_SurveyOptions_SurveyQuestions_QuestionId` FOREIGN KEY (`QuestionId`) REFERENCES `SurveyQuestions` (`SurveyQuestionId`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE TABLE `SurveyAnswerOptions` (
        `SurveyAnswerId` char(36) COLLATE ascii_general_ci NOT NULL,
        `OptionId` char(36) COLLATE ascii_general_ci NOT NULL,
        `CreatedAt` datetime(6) NOT NULL,
        CONSTRAINT `PK_SurveyAnswerOptions` PRIMARY KEY (`SurveyAnswerId`, `OptionId`),
        CONSTRAINT `FK_SurveyAnswerOptions_SurveyAnswers_SurveyAnswerId` FOREIGN KEY (`SurveyAnswerId`) REFERENCES `SurveyAnswers` (`SurveyAnswerId`) ON DELETE CASCADE,
        CONSTRAINT `FK_SurveyAnswerOptions_SurveyOptions_OptionId` FOREIGN KEY (`OptionId`) REFERENCES `SurveyOptions` (`SurveyOptionId`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    INSERT INTO `Organizations` (`OrganizationId`, `CreatedAt`, `IsActive`, `LicenseExpiry`, `LicenseKey`, `Logo`, `Name`, `NameEn`, `PrimaryColor`, `SecondaryColor`, `Settings`, `Type`)
    VALUES ('11111111-1111-1111-1111-111111111111', TIMESTAMP '2025-01-01 00:00:00', TRUE, TIMESTAMP '2030-12-31 23:59:59', 'MINA-SEED-2025', '', 'الجهة التجريبية', 'Test Organization', '#0d6efd', '#6c757d', '{}', 'Other');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    INSERT INTO `PlatformAdmins` (`Id`, `CreatedAt`, `Email`, `FullName`, `IsActive`, `LastLogin`, `Phone`, `ProfilePicture`)
    VALUES ('22222222-2222-2222-2222-222222222222', TIMESTAMP '2025-01-01 00:00:00', 'admin@mina.local', 'مدير النظام', TRUE, NULL, '0500000001', '');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    INSERT INTO `Users` (`UserId`, `CreatedAt`, `Email`, `FullName`, `IsActive`, `LastLogin`, `OrganizationId`, `Phone`, `ProfilePicture`, `Role`)
    VALUES ('33333333-3333-3333-3333-333333333333', TIMESTAMP '2025-01-01 00:00:00', 'user@mina.local', 'مستخدم تجريبي', TRUE, NULL, '11111111-1111-1111-1111-111111111111', '0500000000', '', 'Attendee');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_AgendaItems_EventId_OrderIndex` ON `AgendaItems` (`EventId`, `OrderIndex`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_AgendaItems_PresenterId` ON `AgendaItems` (`PresenterId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_AgendaItems_Type` ON `AgendaItems` (`Type`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_AppModules_IsActive` ON `AppModules` (`IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE UNIQUE INDEX `IX_AppModules_Name` ON `AppModules` (`Name`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Attachments_EventId_Order` ON `Attachments` (`EventId`, `Order`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Attachments_SectionId` ON `Attachments` (`SectionId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Attachments_Type` ON `Attachments` (`Type`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_AttendanceLogs_AttendanceType` ON `AttendanceLogs` (`AttendanceType`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_AttendanceLogs_EventId_UserId_JoinTime` ON `AttendanceLogs` (`EventId`, `UserId`, `JoinTime`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_AttendanceLogs_UserId` ON `AttendanceLogs` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_AuditLogs_ActorId` ON `AuditLogs` (`ActorId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_AuditLogs_At` ON `AuditLogs` (`At`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_AuditLogs_Entity_EntityId` ON `AuditLogs` (`Entity`, `EntityId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_DecisionItems_DecisionId_Order` ON `DecisionItems` (`DecisionId`, `Order`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Decisions_SectionId_Order` ON `Decisions` (`SectionId`, `Order`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_DiscussionPosts_EventId` ON `DiscussionPosts` (`EventId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_DiscussionPosts_ParentId` ON `DiscussionPosts` (`ParentId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_DiscussionPosts_UserId` ON `DiscussionPosts` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_DiscussionReplies_DiscussionId_CreatedAt` ON `DiscussionReplies` (`DiscussionId`, `CreatedAt`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_DiscussionReplies_UserId` ON `DiscussionReplies` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Discussions_EventId_Order` ON `Discussions` (`EventId`, `Order`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Discussions_SectionId` ON `Discussions` (`SectionId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Documents_AgendaItemId` ON `Documents` (`AgendaItemId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Documents_EventId` ON `Documents` (`EventId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Documents_FileType` ON `Documents` (`FileType`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Documents_UploadedAt` ON `Documents` (`UploadedAt`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Documents_UploadedById` ON `Documents` (`UploadedById`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE UNIQUE INDEX `IX_EventParticipants_EventId_UserId` ON `EventParticipants` (`EventId`, `UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_EventParticipants_Role` ON `EventParticipants` (`Role`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_EventParticipants_Status` ON `EventParticipants` (`Status`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_EventParticipants_UserId` ON `EventParticipants` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_EventPublicLinks_EventId` ON `EventPublicLinks` (`EventId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE UNIQUE INDEX `IX_EventPublicLinks_Token` ON `EventPublicLinks` (`Token`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Events_AppModuleModuleId` ON `Events` (`AppModuleModuleId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Events_CreatedById` ON `Events` (`CreatedById`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Events_EndAt` ON `Events` (`EndAt`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Events_OrganizationId_Status` ON `Events` (`OrganizationId`, `Status`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Events_StartAt` ON `Events` (`StartAt`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Events_Status` ON `Events` (`Status`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE UNIQUE INDEX `IX_Localizations_OrganizationId_Key_Language` ON `Localizations` (`OrganizationId`, `Key`, `Language`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Notifications_CreatedAt` ON `Notifications` (`CreatedAt`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Notifications_EventId` ON `Notifications` (`EventId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Notifications_Type` ON `Notifications` (`Type`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Notifications_UserId_IsRead` ON `Notifications` (`UserId`, `IsRead`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Organizations_IsActive` ON `Organizations` (`IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Organizations_LicenseKey` ON `Organizations` (`LicenseKey`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Organizations_Name` ON `Organizations` (`Name`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE UNIQUE INDEX `IX_PlatformAdmins_Email` ON `PlatformAdmins` (`Email`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Proposals_EventId` ON `Proposals` (`EventId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Proposals_UserId` ON `Proposals` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_ProposalUpvotes_ProposalId` ON `ProposalUpvotes` (`ProposalId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_ProposalUpvotes_UserId` ON `ProposalUpvotes` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE UNIQUE INDEX `IX_PublicEventGuests_EventId_UserId` ON `PublicEventGuests` (`EventId`, `UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_PublicEventGuests_UserId` ON `PublicEventGuests` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Sections_EventId_Order` ON `Sections` (`EventId`, `Order`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_SurveyAnswerOptions_OptionId` ON `SurveyAnswerOptions` (`OptionId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_SurveyAnswers_EventId` ON `SurveyAnswers` (`EventId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE UNIQUE INDEX `IX_SurveyAnswers_QuestionId_UserId` ON `SurveyAnswers` (`QuestionId`, `UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_SurveyAnswers_UserId` ON `SurveyAnswers` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_SurveyOptions_QuestionId_Order` ON `SurveyOptions` (`QuestionId`, `Order`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_SurveyQuestions_SurveyId_Order` ON `SurveyQuestions` (`SurveyId`, `Order`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Surveys_EventId_Order` ON `Surveys` (`EventId`, `Order`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Surveys_SectionId` ON `Surveys` (`SectionId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_TableBlocks_EventId_Order` ON `TableBlocks` (`EventId`, `Order`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_TableBlocks_SectionId` ON `TableBlocks` (`SectionId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_UserHiddenEvents_EventId` ON `UserHiddenEvents` (`EventId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE UNIQUE INDEX `IX_Users_Email` ON `Users` (`Email`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Users_IsActive` ON `Users` (`IsActive`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE UNIQUE INDEX `IX_Users_OrganizationId_Email` ON `Users` (`OrganizationId`, `Email`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Users_Role` ON `Users` (`Role`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE UNIQUE INDEX `IX_UserSignatures_EventId_UserId` ON `UserSignatures` (`EventId`, `UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_UserSignatures_UserId` ON `UserSignatures` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Votes_UserId` ON `Votes` (`UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Votes_VotedAt` ON `Votes` (`VotedAt`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_Votes_VotingOptionId` ON `Votes` (`VotingOptionId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE UNIQUE INDEX `IX_Votes_VotingSessionId_UserId` ON `Votes` (`VotingSessionId`, `UserId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_VotingOptions_VotingSessionId_OrderIndex` ON `VotingOptions` (`VotingSessionId`, `OrderIndex`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_VotingSessions_AgendaItemId` ON `VotingSessions` (`AgendaItemId`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_VotingSessions_EventId_StartTime` ON `VotingSessions` (`EventId`, `StartTime`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_VotingSessions_StartTime` ON `VotingSessions` (`StartTime`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    CREATE INDEX `IX_VotingSessions_Status` ON `VotingSessions` (`Status`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20251011011039_InitialMySqlFinal') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20251011011039_InitialMySqlFinal', '9.0.9');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

