/*
    Procera Assessment
    Database schema creation

    Run this script against an empty SQL Server database.
*/

-- 1. Persons
CREATE TABLE Persons
(
    code INT NOT NULL IDENTITY(1,1),
    name VARCHAR(50) NULL,
    surname VARCHAR(50) NULL,
    id_number VARCHAR(50) NOT NULL,

    CONSTRAINT PK_Persons PRIMARY KEY CLUSTERED (code)
);
GO

-- 2. Account statuses
CREATE TABLE AccountStatuses
(
    code INT NOT NULL,
    status_name VARCHAR(20) NOT NULL,

    CONSTRAINT PK_AccountStatuses PRIMARY KEY (code),
    CONSTRAINT UQ_AccountStatuses_StatusName UNIQUE (status_name)
);
GO

INSERT INTO AccountStatuses (code, status_name)
VALUES
    (1, 'Open'),
    (2, 'Closed');
GO

-- 3. Accounts
CREATE TABLE Accounts
(
    code INT NOT NULL IDENTITY(1,1),
    person_code INT NOT NULL,
    account_number VARCHAR(50) NOT NULL,
    outstanding_balance MONEY NOT NULL,
    status_code INT NOT NULL
        CONSTRAINT DF_Accounts_StatusCode DEFAULT 1,

    CONSTRAINT PK_Accounts PRIMARY KEY CLUSTERED (code),
    CONSTRAINT FK_Account_Person
        FOREIGN KEY (person_code) REFERENCES Persons(code),
    CONSTRAINT FK_Accounts_AccountStatuses
        FOREIGN KEY (status_code) REFERENCES AccountStatuses(code)
);
GO

-- 4. Transactions
CREATE TABLE Transactions
(
    code INT NOT NULL IDENTITY(1,1),
    account_code INT NOT NULL,
    transaction_date DATETIME NOT NULL,
    capture_date DATETIME NOT NULL,
    amount MONEY NOT NULL,
    description VARCHAR(100) NOT NULL,

    CONSTRAINT PK_Transactions PRIMARY KEY CLUSTERED (code),
    CONSTRAINT FK_Transaction_Account
        FOREIGN KEY (account_code) REFERENCES Accounts(code)
);
GO

-- 5. Original assessment indexes
CREATE UNIQUE NONCLUSTERED INDEX IX_Person_id
    ON Persons(id_number);
GO

CREATE UNIQUE NONCLUSTERED INDEX IX_Account_num
    ON Accounts(account_number);
GO