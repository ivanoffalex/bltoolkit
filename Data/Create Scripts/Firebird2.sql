/*
Dual table FOR supporting queryies LIKE:
SELECT 1 AS id => SELECT 1 AS "id" *FROM Dual*
*/
CREATE TABLE Dual (Dummy  VARCHAR(10));
INSERT INTO  Dual (Dummy) VALUES ('X');
COMMIT;

-- Person Table

CREATE TABLE Person
(
	PersonID   INTEGER     NOT NULL  PRIMARY KEY,
	FirstName  VARCHAR(50) NOT NULL,
	LastName   VARCHAR(50) NOT NULL,
	MiddleName VARCHAR(50),
	Gender     CHAR(1)     NOT NULL CHECK (Gender in ('M', 'F', 'U', 'O'))
); 

CREATE GENERATOR PersonID;

CREATE GENERATOR TimestampGen;

SET TERM !!;

CREATE TRIGGER CREATE_PersonID FOR Person
BEFORE INSERT POSITION 0
AS BEGIN
	NEW.PersonID = GEN_ID(PersonID, 1); 
END!!

SET TERM ; !!

INSERT INTO Person (FirstName, LastName, Gender) VALUES ('John',   'Pupkin',    'M');
INSERT INTO Person (FirstName, LastName, Gender) VALUES ('Tester', 'Testerson', 'M');
COMMIT;

-- Doctor Table Extension

CREATE TABLE Doctor
(
	PersonID INTEGER     NOT NULL,
	Taxonomy VARCHAR(50) NOT NULL,
		FOREIGN KEY (PersonID) REFERENCES Person (PersonID)
			ON DELETE CASCADE

);

INSERT INTO Doctor (PersonID, Taxonomy) VALUES (1, 'Psychiatry');
COMMIT;

-- Patient Table Extension

CREATE TABLE Patient
(
	PersonID  int           NOT NULL,
	Diagnosis VARCHAR(256)  NOT NULL,
	FOREIGN KEY (PersonID) REFERENCES Person (PersonID)
			ON DELETE CASCADE
);

INSERT INTO Patient (PersonID, Diagnosis) VALUES (2, 'Hallucination with Paranoid Bugs'' Delirium of Persecution');
COMMIT;


SET TERM !!;

-- Person_SelectByKey

CREATE PROCEDURE Person_SelectByKey(id INTEGER)
RETURNS (
	PersonID   INTEGER,
	FirstName  VARCHAR(50),
	LastName   VARCHAR(50),
	MiddleName VARCHAR(50),
	Gender     CHAR(1)
	)
AS
BEGIN
	SELECT PersonID, FirstName, LastName, MiddleName, Gender FROM Person 
	WHERE PersonID = :id
	INTO
		:PersonID,   
		:FirstName,  
		:LastName,   
		:MiddleName, 
		:Gender ;     
	SUSPEND;
END!!

-- Person_SelectAll

CREATE PROCEDURE Person_SelectAll
RETURNS (
	PersonID   INTEGER,
	FirstName  VARCHAR(50),
	LastName   VARCHAR(50),
	MiddleName VARCHAR(50),
	Gender     CHAR(1)
	)
AS
BEGIN
	FOR 
		SELECT PersonID, FirstName, LastName, MiddleName, Gender FROM Person 
		INTO
			:PersonID,   
			:FirstName,  
			:LastName,   
			:MiddleName, 
			:Gender     
	DO SUSPEND;
END!!

-- Person_SelectByName

CREATE PROCEDURE Person_SelectByName (
	in_FirstName VARCHAR(50),
	in_LastName  VARCHAR(50)
	)
RETURNS (
	PersonID   int,
	FirstName  VARCHAR(50),
	LastName   VARCHAR(50),
	MiddleName VARCHAR(50),
	Gender     CHAR(1)
	)
AS
BEGIN

	FOR SELECT PersonID, FirstName, LastName, MiddleName, Gender FROM Person 
		WHERE FirstName LIKE :in_FirstName and LastName LIKE :in_LastName
	INTO
		:PersonID,   
		:FirstName,  
		:LastName,   
		:MiddleName, 
		:Gender 
	DO SUSPEND;
END!!

-- Person_Insert

CREATE PROCEDURE Person_Insert(
	FirstName  VARCHAR(50),
	LastName   VARCHAR(50),
	MiddleName VARCHAR(50),
	Gender     CHAR(1)
	)
