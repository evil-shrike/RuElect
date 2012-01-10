/* DROP:
drop table ProtocolImage
drop table ProtocolResult
drop table Protocol
drop table Comission
drop table Candidate
drop table Poll
drop table [Region]
drop table ResultProvider
*/

/* CLEAN:
delete ProtocolImage
delete ProtocolResult
delete Protocol
delete Comission
delete Candidate
delete Poll
delete Region
delete ResultProvider
*/

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ProtocolImage]') AND type in (N'U'))
	DROP TABLE [dbo].ProtocolImage
GO
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ProtocolResult]') AND type in (N'U'))
	DROP TABLE [dbo].ProtocolResult
GO
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Protocol]') AND type in (N'U'))
	DROP TABLE [dbo].Protocol
GO
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Comission]') AND type in (N'U'))
	DROP TABLE [dbo].Comission
GO
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Candidate]') AND type in (N'U'))
	DROP TABLE [dbo].Candidate
GO
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Poll]') AND type in (N'U'))
	DROP TABLE [dbo].Poll
GO
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Region]') AND type in (N'U'))
	DROP TABLE [dbo].Region
GO
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ResultProvider]') AND type in (N'U'))
	DROP TABLE [dbo].ResultProvider
GO


-- Регион (пока без иерархии)
CREATE TABLE [dbo].[Region] (
	[ObjectID] uniqueidentifier NOT NULL PRIMARY KEY,
	[Name] varchar(255) NOT NULL
--CONSTRAINT [PK_Region] PRIMARY KEY ([ObjectID])
--CONSTRAINT [UN_Region_Code] UNIQUE ([Code])
)
GO


-- Поставщик данных протоколов (ЦИК, ruelect, kartaitogov, etc)
CREATE TABLE ResultProvider (
	[ObjectID] uniqueidentifier NOT NULL PRIMARY KEY,
	[Name] varchar(256) NOT NULL,
	[IsFile] bit NOT NULL
)
GO

-- УИК (пока без иерархии)
CREATE TABLE Comission (
	[ObjectID] uniqueidentifier NOT NULL PRIMARY KEY,
	[Region] uniqueidentifier NOT NULL FOREIGN KEY REFERENCES [Region] (ObjectID),
	[Number] int NOT NULL
)
GO

-- Данные протокола для одного провайдера и комиссии (содержит первые 18 результатов протокол, без числа голосов за кандидатов)
CREATE TABLE Protocol (
	[ObjectID] uniqueidentifier NOT NULL PRIMARY KEY,
	[Provider] uniqueidentifier NOT NULL FOREIGN KEY REFERENCES [ResultProvider] (ObjectID),
	[Comission] uniqueidentifier NOT NULL FOREIGN KEY REFERENCES [Comission] (ObjectID),
	[Value1] int NOT NULL,
	[Value2] int NOT NULL,
	[Value3] int NOT NULL,
	[Value4] int NOT NULL,
	[Value5] int NOT NULL,
	[Value6] int NOT NULL,
	[Value7] int NOT NULL,
	[Value8] int NOT NULL,
	[Value9] int NOT NULL,
	[Value10] int NOT NULL,
	[Value11] int NOT NULL,
	[Value12] int NOT NULL,
	[Value13] int NOT NULL,
	[Value14] int NOT NULL,
	[Value15] int NOT NULL,
	[Value16] int NOT NULL,
	[Value17] int NOT NULL,
	[Value18] int NOT NULL,
)
GO

-- Образ протокола (картинка)
CREATE TABLE ProtocolImage (
	[ObjectID] uniqueidentifier NOT NULL PRIMARY KEY,
	[Protocol] uniqueidentifier NOT NULL FOREIGN KEY REFERENCES [Protocol] (ObjectID),
	[Uri] varchar(max) NULL,
	[Image] varbinary(max) NULL,
	[Index] int NOT NULL
)	
GO

-- Голосование
CREATE TABLE Poll (
	[ObjectID] uniqueidentifier NOT NULL PRIMARY KEY,
	[Name] varchar(512) NOT NULL,
)
GO

-- Кандидаты голосования
CREATE TABLE Candidate (
	[ObjectID] uniqueidentifier NOT NULL PRIMARY KEY,
	[Poll] uniqueidentifier NOT NULL FOREIGN KEY REFERENCES [Poll] (ObjectID),
	[Name] varchar(512) NOT NULL,
	[Index] int NOT NULL
)
GO

-- Результаты протокола (число голосов в протоколе за одного кандидата)
CREATE TABLE ProtocolResult (
	[ObjectID] uniqueidentifier NOT NULL PRIMARY KEY,
	[Protocol] uniqueidentifier NOT NULL FOREIGN KEY REFERENCES [Protocol] (ObjectID),
	[Candidate] uniqueidentifier NOT NULL FOREIGN KEY REFERENCES [Candidate] (ObjectID),
	[Value] int NOT NULL,
	[Index] int NOT NULL
)
GO
