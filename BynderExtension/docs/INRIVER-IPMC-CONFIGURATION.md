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
| METAPROPERTY_MAP | [{<br>&nbsp;&nbsp;"bynderMetaProperty": "D38054AD-8C0F-451C-99F675D689EAA0BD",<br>&nbsp;&nbsp;"inRiverFieldTypeId": "ResourceDescription",<br>&nbsp;&nbsp;"isMultiValue": false<br>},<br>{<br>&nbsp;&nbsp;"bynderMetaProperty": "50B5233E-AD1C-4CF5-82B910BADA62F30F",<br>&nbsp;&nbsp;"inRiverFieldTypeId": "ProductTargetMarkets",<br>&nbsp;&nbsp;"isMultiValue": true<br>}]|Mapping of Bynder MetaProperties to InRiver Fields.<br><br>Extensions use different configurations for the Bynder MetaProperty:<br>* 'Uploader' and 'Worker': BynderMetaPropertyId, so data can be uploaded by meta property id.<br>* 'AssetLoader' and 'NotificationListener': BynderMetaPropertyName, so we can get the property from the Asset JSON which contains properties as property_\{propertyname\}, e.g. property_EANcode<br><br>So the BynderMetaProperty can be the ID or the Name depending on which extension you use.<br><br>You can use two formats.<br><br>The old format is:<br>\{BynderMetaProperty\}=\{InRiverFieldTypeId\},\{BynderMetaProperty\}=\{InRiverFieldTypeId\}<br>This format is limited to only the bynderMetaProperty and the InRiver FieldTypeId.<br><br>The new format is:<br>[{<br>&nbsp;&nbsp;"bynderMetaProperty": "\{BynderMetaPropertyName\}",<br>&nbsp;&nbsp;"inRiverFieldTypeId": "\{InRiverFieldTypeId\}",<br>&nbsp;&nbsp;"isMultiValue": true<br>},<br>{<br>&nbsp;&nbsp;"bynderMetaProperty": "\{BynderMetaPropertyName\}",<br>&nbsp;&nbsp;"inRiverFieldTypeId": "\{InRiverFieldTypeId\}",<br>&nbsp;&nbsp;"isMultiValue": false<br>}]<br><br>bynderMetaProperty can be the ID or the Name depending on which extension you use.<br><br>isMultiValue shows if the Bynder MetaProperty is multivalue or not.<br>This way we can filter out multiple values so the request does not fail if there are multiple values found in inRiver. It takes the first value.<br>Default isMultiValue is true, because the old code didn't filter out values. It always sent multiple values when present. <br><br>Duplicate values are removed.
| ASSET_PROPERTY_MAP | description=ResourceDescription, fileSize=ResourceSize, tags=ResourceTags |Way to import Asset data into inRiver FieldTypes by configuring {AssetPropertyName}={FieldTypeId}. Works with extensions 'AssetLoader' and 'NotificationListener'. Available asset properties to map are name,description,copyright,brandId,tags,datePublished,archive,limited,isPublic,userCreated,fileSize,dateCreated,width,id,idHash,dateModified,extension,height,type,orientation,watermarked.
| INITIAL_ASSET_LOAD_URL_QUERY | type=image | Filter query for the initial asset loader
| IMPORT_CONDITIONS | [{"propertyName": "synctoinriver", "values":["True"], "matchType": "EqualsCaseInsensitive"},{"propertyName": "assetSubType", "values":["Product_Image", "Item_Image"], "matchType": "ContainsAny"}] | Filter out assets from the import of the NotificationListener and the AssetLoader. The metaproperties and conditions both have values as array, as per Bynder's default way to deliver metaproperty values. The properties to check do not need to be in the METAPROPERTY_MAP setting, they will be retreived directly from the Asset. Every condition will be executed, so not only the ones that are found. When a value is null for a metaproperty on the Asset, then we don't receive the metaproperty from Bynder('s API response). When the metaproperty is not found and the condition for this property has no values or the only value is null, then it will return true. There are multiple match types you can choose. Check the table below for the match types.
| INRIVER_INTEGRATION_ID | 41a92562-bfd9-4847-a34d-4320bcef5e4a | See https://help.bynder.com/System/Integrations/asset-tracker.htm
| INRIVER_RESOURCE_URL | https://inriver.productmarketingcloud.com//app/enrich#entity{entityId} | Deeplink to resource entity in inRiver |
| BYNDER_BRAND_NAME | Customer Brand Name | Used to set the BrandId in the upload of Assets. Can be found under Brand Management in Bynder or with the API by running the GetAvailableBranches() method on the BynderClient. |
| LOCALESTRING_LANGUAGES_TO_SET | en-GB, nl-NL | Languages to set on the Entity, when a FieldType in the METAPROPERTY_MAP is of type LocaleString. Values in Bynder are not language specific, so the value on the property will be copied to the configured languages (in the AssetUpdatedWorker).|
| MULTIVALUE_SEPARATOR | ,  | Separator which will be used to concat multiple values delivered by Bynder into a (locale)string field for metadataproperties. This separator will only be used on string and LocaleString fields, for CVL we concat the values with a semicolon(;), because that's what inRiver expects. |
| CREATE_MISSING_CVL_KEYS| True | Allow extension to create missing CVL Keys when true. |
| DELETE_RESOURCE_ON_DELETE_EVENT | True | Deletes inRiver resource for asset of incoming event "asset_bank.media.deleted". Default `false`. |
| FIELD_VALUES_TO_SET_ON_ARCHIVE_EVENT | [{"fieldTypeId":"ResourceArchived","value": true},{"fieldTypeId":"ResourceArchivedDate","setTimestamp": true}] | Sets the value on the field when resource is archived by receiving event "asset_bank.media.archived". Value may be any datatype except LocaleString. Use setTimestamp in combination with the 'TIMESTAMP_SETTINGS' setting.
| TIMESTAMP_SETTINGS | {"dateTimeKind": "Utc","localTimeZone": "W. Europe Standard Time","localDstEnabled": true} | Settings to use when setting a timestamp on archive events. DateTimeKind can be 'Utc' or 'Local'. DstEnabled should be true if your timezone uses Daylight Saving Time. Timezone id's can be found here 'http://www.xiirus.net/articles/article-_net-convert-datetime-from-one-timezone-to-another-7e44y.aspx' |
| DOWNLOAD_MEDIA_TYPE | webimage | The media type you want to use for downloads of the Bynder file to inriver. This could be `original` or a derivative/thumbnail. Default `original`  |
| ADD_ASSET_ID_PREFIX_TO_FILENAME_OF_NEW_RESOURCE | false | Adds prefix `{assetId}_` to the filename to make it more unique. Default `true`  |
| RESOURCE_SEARCH_TYPE | Filename | Searches the existing Resource in the AssetUpdatedWorker by `AssetId`, `Filename` or `PrefixedFilename`. Default `AssetId`  |

Press Test on each connector (in the extensions page) to see if the connector works and your settings are valid

To read more about how to setup metaproperties go to [Add metaproperties](BYNDER-CONFIGURATION.md#Add-metaproperties)

#### Match types
| Type | Explanation |
| ---- | ----------- |
| EqualSorted | Sorts the metaproperty's values and the import condition values, then compares them. The default matchtype. If you don't fill this property in the JSON, then this one is used. |
| EqualSortedCaseInsensitive | Sorts the metaproperty's values and the import condition values. Then compares them, while ignoring the casing.  |
| Equal | Compares the values, the order of the values must be the same in the settings and in the metaproperty. |
| EqualCaseInsensitive | Compares the values while ignoring casing, the order of the values must be the same in the settings and in the metaproperty.|
| ContainsAll | Checks if the import condition values are all selected in the metaproperty. Example: metaproperty values a,b,c,d and condition values a,c,d results in true, because a,c,d exist in the metaproperty. |
| ContainsAllCaseInsensitive | Checks if the import condition values are all selected in the metaproperty. Case insensitive by making the values lowercase.|
| ContainsAny | Checks if one of the the import condition values is selected in the metaproperty. Example: metaproperty values a,b,c,d and condition values x,d,m results in true, because d exist in the metaproperty values. |
| ContainsAnyCaseInsensitive | Checks if one of the the import condition values is selected in the metaproperty.  Case insensitive by making the values lowercase. |