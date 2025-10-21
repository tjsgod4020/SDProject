# SDProject — Project Status
_Last updated: 2025-10-21 21:55:04 (local)_

## 1) Overview
- Engine: **Unity 6000.2.7f2 (PC/2D)**
- Version control: **Git**
- Architecture: **Domain / Presentation / Infrastructure / DataTable** with asmdef boundaries.
- Current focus: **Data pipeline (XLSX→CSV→Runtime)**, **DataTableConfig Auto-Sync**, folder/asmdef hygiene.

## 2) Data Pipeline — Rules
- Editor: **ExcelDataReader** 기반 **XLSX→CSV** 변환 (첫 번째 시트만, 출력 파일명 동일).
- CSV 규약: **Row1=Columns / Row2=Types / Row3+=Data** (Row2 권장).
- Runtime: **DataTableLoader(Awake) → TableRegistry** 등록 → 코드에서 `TableRegistry.Get<T>()` 접근.
- Auto-Sync: `Assets/SDProject/DataTables/Csv` 변화 시 **DataTableConfig** 자동/수동 동기화 지원.

## 3) Current Data Tables (CSV)
- Assets/SDProject/DataTables/Csv/CardData.csv
- Assets/SDProject/DataTables/Csv/CardDesc.csv
- Assets/SDProject/DataTables/Csv/CardName.csv

## 4) DataTableConfig Assets
- **DataTableConfig**  (Assets/SDProject/Config/DataTableConfig.asset)  Enabled=true
  - Id=`CardData`  Csv=`Assets/SDProject/DataTables/Csv/CardData.csv`  RowType=`SD.Gameplay.Cards.Domain.CardDataModel, SD.Gameplay.Cards.Domain, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null`
  - Id=`CardDesc`  Csv=`Assets/SDProject/DataTables/Csv/CardDesc.csv`  RowType=`SD.Gameplay.Cards.Domain.Localization.CardDescRow, SD.Gameplay.Cards.Domain, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null`
  - Id=`CardName`  Csv=`Assets/SDProject/DataTables/Csv/CardName.csv`  RowType=`SD.Gameplay.Cards.Domain.Localization.CardNameRow, SD.Gameplay.Cards.Domain, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null`

## 5) Assembly Definitions (under SDProject)
- Assets/SDProject/Scripts/Core/Domain/SD.Core.Domain.asmdef
- Assets/SDProject/Scripts/Core/Infrastructure/SD.Core.Infrastructure.asmdef
- Assets/SDProject/Scripts/Core/Presentation/SD.Core.Presentation.asmdef
- Assets/SDProject/Scripts/Core/Tests/SD.Tests.EditMode.asmdef
- Assets/SDProject/Scripts/DataTable/Editor/SD.DataTables.Editor.asmdef
- Assets/SDProject/Scripts/DataTable/Runtime/SD.DataTables.Runtime.asmdef
- Assets/SDProject/Scripts/Gameplay/Battle/Domain/SD.Gameplay.Battle.Domain.asmdef
- Assets/SDProject/Scripts/Gameplay/Battle/Infrastructure/SD.Gameplay.Battle.Infrastructure.asmdef
- Assets/SDProject/Scripts/Gameplay/Battle/Presentation/SD.Gameplay.Battle.Presentation.asmdef
- Assets/SDProject/Scripts/Gameplay/Battle/Tests/SD.Gameplay.Tests.asmdef
- Assets/SDProject/Scripts/Gameplay/Cards/Domain/SD.Gameplay.Cards.Domain.asmdef
- Assets/SDProject/Scripts/Gameplay/Cards/Infrastructure/SD.Gameplay.Cards.Infrastructure.asmdef
- Assets/SDProject/Scripts/Gameplay/Cards/Presentation/SD.Gameplay.Cards.Presentation.asmdef
- Assets/SDProject/Scripts/Tools/Editor/SD.Tools.Editor.asmdef
- Assets/SDProject/Scripts/UI/Common/SD.UI.Common.asmdef
- Assets/SDProject/Scripts/UI/Editor/SD.UI.Editor.asmdef

