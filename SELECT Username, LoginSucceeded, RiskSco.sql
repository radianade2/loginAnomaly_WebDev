SELECT Id, Username, LoginSucceeded, RiskScore, Decision FROM LoginEvents ORDER BY Id DESC;
SELECT * FROM RuleHits ORDER BY Id DESC;
SELECT * FROM Alerts ORDER BY Id DESC;
SELECT * FROM KnownDevices;

-- DELETE FROM RuleHits;
-- DELETE FROM Alerts;
-- DELETE FROM LoginEvents;
-- DELETE FROM KnownDevices;

-- DBCC CHECKIDENT ('LoginEvents', RESEED, 0);
-- DBCC CHECKIDENT ('RuleHits', RESEED, 0);
-- DBCC CHECKIDENT ('Alerts', RESEED, 0);
-- DBCC CHECKIDENT ('KnownDevices', RESEED, 0);