RETURNS (PersonID INTEGER)
AS
BEGIN
	INSERT INTO Person
		( LastName,  FirstName,  MiddleName,  Gender)
	VALUES
		(:LastName, :FirstName, :MiddleName, :Gender);

	SELECT MAX(PersonID) FROM person
		INTO :PersonID;
	SUSPEND;
END!! 

-- Person_Insert_OutputParameter

CREATE PROCEDURE Person_Insert_OutputParameter(
	FirstName  VARCHAR(50),
	LastName   VARCHAR(50),
	MiddleName VARCHAR(50),
	Gender     CHAR(1)
	)
RETURNS (PersonID INTEGER)
AS
BEGIN
	INSERT INTO Person
		( LastName,  FirstName,  MiddleName,  Gender)
	VALUES
		(:LastName, :FirstName, :MiddleName, :Gender);

	SELECT max(PersonID) FROM person
	INTO :PersonID;
	SUSPEND;
END!! 

-- Person_Update

CREATE PROCEDURE Person_Update(
	PersonID   INTEGER,
	FirstName  VARCHAR(50),
	LastName   VARCHAR(50),
	MiddleName VARCHAR(50),
	Gender     CHAR(1)
	)
AS
BEGIN
	UPDATE
		Person
	SET
		LastName   = :LastName,
		FirstName  = :FirstName,
		MiddleName = :MiddleName,
		Gender     = :Gender
	WHERE
		PersonID = :PersonID;
END !!

-- Person_Delete

CREATE PROCEDURE Person_Delete(
	PersonID INTEGER
	)
AS
BEGIN
	DELETE FROM Person WHERE PersonID = :PersonID;
END !!

-- Patient_SelectAll

CREATE PROCEDURE Patient_SelectAll
RETURNS (
	PersonID   int,
	FirstName  VARCHAR(50),
	LastName   VARCHAR(50),
	MiddleName VARCHAR(50),
	Gender     CHAR(1),
	Diagnosis  VARCHAR(256)
	)
AS
BEGIN
	FOR 
		SELECT
			Person.PersonID, 
			FirstName,
			LastName,
			MiddleName,
			Gender,
			Patient.Diagnosis
		FROM
			Patient, Person
		WHERE
			Patient.PersonID = Person.PersonID
		INTO
			:PersonID,   
			:FirstName,  
			:LastName,   
			:MiddleName, 
			:Gender,
			:Diagnosis
	DO SUSPEND;
END !!

-- Patient_SelectByName

CREATE PROCEDURE Patient_SelectByName(
	FirstName VARCHAR(50),
	LastName  VARCHAR(50)
	)
RETURNS (
	PersonID   int,
	MiddleName VARCHAR(50),
	Gender     CHAR(1),
	Diagnosis  VARCHAR(256)
	)
AS
BEGIN
	FOR 
		SELECT
			Person.PersonID, 
			MiddleName,
			Gender,
			Patient.Diagnosis
		FROM
			Patient, Person
		WHERE
			Patient.PersonID = Person.PersonID
			and FirstName = :FirstName and LastName = :LastName
		INTO
			:PersonID,   
			:MiddleName, 
			:Gender,
			:Diagnosis
	DO SUSPEND;
END !!

SET TERM ; !!

-- BinaryData Table

CREATE TABLE BinaryData
(
	BinaryDataID INTEGER       NOT NULL PRIMARY KEY,
	Stamp        INTEGER       NOT NULL,
	Data         BLOB          NOT NULL
);


SET TERM !!;


CREATE TRIGGER CREATE_BinaryDataID FOR BinaryData
BEFORE INSERT POSITION 0
AS BEGIN
	NEW.BinaryDataID = GEN_ID(PersonID, 1); 
	NEW.Stamp = GEN_ID(TimestampGen, 1);
END!!

CREATE TRIGGER CHANGE_BinaryData FOR BinaryData
beFORe update 
AS BEGIN
	NEW.Stamp = GEN_ID(TimestampGen, 1);
END!! 

-- OutRefTest

/*
Fake input parameters are used to "emulate" input/output parameters.
Each inout parameter should be defined in RETURNS(...) section
and allso have a "mirror" in input section, mirror name shoul be:
FdpDataProvider.InOutInputParameterPrefix + [parameter name]
ex:
in_inputOutputID is input mirror FOR inout parameter inputOutputID
*/
CREATE PROCEDURE OutRefTest(
	ID					INTEGER,
	in_inputOutputID	INTEGER,
	str					VARCHAR(50),
	in_inputOutputStr	VARCHAR(50)
	)
RETURNS(
	inputOutputID  INTEGER,
	inputOutputStr VARCHAR(50),
	outputID       INTEGER,
	outputStr      VARCHAR(50)
	)
