
UPDATE settlemententry 
	SET TransactionLedgerId = NULL, Status = 'Imported'
	WHERE TransactionLedgerId IS NOT NULL;

UPDATE TransactionLedger SET Status = 'Imported';


CALL Reconciliation_DirectMatch(null, null);
CALL Reconciliation_MatchWithWiggleRoom(null, null, 2);
CALL Reconciliation_MatchWithWiggleRoom(null, null, 200000);

DELIMITER //

DROP PROCEDURE IF EXISTS Reconciliation_MatchSplitSettlement //


CREATE PROCEDURE Reconciliation_MatchSplitSettlement( IN dtStart Date , IN dtEnd Date, IN WiggleAmount int )
BEGIN
/*
Sproc:		Reconciliation_MatchSplitSettlement
Purpose:	Matches SettlementEntries to TransactionLedger items, in an exact way ( all matching and within time )
Author:		PM
Created:	2026.07.11
Returns:	Number of rows matched
Notes:		
Example:	
CALL Reconciliation_MatchSplitSettlement('2026-07-01', '2026-07-11', 2);
*/

DROP TEMPORARY TABLE IF EXISTS TmpSettlementMatches20260711;

-- Match exact in a dup
CREATE TEMPORARY TABLE TmpSettlementMatches20260711 AS
	SELECT
			SA.Id AS SettlementEntryId1,
            SB.Id AS SettlementEntryId2,
			T.Id AS TransactionLedgerId,
			T.CapturedAt
		FROM Settlemententry SA 
			INNER JOIN settlemententry SB ON 
			(
				AND SA.Id != SB.Id
				AND SA.MerchantRef = SB.MerchantRef
				AND SA.MerchantId = SB.MerchantId
				AND SA.CardType = SB.CardType
				AND SA.CardLast4 = SB.CardLast4
				AND SA.SettlementDate = SB.SettlementDate
				
				AND SA.SettlementDate >= COALESCE(dtStart,SA.SettlementDate)
				AND SA.SettlementDate <= COALESCE(dtEnd,SA.SettlementDate)
				AND SB.SettlementDate >= COALESCE(dtStart,SB.SettlementDate)
				AND SB.SettlementDate <= COALESCE(dtEnd,SB.SettlementDate)
			) 
			INNER JOIN transactionledger AS T ON 
            (
				T.MerchantReferenceNo = S.MerchantRef
				AND T.CardType = S.CardType
				AND T.MerchantId = S.MerchantId
				AND T.CardLast4 = S.CardLast4
				AND 
				(
					((T.GrossAmount + T.ExpectedInterchangeCents + T.ExpectedSettledCents + T.ExpectedProcessorFeeCents) -
					(SA.ExpectedGrossOriginalCents + SA.InterchangeFeeCents + SA.SettledAmountCents + SA.ProcessorFeeCents ))
                    (SB.ExpectedGrossOriginalCents + SB.InterchangeFeeCents + SB.SettledAmountCents + SB.ProcessorFeeCents ))
					= 0
				)

				AND SA.SettlementDate IS NOT NULL
                AND SB.SettlementDate IS NOT NULL
				AND T.CapturedAt IS NOT NULL

				AND SA.SettlementDate >= DATE(T.CapturedAt)	-- Could be on the same day?
				AND SA.SettlementDate <= DATE_ADD(DATE(T.CapturedAt), INTERVAL 4 DAY)	-- Settlement must be within four calendar days of capture
                AND SA.SettlementDate >= COALESCE(dtStart,SA.SettlementDate)
                AND SA.SettlementDate <= COALESCE(dtEnd,SA.SettlementDate)
                
                AND SB.SettlementDate >= DATE(T.CapturedAt)	-- Could be on the same day?
				AND SB.SettlementDate <= DATE_ADD(DATE(T.CapturedAt), INTERVAL 4 DAY)	-- Settlement must be within four calendar days of capture
                AND SB.SettlementDate >= COALESCE(dtStart,SB.SettlementDate)
                AND SB.SettlementDate <= COALESCE(dtEnd,SB.SettlementDate)
			)
		WHERE 1=1
			AND S.TransactionLedgerId IS NULL
			AND SA.Status = 'Imported'
            AND SB.Status = 'Imported'
			AND T.Status = 'Imported'
        ;


UPDATE TmpSettlementMatches20260711 AS M
	INNER JOIN settlemententry AS SA ON SA.Id = M.SettlementEntryId1
	INNER JOIN settlemententry AS SB ON SB.Id = M.SettlementEntryId2
	INNER JOIN transactionledger AS T ON T.Id = M.TransactionLedgerId
	SET
		SA.TransactionLedgerId = M.TransactionLedgerId,
		SA.Status = 'Match',
		SA.Notification = CONCAT( 'Split settlement matched with settlement entry ', M.SettlementEntryId2),
		SB.TransactionLedgerId = M.TransactionLedgerId,
		SB.Status = 'Match',
		SB.Notification = CONCAT('Split settlement matched with settlement entry ',M.SettlementEntryId1),
		T.Status = 'Match'
	WHERE 1=1	
		AND SA.TransactionLedgerId IS NULL
		AND SB.TransactionLedgerId IS NULL
		AND SA.Id <> SB.Id;


-- Shows the number of SettlementEntry rows updated. 
SELECT ROW_COUNT() AS SettlementEntriesUpdated;



/* 
Temporary tables normally disappear when the connection closes,
*/
DROP TEMPORARY TABLE IF EXISTS TmpSettlementMatches20260711;


END //
DELIMITER ;

/*

CALL Reconciliation_MatchSplitSettlement('2026-07-01', '2026-07-11', 1);
CALL Reconciliation_MatchSplitSettlement(null, null, 2);

-- Optionally verify results
SELECT * FROM settlemententry 
	WHERE TransactionLedgerId IS NOT NULL 
	ORDER BY Id DESC LIMIT 10;

-- Show count of updated records
SELECT COUNT(*) AS TotalMatched 
	FROM settlemententry 
	WHERE TransactionLedgerId IS NOT NULL;

*/
