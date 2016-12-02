USE vending
GO
CREATE TABLE [Transactions] (
	TransactionId int IDENTITY NOT NULL PRIMARY KEY NONCLUSTERED,
	VendingMachineId char(36),
	ItemName varchar(255),
	ItemId int,
	PurchasePrice smallmoney,
	TransactionStatus int,
	TransactionDate datetime,
	INDEX Transactions_CCI CLUSTERED COLUMNSTORE
) WITH (
	MEMORY_OPTIMIZED = ON
);

ALTER DATABASE CURRENT
SET MEMORY_OPTIMIZED_ELEVATE_TO_SNAPSHOT=ON;
