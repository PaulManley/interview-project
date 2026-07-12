DROP PROCEDURE IF EXISTS Reconciliation_MatchWithWiggleRoom;


CREATE PROCEDURE Reconciliation_MatchWithWiggleRoom( IN dtStart Date , IN dtEnd Date, IN WiggleAmount int )
BEGIN
/*
Sproc:		Reconciliation_MatchWithWiggleRoom
Purpose:	Matches SettlementEntries to TransactionLedger items, in an exact way ( all matching and within time )
Author:		PM
Created:	2026.07.11
Returns:	Number of rows matched
Notes:		
Example:	
CALL Reconciliation_MatchWithWiggleRoom('2026-07-01', '2026-07-11', 2);
*/

DROP TEMPORARY TABLE IF EXISTS TmpSettlementMatches20260711;
DROP TEMPORARY TABLE IF EXISTS TmpFirstSettlementMatches20260711;

-- This could be a LAAAARGE amount of data.
SELECT 
		S1.Id SE1Id, S2.Id SE2Id, 
		S1.ExpectedGrossOriginalCents + S1.InterchangeFeeCents + S1.SettledAmountCents + S1.ProcessorFeeCents +
        S2.ExpectedGrossOriginalCents + S2.InterchangeFeeCents + S2.SettledAmountCents + S2.ProcessorFeeCents 
	FROM settlemententry S1
		INNER JOIN settlemententry S2 ON 
        ( 
			1=1
			AND S1.CardType = S2.CardType
            AND ABS(DATEDIFF(S1.SettlementDate, S2.SettlementDate)) <= 4
            AND ( S2.MerchantRef IS NULL OR S2.MerchantRef = '' )
		)
	WHERE 1=1
		AND S1.TransactionLedgerId IS NULL
        AND S2.TransactionLedgerId IS NULL
        AND S1.SettlementDate >= COALESCE(dtStart,S1.SettlementDate)
		AND S1.SettlementDate <= COALESCE(dtEnd,S1.SettlementDate)
        AND S2.SettlementDate >= COALESCE(dtStart,S2.SettlementDate)
		AND S2.SettlementDate <= COALESCE(dtEnd,S2.SettlementDate)
        
	;


-- Match exact, but could have dups
CREATE TEMPORARY TABLE TmpSettlementMatches20260711 AS
	SELECT
			S.Id AS SettlementEntryId,
			T.Id AS TransactionLedgerId,
			T.CapturedAt
		FROM settlemententry AS S
			INNER JOIN transactionledger AS T ON 
            (
				1=1
				AND T.MerchantReferenceNo = S.MerchantRef
				AND T.CardType = S.CardType
				AND T.MerchantId = S.MerchantId
				AND T.CardLast4 = S.CardLast4
				AND 
				(
					((T.GrossAmount + T.ExpectedInterchangeCents + T.ExpectedSettledCents + T.ExpectedProcessorFeeCents) -
					(S.ExpectedGrossOriginalCents + S.InterchangeFeeCents + S.SettledAmountCents + S.ProcessorFeeCents ))
					< WiggleAmount
				)

				AND S.SettlementDate IS NOT NULL
				AND T.CapturedAt IS NOT NULL

				-- AND S.SettlementDate > T.CapturedAt	-- After at all
				AND S.SettlementDate >= DATE(T.CapturedAt)	-- Could be on the same day?
				AND S.SettlementDate <= DATE_ADD(DATE(T.CapturedAt), INTERVAL 4 DAY)	-- Settlement must be within four calendar days of capture
                AND S.SettlementDate >= COALESCE(dtStart,S.SettlementDate)
                AND S.SettlementDate <= COALESCE(dtEnd,S.SettlementDate)
                AND S.Status = 'Imported'
                AND T.Status = 'Imported'
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

/*    
UPDATE settlemententry AS S
	INNER JOIN TmpFirstSettlementMatches20260711 AS M ON (M.SettlementEntryId = S.Id)
	INNER JOIN transactionledger AS T ON (T.Id = M.TransactionLedgerId)
	SET 
		S.TransactionLedgerId = M.TransactionLedgerId,
		S.Status = 'Match',
		T.Status = 'Match',
		S.Notification = 'Off by a wiggle amount'
	WHERE S.TransactionLedgerId IS NULL 
	;
*/

-- Update SettlementEntry
UPDATE settlemententry AS S
	INNER JOIN TmpFirstSettlementMatches20260711 AS M ON ( M.SettlementEntryId = S.Id )
	SET
		S.TransactionLedgerId = M.TransactionLedgerId,
		S.Status = 'Match',
		S.Notification = 'Off by a wiggle amount'
	WHERE S.TransactionLedgerId IS NULL;

-- Shows the number of SettlementEntry rows updated. 
SELECT ROW_COUNT() AS SettlementEntriesUpdated;


-- Update TransactionLedger
UPDATE transactionledger AS T
	INNER JOIN TmpFirstSettlementMatches20260711 AS M ON ( M.TransactionLedgerId = T.Id )
	INNER JOIN settlemententry AS S ON ( S.Id = M.SettlementEntryId )
	SET
		T.Status = 'Match'
	WHERE S.TransactionLedgerId = M.TransactionLedgerId;


/* 
Temporary tables normally disappear when the connection closes,
*/
DROP TEMPORARY TABLE IF EXISTS TmpFirstSettlementMatches20260711;
DROP TEMPORARY TABLE IF EXISTS TmpSettlementMatches20260711;


END
;