USE [CoJMessaging]
GO
/****** Object:  Schema [Messaging]    Script Date: 02/13/2013 11:47:49 ******/
CREATE SCHEMA [Messaging] AUTHORIZATION [dbo]
GO
/****** Object:  Table [Messaging].[Audit]    Script Date: 02/13/2013 11:47:43 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Messaging].[Audit](
	[pkMessageID] [int] IDENTITY(1,1) NOT NULL,
	[MessageType] [nvarchar](50) NOT NULL,
	[ReceivedDate] [datetime] NOT NULL,
	[ExceptionDate] [datetime] NULL,
	[ExceptionMessage] [text] NULL,
	[Status] [int] NOT NULL,
	[Message] [text] NOT NULL,
 CONSTRAINT [PK_Audit] PRIMARY KEY CLUSTERED 
(
	[pkMessageID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

