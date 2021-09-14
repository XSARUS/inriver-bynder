# inRiver IPMC configuration

These instructions will get you an instance of the Bynder integration up and running in your inRiver IPMC environment.
All instructions in this document are for the inRiver IPMC **Control Center** environment.

## Setting up the model

For the integration you need to add the following CVLs and values to the model:

### ResourceBynderState CVL

| Key | Value |
| ----| ------ |
|`todo`| Todo|
|`done`| Done|
|`error`| Error|

Also you need the have the following fields on the `Resource` entity:

| Name | DataType | CVL | Unique | Multivalue | ReadOnly |
|-----| -----| ---| --| --| -- |
|`ResourceBynderId`|string||*||*
|`ResourceBynderIdHash`|string||||*
|`ResourceBynderDownloadState`|CVL|`ResourceBynderState`|
|`ResourceMimeType`|string|
|`ResourceFileId`|file|
|`ResourceFilename`|string|

## Adding connectors / extensions
After building the project, make a zipfile from the build directory called **Bynder.zip**. Upload this package in the **packages** section in the control center.

At the extensions page add the following connectors with configurations. 

After adding and saving a connector press 'Get Default Settings' to get the default configuration options. After editing and saving this settings, restart the service.

| ExtensionId | Package | Assemby Name | Assembly Type | Extension Type |ApiKey|
|-|-|-|-|-|-|
| BynderAssetNotify | Bynder.zip | Bynder.dll | Bynder.Extension.NotificationListener | InboundDataExtension |
| BynderAssetLoader | Bynder.zip | Bynder.dll | Bynder.Extension.AssetLoader | ScheduledExtension |
| BynderAssetWorkerEntities | Bynder.zip | Bynder.dll | Bynder.Extension.Worker | EntityListener |
| BynderAssetWorkerLinks | Bynder.zip | Bynder.dll | Bynder.Extension.Worker | LinkListener |

### Extension settings
Unfortunately you have to configure 4 extensions to make the integration complete and they cannot share configuration values.

| Key| Value (example) | Explanation |
|----| ----- | --|
| CUSTOMER_BYNDER_URL | https://[CUSTOMER].getbynder.com | Bynder tenant URL, also prefix for the API
| CONSUMER_KEY | YOUR-CONSUMER-KEY | Bynder API Consumer key
| CONSUMER_SECRET | YOUR-CONSUMER-SECRET | Bynder API Consumer secret
| TOKEN | YOUR-TOKEN | Bynder API Token
| TOKEN_SECRET | YOUR-TOKEN-SECRET | Bynder API Token secret
| REGULAR_EXPRESSION_FOR_FILENAME | ^(?\<ProductNumber\>[0-9a-zA-Z]+)\_(?\<ResourceType\>image\|document\|resource)\_(?\<ResourcePosition\>[0-9]+)| Regular expression to which the filename is matched; named groups are used to store in inRiver and create content relationship.
| METAPROPERTY_MAP | D38054AD-8C0F-451C-99F675D689EAA0BD=ResourceDescription, 50B5233E-AD1C-4CF5-82B910BADA62F30F=ProductName, 1A76B650-FF7A-483A-96FD506C29576C23=ResourceDescription,  5E2A0950-FD44-47FC-9A5558105BA9D977=ProductName | For extensions 'Uploader' and 'Worker' it will be comma separated mapping list BynderMetapropertyId=InRiverFieldId, so data can be uploaded by metaproperty id. For the 'AssetLoader' and 'NotificationListener' will be {PropertyName}={FieldTypeId}. To import updated metadata we get the properties from the asset. The asset json contains properties name property_{propertyname} f.e. property_EANcode
| ASSET_PROPERTY_MAP | description=ResourceDescription, fileSize=ResourceSize, tags=ResourceTags |Way to import Asset data into inRiver FieldTypes by configuring {AssetPropertyName}={FieldTypeId}. Works with extensions 'AssetLoader' and 'NotificationListener'. Available asset properties to map are name,description,copyright,brandId,tags,datePublished,archive,limited,isPublic,userCreated,fileSize,dateCreated,width,id,idHash,dateModified,extension,height,type,orientation,watermarked.
| INITIAL_ASSET_LOAD_URL_QUERY | type=image | Filter query for the initial asset loader
| IMPORT_CONDITIONS | [{"propertyName": "synctoinriver", "values":["True"]}] | Filter out assets from the import of the NotificationListener and the AssetLoader. The metaproperties and conditions both have values as array, as per Bynder's default way to deliver metaproperty values. The values of the conditions and metaproperties will be sorted and then compared to each other. The properties to check do not need to be in the METAPROPERTY_MAP setting, they will be retreived directly from the Asset. Every condition will be executed, so not only the ones that are found. When a value is null for a metaproperty on the Asset, then we don't receive the metaproperty from Bynder('s API response). When the metaproperty is not found and the condition for this property has no values or the only value is null, then it will return true.
| INRIVER_INTEGRATION_ID | 41a92562-bfd9-4847-a34d-4320bcef5e4a | See https://help.bynder.com/System/Integrations/asset-tracker.htm
| INRIVER_RESOURCE_URL | https://inriver.productmarketingcloud.com//app/enrich#entity{entityId} | Deeplink to resource entity in inRiver |
| BYNDER_BRAND_NAME | Customer Brand Name | Used to set the BrandId in the upload of Assets. Can be found under Brand Management in Bynder or with the API by running the GetAvailableBranches() method on the BynderClient. |
| LOCALESTRING_LANGUAGES_TO_SET | en-GB, nl-NL | Languages to set on the Entity, when a FieldType in the METAPROPERTY_MAP is of type LocaleString. Values in Bynder are not language specific, so the value on the property will be copied to the configured languages (in the AssetUpdatedWorker).|
| MULTIVALUE_SEPARATOR | ,  | Separator which will be used to concat multiple values delivered by Bynder into a (locale)string field for metadataproperties. This separator will only be used on string and LocaleString fields, for CVL we concat the values with a semicolon(;), because that's what inRiver expects. |
| CREATE_MISSING_CVL_KEYS| True | Allow extension to create missing CVL Keys

Press Test on each connector (in the extensions page) to see if the connector works and your settings are valid

To read more about how to setup metaproperties go to [Add metaproperties](BYNDER-CONFIGURATION.md#Add-metaproperties)