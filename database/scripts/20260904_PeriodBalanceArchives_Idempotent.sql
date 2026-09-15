IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722080000_AddTypesMatiereAndDechetsFonderie'
)
BEGIN
    CREATE TABLE [TypesMatiere] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(30) NOT NULL,
        [Libelle] nvarchar(100) NOT NULL,
        [EstActif] bit NOT NULL,
        [DateCreationUtc] datetime2 NOT NULL,
        [DateModificationUtc] datetime2 NULL,
        CONSTRAINT [PK_TypesMatiere] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722080000_AddTypesMatiereAndDechetsFonderie'
)
BEGIN
    CREATE TABLE [DechetsFonderie] (
        [Id] uniqueidentifier NOT NULL,
        [DateDeclaration] date NOT NULL,
        [SemaineIso] int NOT NULL,
        [TypeMatiereId] uniqueidentifier NOT NULL,
        [TypeDechet] nvarchar(20) NOT NULL,
        [PoidsKg] decimal(18,3) NOT NULL,
        [Observation] nvarchar(500) NULL,
        [Statut] nvarchar(20) NOT NULL,
        [CreeParUtilisateurId] uniqueidentifier NULL,
        [CreeParNom] nvarchar(150) NULL,
        [DateCreationUtc] datetime2 NOT NULL,
        [DateModificationUtc] datetime2 NULL,
        [EstSupprime] bit NOT NULL,
        [DateSuppressionUtc] datetime2 NULL,
        CONSTRAINT [PK_DechetsFonderie] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DechetsFonderie_TypesMatiere_TypeMatiereId] FOREIGN KEY ([TypeMatiereId]) REFERENCES [TypesMatiere] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722080000_AddTypesMatiereAndDechetsFonderie'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'DateCreationUtc', N'DateModificationUtc', N'EstActif', N'Libelle') AND [object_id] = OBJECT_ID(N'[TypesMatiere]'))
        SET IDENTITY_INSERT [TypesMatiere] ON;
    EXEC(N'INSERT INTO [TypesMatiere] ([Id], [Code], [DateCreationUtc], [DateModificationUtc], [EstActif], [Libelle])
    VALUES (''30000000-0000-0000-0000-000000000001'', N''M3000L0'', ''2026-01-01T00:00:00.0000000Z'', NULL, CAST(1 AS bit), N''Matière M3000L0''),
    (''30100000-0000-0000-0000-000000000001'', N''M3010L0'', ''2026-01-01T00:00:00.0000000Z'', NULL, CAST(1 AS bit), N''Matière M3010L0''),
    (''30600000-0000-0000-0000-000000000001'', N''M3060L0'', ''2026-01-01T00:00:00.0000000Z'', NULL, CAST(1 AS bit), N''Matière M3060L0'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'DateCreationUtc', N'DateModificationUtc', N'EstActif', N'Libelle') AND [object_id] = OBJECT_ID(N'[TypesMatiere]'))
        SET IDENTITY_INSERT [TypesMatiere] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722080000_AddTypesMatiereAndDechetsFonderie'
)
BEGIN
    CREATE INDEX [IX_DechetsFonderie_DateDeclaration] ON [DechetsFonderie] ([DateDeclaration]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722080000_AddTypesMatiereAndDechetsFonderie'
)
BEGIN
    CREATE INDEX [IX_DechetsFonderie_EstSupprime] ON [DechetsFonderie] ([EstSupprime]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722080000_AddTypesMatiereAndDechetsFonderie'
)
BEGIN
    CREATE INDEX [IX_DechetsFonderie_TypeDechet] ON [DechetsFonderie] ([TypeDechet]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722080000_AddTypesMatiereAndDechetsFonderie'
)
BEGIN
    CREATE INDEX [IX_DechetsFonderie_TypeMatiereId] ON [DechetsFonderie] ([TypeMatiereId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722080000_AddTypesMatiereAndDechetsFonderie'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_TypesMatiere_Code] ON [TypesMatiere] ([Code]) WHERE [Code] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722080000_AddTypesMatiereAndDechetsFonderie'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260722080000_AddTypesMatiereAndDechetsFonderie', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723113401_AddImportX3Tables'
)
BEGIN
    CREATE TABLE [ImportX3Batches] (
        [Id] uniqueidentifier NOT NULL,
        [HashCoupleSha256] nvarchar(64) NOT NULL,
        [Statut] nvarchar(30) NOT NULL,
        [LignesEncoursLues] int NOT NULL,
        [LignesEncoursValides] int NOT NULL,
        [LignesMouvementsLues] int NOT NULL,
        [LignesMouvementsValides] int NOT NULL,
        [NombreSortiesGenerees] int NOT NULL,
        [NombreAnomalies] int NOT NULL,
        [CreeParUtilisateurId] uniqueidentifier NULL,
        [CreeParNom] nvarchar(150) NULL,
        [DateCreationUtc] datetime2 NOT NULL,
        [DateFinUtc] datetime2 NULL,
        CONSTRAINT [PK_ImportX3Batches] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723113401_AddImportX3Tables'
)
BEGIN
    CREATE TABLE [ImportX3Anomalies] (
        [Id] uniqueidentifier NOT NULL,
        [ImportBatchId] uniqueidentifier NOT NULL,
        [TypeFichier] nvarchar(30) NOT NULL,
        [NumeroLigneSource] int NULL,
        [Statut] nvarchar(30) NOT NULL,
        [Message] nvarchar(1000) NOT NULL,
        [NumeroOF] nvarchar(50) NULL,
        [CodeTypeMatiere] nvarchar(80) NULL,
        [NomFeuilleSource] nvarchar(128) NULL,
        [DateCreationUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_ImportX3Anomalies] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ImportX3Anomalies_ImportX3Batches_ImportBatchId] FOREIGN KEY ([ImportBatchId]) REFERENCES [ImportX3Batches] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723113401_AddImportX3Tables'
)
BEGIN
    CREATE TABLE [ImportX3Files] (
        [Id] uniqueidentifier NOT NULL,
        [ImportBatchId] uniqueidentifier NOT NULL,
        [TypeFichier] nvarchar(30) NOT NULL,
        [NomFichier] nvarchar(260) NOT NULL,
        [Extension] nvarchar(10) NOT NULL,
        [TailleOctets] bigint NOT NULL,
        [HashSha256] nvarchar(64) NOT NULL,
        [DateCreationUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_ImportX3Files] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ImportX3Files_ImportX3Batches_ImportBatchId] FOREIGN KEY ([ImportBatchId]) REFERENCES [ImportX3Batches] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723113401_AddImportX3Tables'
)
BEGIN
    CREATE TABLE [OrdresFabrication] (
        [Id] uniqueidentifier NOT NULL,
        [NumeroOF] nvarchar(50) NOT NULL,
        [ArticlePrincipal] nvarchar(80) NULL,
        [DesignationPrincipale] nvarchar(250) NULL,
        [SituationOF] nvarchar(80) NULL,
        [Atelier] nvarchar(30) NULL,
        [DateObjectifOF] date NULL,
        [StatutX3] nvarchar(80) NULL,
        [DernierImportBatchId] uniqueidentifier NOT NULL,
        [DateCreationUtc] datetime2 NOT NULL,
        [DateModificationUtc] datetime2 NULL,
        CONSTRAINT [PK_OrdresFabrication] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrdresFabrication_ImportX3Batches_DernierImportBatchId] FOREIGN KEY ([DernierImportBatchId]) REFERENCES [ImportX3Batches] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723113401_AddImportX3Tables'
)
BEGIN
    CREATE TABLE [ConsommationsComposants] (
        [Id] uniqueidentifier NOT NULL,
        [ImportBatchId] uniqueidentifier NOT NULL,
        [DateImputation] date NOT NULL,
        [OrdreFabricationId] uniqueidentifier NOT NULL,
        [NumeroOF] nvarchar(50) NOT NULL,
        [ArticlePrincipal] nvarchar(80) NOT NULL,
        [DesignationPrincipale] nvarchar(250) NULL,
        [CodeTypeMatiere] nvarchar(80) NOT NULL,
        [DesignationComposant] nvarchar(250) NULL,
        [Unite] nvarchar(20) NOT NULL,
        [CleMetier] nvarchar(250) NOT NULL,
        [SommeQuantiteX3Signee] decimal(18,4) NOT NULL,
        [Consommation] decimal(18,4) NOT NULL,
        [Statut] nvarchar(30) NOT NULL,
        [DateCreationUtc] datetime2 NOT NULL,
        [DateMiseAJourUtc] datetime2 NULL,
        CONSTRAINT [PK_ConsommationsComposants] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ConsommationsComposants_ImportX3Batches_ImportBatchId] FOREIGN KEY ([ImportBatchId]) REFERENCES [ImportX3Batches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ConsommationsComposants_OrdresFabrication_OrdreFabricationId] FOREIGN KEY ([OrdreFabricationId]) REFERENCES [OrdresFabrication] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723113401_AddImportX3Tables'
)
BEGIN
    CREATE TABLE [OperationsOF] (
        [Id] uniqueidentifier NOT NULL,
        [OrdreFabricationId] uniqueidentifier NOT NULL,
        [NumeroLigneSource] int NOT NULL,
        [NumeroOperation] nvarchar(30) NOT NULL,
        [CodePoste] nvarchar(50) NOT NULL,
        [TypePoste] nvarchar(30) NULL,
        [NumeroPoste] nvarchar(30) NULL,
        [EffectifPoste] nvarchar(30) NULL,
        [Cad] nvarchar(30) NULL,
        [EffectifOperation] nvarchar(30) NULL,
        [QuantitePrevue] decimal(18,4) NULL,
        [QuantiteRestante] decimal(18,4) NULL,
        [DateOperation] date NULL,
        [ImportBatchId] uniqueidentifier NOT NULL,
        [DateCreationUtc] datetime2 NOT NULL,
        [DateModificationUtc] datetime2 NULL,
        CONSTRAINT [PK_OperationsOF] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OperationsOF_ImportX3Batches_ImportBatchId] FOREIGN KEY ([ImportBatchId]) REFERENCES [ImportX3Batches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OperationsOF_OrdresFabrication_OrdreFabricationId] FOREIGN KEY ([OrdreFabricationId]) REFERENCES [OrdresFabrication] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723113401_AddImportX3Tables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ConsommationsComposants_CleMetier] ON [ConsommationsComposants] ([CleMetier]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723113401_AddImportX3Tables'
)
BEGIN
    CREATE INDEX [IX_ConsommationsComposants_CodeTypeMatiere] ON [ConsommationsComposants] ([CodeTypeMatiere]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723113401_AddImportX3Tables'
)
BEGIN
    CREATE INDEX [IX_ConsommationsComposants_ImportBatchId] ON [ConsommationsComposants] ([ImportBatchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723113401_AddImportX3Tables'
)
BEGIN
    CREATE INDEX [IX_ConsommationsComposants_NumeroOF] ON [ConsommationsComposants] ([NumeroOF]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723113401_AddImportX3Tables'
)
BEGIN
    CREATE INDEX [IX_ConsommationsComposants_OrdreFabricationId] ON [ConsommationsComposants] ([OrdreFabricationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723113401_AddImportX3Tables'
)
BEGIN
    CREATE INDEX [IX_ImportX3Anomalies_ImportBatchId] ON [ImportX3Anomalies] ([ImportBatchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723113401_AddImportX3Tables'
)
BEGIN
    CREATE INDEX [IX_ImportX3Batches_HashCoupleSha256] ON [ImportX3Batches] ([HashCoupleSha256]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723113401_AddImportX3Tables'
)
BEGIN
    CREATE INDEX [IX_ImportX3Files_ImportBatchId] ON [ImportX3Files] ([ImportBatchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723113401_AddImportX3Tables'
)
BEGIN
    CREATE INDEX [IX_OperationsOF_ImportBatchId] ON [OperationsOF] ([ImportBatchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723113401_AddImportX3Tables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_OperationsOF_OrdreFabricationId_NumeroOperation_CodePoste] ON [OperationsOF] ([OrdreFabricationId], [NumeroOperation], [CodePoste]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723113401_AddImportX3Tables'
)
BEGIN
    CREATE INDEX [IX_OrdresFabrication_ArticlePrincipal] ON [OrdresFabrication] ([ArticlePrincipal]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723113401_AddImportX3Tables'
)
BEGIN
    CREATE INDEX [IX_OrdresFabrication_DernierImportBatchId] ON [OrdresFabrication] ([DernierImportBatchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723113401_AddImportX3Tables'
)
BEGIN
    CREATE UNIQUE INDEX [IX_OrdresFabrication_NumeroOF] ON [OrdresFabrication] ([NumeroOF]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723113401_AddImportX3Tables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260723113401_AddImportX3Tables', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082920_AddManufacturingReferencesPersistence'
)
BEGIN
    CREATE TABLE [ImportBatches] (
        [Id] uniqueidentifier NOT NULL,
        [HashFichierSha256] nvarchar(64) NOT NULL,
        [TypeImport] int NOT NULL,
        [ProfilImport] nvarchar(100) NULL,
        [Statut] int NOT NULL,
        [CreeParUtilisateurId] uniqueidentifier NULL,
        [CreeParNom] nvarchar(max) NULL,
        [DateCreationUtc] datetime2 NOT NULL,
        [DateFinUtc] datetime2 NULL,
        CONSTRAINT [PK_ImportBatches] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082920_AddManufacturingReferencesPersistence'
)
BEGIN
    CREATE TABLE [RefGammesFabrication] (
        [Id] uniqueidentifier NOT NULL,
        [CodeArticle] nvarchar(100) NOT NULL,
        [AlternativeGamme] nvarchar(50) NULL,
        [NumeroOperation] nvarchar(50) NOT NULL,
        [CodePoste] nvarchar(50) NOT NULL,
        [CleMetier] nvarchar(500) NOT NULL,
        [SourceSystem] nvarchar(100) NOT NULL,
        [ImportBatchId] uniqueidentifier NOT NULL,
        [ImportedAt] datetime2 NOT NULL,
        [SourceUpdatedAt] datetime2 NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_RefGammesFabrication] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RefGammesFabrication_ImportBatches_ImportBatchId] FOREIGN KEY ([ImportBatchId]) REFERENCES [ImportBatches] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082920_AddManufacturingReferencesPersistence'
)
BEGIN
    CREATE TABLE [RefNomenclatures] (
        [Id] uniqueidentifier NOT NULL,
        [CodeArticleParent] nvarchar(100) NOT NULL,
        [AlternativeNomenclature] nvarchar(50) NULL,
        [CodeParentNiveau] nvarchar(50) NOT NULL,
        [CodeComposant] nvarchar(100) NOT NULL,
        [Niveau] nvarchar(50) NOT NULL,
        [Sequence] nvarchar(50) NOT NULL,
        [CleMetier] nvarchar(500) NOT NULL,
        [SourceSystem] nvarchar(100) NOT NULL,
        [ImportBatchId] uniqueidentifier NOT NULL,
        [ImportedAt] datetime2 NOT NULL,
        [SourceUpdatedAt] datetime2 NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_RefNomenclatures] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RefNomenclatures_ImportBatches_ImportBatchId] FOREIGN KEY ([ImportBatchId]) REFERENCES [ImportBatches] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082920_AddManufacturingReferencesPersistence'
)
BEGIN
    CREATE INDEX [IX_ImportBatches_HashFichierSha256] ON [ImportBatches] ([HashFichierSha256]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082920_AddManufacturingReferencesPersistence'
)
BEGIN
    CREATE INDEX [IX_ImportBatches_ProfilImport] ON [ImportBatches] ([ProfilImport]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082920_AddManufacturingReferencesPersistence'
)
BEGIN
    CREATE INDEX [IX_ImportBatches_TypeImport] ON [ImportBatches] ([TypeImport]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082920_AddManufacturingReferencesPersistence'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RefGammesFabrication_CleMetier] ON [RefGammesFabrication] ([CleMetier]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082920_AddManufacturingReferencesPersistence'
)
BEGIN
    CREATE INDEX [IX_RefGammesFabrication_CodeArticle] ON [RefGammesFabrication] ([CodeArticle]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082920_AddManufacturingReferencesPersistence'
)
BEGIN
    CREATE INDEX [IX_RefGammesFabrication_ImportBatchId] ON [RefGammesFabrication] ([ImportBatchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082920_AddManufacturingReferencesPersistence'
)
BEGIN
    CREATE INDEX [IX_RefGammesFabrication_IsActive] ON [RefGammesFabrication] ([IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082920_AddManufacturingReferencesPersistence'
)
BEGIN
    CREATE INDEX [IX_RefGammesFabrication_NumeroOperation] ON [RefGammesFabrication] ([NumeroOperation]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082920_AddManufacturingReferencesPersistence'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RefNomenclatures_CleMetier] ON [RefNomenclatures] ([CleMetier]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082920_AddManufacturingReferencesPersistence'
)
BEGIN
    CREATE INDEX [IX_RefNomenclatures_CodeArticleParent] ON [RefNomenclatures] ([CodeArticleParent]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082920_AddManufacturingReferencesPersistence'
)
BEGIN
    CREATE INDEX [IX_RefNomenclatures_CodeComposant] ON [RefNomenclatures] ([CodeComposant]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082920_AddManufacturingReferencesPersistence'
)
BEGIN
    CREATE INDEX [IX_RefNomenclatures_ImportBatchId] ON [RefNomenclatures] ([ImportBatchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082920_AddManufacturingReferencesPersistence'
)
BEGIN
    CREATE INDEX [IX_RefNomenclatures_IsActive] ON [RefNomenclatures] ([IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082920_AddManufacturingReferencesPersistence'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260724082920_AddManufacturingReferencesPersistence', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    ALTER TABLE [RefNomenclatures] ADD [CategorieComposant] nvarchar(80) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    ALTER TABLE [RefNomenclatures] ADD [DesignationComposant] nvarchar(250) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    ALTER TABLE [RefNomenclatures] ADD [PourcentageRebut] decimal(18,6) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    ALTER TABLE [RefNomenclatures] ADD [QuantiteLien] decimal(18,6) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    ALTER TABLE [RefNomenclatures] ADD [QuantiteLienCumulee] decimal(18,6) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    ALTER TABLE [RefNomenclatures] ADD [TypeLien] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    ALTER TABLE [RefNomenclatures] ADD [UniteQuantite] nvarchar(20) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    ALTER TABLE [RefGammesFabrication] ADD [Cadence] decimal(18,6) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    ALTER TABLE [RefGammesFabrication] ADD [CodeAtelier] nvarchar(50) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    ALTER TABLE [RefGammesFabrication] ADD [CoefficientCcpt] decimal(18,6) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    ALTER TABLE [RefGammesFabrication] ADD [CoefficientOperationStock] decimal(18,6) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    ALTER TABLE [RefGammesFabrication] ADD [DateFinValidite] date NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    ALTER TABLE [RefGammesFabrication] ADD [DateModificationX3] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    ALTER TABLE [RefGammesFabrication] ADD [DesignationArticle] nvarchar(250) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    ALTER TABLE [RefGammesFabrication] ADD [DesignationOperation] nvarchar(250) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    ALTER TABLE [RefGammesFabrication] ADD [EtapeSuivi] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    ALTER TABLE [RefGammesFabrication] ADD [GroupePosteCharge] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    ALTER TABLE [RefGammesFabrication] ADD [Jalon] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    ALTER TABLE [RefGammesFabrication] ADD [NombrePostes] decimal(18,4) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    ALTER TABLE [RefGammesFabrication] ADD [Quantite] decimal(18,6) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    ALTER TABLE [RefGammesFabrication] ADD [TempsAttente] decimal(18,6) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    ALTER TABLE [RefGammesFabrication] ADD [TempsCycle] decimal(18,6) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    ALTER TABLE [RefGammesFabrication] ADD [TempsReglage] decimal(18,6) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    ALTER TABLE [RefGammesFabrication] ADD [Trs] decimal(18,6) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    ALTER TABLE [RefGammesFabrication] ADD [UniteOperation] nvarchar(20) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    ALTER TABLE [ImportBatches] ADD [NombreAnomalies] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    ALTER TABLE [ImportBatches] ADD [NombreLignesImportees] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    ALTER TABLE [ImportBatches] ADD [NombreLignesLues] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    ALTER TABLE [ImportBatches] ADD [NombreLignesValides] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    UPDATE RefGammesFabrication
    SET CleMetier = UPPER(LTRIM(RTRIM(CodeArticle))) + '|' +
        UPPER(LTRIM(RTRIM(NumeroOperation))) + '|' +
        UPPER(LTRIM(RTRIM(ISNULL(CodeAtelier, '')))) + '|' +
        UPPER(LTRIM(RTRIM(ISNULL(CodePoste, '')))) + '|' +
        UPPER(LTRIM(RTRIM(ISNULL(AlternativeGamme, ''))));
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    CREATE TABLE [ImportReferenceAnomalies] (
        [Id] uniqueidentifier NOT NULL,
        [ImportBatchId] uniqueidentifier NOT NULL,
        [TypeImport] int NOT NULL,
        [NumeroLigneSource] int NOT NULL,
        [NomFeuilleSource] nvarchar(128) NULL,
        [Message] nvarchar(1000) NOT NULL,
        [DateCreationUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_ImportReferenceAnomalies] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ImportReferenceAnomalies_ImportBatches_ImportBatchId] FOREIGN KEY ([ImportBatchId]) REFERENCES [ImportBatches] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    CREATE TABLE [X3RapportsMachine] (
        [Id] uniqueidentifier NOT NULL,
        [DateProduction] date NOT NULL,
        [CodePoste] nvarchar(50) NOT NULL,
        [NumeroOperation] nvarchar(50) NOT NULL,
        [CodeArticle] nvarchar(100) NOT NULL,
        [DesignationArticle] nvarchar(250) NULL,
        [NumeroOF] nvarchar(50) NOT NULL,
        [Equipe] nvarchar(50) NULL,
        [QuantiteEMR] decimal(18,4) NOT NULL,
        [QuantiteRebutEMR] decimal(18,4) NOT NULL,
        [AlternativeGamme] nvarchar(50) NULL,
        [CleMetier] nvarchar(500) NOT NULL,
        [SourceSystem] nvarchar(100) NOT NULL,
        [ImportBatchId] uniqueidentifier NOT NULL,
        [ImportedAt] datetime2 NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_X3RapportsMachine] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_X3RapportsMachine_ImportBatches_ImportBatchId] FOREIGN KEY ([ImportBatchId]) REFERENCES [ImportBatches] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    CREATE INDEX [IX_RefGammesFabrication_CodeAtelier] ON [RefGammesFabrication] ([CodeAtelier]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    CREATE INDEX [IX_ImportReferenceAnomalies_ImportBatchId] ON [ImportReferenceAnomalies] ([ImportBatchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    CREATE UNIQUE INDEX [IX_X3RapportsMachine_CleMetier] ON [X3RapportsMachine] ([CleMetier]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    CREATE INDEX [IX_X3RapportsMachine_ImportBatchId] ON [X3RapportsMachine] ([ImportBatchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    CREATE INDEX [IX_X3RapportsMachine_IsActive] ON [X3RapportsMachine] ([IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    CREATE INDEX [IX_X3RapportsMachine_NumeroOF_CodeArticle] ON [X3RapportsMachine] ([NumeroOF], [CodeArticle]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092133_AddRapportMachineAndReferenceColumns'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260724092133_AddRapportMachineAndReferenceColumns', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092829_AddArticleResolutionStatus'
)
BEGIN
    ALTER TABLE [OrdresFabrication] ADD [StatutArticle] nvarchar(50) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724092829_AddArticleResolutionStatus'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260724092829_AddArticleResolutionStatus', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724131136_RenameRapportMachineQuantitesAddOrigineProcessus'
)
BEGIN
    EXEC sp_rename N'[X3RapportsMachine].[QuantiteRebutEMR]', N'QuantiteRebut', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724131136_RenameRapportMachineQuantitesAddOrigineProcessus'
)
BEGIN
    EXEC sp_rename N'[X3RapportsMachine].[QuantiteEMR]', N'QuantiteCTRL', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724131136_RenameRapportMachineQuantitesAddOrigineProcessus'
)
BEGIN
    ALTER TABLE [X3RapportsMachine] ADD [OrigineProcessus] nvarchar(20) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724131136_RenameRapportMachineQuantitesAddOrigineProcessus'
)
BEGIN
    CREATE TABLE [ControlesPoidsFonderie] (
        [Id] uniqueidentifier NOT NULL,
        [DateControle] date NOT NULL,
        [NumeroOF] nvarchar(50) NULL,
        [CodeArticle] nvarchar(80) NOT NULL,
        [TypeMatiereId] uniqueidentifier NOT NULL,
        [NumeroLotOuSerie] nvarchar(80) NULL,
        [PoidsPhysiqueUnitaireKg] decimal(18,6) NOT NULL,
        [NombreEchantillons] int NULL,
        [Observation] nvarchar(500) NULL,
        [Statut] nvarchar(20) NOT NULL DEFAULT N'Brouillon',
        [CreeParUtilisateurId] uniqueidentifier NULL,
        [CreeParNom] nvarchar(120) NULL,
        [CreeLeUtc] datetime2 NOT NULL,
        [ValideParUtilisateurId] uniqueidentifier NULL,
        [ValideParNom] nvarchar(120) NULL,
        [ValideLeUtc] datetime2 NULL,
        [ModifieLeUtc] datetime2 NULL,
        [EstSupprime] bit NOT NULL,
        CONSTRAINT [PK_ControlesPoidsFonderie] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ControlesPoidsFonderie_TypesMatiere_TypeMatiereId] FOREIGN KEY ([TypeMatiereId]) REFERENCES [TypesMatiere] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724131136_RenameRapportMachineQuantitesAddOrigineProcessus'
)
BEGIN
    CREATE INDEX [IX_X3RapportsMachine_OrigineProcessus] ON [X3RapportsMachine] ([OrigineProcessus]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724131136_RenameRapportMachineQuantitesAddOrigineProcessus'
)
BEGIN
    CREATE INDEX [IX_ControlesPoidsFonderie_CodeArticle_TypeMatiereId_DateControle] ON [ControlesPoidsFonderie] ([CodeArticle], [TypeMatiereId], [DateControle]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724131136_RenameRapportMachineQuantitesAddOrigineProcessus'
)
BEGIN
    CREATE INDEX [IX_ControlesPoidsFonderie_NumeroOF] ON [ControlesPoidsFonderie] ([NumeroOF]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724131136_RenameRapportMachineQuantitesAddOrigineProcessus'
)
BEGIN
    CREATE INDEX [IX_ControlesPoidsFonderie_TypeMatiereId] ON [ControlesPoidsFonderie] ([TypeMatiereId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724131136_RenameRapportMachineQuantitesAddOrigineProcessus'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260724131136_RenameRapportMachineQuantitesAddOrigineProcessus', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725122850_WidenRefNomenclatureTextColumns'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RefNomenclatures]') AND [c].[name] = N'UniteQuantite');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [RefNomenclatures] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [RefNomenclatures] ALTER COLUMN [UniteQuantite] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725122850_WidenRefNomenclatureTextColumns'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RefNomenclatures]') AND [c].[name] = N'TypeLien');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [RefNomenclatures] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [RefNomenclatures] ALTER COLUMN [TypeLien] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725122850_WidenRefNomenclatureTextColumns'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RefNomenclatures]') AND [c].[name] = N'CategorieComposant');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [RefNomenclatures] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [RefNomenclatures] ALTER COLUMN [CategorieComposant] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725122850_WidenRefNomenclatureTextColumns'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260725122850_WidenRefNomenclatureTextColumns', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726121456_AddRefPostesProcessus'
)
BEGIN
    CREATE TABLE [RefPostesProcessus] (
        [Id] uniqueidentifier NOT NULL,
        [CodePoste] nvarchar(50) NOT NULL,
        [LibellePoste] nvarchar(200) NULL,
        [OrigineProcessus] nvarchar(20) NOT NULL,
        [IsActive] bit NOT NULL,
        [DateCreationUtc] datetime2 NOT NULL,
        [DateModificationUtc] datetime2 NULL,
        [CreePar] nvarchar(150) NULL,
        [ModifiePar] nvarchar(150) NULL,
        CONSTRAINT [PK_RefPostesProcessus] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726121456_AddRefPostesProcessus'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RefPostesProcessus_CodePoste] ON [RefPostesProcessus] ([CodePoste]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726121456_AddRefPostesProcessus'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260726121456_AddRefPostesProcessus', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726135531_AddImportSourceFiles'
)
BEGIN
    CREATE TABLE [ImportSourceFiles] (
        [Id] uniqueidentifier NOT NULL,
        [ImportBatchId] uniqueidentifier NOT NULL,
        [TypeImport] int NOT NULL,
        [ProfilImport] nvarchar(100) NULL,
        [NomFichierOriginal] nvarchar(260) NOT NULL,
        [Extension] nvarchar(20) NOT NULL,
        [MimeType] nvarchar(150) NOT NULL,
        [TailleOctets] bigint NOT NULL,
        [HashSha256] nvarchar(64) NOT NULL,
        [CheminStockage] nvarchar(500) NOT NULL,
        [DateUploadUtc] datetime2 NOT NULL,
        [UploadeParUtilisateurId] uniqueidentifier NULL,
        [UploadeParNom] nvarchar(150) NULL,
        CONSTRAINT [PK_ImportSourceFiles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726135531_AddImportSourceFiles'
)
BEGIN
    CREATE INDEX [IX_ImportSourceFiles_HashSha256] ON [ImportSourceFiles] ([HashSha256]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726135531_AddImportSourceFiles'
)
BEGIN
    CREATE INDEX [IX_ImportSourceFiles_ImportBatchId] ON [ImportSourceFiles] ([ImportBatchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726135531_AddImportSourceFiles'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260726135531_AddImportSourceFiles', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726171843_AddReceptionsUsinage2'
)
BEGIN
    CREATE TABLE [ReceptionsUsinage2] (
        [Id] uniqueidentifier NOT NULL,
        [NumeroOF] nvarchar(50) NOT NULL,
        [DateReception] date NOT NULL,
        [NombrePiecesReceptionnees] decimal(18,3) NOT NULL,
        [PoidsTotalReceptionneKg] decimal(18,6) NULL,
        [Origine] nvarchar(100) NULL,
        [Observation] nvarchar(500) NULL,
        [CodeArticle] nvarchar(100) NULL,
        [DesignationArticle] nvarchar(250) NULL,
        [SituationX3] nvarchar(100) NULL,
        [AtelierOrigine] nvarchar(50) NULL,
        [CodeTypeMatiere] nvarchar(50) NULL,
        [QuantiteLienUtilisee] decimal(18,6) NULL,
        [UniteQuantiteLien] nvarchar(20) NULL,
        [PoidsTheoriqueUnitaireKg] decimal(18,6) NULL,
        [PoidsPhysiqueUnitaireKg] decimal(18,6) NULL,
        [ControlePoidsFonderieId] uniqueidentifier NULL,
        [TotalCtrlSci] decimal(18,3) NULL,
        [TotalCtrlEmr] decimal(18,3) NULL,
        [TotalCtrlTrn] decimal(18,3) NULL,
        [TotalRebutSci] decimal(18,3) NULL,
        [TotalRebutEmr] decimal(18,3) NULL,
        [TotalRebutTrn] decimal(18,3) NULL,
        [StatutArticle] nvarchar(50) NOT NULL,
        [StatutAtelier] nvarchar(50) NOT NULL,
        [StatutTypeMatiere] nvarchar(50) NOT NULL,
        [StatutNomenclature] nvarchar(50) NOT NULL,
        [StatutPoidsPhysique] nvarchar(50) NOT NULL,
        [StatutRapportMachine] nvarchar(50) NOT NULL,
        [AvertissementsJson] nvarchar(max) NULL,
        [StatutReception] nvarchar(20) NOT NULL,
        [DerogationJustification] nvarchar(500) NULL,
        [CreeParUtilisateurId] uniqueidentifier NULL,
        [CreeParNom] nvarchar(150) NULL,
        [DateCreationUtc] datetime2 NOT NULL,
        [ModifieParUtilisateurId] uniqueidentifier NULL,
        [ModifieParNom] nvarchar(150) NULL,
        [DateModificationUtc] datetime2 NULL,
        CONSTRAINT [PK_ReceptionsUsinage2] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726171843_AddReceptionsUsinage2'
)
BEGIN
    CREATE INDEX [IX_ReceptionsUsinage2_DateReception] ON [ReceptionsUsinage2] ([DateReception]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726171843_AddReceptionsUsinage2'
)
BEGIN
    CREATE INDEX [IX_ReceptionsUsinage2_NumeroOF] ON [ReceptionsUsinage2] ([NumeroOF]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726171843_AddReceptionsUsinage2'
)
BEGIN
    CREATE INDEX [IX_ReceptionsUsinage2_StatutReception] ON [ReceptionsUsinage2] ([StatutReception]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260726171843_AddReceptionsUsinage2'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260726171843_AddReceptionsUsinage2', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727070010_AddEntreesOfComposants'
)
BEGIN
    CREATE TABLE [EntreesOfComposants] (
        [Id] uniqueidentifier NOT NULL,
        [ImportBatchId] uniqueidentifier NOT NULL,
        [DateImputation] date NOT NULL,
        [NumeroOF] nvarchar(50) NOT NULL,
        [CodeArticle] nvarchar(100) NOT NULL,
        [DesignationArticle] nvarchar(250) NULL,
        [Unite] nvarchar(20) NOT NULL,
        [CleMetier] nvarchar(250) NOT NULL,
        [QuantiteNetteSignee] decimal(18,6) NOT NULL,
        [DateCreationUtc] datetime2 NOT NULL,
        [DateMiseAJourUtc] datetime2 NULL,
        CONSTRAINT [PK_EntreesOfComposants] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727070010_AddEntreesOfComposants'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EntreesOfComposants_CleMetier] ON [EntreesOfComposants] ([CleMetier]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727070010_AddEntreesOfComposants'
)
BEGIN
    CREATE INDEX [IX_EntreesOfComposants_NumeroOF_DateImputation] ON [EntreesOfComposants] ([NumeroOF], [DateImputation]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727070010_AddEntreesOfComposants'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260727070010_AddEntreesOfComposants', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727130000_AddImportBatchReimportCounters'
)
BEGIN
    ALTER TABLE [ImportBatches] ADD [ArchiveErrorMessage] nvarchar(2000) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727130000_AddImportBatchReimportCounters'
)
BEGIN
    ALTER TABLE [ImportBatches] ADD [ReadCount] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727130000_AddImportBatchReimportCounters'
)
BEGIN
    ALTER TABLE [ImportBatches] ADD [ValidCount] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727130000_AddImportBatchReimportCounters'
)
BEGIN
    ALTER TABLE [ImportBatches] ADD [InsertedCount] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727130000_AddImportBatchReimportCounters'
)
BEGIN
    ALTER TABLE [ImportBatches] ADD [UpdatedCount] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727130000_AddImportBatchReimportCounters'
)
BEGIN
    ALTER TABLE [ImportBatches] ADD [UnchangedCount] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727130000_AddImportBatchReimportCounters'
)
BEGIN
    ALTER TABLE [ImportBatches] ADD [RejectedCount] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727130000_AddImportBatchReimportCounters'
)
BEGIN
    UPDATE ImportBatches
    SET ReadCount = NombreLignesLues,
        ValidCount = NombreLignesValides,
        InsertedCount = CASE
            WHEN NombreLignesImportees > NombreLignesValides THEN NombreLignesValides
            ELSE NombreLignesImportees
        END,
        UpdatedCount = 0,
        UnchangedCount = CASE
            WHEN NombreLignesValides > NombreLignesImportees THEN NombreLignesValides - NombreLignesImportees
            ELSE 0
        END,
        RejectedCount = CASE
            WHEN NombreLignesLues > NombreLignesValides THEN NombreLignesLues - NombreLignesValides
            ELSE 0
        END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260727130000_AddImportBatchReimportCounters'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260727130000_AddImportBatchReimportCounters', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728074052_AddRebutsUsinage2'
)
BEGIN
    CREATE TABLE [RebutsUsinage2] (
        [Id] uniqueidentifier NOT NULL,
        [DateDeclaration] date NOT NULL,
        [Annee] int NOT NULL,
        [Mois] int NOT NULL,
        [NumeroOF] nvarchar(50) NOT NULL,
        [CodeArticle] nvarchar(80) NOT NULL,
        [QuantiteRebutPieces] decimal(18,4) NOT NULL,
        [Motif] nvarchar(200) NULL,
        [Observation] nvarchar(500) NULL,
        [Statut] nvarchar(20) NOT NULL DEFAULT N'Brouillon',
        [CreeParUtilisateurId] uniqueidentifier NULL,
        [CreeParNom] nvarchar(120) NULL,
        [CreeLeUtc] datetime2 NOT NULL,
        [ValideParUtilisateurId] uniqueidentifier NULL,
        [ValideParNom] nvarchar(120) NULL,
        [ValideLeUtc] datetime2 NULL,
        [ModifieLeUtc] datetime2 NULL,
        [EstSupprime] bit NOT NULL,
        CONSTRAINT [PK_RebutsUsinage2] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728074052_AddRebutsUsinage2'
)
BEGIN
    CREATE INDEX [IX_RebutsUsinage2_Annee_Mois_NumeroOF_CodeArticle] ON [RebutsUsinage2] ([Annee], [Mois], [NumeroOF], [CodeArticle]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728074052_AddRebutsUsinage2'
)
BEGIN
    CREATE INDEX [IX_RebutsUsinage2_NumeroOF] ON [RebutsUsinage2] ([NumeroOF]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728074052_AddRebutsUsinage2'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260728074052_AddRebutsUsinage2', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728102153_DropRefPostesProcessus'
)
BEGIN
    DROP TABLE [RefPostesProcessus];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728102153_DropRefPostesProcessus'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260728102153_DropRefPostesProcessus', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729090634_RenameRapportMachineQuantiteTotale'
)
BEGIN
    EXEC sp_rename N'[X3RapportsMachine].[QuantiteCTRL]', N'QuantiteTotale', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729090634_RenameRapportMachineQuantiteTotale'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260729090634_RenameRapportMachineQuantiteTotale', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729095157_AddParametresApplication'
)
BEGIN
    CREATE TABLE [ParametresApplication] (
        [Cle] nvarchar(100) NOT NULL,
        [Valeur] nvarchar(500) NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        CONSTRAINT [PK_ParametresApplication] PRIMARY KEY ([Cle])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729095157_AddParametresApplication'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Cle', N'Description', N'Valeur') AND [object_id] = OBJECT_ID(N'[ParametresApplication]'))
        SET IDENTITY_INSERT [ParametresApplication] ON;
    EXEC(N'INSERT INTO [ParametresApplication] ([Cle], [Description], [Valeur])
    VALUES (N''CODE_ATELIER_UAP2'', N''Code atelier X3 identifiant le perimetre UAP2 pour les calculs.'', N''16''),
    (N''SEUIL_ECART_FONDERIE'', N''Seuil d''''ecart Fonderie en pourcentage.'', N''5.0''),
    (N''SEUIL_ECART_UAP2'', N''Seuil d''''ecart UAP2 en pourcentage.'', N''1.5''),
    (N''TRANSACTION_ENTREE_OF'', N''Libelle de transaction X3 retenu pour les mouvements d''''entree OF.'', N''Entree OF''),
    (N''TYPES_MATIERE'', N''Codes matiere autorises pour le rapprochement Sortie MP et Nomenclature.'', N''M3000L0,M3010L0,M3060L0'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Cle', N'Description', N'Valeur') AND [object_id] = OBJECT_ID(N'[ParametresApplication]'))
        SET IDENTITY_INSERT [ParametresApplication] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729095157_AddParametresApplication'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260729095157_AddParametresApplication', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729140755_SyncPostesProcessusEtInventairesFonderie'
)
BEGIN
    ALTER TABLE [ReceptionsUsinage2] ADD [EligibleAtelier16] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729140755_SyncPostesProcessusEtInventairesFonderie'
)
BEGIN
    ALTER TABLE [ReceptionsUsinage2] ADD [SourceTypeMatiere] nvarchar(30) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729140755_SyncPostesProcessusEtInventairesFonderie'
)
BEGIN
    ALTER TABLE [DechetsFonderie] ADD [AnnuleLeUtc] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729140755_SyncPostesProcessusEtInventairesFonderie'
)
BEGIN
    ALTER TABLE [DechetsFonderie] ADD [AnnuleParNom] nvarchar(150) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729140755_SyncPostesProcessusEtInventairesFonderie'
)
BEGIN
    ALTER TABLE [DechetsFonderie] ADD [AnnuleParUtilisateurId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729140755_SyncPostesProcessusEtInventairesFonderie'
)
BEGIN
    ALTER TABLE [DechetsFonderie] ADD [Origine] nvarchar(20) NOT NULL DEFAULT N'Fonderie';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729140755_SyncPostesProcessusEtInventairesFonderie'
)
BEGIN
    ALTER TABLE [DechetsFonderie] ADD [ValideLeUtc] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729140755_SyncPostesProcessusEtInventairesFonderie'
)
BEGIN
    ALTER TABLE [DechetsFonderie] ADD [ValideParNom] nvarchar(150) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729140755_SyncPostesProcessusEtInventairesFonderie'
)
BEGIN
    ALTER TABLE [DechetsFonderie] ADD [ValideParUtilisateurId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729140755_SyncPostesProcessusEtInventairesFonderie'
)
BEGIN
    CREATE TABLE [InventairesFonderie] (
        [Id] uniqueidentifier NOT NULL,
        [Periode] date NOT NULL,
        [TypeMatiereId] uniqueidentifier NOT NULL,
        [StockDebutKg] decimal(18,3) NOT NULL,
        [StockFinKg] decimal(18,3) NOT NULL,
        [Observation] nvarchar(500) NULL,
        [Statut] nvarchar(20) NOT NULL,
        [CreeParUtilisateurId] uniqueidentifier NULL,
        [CreeParNom] nvarchar(150) NULL,
        [CreeLeUtc] datetime2 NOT NULL,
        [ModifieParUtilisateurId] uniqueidentifier NULL,
        [ModifieParNom] nvarchar(150) NULL,
        [ModifieLeUtc] datetime2 NULL,
        [ValideParUtilisateurId] uniqueidentifier NULL,
        [ValideParNom] nvarchar(150) NULL,
        [ValideLeUtc] datetime2 NULL,
        [AnnuleParUtilisateurId] uniqueidentifier NULL,
        [AnnuleParNom] nvarchar(150) NULL,
        [AnnuleLeUtc] datetime2 NULL,
        CONSTRAINT [PK_InventairesFonderie] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InventairesFonderie_TypesMatiere_TypeMatiereId] FOREIGN KEY ([TypeMatiereId]) REFERENCES [TypesMatiere] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729140755_SyncPostesProcessusEtInventairesFonderie'
)
BEGIN
    CREATE TABLE [RefPostesProcessus] (
        [Id] uniqueidentifier NOT NULL,
        [CodePoste] nvarchar(50) NOT NULL,
        [LibellePoste] nvarchar(200) NULL,
        [OrigineProcessus] nvarchar(20) NOT NULL,
        [IsActive] bit NOT NULL,
        [DateCreationUtc] datetime2 NOT NULL,
        [DateModificationUtc] datetime2 NULL,
        [CreePar] nvarchar(150) NULL,
        [ModifiePar] nvarchar(150) NULL,
        CONSTRAINT [PK_RefPostesProcessus] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729140755_SyncPostesProcessusEtInventairesFonderie'
)
BEGIN
    CREATE TABLE [X3MouvementsStock] (
        [Id] uniqueidentifier NOT NULL,
        [ImportBatchId] uniqueidentifier NOT NULL,
        [NumeroLigneSource] int NOT NULL,
        [NomFeuilleSource] nvarchar(128) NULL,
        [DateImputation] date NOT NULL,
        [Code] nvarchar(80) NULL,
        [Designation] nvarchar(250) NULL,
        [DateCreationX3] date NULL,
        [HeureCreationX3] nvarchar(30) NULL,
        [Utilisateur] nvarchar(100) NULL,
        [QuantiteX3Signee] decimal(18,4) NOT NULL,
        [Unite] nvarchar(20) NULL,
        [Transaction] nvarchar(80) NULL,
        [TypePiece] nvarchar(80) NULL,
        [Lot] nvarchar(80) NULL,
        [PieceOrigine] nvarchar(100) NULL,
        [NumeroOFNormalise] nvarchar(100) NOT NULL,
        [DesignationMouvement] nvarchar(250) NULL,
        [Emplacement] nvarchar(80) NULL,
        [StatutX3] nvarchar(80) NULL,
        [Nature] nvarchar(20) NOT NULL,
        [StatutRapprochement] nvarchar(40) NOT NULL,
        [DateCreationUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_X3MouvementsStock] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729140755_SyncPostesProcessusEtInventairesFonderie'
)
BEGIN
    CREATE TABLE [RefPostesProcessusHistorique] (
        [Id] uniqueidentifier NOT NULL,
        [PosteProcessusId] uniqueidentifier NOT NULL,
        [CodePoste] nvarchar(50) NOT NULL,
        [OrigineProcessus] nvarchar(20) NOT NULL,
        [Actif] bit NOT NULL,
        [Action] nvarchar(30) NOT NULL,
        [Utilisateur] nvarchar(150) NULL,
        [DateUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_RefPostesProcessusHistorique] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RefPostesProcessusHistorique_RefPostesProcessus_PosteProcessusId] FOREIGN KEY ([PosteProcessusId]) REFERENCES [RefPostesProcessus] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729140755_SyncPostesProcessusEtInventairesFonderie'
)
BEGIN
    CREATE INDEX [IX_DechetsFonderie_Origine] ON [DechetsFonderie] ([Origine]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729140755_SyncPostesProcessusEtInventairesFonderie'
)
BEGIN
    CREATE INDEX [IX_DechetsFonderie_Statut] ON [DechetsFonderie] ([Statut]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729140755_SyncPostesProcessusEtInventairesFonderie'
)
BEGIN
    CREATE INDEX [IX_InventairesFonderie_Periode_TypeMatiereId] ON [InventairesFonderie] ([Periode], [TypeMatiereId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729140755_SyncPostesProcessusEtInventairesFonderie'
)
BEGIN
    CREATE INDEX [IX_InventairesFonderie_Statut] ON [InventairesFonderie] ([Statut]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729140755_SyncPostesProcessusEtInventairesFonderie'
)
BEGIN
    CREATE INDEX [IX_InventairesFonderie_TypeMatiereId] ON [InventairesFonderie] ([TypeMatiereId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729140755_SyncPostesProcessusEtInventairesFonderie'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RefPostesProcessus_CodePoste] ON [RefPostesProcessus] ([CodePoste]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729140755_SyncPostesProcessusEtInventairesFonderie'
)
BEGIN
    CREATE INDEX [IX_RefPostesProcessus_IsActive_OrigineProcessus] ON [RefPostesProcessus] ([IsActive], [OrigineProcessus]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729140755_SyncPostesProcessusEtInventairesFonderie'
)
BEGIN
    CREATE INDEX [IX_RefPostesProcessusHistorique_PosteProcessusId_DateUtc] ON [RefPostesProcessusHistorique] ([PosteProcessusId], [DateUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729140755_SyncPostesProcessusEtInventairesFonderie'
)
BEGIN
    CREATE INDEX [IX_X3MouvementsStock_Code] ON [X3MouvementsStock] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729140755_SyncPostesProcessusEtInventairesFonderie'
)
BEGIN
    CREATE INDEX [IX_X3MouvementsStock_DateImputation] ON [X3MouvementsStock] ([DateImputation]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729140755_SyncPostesProcessusEtInventairesFonderie'
)
BEGIN
    CREATE INDEX [IX_X3MouvementsStock_ImportBatchId] ON [X3MouvementsStock] ([ImportBatchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729140755_SyncPostesProcessusEtInventairesFonderie'
)
BEGIN
    CREATE INDEX [IX_X3MouvementsStock_Nature] ON [X3MouvementsStock] ([Nature]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729140755_SyncPostesProcessusEtInventairesFonderie'
)
BEGIN
    CREATE INDEX [IX_X3MouvementsStock_NumeroOFNormalise] ON [X3MouvementsStock] ([NumeroOFNormalise]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729140755_SyncPostesProcessusEtInventairesFonderie'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260729140755_SyncPostesProcessusEtInventairesFonderie', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730081144_AddInventaireComptagePhysique'
)
BEGIN
    CREATE TABLE [InventairesCategories] (
        [Id] uniqueidentifier NOT NULL,
        [Categorie] nvarchar(100) NOT NULL,
        [Periode] date NOT NULL,
        [TypeMatiereId] uniqueidentifier NOT NULL,
        [QuantiteKg] decimal(18,3) NOT NULL,
        [Observation] nvarchar(500) NULL,
        [Statut] nvarchar(20) NOT NULL,
        [CreeParUtilisateurId] uniqueidentifier NULL,
        [CreeParNom] nvarchar(150) NULL,
        [CreeLeUtc] datetime2 NOT NULL,
        [ModifieParUtilisateurId] uniqueidentifier NULL,
        [ModifieParNom] nvarchar(150) NULL,
        [ModifieLeUtc] datetime2 NULL,
        [ValideParUtilisateurId] uniqueidentifier NULL,
        [ValideParNom] nvarchar(150) NULL,
        [ValideLeUtc] datetime2 NULL,
        CONSTRAINT [PK_InventairesCategories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InventairesCategories_TypesMatiere_TypeMatiereId] FOREIGN KEY ([TypeMatiereId]) REFERENCES [TypesMatiere] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730081144_AddInventaireComptagePhysique'
)
BEGIN
    CREATE TABLE [InventairesConteneurs] (
        [Id] uniqueidentifier NOT NULL,
        [DateInventaire] date NOT NULL,
        [NumeroConteneur] nvarchar(50) NOT NULL,
        [NumeroOF] nvarchar(50) NULL,
        [CodeArticle] nvarchar(50) NULL,
        [DesignationArticle] nvarchar(250) NULL,
        [Periode] date NOT NULL,
        [TypeMatiereId] uniqueidentifier NOT NULL,
        [QuantiteKg] decimal(18,3) NOT NULL,
        [Observation] nvarchar(500) NULL,
        [Statut] nvarchar(20) NOT NULL,
        [CreeParUtilisateurId] uniqueidentifier NULL,
        [CreeParNom] nvarchar(150) NULL,
        [CreeLeUtc] datetime2 NOT NULL,
        [ModifieParUtilisateurId] uniqueidentifier NULL,
        [ModifieParNom] nvarchar(150) NULL,
        [ModifieLeUtc] datetime2 NULL,
        [ValideParUtilisateurId] uniqueidentifier NULL,
        [ValideParNom] nvarchar(150) NULL,
        [ValideLeUtc] datetime2 NULL,
        CONSTRAINT [PK_InventairesConteneurs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InventairesConteneurs_TypesMatiere_TypeMatiereId] FOREIGN KEY ([TypeMatiereId]) REFERENCES [TypesMatiere] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730081144_AddInventaireComptagePhysique'
)
BEGIN
    CREATE TABLE [InventairesFours] (
        [Id] uniqueidentifier NOT NULL,
        [DateInventaire] date NOT NULL,
        [CodeFour] nvarchar(50) NOT NULL,
        [Periode] date NOT NULL,
        [TypeMatiereId] uniqueidentifier NOT NULL,
        [QuantiteKg] decimal(18,3) NOT NULL,
        [Observation] nvarchar(500) NULL,
        [Statut] nvarchar(20) NOT NULL,
        [CreeParUtilisateurId] uniqueidentifier NULL,
        [CreeParNom] nvarchar(150) NULL,
        [CreeLeUtc] datetime2 NOT NULL,
        [ModifieParUtilisateurId] uniqueidentifier NULL,
        [ModifieParNom] nvarchar(150) NULL,
        [ModifieLeUtc] datetime2 NULL,
        [ValideParUtilisateurId] uniqueidentifier NULL,
        [ValideParNom] nvarchar(150) NULL,
        [ValideLeUtc] datetime2 NULL,
        CONSTRAINT [PK_InventairesFours] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InventairesFours_TypesMatiere_TypeMatiereId] FOREIGN KEY ([TypeMatiereId]) REFERENCES [TypesMatiere] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730081144_AddInventaireComptagePhysique'
)
BEGIN
    CREATE INDEX [IX_InventairesCategories_Periode_TypeMatiereId] ON [InventairesCategories] ([Periode], [TypeMatiereId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730081144_AddInventaireComptagePhysique'
)
BEGIN
    CREATE INDEX [IX_InventairesCategories_Statut] ON [InventairesCategories] ([Statut]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730081144_AddInventaireComptagePhysique'
)
BEGIN
    CREATE INDEX [IX_InventairesCategories_TypeMatiereId] ON [InventairesCategories] ([TypeMatiereId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730081144_AddInventaireComptagePhysique'
)
BEGIN
    CREATE INDEX [IX_InventairesConteneurs_NumeroConteneur] ON [InventairesConteneurs] ([NumeroConteneur]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730081144_AddInventaireComptagePhysique'
)
BEGIN
    CREATE INDEX [IX_InventairesConteneurs_Periode_TypeMatiereId] ON [InventairesConteneurs] ([Periode], [TypeMatiereId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730081144_AddInventaireComptagePhysique'
)
BEGIN
    CREATE INDEX [IX_InventairesConteneurs_Statut] ON [InventairesConteneurs] ([Statut]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730081144_AddInventaireComptagePhysique'
)
BEGIN
    CREATE INDEX [IX_InventairesConteneurs_TypeMatiereId] ON [InventairesConteneurs] ([TypeMatiereId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730081144_AddInventaireComptagePhysique'
)
BEGIN
    CREATE INDEX [IX_InventairesFours_Periode_TypeMatiereId] ON [InventairesFours] ([Periode], [TypeMatiereId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730081144_AddInventaireComptagePhysique'
)
BEGIN
    CREATE INDEX [IX_InventairesFours_Statut] ON [InventairesFours] ([Statut]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730081144_AddInventaireComptagePhysique'
)
BEGIN
    CREATE INDEX [IX_InventairesFours_TypeMatiereId] ON [InventairesFours] ([TypeMatiereId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730081144_AddInventaireComptagePhysique'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260730081144_AddInventaireComptagePhysique', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    ALTER TABLE [InventairesCategories] DROP CONSTRAINT [FK_InventairesCategories_TypesMatiere_TypeMatiereId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    ALTER TABLE [InventairesConteneurs] DROP CONSTRAINT [FK_InventairesConteneurs_TypesMatiere_TypeMatiereId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    ALTER TABLE [InventairesFours] DROP CONSTRAINT [FK_InventairesFours_TypesMatiere_TypeMatiereId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    DROP INDEX [IX_InventairesFours_Periode_TypeMatiereId] ON [InventairesFours];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    DROP INDEX [IX_InventairesFours_Statut] ON [InventairesFours];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    DROP INDEX [IX_InventairesFours_TypeMatiereId] ON [InventairesFours];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    DROP INDEX [IX_InventairesFonderie_Periode_TypeMatiereId] ON [InventairesFonderie];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    DROP INDEX [IX_InventairesCategories_Periode_TypeMatiereId] ON [InventairesCategories];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    DROP INDEX [IX_InventairesCategories_Statut] ON [InventairesCategories];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    DROP INDEX [IX_InventairesCategories_TypeMatiereId] ON [InventairesCategories];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    ALTER TABLE [InventairesConteneurs] DROP CONSTRAINT [PK_InventairesConteneurs];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InventairesFours]') AND [c].[name] = N'CreeParNom');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [InventairesFours] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [InventairesFours] DROP COLUMN [CreeParNom];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InventairesFours]') AND [c].[name] = N'CreeParUtilisateurId');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [InventairesFours] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [InventairesFours] DROP COLUMN [CreeParUtilisateurId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    DECLARE @var5 sysname;
    SELECT @var5 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InventairesFours]') AND [c].[name] = N'ModifieParNom');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [InventairesFours] DROP CONSTRAINT [' + @var5 + '];');
    ALTER TABLE [InventairesFours] DROP COLUMN [ModifieParNom];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    DECLARE @var6 sysname;
    SELECT @var6 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InventairesFours]') AND [c].[name] = N'ModifieParUtilisateurId');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [InventairesFours] DROP CONSTRAINT [' + @var6 + '];');
    ALTER TABLE [InventairesFours] DROP COLUMN [ModifieParUtilisateurId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    DECLARE @var7 sysname;
    SELECT @var7 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InventairesFours]') AND [c].[name] = N'Periode');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [InventairesFours] DROP CONSTRAINT [' + @var7 + '];');
    ALTER TABLE [InventairesFours] DROP COLUMN [Periode];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    DECLARE @var8 sysname;
    SELECT @var8 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InventairesFours]') AND [c].[name] = N'TypeMatiereId');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [InventairesFours] DROP CONSTRAINT [' + @var8 + '];');
    ALTER TABLE [InventairesFours] DROP COLUMN [TypeMatiereId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    DECLARE @var9 sysname;
    SELECT @var9 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InventairesFours]') AND [c].[name] = N'ValideParNom');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [InventairesFours] DROP CONSTRAINT [' + @var9 + '];');
    ALTER TABLE [InventairesFours] DROP COLUMN [ValideParNom];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    DECLARE @var10 sysname;
    SELECT @var10 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InventairesFours]') AND [c].[name] = N'ValideParUtilisateurId');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [InventairesFours] DROP CONSTRAINT [' + @var10 + '];');
    ALTER TABLE [InventairesFours] DROP COLUMN [ValideParUtilisateurId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    DECLARE @var11 sysname;
    SELECT @var11 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InventairesCategories]') AND [c].[name] = N'CreeParNom');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [InventairesCategories] DROP CONSTRAINT [' + @var11 + '];');
    ALTER TABLE [InventairesCategories] DROP COLUMN [CreeParNom];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    DECLARE @var12 sysname;
    SELECT @var12 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InventairesCategories]') AND [c].[name] = N'CreeParUtilisateurId');
    IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [InventairesCategories] DROP CONSTRAINT [' + @var12 + '];');
    ALTER TABLE [InventairesCategories] DROP COLUMN [CreeParUtilisateurId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    DECLARE @var13 sysname;
    SELECT @var13 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InventairesCategories]') AND [c].[name] = N'ModifieParNom');
    IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [InventairesCategories] DROP CONSTRAINT [' + @var13 + '];');
    ALTER TABLE [InventairesCategories] DROP COLUMN [ModifieParNom];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    DECLARE @var14 sysname;
    SELECT @var14 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InventairesCategories]') AND [c].[name] = N'ModifieParUtilisateurId');
    IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [InventairesCategories] DROP CONSTRAINT [' + @var14 + '];');
    ALTER TABLE [InventairesCategories] DROP COLUMN [ModifieParUtilisateurId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    DECLARE @var15 sysname;
    SELECT @var15 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InventairesCategories]') AND [c].[name] = N'TypeMatiereId');
    IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [InventairesCategories] DROP CONSTRAINT [' + @var15 + '];');
    ALTER TABLE [InventairesCategories] DROP COLUMN [TypeMatiereId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    DECLARE @var16 sysname;
    SELECT @var16 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InventairesCategories]') AND [c].[name] = N'ValideParNom');
    IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [InventairesCategories] DROP CONSTRAINT [' + @var16 + '];');
    ALTER TABLE [InventairesCategories] DROP COLUMN [ValideParNom];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    DECLARE @var17 sysname;
    SELECT @var17 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InventairesCategories]') AND [c].[name] = N'ValideParUtilisateurId');
    IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [InventairesCategories] DROP CONSTRAINT [' + @var17 + '];');
    ALTER TABLE [InventairesCategories] DROP COLUMN [ValideParUtilisateurId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    EXEC sp_rename N'[InventairesConteneurs]', N'InventairesConteneursComptage';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    EXEC sp_rename N'[InventairesCategories].[QuantiteKg]', N'PoidsReelKg', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    EXEC sp_rename N'[InventairesCategories].[Periode]', N'DateInventaire', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    EXEC sp_rename N'[InventairesCategories].[Observation]', N'Commentaire', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    EXEC sp_rename N'[InventairesConteneursComptage].[IX_InventairesConteneurs_TypeMatiereId]', N'IX_InventairesConteneursComptage_TypeMatiereId', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    EXEC sp_rename N'[InventairesConteneursComptage].[IX_InventairesConteneurs_Statut]', N'IX_InventairesConteneursComptage_Statut', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    EXEC sp_rename N'[InventairesConteneursComptage].[IX_InventairesConteneurs_Periode_TypeMatiereId]', N'IX_InventairesConteneursComptage_Periode_TypeMatiereId', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    EXEC sp_rename N'[InventairesConteneursComptage].[IX_InventairesConteneurs_NumeroConteneur]', N'IX_InventairesConteneursComptage_NumeroConteneur', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    DECLARE @var18 sysname;
    SELECT @var18 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InventairesFours]') AND [c].[name] = N'Statut');
    IF @var18 IS NOT NULL EXEC(N'ALTER TABLE [InventairesFours] DROP CONSTRAINT [' + @var18 + '];');
    ALTER TABLE [InventairesFours] ALTER COLUMN [Statut] int NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    ALTER TABLE [InventairesFours] ADD [Annee] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    ALTER TABLE [InventairesFours] ADD [AnnuleLeUtc] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    ALTER TABLE [InventairesFours] ADD [AnnulePar] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    ALTER TABLE [InventairesFours] ADD [CodeTypeMatiere] nvarchar(30) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    ALTER TABLE [InventairesFours] ADD [Mois] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    ALTER TABLE [InventairesFours] ADD [ValidePar] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    DECLARE @var19 sysname;
    SELECT @var19 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InventairesCategories]') AND [c].[name] = N'Statut');
    IF @var19 IS NOT NULL EXEC(N'ALTER TABLE [InventairesCategories] DROP CONSTRAINT [' + @var19 + '];');
    ALTER TABLE [InventairesCategories] ALTER COLUMN [Statut] int NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    DECLARE @var20 sysname;
    SELECT @var20 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InventairesCategories]') AND [c].[name] = N'Categorie');
    IF @var20 IS NOT NULL EXEC(N'ALTER TABLE [InventairesCategories] DROP CONSTRAINT [' + @var20 + '];');
    ALTER TABLE [InventairesCategories] ALTER COLUMN [Categorie] int NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    ALTER TABLE [InventairesCategories] ADD [AnnuleLeUtc] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    ALTER TABLE [InventairesCategories] ADD [AnnulePar] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    ALTER TABLE [InventairesCategories] ADD [CodeTypeMatiere] nvarchar(30) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    ALTER TABLE [InventairesCategories] ADD [ValidePar] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    ALTER TABLE [InventairesConteneursComptage] ADD CONSTRAINT [PK_InventairesConteneursComptage] PRIMARY KEY ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    CREATE TABLE [InventairesCategoriesComptage] (
        [Id] uniqueidentifier NOT NULL,
        [Categorie] nvarchar(100) NOT NULL,
        [Periode] date NOT NULL,
        [TypeMatiereId] uniqueidentifier NOT NULL,
        [QuantiteKg] decimal(18,3) NOT NULL,
        [Observation] nvarchar(500) NULL,
        [Statut] nvarchar(20) NOT NULL,
        [CreeParUtilisateurId] uniqueidentifier NULL,
        [CreeParNom] nvarchar(150) NULL,
        [CreeLeUtc] datetime2 NOT NULL,
        [ModifieParUtilisateurId] uniqueidentifier NULL,
        [ModifieParNom] nvarchar(150) NULL,
        [ModifieLeUtc] datetime2 NULL,
        [ValideParUtilisateurId] uniqueidentifier NULL,
        [ValideParNom] nvarchar(150) NULL,
        [ValideLeUtc] datetime2 NULL,
        CONSTRAINT [PK_InventairesCategoriesComptage] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InventairesCategoriesComptage_TypesMatiere_TypeMatiereId] FOREIGN KEY ([TypeMatiereId]) REFERENCES [TypesMatiere] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    CREATE TABLE [InventairesConteneursOf] (
        [Id] uniqueidentifier NOT NULL,
        [DateInventaire] date NOT NULL,
        [NumeroConteneur] nvarchar(50) NOT NULL,
        [NumeroOF] nvarchar(50) NOT NULL,
        [CodeTypeMatiere] nvarchar(30) NOT NULL,
        [PoidsReelKg] decimal(18,3) NOT NULL,
        [Observation] nvarchar(500) NULL,
        [Statut] int NOT NULL,
        [CreeLeUtc] datetime2 NOT NULL,
        [ModifieLeUtc] datetime2 NULL,
        [ValideLeUtc] datetime2 NULL,
        [ValidePar] nvarchar(100) NULL,
        [AnnuleLeUtc] datetime2 NULL,
        [AnnulePar] nvarchar(100) NULL,
        CONSTRAINT [PK_InventairesConteneursOf] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    CREATE TABLE [InventairesFonderiePeriodes] (
        [Id] uniqueidentifier NOT NULL,
        [Mois] int NOT NULL,
        [Annee] int NOT NULL,
        [Statut] int NOT NULL,
        [CreeLeUtc] datetime2 NOT NULL,
        [ValideLeUtc] datetime2 NULL,
        [ValidePar] nvarchar(100) NULL,
        [ClotureLeUtc] datetime2 NULL,
        [CloturePar] nvarchar(100) NULL,
        CONSTRAINT [PK_InventairesFonderiePeriodes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    CREATE TABLE [InventairesFoursComptage] (
        [Id] uniqueidentifier NOT NULL,
        [DateInventaire] date NOT NULL,
        [CodeFour] nvarchar(50) NOT NULL,
        [Periode] date NOT NULL,
        [TypeMatiereId] uniqueidentifier NOT NULL,
        [QuantiteKg] decimal(18,3) NOT NULL,
        [Observation] nvarchar(500) NULL,
        [Statut] nvarchar(20) NOT NULL,
        [CreeParUtilisateurId] uniqueidentifier NULL,
        [CreeParNom] nvarchar(150) NULL,
        [CreeLeUtc] datetime2 NOT NULL,
        [ModifieParUtilisateurId] uniqueidentifier NULL,
        [ModifieParNom] nvarchar(150) NULL,
        [ModifieLeUtc] datetime2 NULL,
        [ValideParUtilisateurId] uniqueidentifier NULL,
        [ValideParNom] nvarchar(150) NULL,
        [ValideLeUtc] datetime2 NULL,
        CONSTRAINT [PK_InventairesFoursComptage] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InventairesFoursComptage_TypesMatiere_TypeMatiereId] FOREIGN KEY ([TypeMatiereId]) REFERENCES [TypesMatiere] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    CREATE INDEX [IX_InventairesFours_Annee_Mois_CodeFour] ON [InventairesFours] ([Annee], [Mois], [CodeFour]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    CREATE INDEX [IX_InventairesFours_DateInventaire_CodeTypeMatiere_Statut] ON [InventairesFours] ([DateInventaire], [CodeTypeMatiere], [Statut]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    CREATE INDEX [IX_InventairesFonderie_Periode_TypeMatiereId] ON [InventairesFonderie] ([Periode], [TypeMatiereId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    CREATE INDEX [IX_InventairesCategories_DateInventaire_Categorie] ON [InventairesCategories] ([DateInventaire], [Categorie]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    CREATE INDEX [IX_InventairesCategories_DateInventaire_CodeTypeMatiere_Statut] ON [InventairesCategories] ([DateInventaire], [CodeTypeMatiere], [Statut]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    CREATE INDEX [IX_InventairesCategoriesComptage_Periode_TypeMatiereId] ON [InventairesCategoriesComptage] ([Periode], [TypeMatiereId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    CREATE INDEX [IX_InventairesCategoriesComptage_Statut] ON [InventairesCategoriesComptage] ([Statut]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    CREATE INDEX [IX_InventairesCategoriesComptage_TypeMatiereId] ON [InventairesCategoriesComptage] ([TypeMatiereId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    CREATE INDEX [IX_InventairesConteneursOf_DateInventaire_CodeTypeMatiere_Statut] ON [InventairesConteneursOf] ([DateInventaire], [CodeTypeMatiere], [Statut]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    CREATE INDEX [IX_InventairesConteneursOf_DateInventaire_NumeroConteneur] ON [InventairesConteneursOf] ([DateInventaire], [NumeroConteneur]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    CREATE INDEX [IX_InventairesConteneursOf_NumeroOF_CodeTypeMatiere] ON [InventairesConteneursOf] ([NumeroOF], [CodeTypeMatiere]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InventairesFonderiePeriodes_Annee_Mois] ON [InventairesFonderiePeriodes] ([Annee], [Mois]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    CREATE INDEX [IX_InventairesFoursComptage_Periode_TypeMatiereId] ON [InventairesFoursComptage] ([Periode], [TypeMatiereId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    CREATE INDEX [IX_InventairesFoursComptage_Statut] ON [InventairesFoursComptage] ([Statut]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    CREATE INDEX [IX_InventairesFoursComptage_TypeMatiereId] ON [InventairesFoursComptage] ([TypeMatiereId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    ALTER TABLE [InventairesConteneursComptage] ADD CONSTRAINT [FK_InventairesConteneursComptage_TypesMatiere_TypeMatiereId] FOREIGN KEY ([TypeMatiereId]) REFERENCES [TypesMatiere] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731091353_AddInventaireFonderiePersistence'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260731091353_AddInventaireFonderiePersistence', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731121126_FixInventaireFonderieProjectionSchema'
)
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM InventairesFonderie
        GROUP BY Periode, TypeMatiereId
        HAVING COUNT(*) > 1
    )
    BEGIN
        THROW 51000, 'Impossible de creer l index unique InventairesFonderie(Periode, TypeMatiereId) : des doublons existent.', 1;
    END
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731121126_FixInventaireFonderieProjectionSchema'
)
BEGIN
    ALTER TABLE [InventairesFonderie] ALTER COLUMN [StockDebutKg] decimal(18,3) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731121126_FixInventaireFonderieProjectionSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UX_InventairesFonderie_Periode_TypeMatiereId] ON [InventairesFonderie] ([Periode], [TypeMatiereId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731121126_FixInventaireFonderieProjectionSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260731121126_FixInventaireFonderieProjectionSchema', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260806091249_AddPersistentBusinessAlerts'
)
BEGIN
    CREATE TABLE [AlertesMetier] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(100) NOT NULL,
        [Type] nvarchar(30) NOT NULL,
        [Severite] nvarchar(30) NOT NULL,
        [Statut] nvarchar(30) NOT NULL,
        [Titre] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NOT NULL,
        [Source] nvarchar(100) NOT NULL,
        [Periode] nvarchar(7) NULL,
        [NumeroOF] nvarchar(50) NULL,
        [CodeArticle] nvarchar(100) NULL,
        [TypeMatiere] nvarchar(50) NULL,
        [CleDeduplication] nvarchar(600) NOT NULL,
        [DetecteeLeUtc] datetime2 NOT NULL,
        [DerniereDetectionLeUtc] datetime2 NOT NULL,
        [AcquitteeLeUtc] datetime2 NULL,
        [AcquitteePar] nvarchar(150) NULL,
        [ResolueLeUtc] datetime2 NULL,
        [ResoluePar] nvarchar(150) NULL,
        [IgnoreeLeUtc] datetime2 NULL,
        [IgnoreePar] nvarchar(150) NULL,
        [CommentaireTraitement] nvarchar(1000) NULL,
        [DonneesAssocieesJson] nvarchar(4000) NULL,
        [ResolutionAutomatique] bit NOT NULL,
        [EstActive] bit NOT NULL,
        CONSTRAINT [PK_AlertesMetier] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260806091249_AddPersistentBusinessAlerts'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_AlertesMetier_CleDeduplication] ON [AlertesMetier] ([CleDeduplication]) WHERE [EstActive] = 1');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260806091249_AddPersistentBusinessAlerts'
)
BEGIN
    CREATE INDEX [IX_AlertesMetier_CodeArticle] ON [AlertesMetier] ([CodeArticle]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260806091249_AddPersistentBusinessAlerts'
)
BEGIN
    CREATE INDEX [IX_AlertesMetier_DetecteeLeUtc] ON [AlertesMetier] ([DetecteeLeUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260806091249_AddPersistentBusinessAlerts'
)
BEGIN
    CREATE INDEX [IX_AlertesMetier_NumeroOF] ON [AlertesMetier] ([NumeroOF]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260806091249_AddPersistentBusinessAlerts'
)
BEGIN
    CREATE INDEX [IX_AlertesMetier_Periode_Statut] ON [AlertesMetier] ([Periode], [Statut]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260806091249_AddPersistentBusinessAlerts'
)
BEGIN
    CREATE INDEX [IX_AlertesMetier_Source_Severite] ON [AlertesMetier] ([Source], [Severite]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260806091249_AddPersistentBusinessAlerts'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260806091249_AddPersistentBusinessAlerts', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818171215_AddPersistentAuditTrail'
)
BEGIN
    CREATE TABLE [AuditTraces] (
        [Id] uniqueidentifier NOT NULL,
        [DateUtc] datetime2 NOT NULL,
        [Action] nvarchar(40) NOT NULL,
        [Domaine] nvarchar(60) NOT NULL,
        [Entite] nvarchar(100) NOT NULL,
        [EntiteId] nvarchar(150) NOT NULL,
        [ReferenceMetier] nvarchar(300) NULL,
        [UtilisateurId] nvarchar(150) NOT NULL,
        [UtilisateurNom] nvarchar(200) NOT NULL,
        [UtilisateurEmail] nvarchar(320) NULL,
        [Role] nvarchar(200) NOT NULL,
        [Source] nvarchar(150) NOT NULL,
        [CorrelationId] nvarchar(100) NOT NULL,
        [Resume] nvarchar(1000) NULL,
        [AnciennesValeursJson] nvarchar(max) NULL,
        [NouvellesValeursJson] nvarchar(max) NULL,
        [InformationsJson] nvarchar(max) NULL,
        CONSTRAINT [PK_AuditTraces] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818171215_AddPersistentAuditTrail'
)
BEGIN
    CREATE INDEX [IX_AuditTraces_Action] ON [AuditTraces] ([Action]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818171215_AddPersistentAuditTrail'
)
BEGIN
    CREATE INDEX [IX_AuditTraces_CorrelationId] ON [AuditTraces] ([CorrelationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818171215_AddPersistentAuditTrail'
)
BEGIN
    CREATE INDEX [IX_AuditTraces_DateUtc] ON [AuditTraces] ([DateUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818171215_AddPersistentAuditTrail'
)
BEGIN
    CREATE INDEX [IX_AuditTraces_Domaine] ON [AuditTraces] ([Domaine]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818171215_AddPersistentAuditTrail'
)
BEGIN
    CREATE INDEX [IX_AuditTraces_Entite_EntiteId_DateUtc] ON [AuditTraces] ([Entite], [EntiteId], [DateUtc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818171215_AddPersistentAuditTrail'
)
BEGIN
    CREATE INDEX [IX_AuditTraces_ReferenceMetier] ON [AuditTraces] ([ReferenceMetier]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818171215_AddPersistentAuditTrail'
)
BEGIN
    CREATE INDEX [IX_AuditTraces_UtilisateurId] ON [AuditTraces] ([UtilisateurId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260818171215_AddPersistentAuditTrail'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260818171215_AddPersistentAuditTrail', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260903122614_AddApplicationUserProfiles'
)
BEGIN
    CREATE TABLE [UtilisateursApplication] (
        [Id] int NOT NULL IDENTITY,
        [EntraObjectId] nvarchar(150) NOT NULL,
        [Email] nvarchar(320) NULL,
        [NomAffichage] nvarchar(200) NULL,
        [RoleObserve] nvarchar(50) NULL,
        [EstActif] bit NOT NULL,
        [DateCreationUtc] datetime2 NOT NULL,
        [DateModificationUtc] datetime2 NOT NULL,
        [DerniereConnexionUtc] datetime2 NULL,
        CONSTRAINT [PK_UtilisateursApplication] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260903122614_AddApplicationUserProfiles'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UtilisateursApplication_EntraObjectId] ON [UtilisateursApplication] ([EntraObjectId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260903122614_AddApplicationUserProfiles'
)
BEGIN
    CREATE INDEX [IX_UtilisateursApplication_EstActif] ON [UtilisateursApplication] ([EstActif]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260903122614_AddApplicationUserProfiles'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260903122614_AddApplicationUserProfiles', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904111057_AddPeriodBalanceArchives'
)
BEGIN
    CREATE TABLE [PeriodesBilansArchivees] (
        [Id] uniqueidentifier NOT NULL,
        [Periode] nvarchar(7) NOT NULL,
        [DateEnregistrementUtc] datetime2 NOT NULL,
        [EnregistrePar] nvarchar(200) NOT NULL,
        [StatutGlobal] nvarchar(50) NOT NULL,
        [VersionCalcul] nvarchar(50) NOT NULL,
        [SchemaVersion] int NOT NULL,
        [NombreAlertes] int NOT NULL,
        [NombreAnomalies] int NOT NULL,
        [NombreDonneesManquantes] int NOT NULL,
        [SnapshotJson] nvarchar(max) NOT NULL,
        [RapportTemplateVersion] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_PeriodesBilansArchivees] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904111057_AddPeriodBalanceArchives'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PeriodesBilansArchivees_Periode] ON [PeriodesBilansArchivees] ([Periode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904111057_AddPeriodBalanceArchives'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260904111057_AddPeriodBalanceArchives', N'8.0.11');
END;
GO

COMMIT;
GO