AS
BEGIN
	outputID       = ID;
	inputOutputID  = ID + in_inputOutputID;
	outputStr      = str;
	inputOutputStr = str || in_inputOutputStr;
	SUSPEND;
END !!

-- OutRefEnumTest

CREATE PROCEDURE OutRefEnumTest(
		str					VARCHAR(50),
		in_inputOutputStr	VARCHAR(50)
		)
RETURNS (
	inputOutputStr VARCHAR(50),
	outputStr      VARCHAR(50)
	)
AS
BEGIN
	outputStr      = str;
	inputOutputStr = str || in_inputOutputStr;
	SUSPEND;
END !!

-- ExecuteScalarTest

CREATE PROCEDURE Scalar_DataReader
RETURNS(
	intField	INTEGER,
	stringField	VARCHAR(50)
	)
AS
BEGIN
	intField = 12345;
	stringField = '54321';
	SUSPEND;
END!!

CREATE PROCEDURE Scalar_OutputParameter
RETURNS (
	outputInt      INTEGER,
	outputString   VARCHAR(50)
	)
AS
BEGIN
	outputInt = 12345;
	outputString = '54321';
	SUSPEND;
END!!

/*
"Return_Value" is the name for ReturnValue "emulating"
may be changed: FdpDataProvider.ReturnParameterName
*/
CREATE PROCEDURE Scalar_ReturnParameter
RETURNS (Return_Value INTEGER)
AS
BEGIN
	Return_Value = 12345;
	SUSPEND;
END!!

SET TERM ; !!

-- Data Types test

/*
Data definitions according to:
http://www.firebirdsql.org/manual/migration-mssql-data-types.html

BUT! BLOB is ised for BINARY data! not CHAR
*/

CREATE TABLE DataTypeTest
(
	DataTypeID      INTEGER NOT NULL PRIMARY KEY,
	Binary_         BLOB						,
	Boolean_        CHAR(1)						,
	Byte_           SMALLINT					,
	Bytes_          BLOB						,
	CHAR_           CHAR(1)						,
	DateTime_       TIMESTAMP					,
	Decimal_        DECIMAL(10, 2)				,
	Double_         DOUBLE	PRECISION			,
	Guid_           CHAR(38)					,
	Int16_          SMALLINT					,
	Int32_          INTEGER						,
	Int64_          NUMERIC(11)					,
	Money_          DECIMAL(18, 4)				,
	SByte_          SMALLINT					,
	Single_         FLOAT						,
	Stream_         BLOB						,
	String_         VARCHAR(50) 
			CHARACTER SET UNICODE_FSS			,
	UInt16_         SMALLINT					,
	UInt32_         INTEGER						,
	UInt64_         NUMERIC(11)					,
	Xml_            CHAR(1000)     
) ;


CREATE GENERATOR DataTypeID;

SET TERM !!;

CREATE TRIGGER CREATE_DataTypeTest FOR DataTypeTest
BEFORE INSERT POSITION 0
AS BEGIN
	NEW.DataTypeID = GEN_ID(DataTypeID, 1); 
END!!

SET TERM ; !!

INSERT INTO DataTypeTest
	(Binary_, Boolean_,   Byte_,  Bytes_,  CHAR_,  DateTime_, Decimal_,
	 Double_,    Guid_,  Int16_,  Int32_,  Int64_,    Money_,   SByte_,
	 Single_,  Stream_, String_, UInt16_, UInt32_,   UInt64_,     Xml_)
VALUES
	(   NULL,     NULL,    NULL,    NULL,    NULL,      NULL,     NULL,
	    NULL,     NULL,    NULL,    NULL,    NULL,      NULL,     NULL,
	    NULL,     NULL,    NULL,    NULL,    NULL,      NULL,     NULL);

COMMIT;

INSERT INTO DataTypeTest
	(Binary_,	Boolean_,	Byte_,   Bytes_,  CHAR_,	DateTime_, Decimal_,
	 Double_,	Guid_,		Int16_,  Int32_,  Int64_,    Money_,   SByte_,
	 Single_,	Stream_,	String_, UInt16_, UInt32_,   UInt64_,
	 Xml_)
VALUES
	('dddddddddddddddd', 1,  255,'dddddddddddddddd', 'B', 'NOW', 12345.67,
	1234.567, 'dddddddddddddddddddddddddddddddd', 32767, 32768, 1000000, 12.3456, 127,
	1234.123, 'dddddddddddddddd', 'string', 32767, 32768, 200000000,
	'<root><element strattr="strvalue" intattr="12345"/></root>');

COMMIT;