## 6) Folder Tree Snapshot (Assets/SDProject)
```
SDProject/
  AddressablesGroups/
  Art/
    Characters/
    Enemies/
    FX/
    UI/
  Audio/
    BGM/
    SFX/
  Config/
    - DataTableConfig.asset
  DataTables/
    Csv/
      - CardData.csv
      - CardDesc.csv
      - CardName.csv
    Schemas/
    Xlsx/
      - CardData.xlsx
      - CardDesc.xlsx
      - CardName.xlsx
    - CardData.csv
    - CardDesc.csv
    - CardName.csv
    - CardData.xlsx
    - CardDesc.xlsx
    - CardName.xlsx
  Docs/
  Prefabs/
    Battle/
    Common/
    UI/
  Scenes/
    Battle/
    Boot/
    Dev/
  Scripts/
    Core/
      Domain/
        - SD.Core.Domain.asmdef
      Infrastructure/
        - SD.Core.Infrastructure.asmdef
      Presentation/
        - SD.Core.Presentation.asmdef
      Tests/
        - SD.Tests.EditMode.asmdef
      - SD.Core.Domain.asmdef
      - SD.Core.Infrastructure.asmdef
      - SD.Core.Presentation.asmdef
      - SD.Tests.EditMode.asmdef
    DataTable/
      Editor/
        - DataTableConfigAutoSync.cs
        - DataTableConfigSyncEditor.cs
        - ExcelCodePagesInit.cs
        - SD.DataTables.Editor.asmdef
        - XlsxAssetPostprocessor.cs
        - XlsxToCsvConverter.cs
      Runtime/
        - CsvReader.cs
        - DataTableConfig.cs
        - DataTableIdAttribute.cs
        - DataTableLoader.cs
        - ICsvTable.cs
        - IHasStringId.cs
        - ReflectionMapper.cs
        - SD.DataTables.Runtime.asmdef
        - Table.cs
        - TableRegistry.cs
      - DataTableConfigAutoSync.cs
      - DataTableConfigSyncEditor.cs
      - ExcelCodePagesInit.cs
      - SD.DataTables.Editor.asmdef
      - XlsxAssetPostprocessor.cs
      - XlsxToCsvConverter.cs
      - CsvReader.cs
      - DataTableConfig.cs
      - DataTableIdAttribute.cs
      - DataTableLoader.cs
      - ICsvTable.cs
      - IHasStringId.cs
      - ReflectionMapper.cs
      - SD.DataTables.Runtime.asmdef
      - Table.cs
      - TableRegistry.cs
    Gameplay/
      Battle/
        Domain/
          - SD.Gameplay.Battle.Domain.asmdef
        Infrastructure/
          - SD.Gameplay.Battle.Infrastructure.asmdef
        Presentation/
          - SD.Gameplay.Battle.Presentation.asmdef
        Tests/
          - SD.Gameplay.Tests.asmdef
        - SD.Gameplay.Battle.Domain.asmdef
        - SD.Gameplay.Battle.Infrastructure.asmdef
        - SD.Gameplay.Battle.Presentation.asmdef
        - SD.Gameplay.Tests.asmdef
      Cards/
        Domain/
          Effects/
            - EffectModel.cs
          Localization/
            - CardTextRows.cs
          - CardDataModel.cs
          - CardDefinition.cs
          - CardEffects.cs
          - CardEnums.cs
          - EffectModel.cs
          - ICardRepository.cs
          - CardTextRows.cs
          - SD.Gameplay.Cards.Domain.asmdef
        Infrastructure/
          Configs/
          Csv/
            - CardCsvParser.cs
            - CardCsvRow.cs
            - CsvCardRepository.cs
            - CsvReader.cs
          Editor/
            - CardDataValidateMenu.cs
          - CardCatalog.cs
          - CardFactory.cs
          - CardCsvParser.cs
          - CardCsvRow.cs
          - CsvCardRepository.cs
          - CsvReader.cs
          - CardDataValidateMenu.cs
          - GameBootstrap.cs
          - SD.Gameplay.Cards.Infrastructure.asmdef
        Presentation/
          - SD.Gameplay.Cards.Presentation.asmdef
        - CardDataModel.cs
        - CardDefinition.cs
        - CardEffects.cs
        - CardEnums.cs
        - EffectModel.cs
        - ICardRepository.cs
        - CardTextRows.cs
        - SD.Gameplay.Cards.Domain.asmdef
        - CardCatalog.cs
        - CardFactory.cs
        - CardCsvParser.cs
        - CardCsvRow.cs
        - CsvCardRepository.cs
        - CsvReader.cs
        - CardDataValidateMenu.cs
        - GameBootstrap.cs
        - SD.Gameplay.Cards.Infrastructure.asmdef
        - SD.Gameplay.Cards.Presentation.asmdef
      - SD.Gameplay.Battle.Domain.asmdef
      - SD.Gameplay.Battle.Infrastructure.asmdef
      - SD.Gameplay.Battle.Presentation.asmdef
      - SD.Gameplay.Tests.asmdef
      - CardDataModel.cs
      - CardDefinition.cs
      - CardEffects.cs
      - CardEnums.cs
      - EffectModel.cs
      - ICardRepository.cs
      - CardTextRows.cs
      - SD.Gameplay.Cards.Domain.asmdef
      - CardCatalog.cs
      - CardFactory.cs
      - CardCsvParser.cs
      - CardCsvRow.cs
      - CsvCardRepository.cs
      - CsvReader.cs
      - CardDataValidateMenu.cs
      - GameBootstrap.cs
      - SD.Gameplay.Cards.Infrastructure.asmdef
      - SD.Gameplay.Cards.Presentation.asmdef
    Tools/
      Editor/
        - AsmdefBootstrapper.cs
        - FolderTreeSnapshot.cs
        - SD.Tools.Editor.asmdef
        - StatusFileGenerator.cs
      - AsmdefBootstrapper.cs
      - FolderTreeSnapshot.cs
      - SD.Tools.Editor.asmdef
      - StatusFileGenerator.cs
    UI/
      Common/
        - SD.UI.Common.asmdef
      Editor/
        - SD.UI.Editor.asmdef
      - SD.UI.Common.asmdef
      - SD.UI.Editor.asmdef
    - SD.Core.Domain.asmdef
    - SD.Core.Infrastructure.asmdef
    - SD.Core.Presentation.asmdef
    - SD.Tests.EditMode.asmdef
    - DataTableConfigAutoSync.cs
    - DataTableConfigSyncEditor.cs
    - ExcelCodePagesInit.cs
    - SD.DataTables.Editor.asmdef
    - XlsxAssetPostprocessor.cs
    - XlsxToCsvConverter.cs
    - CsvReader.cs
    - DataTableConfig.cs
    - DataTableIdAttribute.cs
    - DataTableLoader.cs
    - ICsvTable.cs
    - IHasStringId.cs
    - ReflectionMapper.cs
    - SD.DataTables.Runtime.asmdef
    - Table.cs
    - TableRegistry.cs
    - SD.Gameplay.Battle.Domain.asmdef
    - SD.Gameplay.Battle.Infrastructure.asmdef
    - SD.Gameplay.Battle.Presentation.asmdef
    - SD.Gameplay.Tests.asmdef
    - CardDataModel.cs
    - CardDefinition.cs
    - CardEffects.cs
    - CardEnums.cs
    - EffectModel.cs
    - ICardRepository.cs
    - CardTextRows.cs
    - SD.Gameplay.Cards.Domain.asmdef
    - CardCatalog.cs
    - CardFactory.cs
    - CardCsvParser.cs
    - CardCsvRow.cs
    - CsvCardRepository.cs
    - CsvReader.cs
    - CardDataValidateMenu.cs
    - GameBootstrap.cs
    - SD.Gameplay.Cards.Infrastructure.asmdef
    - SD.Gameplay.Cards.Presentation.asmdef
    - AsmdefBootstrapper.cs
    - FolderTreeSnapshot.cs
    - SD.Tools.Editor.asmdef
    - StatusFileGenerator.cs
    - SD.UI.Common.asmdef
    - SD.UI.Editor.asmdef
  Settings/
    Addressables/
    Input/
    Localization/
    Quality/
    URP/
  UI/
    Battle/
    Common/
    Screens/
    Widgets/
  - DataTableConfig.asset
  - CardData.csv
  - CardDesc.csv
  - CardName.csv
  - CardData.xlsx
  - CardDesc.xlsx
  - CardName.xlsx
  - SD.Core.Domain.asmdef
  - SD.Core.Infrastructure.asmdef
  - SD.Core.Presentation.asmdef
  - SD.Tests.EditMode.asmdef
  - DataTableConfigAutoSync.cs
  - DataTableConfigSyncEditor.cs
  - ExcelCodePagesInit.cs
  - SD.DataTables.Editor.asmdef
  - XlsxAssetPostprocessor.cs
  - XlsxToCsvConverter.cs
  - CsvReader.cs
  - DataTableConfig.cs
  - DataTableIdAttribute.cs
  - DataTableLoader.cs
  - ICsvTable.cs
  - IHasStringId.cs
  - ReflectionMapper.cs
  - SD.DataTables.Runtime.asmdef
  - Table.cs
  - TableRegistry.cs
  - SD.Gameplay.Battle.Domain.asmdef
  - SD.Gameplay.Battle.Infrastructure.asmdef
  - SD.Gameplay.Battle.Presentation.asmdef
  - SD.Gameplay.Tests.asmdef
  - CardDataModel.cs
  - CardDefinition.cs
  - CardEffects.cs
  - CardEnums.cs
  - EffectModel.cs
  - ICardRepository.cs
  - CardTextRows.cs
  - SD.Gameplay.Cards.Domain.asmdef
  - CardCatalog.cs
  - CardFactory.cs
  - CardCsvParser.cs
  - CardCsvRow.cs
  - CsvCardRepository.cs
  - CsvReader.cs
  - CardDataValidateMenu.cs
  - GameBootstrap.cs
  - SD.Gameplay.Cards.Infrastructure.asmdef
  - SD.Gameplay.Cards.Presentation.asmdef
  - AsmdefBootstrapper.cs
  - FolderTreeSnapshot.cs
  - SD.Tools.Editor.asmdef
  - StatusFileGenerator.cs
  - SD.UI.Common.asmdef
  - SD.UI.Editor.asmdef

```

## 7) Open Tasks / Next Steps
- [ ] Add/verify Row models & `[DataTableId]` attributes for new CSVs
- [ ] Decide if Row2(types) should be required & enforce
- [ ] Flags/bitfield parsing helpers for position/tag columns
- [ ] Unit tests: CSV parsing, enum mapping, duplicate Id detection

## 8) Changelog

- 2025-10-20 19:43:29: Status file regenerated.

- 2025-10-20 19:57:30: Status file regenerated.

- 2025-10-20 20:04:13: Status file regenerated.

- 2025-10-20 20:04:44: Status file regenerated.

- 2025-10-21 15:56:28: Status file regenerated.

- 2025-10-21 21:14:22: Status file regenerated.

- 2025-10-21 21:55:04: Status file regenerated.
