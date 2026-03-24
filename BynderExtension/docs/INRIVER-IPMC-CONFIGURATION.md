# Inriver IPMC configuration

These instructions will get you an instance of the Bynder integration up and running in your Inriver IPMC environment.
All instructions in this document are for the Inriver IPMC **Control Center** environment.

## Important inRiver
Please be aware of the following inRiver situations.

### Scheduled Extensions
When replacing the package and restarting service, disabled scheduled extensions sometimes are enabled, but the status indicates otherwise.
Enabled + disable the extensions to be sure

Updating settings on a scheduled extensions: it isn't enough to Reload the Extension Settings! It's a little bit comprehensive but do this:
- Disable the extension
- Restart the Extension Service
- Update the settings
- Reload the extension settings
- Restart the Extension Service
- Enable the extension
- Restart the Extension Service

And sometimes it's even better to restart the Extension Service multiple times in a row.

## Setting up the model

For the integration you need to add the following CVLs and values to the model:

### ResourceBynderState CVL

| Key | Value |
| --- | ----- |
| `todo` | Todo |
| `done` | Done |
| `error` | Error |

## ResourceBynderUploadState CVL

| Key | Value |
| --- | ----- |
| `todo` | Todo |
| `done` | Done |
| `error` | Error |

Also you need the have the following fields on the `Resource` Entity:

| Name | DataType | CVL | Unique | Multivalue | ReadOnly |
| ---- | -------- | --- | ------ | ---------- | -------- |
| `ResourceBynderId` | string | | * | | * |
| `ResourceBynderIdHash` | string | | | | * |
| `ResourceBynderDownloadState` | CVL | `ResourceBynderState` | | | |
| `ResourceBynderUploadState` | CVL | `ResourceBynderUploadState`| | | |
| `ResourceMimeType` | string | | | | |
| `ResourceFileId` | file | | | | |
| `ResourceFilename` | string | | | | |

Note: you may also use 1 CVL for both ResourceBynderDownload- and ResourceBynderUploadState since the key/values are te same.

## Adding connectors / extensions
After building the project, make a zipfile from the build directory called **Bynder.zip**. Upload this package in the **packages** section in the control center.

At the extensions page add the following connectors with configurations. 

After adding and saving a connector press 'Get Default Settings' to get the default configuration options. After editing and saving this settings, restart the service.

| ExtensionId | Package | Assembly Name | Assembly Type | Extension Type | ApiKey |
| ----------- | ------- | ------------ | ------------- | -------------- | ------ |
| BynderAssetNotify | Bynder.zip | Bynder.dll | Bynder.Extension.NotificationListener | InboundDataExtension |
| BynderAssetLoader | Bynder.zip | Bynder.dll | Bynder.Extension.AssetLoader | ScheduledExtension |
| BynderAssetWorkerEntities | Bynder.zip | Bynder.dll | Bynder.Extension.Worker | EntityListener |
| BynderUploader | Bynder.zip | Bynder.dll | Bynder.Extension.Uploader | EntityListener |
| BynderAssetWorkerLinks | Bynder.zip | Bynder.dll | Bynder.Extension.Worker | LinkListener |
| ScheduledNotificationHandler| Bynder.zip | Bynder.dll | Bynder.Extension.ScheduledNotificationHandler | ScheduledExtension |
| CvlSyncListener | Bynder.zip | Bynder.dll | Bynder.Extension.CvlSyncListener | CvlListener |

### Extension settings
Unfortunately you have to configure 4 extensions to make the integration complete and they cannot share configuration values.

