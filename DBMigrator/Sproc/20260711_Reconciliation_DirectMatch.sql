

DELIMITER //

DROP PROCEDURE IF EXISTS Reconciliation_DirectMatch //


CREATE PROCEDURE Reconciliation_DirectMatch( IN dtStart Date , IN dtEnd Date )
BEGIN
/*
Sproc:		Reconciliation_DirectMatch
Purpose:	Matches SettlementEntries to TransactionLedger items, in an exact way ( all matching and within time )
Author:		PM
Created:	2026.07.11
Returns:	Number of rows matched
Notes:		
Example:	
CALL Reconciliation_DirectMatch('2026-07-01', '2026-07-11');
*/

DROP TEMPORARY TABLE IF EXISTS TmpSettlementMatches20260711;
DROP TEMPORARY TABLE IF EXISTS TmpFirstSettlementMatches20260711;

-- Match exact, but could have dups
CREATE TEMPORARY TABLE TmpSettlementMatches20260711 AS
	SELECT
			S.Id AS SettlementEntryId,
			T.Id AS TransactionLedgerId,
			T.CapturedAt
		FROM settlemententry AS S
			INNER JOIN transactionledger AS T ON 
            (
				T.MerchantReferenceNo = S.MerchantRef
				AND T.CardType = S.CardType
				AND T.MerchantId = S.MerchantId
				AND T.CardLast4 = S.CardLast4
				AND T.GrossAmount = S.ExpectedGrossOriginalCents
				AND T.ExpectedInterchangeCents = S.InterchangeFeeCents
				AND T.ExpectedSettledCents = S.SettledAmountCents
				AND T.ExpectedProcessorFeeCents = S.ProcessorFeeCents

				AND S.SettlementDate IS NOT NULL
				AND T.CapturedAt IS NOT NULL

				-- AND S.SettlementDate > T.CapturedAt	-- After at all
				AND S.SettlementDate >= DATE(T.CapturedAt)	-- Could be on the same day?
				AND S.SettlementDate <= DATE_ADD(DATE(T.CapturedAt), INTERVAL 4 DAY)	-- Settlement must be within four calendar days of capture
                AND S.SettlementDate >= COALESCE(dtStart,S.SettlementDate)
                AND S.SettlementDate <= COALESCE(dtEnd,S.SettlementDate)
			)
		WHERE S.TransactionLedgerId IS NULL;


-- Indices for review
ALTER TABLE TmpSettlementMatches20260711 
	ADD INDEX Idx_TmpSettlementMatches20260711_SettlementEntryId (SettlementEntryId),
    ADD INDEX Idx_TmpSettlementMatches20260711_Order (SettlementEntryId, CapturedAt, TransactionLedgerId);


/*
Rank each TransactionLedger match by CapturedAt.
ROW_NUMBER guarantees exactly one selected row per SettlementEntry.
TransactionLedgerId is used as a deterministic tie-breaker when two ledger rows have the same CapturedAt ( which maybe shouldn't happen? )
*/

CREATE TEMPORARY TABLE TmpFirstSettlementMatches20260711 AS
	SELECT
			Ranked.SettlementEntryId,
			Ranked.TransactionLedgerId,
			Ranked.CapturedAt
		FROM
		(
			SELECT
					M.SettlementEntryId,
					M.TransactionLedgerId,
					M.CapturedAt,
					ROW_NUMBER() OVER	-- Window function
					(
						PARTITION BY M.SettlementEntryId
						ORDER BY
							M.CapturedAt ASC,
							M.TransactionLedgerId ASC
					) AS MatchNumber
				FROM TmpSettlementMatches20260711 AS M
		) AS Ranked
		WHERE Ranked.MatchNumber = 1;	-- Just get the first in the window


ALTER TABLE TmpFirstSettlementMatches20260711
    ADD PRIMARY KEY (SettlementEntryId),
    ADD INDEX Idx_TmpFirstSettlementMatches20260711_TransactionLedgerId (TransactionLedgerId);


/*
Update each SettlementEntry with its earliest matching ledger record.
*/

    
UPDATE settlemententry AS S
	INNER JOIN TmpFirstSettlementMatches20260711 AS M ON (M.SettlementEntryId = S.Id)
	INNER JOIN transactionledger AS T ON (T.Id = M.TransactionLedgerId)
	SET 
		S.TransactionLedgerId = M.TransactionLedgerId,
		S.Status = 'Match',
		T.Status = 'Match'
	WHERE S.TransactionLedgerId IS NULL 
	;


-- Shows the number of SettlementEntry rows updated. 
SELECT ROW_COUNT() AS SettlementEntriesUpdated;



/* 
Temporary tables normally disappear when the connection closes,
*/
DROP TEMPORARY TABLE IF EXISTS TmpFirstSettlementMatches20260711;
DROP TEMPORARY TABLE IF EXISTS TmpSettlementMatches20260711;


END //
DELIMITER ;

/*

CALL Reconciliation_DirectMatch('2026-07-01', '2026-07-11');
CALL Reconciliation_DirectMatch(null, null);

-- Optionally verify results
SELECT * FROM settlemententry 
	WHERE TransactionLedgerId IS NOT NULL 
	ORDER BY Id DESC LIMIT 10;

-- Show count of updated records
SELECT COUNT(*) AS TotalMatched 
	FROM settlemententry 
	WHERE TransactionLedgerId IS NOT NULL;

*/
