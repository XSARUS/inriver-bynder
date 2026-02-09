
# Bynder configuration

These instructions help you configure your Bynder account for intergration with Inriver.

## Prerequisites

You will need the following setup to configure Bynder.

- Bynder account.
- Amazon Simple Notification Service (Bynder needs set this this for you)

## Subscribe To Amazon SNS
Bynder uses Amazon SNS for messaging. Ask bynder to set this up for you.
You need to give them an end-point to receive asset updates on that end-point. 

The URL should look something like this: https://inbound.productmarketingcloud.com/api/inbounddata/[CUSTOMERNAME]/[ENVIRONMENT]/[EXSTENSIONID]

- [CUSTOMERNAME] = the name of your customer
- [ENVIRONMENT] = one of your configured environments production/test
- [EXTENSIONID] = we wil use 'BynderAssetNotify' later on in the Inriver documentation. 
  So that one we will use. But you're free to use your own extension Id.

## Oauth 2
Thise major release we moved to Oauth2.
This requires an Oauth app from your Bynder environment instead of a consumer key + secret in combination with a token.

### Create an Oauth app
- Login to your Bynder account and go to https://[YOURBYNDERACCOUNT].getbynder.com/pysettings/ or
- At the top-right corner click on "gear"-icon next to your name
- Go to "advanced settings" and click on "portal-settings"
- At the left click on "Oauth apps" and at the right click on "Add new app".
- Select grant type > "Client Credentials"
- Set HTTP access control (CORS) > add at least "Inriver.com"
- Set HTTP access control (CORS) > also add "productmarketingcloud.com"
- Select scopes: at least select all "Assets", "Collections", "Metaproperties" and "User"
- Select scopes: at least select "admin.profile:read" and "admin.user:read" for "Admin"
- Next click on "Register application".
- Store the client_id and client_secret someware safe. We need this later!

## Add metaproperties
If you want to update a Bynder asset with information from Inriver,
you can add metaproperties to store values on a asset. 

To create a metaproperty:

- Go to your Bynder account
- Settings -> Account -> Metaproperties management
- Press Add
- For our implementation we use:
  - Name = ResourceDescription
  - Label = ResourceDescription
  - Type = Text
- Press Save

It's good practice to use metaproperty names similar to fieldTypeId's in your Inriver model.
For this demonstration 'ResourceDescription' will be added in our Inriver model later.

In the URL you wil find the Id of the metaproperty. Write it down for later use!