| Key | Value (example) | Explanation |
| --- | --------------- | ----------- |
| BYNDER_CLIENT_URL | https://[CUSTOMER].getbynder.com | Bynder tenant URL, also prefix for the API
| BYNDER_CLIENT_ID | The client_id of the Oauth app | Bynder Client ID
| BYNDER_CLIENT_SECRET | The secret of the Oauth app | Bynder API Client Secret
| REGULAR_EXPRESSION_FOR_FILENAME | ^(?\<ProductNumber\>[0-9a-zA-Z]+)\_(?\<ResourceType\>image\|document\|resource)\_(?\<ResourcePosition\>[0-9]+)| Regular expression to which the filename is matched; named groups are used to store in Inriver and create content relationship.
| METAPROPERTY_MAP | [{<br>&nbsp;&nbsp;"bynderMetaProperty": "D38054AD-8C0F-451C-99F675D689EAA0BD",<br>&nbsp;&nbsp;"InriverFieldTypeId": "ResourceDescription",<br>&nbsp;&nbsp;"isMultiValue": false<br>},<br>{<br>&nbsp;&nbsp;"bynderMetaProperty": "50B5233E-AD1C-4CF5-82B910BADA62F30F",<br>&nbsp;&nbsp;"InriverFieldTypeId": "ProductTargetMarkets",<br>&nbsp;&nbsp;"isMultiValue": true<br>}]|Mapping of Bynder MetaProperties to Inriver Fields.<br><br>Extensions use different configurations for the Bynder MetaProperty:<br>* 'Uploader' and 'Worker': BynderMetaPropertyId, so data can be uploaded by meta property id.<br>* 'AssetLoader' and 'NotificationListener': BynderMetaPropertyName, so we can get the property from the Asset JSON which contains properties as property_\{propertyname\}, e.g. property_EANcode<br><br>So the BynderMetaProperty can be the ID or the Name depending on which extension you use.<br><br>You can use two formats.<br><br>The old format is:<br>\{BynderMetaProperty\}=\{InriverFieldTypeId\},\{BynderMetaProperty\}=\{InriverFieldTypeId\}<br>This format is limited to only the bynderMetaProperty and the Inriver FieldTypeId.<br><br>The new format is:<br>[{<br>&nbsp;&nbsp;"bynderMetaProperty": "\{BynderMetaPropertyName\}",<br>&nbsp;&nbsp;"InriverFieldTypeId": "\{InriverFieldTypeId\}",<br>&nbsp;&nbsp;"isMultiValue": true<br>},<br>{<br>&nbsp;&nbsp;"bynderMetaProperty": "\{BynderMetaPropertyName\}",<br>&nbsp;&nbsp;"InriverFieldTypeId": "\{InriverFieldTypeId\}",<br>&nbsp;&nbsp;"isMultiValue": false<br>}]<br><br>bynderMetaProperty can be the ID or the Name depending on which extension you use.<br><br>isMultiValue shows if the Bynder MetaProperty is multivalue or not.<br>This way we can filter out multiple values so the request does not fail if there are multiple values found in Inriver. It takes the first value.<br>Default isMultiValue is true, because the old code didn't filter out values. It always sent multiple values when present. <br><br>Duplicate values are removed.
| METAPROPERTY_MAP_TO_BYNDER | {<br>&nbsp;&nbsp;"entityTypeId": "Resource",<br>&nbsp;&nbsp;"inbound": [<br>&nbsp;&nbsp;&nbsp;&nbsp;{<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"linkTypeId": "ItemResource",<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"entityTypeId": "Item",<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"metapropertyMapping": [<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"bynderMetaProperty": "555943D1-E06D-4D81-B3BA305283C90186",<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"inRiverFieldTypeId": "ItemModelNumber",<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"isMultiValue": false<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;},<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"bynderMetaProperty": "9E17FD95-2899-492D-BDCE0F0A4FB9117A",<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"inRiverFieldTypeId": "ItemIntroductionYear",<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"isMultiValue": false<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;},<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"bynderMetaProperty": "B8F7F0B7-53C3-4B12-BBF88CF54FC15FF6",<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"inRiverFieldTypeId": "ItemSeason",<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"isMultiValue": true<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;},<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"bynderMetaProperty": "8381A0EA-6B62-44A9-B92B24BA1ADB2DF1",<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"inRiverFieldTypeId": "ItemColourNamePrimary",<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"isMultiValue": false<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;},<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"bynderMetaProperty": "94C0CBF5-33C5-4C53-B0684BCC9BE8C456",<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"inRiverFieldTypeId": "ItemColourNameSecondary",<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"isMultiValue": false<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;},<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"bynderMetaProperty": "077E5275-CC81-45DB-978BF24AE6905178",<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"inRiverFieldTypeId": "ItemFrameShape",<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"isMultiValue": false<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;}<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;],<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"inbound": [<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"linkTypeId": "ProductItem",<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"entityTypeId": "Product",<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"metapropertyMapping": [<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"bynderMetaProperty": "0B00A59A-E764-4753-8E101C7FB2965724",<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"inRiverFieldTypeId": "ProductModelGroup",<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"isMultiValue": false<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;},<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;{<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"bynderMetaProperty": "FBADAFD6-81CE-454B-A13EF6EAD0329723",<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"inRiverFieldTypeId": "ProductModelName",<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;"isMultiValue": false<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;}<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;]<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;}<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;]<br>&nbsp;&nbsp;&nbsp;&nbsp;}<br>&nbsp;&nbsp;]<br>} | Config to use to gather data from multiple levels in Inriver and map it to Bynder metaproperties, so the Asset can be updated in Bynder. The root should always be Resource and you can also use outbound links as 'outbound' and Fieldset can be used the following: null is all fieldsets, empty is no fieldset, filled is that specific fieldset.|
| ASSET_PROPERTY_MAP | description=ResourceDescription, fileSize=ResourceSize, tags=ResourceTags |Way to import Asset data into Inriver FieldTypes by configuring {AssetPropertyName}={FieldTypeId}. Works with extensions 'AssetLoader' and 'NotificationListener'. Available asset properties to map are name,description,copyright,brandId,tags,datePublished,archive,limited,isPublic,userCreated,fileSize,dateCreated,width,id,idHash,dateModified,extension,height,type,orientation,watermarked.
| INITIAL_ASSET_LOAD_URL_QUERY | type=image | Filter query for the initial asset loader
| INITIAL_ASSET_LOAD_LIMIT | 0 or 10 | 0 = unlimited|
| IMPORT_CONDITIONS | [{"propertyName": "SyncToInriver", "values":["True"], "matchType": "EqualsCaseInsensitive"},{"propertyName": "assetSubType", "values":["Product_Image", "Item_Image"], "matchType": "ContainsAny"}] | Filter out assets from the import into Inriver of the 'NotificationListener' and the 'AssetLoader'. The metaproperties and conditions both have values as array, as per Bynder's default way to deliver metaproperty values. The properties to check do not need to be in the METAPROPERTY_MAP setting, they will be retreived directly from the Asset. Every condition will be executed, so not only the ones that are found. When a value is null for a metaproperty on the Asset, then we don't receive the metaproperty from Bynder('s API response). When the metaproperty is not found and the condition for this property has no values or the only value is null, then it will return true. There are multiple match types you can choose. Check the table below for the match types.
| EXPORT_CONDITIONS<sup>*</sup> | [{"InriverFieldTypeId":"ResourceSyncToBynder","value":["False"],"matchType":"Equals"},{"InriverFieldTypeId":"ResourceType","values":["Image","Video"],"matchType":"ContainsAny"}] |  The metaproperties and conditions both have values as array, as per Bynder's default way to deliver metaproperty values. The properties to check do not need to be in the METAPROPERTY_MAP setting, they will be retreived directly from the Entity. Every condition will be executed, so not only the ones that are found. When the Field is not found and the condition for this Field has no values or the only value is null, then it will return true. There are multiple match types you can choose. Check the table below for the match types.
| INRIVER_INTEGRATION_ID | 41a92562-bfd9-4847-a34d-4320bcef5e4a | See https://support.bynder.com/hc/en-us/articles/360013933619-How-To-Use-The-Bynder-Asset-Tracker
| INRIVER_RESOURCE_URL | https://inriver.productmarketingcloud.com//app/enrich#Entity{EntityId} | Deeplink to resource Entity in Inriver |
| BYNDER_BRAND_NAME | Customer Brand Name | Used to set the BrandId in the upload of Assets. Can be found under Brand Management in Bynder or with the API by running the GetAvailableBranches() method on the BynderClient. |
| LOCALESTRING_LANGUAGES_TO_SET | en-GB, nl-NL | Languages to set on the Entity, when a FieldType in the METAPROPERTY_MAP is of type LocaleString. Values in Bynder are not language specific, so the value on the property will be copied to the configured languages (in the AssetUpdatedWorker).|
| MULTIVALUE_SEPARATOR | ,  | Separator which will be used to concat multiple values delivered by Bynder into a (locale)string field for metadataproperties. This separator will only be used on string and LocaleString fields, for CVL we concat the values with a semicolon(;), because that's what Inriver expects. |
| CREATE_MISSING_CVL_KEYS| True | Allow extension to create missing CVL Keys when true. |
| DELETE_RESOURCE_ON_DELETE_EVENT | True | Deletes Inriver resource for asset of incoming event "asset_bank.media.deleted". Default `false`. |
| FIELD_VALUES_TO_SET_ON_ARCHIVE_EVENT | [{"fieldTypeId":"ResourceArchived","value": true},{"fieldTypeId":"ResourceArchivedDate","setTimestamp": true}] | Sets the value on the field when resource is archived by receiving event "asset_bank.media.archived". Value may be any datatype except LocaleString. Use setTimestamp in combination with the 'TIMESTAMP_SETTINGS' setting.
| TIMESTAMP_SETTINGS | {"dateTimeKind": "Utc","localTimeZone": "W. Europe Standard Time","localDstEnabled": true} | Settings to use when setting a timestamp on archive events. DateTimeKind can be 'Utc' or 'Local'. DstEnabled should be true if your timezone uses Daylight Saving Time. Timezone id's can be found here 'http://www.xiirus.net/articles/article-_net-convert-datetime-from-one-timezone-to-another-7e44y.aspx' |
| DOWNLOAD_MEDIA_TYPE | webimage | The media type you want to use for downloads of the Bynder file to Inriver. This could be `original` or a derivative/thumbnail. Default `original`  |
| ADD_ASSET_ID_PREFIX_TO_FILENAME_OF_NEW_RESOURCE | false | Adds prefix `{assetId}_` to the filename to make it more unique. Default `true`  |
| RESOURCE_SEARCH_TYPE | Filename | Searches the existing Resource in the AssetUpdatedWorker by `AssetId`, `Filename` or `PrefixedFilename`. Default `AssetId`  |
| CRON_EXPRESSION | * * * * * | Cron expression to use for the ScheduledNotificationHandler. Default '* * * * *' which means every minute  |
| MAX_RETRY_ATTEMPTS | 3 | Number of retry attempts for downloading and processing an asset. Default `3` |
| LOCALE_MAPPING_Inriver_TO_BYNDER | {"en-US":"en_US","nl":"nl_NL"} | Mappings from Inriver language codes to Bynder language codes. Used when uploading metaproperty options. |
| BYNDER_LOCALE_FOR_METAPROPERTY_OPTION_LABEL | nl_NL | When syncing the CVL values to Bynder, choose which language you want to use as a base language for the Label/Name in Bynder in configuration. If you leave this empty or the cvl value for that language is empty, then it will set the (not sanitized) CVL key. |
| CVL_METAPROPERTY_MAPPING |{"cvl1":["metapropertyguid1","metapropertyguid2"], "cvl2":["metapropertyguid3","metapropertyguid4"]}|Mapping of CVL Id to Bynder MetaProperties. Option lists in Bynder a coupled to the metaproperty, thats why you could have multiple metaproperties for one cvl in the mapping. Setting is used in the MetapropertyOptionExportListener.
| FILENAME_EXTENSION_MEDIA_TYPE_MAPPING |{"tif":[{"mediaType":"Ecommerce","filenameRegex":"-tif(?=\\.[^.]+$)"}],"jpg":[{"mediaType":"webimage","filenameRegex":"-jpg(?=\\.[^.]+$)"}]}|If you don't want to download the original file you can supply the derivative-type also you can specify a regex to apply on the filename which is being substracted from te download-url for the choosen derivative-type. If no mapping could be found it falls back to `DOWNLOAD_MEDIA_TYPE`|
| EXECUTE_BASE_TESTMETHOD|true/false|Each extension extends a base extension. This is the case for Bynder connection settins. False prevents the test-method from the base-extension to be executed. This will make te result of the actual test-result message much clearer. Once you know Bynder works and all model requirements are met, you can set it to false|
| FIELDTYPE_THUMBNAIL_MAPPING|[{"FieldTypeId":"ResourceBynderURLMedium","ThumbnailType":"webimage","FallBackThumbnailType":"Thumbnail"}]|Write thumbnail url to inriver fieldtypes|

Press the Test button on each extension to see if the extension works and your settings are valid.

<sup>*</sup>EXPORT_CONDITONS: this setting determines wether an asset's metaproperties may be updated or not in Bynder.

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
