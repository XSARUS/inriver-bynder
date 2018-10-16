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

| Name | DataType | CVL | Unique | Multivalue |
|-----| -----| ---| --| --|
|`ResourceBynderId`|string||*||
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
| METAPROPERTY_MAP | D38054AD-8C0F-451C-99F675D689EAA0BD=ResourceDescription, 50B5233E-AD1C-4CF5-82B910BADA62F30F=ProductName, 1A76B650-FF7A-483A-96FD506C29576C23=ResourceDescription,  5E2A0950-FD44-47FC-9A5558105BA9D977=ProductName | comma separated mapping list BynderMetapropertyId=InRiverFieldId, see [Add metaproperties](BYNDER-CONFIGURATION.md#Add-metaproperties)
| INITIAL_ASSET_LOAD_URL_QUERY | type=image | filter query for the initial asset loader
| INRIVER_INTEGRATION_ID | 41a92562-bfd9-4847-a34d-4320bcef5e4a | See https://help.bynder.com/System/Integrations/asset-tracker.htm
| INRIVER_RESOURCE_URL | https://inriver.productmarketingcloud.com//app/enrich#entity{entityId} | Deeplink to resource entity in inRiver |

Press Test on each connector (in the extensions page) to see if the connector works and your settings are valid


To read more about how to setup metaproperties go to [Add metaproperties](BYNDER-CONFIGURATION.md#Add-metaproperties)
