-- Approved P2-02 change: retain the first LearnedAt timestamp after unmarking.
-- No data, table, column, migration or scaffold changes.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.CK_UserLearningProgress_State', N'C') IS NULL
    THROW 50000, 'Expected progress constraint is missing. Stop and verify the target database.', 1;

ALTER TABLE dbo.UserLearningProgress DROP CONSTRAINT CK_UserLearningProgress_State;
ALTER TABLE dbo.UserLearningProgress WITH CHECK
    ADD CONSTRAINT CK_UserLearningProgress_State
    CHECK (IsLearned = 0 OR LearnedAt IS NOT NULL);
ALTER TABLE dbo.UserLearningProgress CHECK CONSTRAINT CK_UserLearningProgress_State;

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
    WHERE object_id = OBJECT_ID(N'dbo.CK_UserLearningProgress_State')
      AND is_disabled = 0 AND is_not_trusted = 0)
    THROW 50001, 'Progress constraint verification failed.', 1;
COMMIT TRANSACTION;
