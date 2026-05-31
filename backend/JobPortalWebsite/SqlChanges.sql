-- =============================================
-- Date: 25th April 2026
-- Change: Added default roles
-- =============================================

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'Admin')
    INSERT INTO Roles (Name) VALUES ('Admin');

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'Recruiter')
    INSERT INTO Roles (Name) VALUES ('Recruiter');

IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'Candidate')
    INSERT INTO Roles (Name) VALUES ('Candidate');


-- =============================================
